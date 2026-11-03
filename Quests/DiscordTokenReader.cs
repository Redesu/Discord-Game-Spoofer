using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DiscordGameSpoofer.Quests
{
    internal sealed class DiscordTokenCandidate
    {
        public string Token { get; init; } = "";
        public string Source { get; init; } = "";
        public DateTime LastWriteTime { get; init; }
    }

    /// <summary>
    /// Reads Discord's own token from its LevelDB storage. Discord stores the token as
    /// "dQw4w9WgXcQ:" followed by a base64 blob. The blob is v10 + AES-256-GCM using a
    /// key that is itself protected with Windows DPAPI inside Local State.
    /// Nothing is written and the token is never logged.
    /// </summary>
    internal static class DiscordTokenReader
    {
        private const string EncryptedPrefix = "dQw4w9WgXcQ:";
        private const string DpapiPrefix = "DPAPI";

        private static readonly Regex TokenPattern = new(
            "^[A-Za-z0-9_-]{20,32}\\.[A-Za-z0-9_-]{5,12}\\.[A-Za-z0-9_-]{20,48}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TokenSearchPattern = new(
            "[A-Za-z0-9_-]{20,32}\\.[A-Za-z0-9_-]{5,12}\\.[A-Za-z0-9_-]{20,48}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static List<DiscordTokenCandidate> ReadTokens(Action<string>? log = null)
        {
            List<DiscordTokenCandidate> results = new List<DiscordTokenCandidate>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            foreach ((string root, string label) in GetDiscordRoots())
            {
                if (!Directory.Exists(root))
                    continue;

                string levelDbPath = Path.Combine(root, "Local Storage", "leveldb");
                if (!Directory.Exists(levelDbPath))
                    continue;

                byte[]? key = GetEncryptionKey(root);

                if (key == null)
                {
                    log?.Invoke($"Found {label}, but its Local State key could not be read.");
                }

                List<string> files;
                try
                {
                    files = Directory.EnumerateFiles(levelDbPath, "*.ldb")
                        .Concat(Directory.EnumerateFiles(levelDbPath, "*.log"))
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .ToList();
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Could not list {label} Local Storage: {ex.Message}");
                    continue;
                }

                foreach (string file in files)
                {
                    byte[] data;
                    try
                    {
                        data = ReadShared(file);
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Could not read {Path.GetFileName(file)} from {label}: {ex.Message}");
                        continue;
                    }

                    if (data.Length == 0)
                        continue;

                    string source = $"{label} ({Path.GetFileName(file)})";
                    DateTime lastWrite = File.GetLastWriteTimeUtc(file);

                    foreach (string token in ExtractTokens(data, key))
                    {
                        if (!seen.Add(token))
                            continue;

                        results.Add(new DiscordTokenCandidate
                        {
                            Token = token,
                            Source = source,
                            LastWriteTime = lastWrite,
                        });
                    }
                }
            }

            if (results.Count == 0)
            {
                log?.Invoke("No Discord token was found in the local Discord storage.");
            }

            return results;
        }

        public static DiscordTokenCandidate? ReadToken(Action<string>? log = null)
        {
            return ReadTokens(log).FirstOrDefault();
        }

        private static IEnumerable<(string Root, string Label)> GetDiscordRoots()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (!string.IsNullOrWhiteSpace(appData))
            {
                yield return (Path.Combine(appData, "discord"), "Discord");
                yield return (Path.Combine(appData, "discordptb"), "Discord PTB");
                yield return (Path.Combine(appData, "discordcanary"), "Discord Canary");
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                yield return (Path.Combine(localAppData, "Discord"), "Discord");
                yield return (Path.Combine(localAppData, "DiscordPTB"), "Discord PTB");
                yield return (Path.Combine(localAppData, "DiscordCanary"), "Discord Canary");
            }
        }

        private static byte[]? GetEncryptionKey(string root)
        {
            try
            {
                string localStatePath = Path.Combine(root, "Local State");
                if (!File.Exists(localStatePath))
                    return null;

                using FileStream stream = new FileStream(
                    localStatePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using JsonDocument document = JsonDocument.Parse(stream);

                if (!document.RootElement.TryGetProperty("os_crypt", out JsonElement osCrypt)
                    || !osCrypt.TryGetProperty("encrypted_key", out JsonElement encryptedKey)
                    || encryptedKey.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                string? value = encryptedKey.GetString();
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                byte[] protectedKey = Convert.FromBase64String(value);
                if (protectedKey.Length <= DpapiPrefix.Length)
                    return null;

                string prefix = Encoding.ASCII.GetString(protectedKey, 0, DpapiPrefix.Length);
                if (!string.Equals(prefix, DpapiPrefix, StringComparison.Ordinal))
                    return null;

                byte[] key = Dpapi.Unprotect(protectedKey[DpapiPrefix.Length..]);
                return key.Length == 32 ? key : null;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] ReadShared(string path)
        {
            using FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using MemoryStream memory = new MemoryStream(
                stream.CanSeek && stream.Length <= int.MaxValue ? (int)stream.Length : 0);
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static IEnumerable<string> ExtractTokens(byte[] data, byte[]? key)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            byte[] marker = Encoding.ASCII.GetBytes(EncryptedPrefix);

            int offset = 0;
            while (offset < data.Length)
            {
                int index = data.AsSpan(offset).IndexOf(marker);
                if (index < 0)
                    break;

                index += offset;
                offset = index + marker.Length;

                if (key == null)
                    continue;

                string? token = ReadEncryptedTokenAt(data, offset, key);
                if (token != null && seen.Add(token))
                    yield return token;
            }

            // Older Discord builds stored a plain token directly in LevelDB.
            string ascii = Encoding.ASCII.GetString(data);
            foreach (Match match in TokenSearchPattern.Matches(ascii))
            {
                if (TokenPattern.IsMatch(match.Value) && seen.Add(match.Value))
                    yield return match.Value;
            }

            string unicode = Encoding.Unicode.GetString(data);
            foreach (Match match in TokenSearchPattern.Matches(unicode))
            {
                if (TokenPattern.IsMatch(match.Value) && seen.Add(match.Value))
                    yield return match.Value;
            }
        }

        private static string? ReadEncryptedTokenAt(byte[] data, int start, byte[] key)
        {
            // Some LevelDB records wrap the value in quotes or a length prefix.
            int position = start;
            while (position < data.Length && (data[position] == '"' || data[position] == '\'' || data[position] == ' '))
                position++;

            int base64Start = position;
            while (position < data.Length && IsBase64Char(data[position]))
                position++;

            if (position - base64Start < 40)
                return null;

            string base64 = Encoding.ASCII.GetString(data, base64Start, position - base64Start);

            try
            {
                byte[] payload = Convert.FromBase64String(PadBase64(base64));
                return DecryptPayload(payload, key);
            }
            catch
            {
                return null;
            }
        }

        private static string? DecryptPayload(byte[] payload, byte[] key)
        {
            if (payload.Length > 15 + 16 && payload[0] == (byte)'v' && payload[1] == (byte)'1' && payload[2] == (byte)'0')
            {
                try
                {
                    byte[] nonce = payload[3..15];
                    byte[] tag = payload[^16..];
                    byte[] ciphertext = payload[15..^16];
                    byte[] plaintext = new byte[ciphertext.Length];

                    using AesGcm aes = new AesGcm(key, 16);
                    aes.Decrypt(nonce, ciphertext, tag, plaintext);

                    string token = Encoding.UTF8.GetString(plaintext).Trim('\0', ' ', '"');
                    if (IsLikelyToken(token))
                        return token;
                }
                catch (CryptographicException)
                {
                }
            }

            // Very old builds protected the token with DPAPI before the AES scheme.
            try
            {
                string token = Encoding.UTF8.GetString(Dpapi.Unprotect(payload)).Trim('\0', ' ', '"');
                if (IsLikelyToken(token))
                    return token;
            }
            catch
            {
            }

            return null;
        }

        private static bool IsBase64Char(byte value)
        {
            return (value >= (byte)'A' && value <= (byte)'Z')
                || (value >= (byte)'a' && value <= (byte)'z')
                || (value >= (byte)'0' && value <= (byte)'9')
                || value == (byte)'+'
                || value == (byte)'/'
                || value == (byte)'=';
        }

        private static string PadBase64(string value)
        {
            int remainder = value.Length % 4;
            return remainder == 0 ? value : value.PadRight(value.Length + (4 - remainder), '=');
        }

        public static bool IsLikelyToken(string? token)
        {
            return !string.IsNullOrWhiteSpace(token) && TokenPattern.IsMatch(token.Trim());
        }
    }
}
