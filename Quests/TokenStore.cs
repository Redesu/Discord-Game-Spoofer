using System.Text;

namespace DiscordGameSpoofer.Quests
{
    /// <summary>
    /// Stores the user's token encrypted with Windows DPAPI. The file can only be
    /// decrypted by the same Windows user on the same machine.
    /// </summary>
    internal static class TokenStore
    {
        public static string? Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                byte[] protectedData = File.ReadAllBytes(path);
                if (protectedData.Length == 0)
                    return null;

                string token = Encoding.UTF8.GetString(Dpapi.Unprotect(protectedData)).Trim();
                return DiscordTokenReader.IsLikelyToken(token) ? token : null;
            }
            catch
            {
                return null;
            }
        }

        public static bool Save(string path, string token)
        {
            try
            {
                if (!DiscordTokenReader.IsLikelyToken(token))
                    return false;

                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                byte[] protectedData = Dpapi.Protect(Encoding.UTF8.GetBytes(token.Trim()));
                File.WriteAllBytes(path, protectedData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Delete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
