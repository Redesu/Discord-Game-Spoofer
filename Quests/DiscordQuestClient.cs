using System.Net;
using System.Text;
using System.Text.Json;

namespace DiscordGameSpoofer.Quests
{
    internal sealed class DiscordUser
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string GlobalName { get; set; } = "";

        public string DisplayName => string.IsNullOrWhiteSpace(GlobalName) ? Username : GlobalName;
    }

    internal sealed class DiscordQuest
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string ApplicationId { get; set; } = "";
        public string ApplicationName { get; set; } = "";

        public bool IsVideoQuest { get; set; }
        public string TaskType { get; set; } = "";

        public long TargetSeconds { get; set; }
        public double ServerProgressSeconds { get; set; }

        public bool Enrolled { get; set; }
        public bool Completed { get; set; }
        public bool Claimed { get; set; }
        public bool Expired { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? EnrolledAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? ClaimedAt { get; set; }

        public string? TrafficMetadataSealed { get; set; }
        public string? TrafficMetadataRaw { get; set; }

        // Filled in by the manager when Discord did not return enrolled_at.
        public double? LocalEnrollmentEpoch { get; set; }
        public double LocalProgressSeconds { get; set; }

        public double EnrollmentEpoch =>
            EnrolledAt?.ToUnixTimeSeconds()
            ?? LocalEnrollmentEpoch
            ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public double EffectiveProgress => Math.Max(ServerProgressSeconds, LocalProgressSeconds);

        public double Percent
        {
            get
            {
                if (TargetSeconds <= 0)
                    return 0;

                return Math.Clamp(EffectiveProgress / TargetSeconds * 100.0, 0, 100);
            }
        }

        public bool IsActive => IsVideoQuest && Enrolled && !Completed && !Expired;

        public static DiscordQuest Parse(JsonElement element)
        {
            DiscordQuest quest = new DiscordQuest
            {
                Id = GetString(element, "id") ?? "",
                Name = GetString(element, "name") ?? "",
                ExpiresAt = GetDate(element, "expires_at", "expiresAt"),
                TrafficMetadataSealed = GetString(element, "traffic_metadata_sealed", "trafficMetadataSealed"),
                TrafficMetadataRaw = GetString(element, "traffic_metadata_raw", "trafficMetadataRaw"),
            };

            JsonElement config = GetObject(element, "config") ?? default;

            if (config.ValueKind == JsonValueKind.Object)
            {
                // Discord only stores the quest expiry inside config. The quest
                // object itself has no top-level expires_at.
                if (!quest.ExpiresAt.HasValue)
                    quest.ExpiresAt = GetDate(config, "expires_at", "expiresAt");

                JsonElement application = GetObject(config, "application") ?? default;
                if (application.ValueKind == JsonValueKind.Object)
                {
                    quest.ApplicationId = GetString(application, "id") ?? "";
                    quest.ApplicationName = GetString(application, "name") ?? "";
                }

                JsonElement messages = GetObject(config, "messages") ?? default;
                if (messages.ValueKind == JsonValueKind.Object)
                {
                    string? questName = GetString(messages, "quest_name", "questName");
                    if (!string.IsNullOrWhiteSpace(questName))
                        quest.Name = questName;
                }

                ParseTaskConfig(quest, config);
            }

            if (string.IsNullOrWhiteSpace(quest.Name))
                quest.Name = quest.ApplicationName;

            if (string.IsNullOrWhiteSpace(quest.Name))
                quest.Name = "Video quest";

            JsonElement userStatus = GetObject(element, "user_status", "userStatus") ?? default;
            if (userStatus.ValueKind == JsonValueKind.Object)
            {
                quest.EnrolledAt = GetDate(userStatus, "enrolled_at", "enrolledAt");
                quest.CompletedAt = GetDate(userStatus, "completed_at", "completedAt");
                quest.ClaimedAt = GetDate(userStatus, "claimed_at", "claimedAt");

                JsonElement progress = GetObject(userStatus, "progress") ?? default;
                if (progress.ValueKind == JsonValueKind.Object)
                {
                    quest.ServerProgressSeconds = ReadProgress(progress, quest.TaskType);
                }

                if (quest.ServerProgressSeconds <= 0)
                {
                    quest.ServerProgressSeconds = GetNumber(userStatus, "stream_progress_seconds", "streamProgressSeconds") ?? 0;
                }

                // Discord returns a user_status with an empty "progress" map for
                // quests the user has not accepted yet. Only enrolled_at (or a
                // later stage timestamp) means the quest is actually enrolled.
                // Posting video progress for an unenrolled quest returns 404.
                quest.Enrolled = quest.EnrolledAt.HasValue
                    || quest.CompletedAt.HasValue
                    || quest.ClaimedAt.HasValue;

                quest.Completed = quest.CompletedAt.HasValue
                    || (quest.Enrolled && quest.TargetSeconds > 0 && quest.ServerProgressSeconds >= quest.TargetSeconds);
                quest.Claimed = quest.ClaimedAt.HasValue;
            }

            if (quest.ExpiresAt.HasValue && quest.ExpiresAt.Value <= DateTimeOffset.UtcNow)
                quest.Expired = true;

            return quest;
        }

        private static void ParseTaskConfig(DiscordQuest quest, JsonElement config)
        {
            JsonElement taskConfig = FindElement(config, "task_config_v2", "taskConfigV2", "task_config", "taskConfig") ?? default;
            if (taskConfig.ValueKind == JsonValueKind.String)
            {
                string? json = taskConfig.GetString();
                if (string.IsNullOrWhiteSpace(json))
                    return;

                try
                {
                    using JsonDocument document = JsonDocument.Parse(json);
                    taskConfig = document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return;
                }
            }

            if (taskConfig.ValueKind != JsonValueKind.Object)
                return;

            JsonElement tasks = FindElement(taskConfig, "tasks") ?? default;
            if (tasks.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in tasks.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    string type = GetString(property.Value, "type") ?? property.Name;
                    if (!type.StartsWith("WATCH_VIDEO", StringComparison.OrdinalIgnoreCase))
                        continue;

                    quest.IsVideoQuest = true;
                    quest.TaskType = type;
                    quest.TargetSeconds = (long)Math.Ceiling(
                        GetNumber(property.Value, "target", "target_seconds", "targetSeconds") ?? 0);
                    break;
                }
            }
            else if (tasks.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement task in tasks.EnumerateArray())
                {
                    string type = GetString(task, "type") ?? GetString(task, "name") ?? "";
                    if (!type.StartsWith("WATCH_VIDEO", StringComparison.OrdinalIgnoreCase))
                        continue;

                    quest.IsVideoQuest = true;
                    quest.TaskType = type;
                    quest.TargetSeconds = (long)Math.Ceiling(
                        GetNumber(task, "target", "target_seconds", "targetSeconds") ?? 0);
                    break;
                }
            }
        }

        private static double ReadProgress(JsonElement progress, string taskType)
        {
            if (!string.IsNullOrWhiteSpace(taskType))
            {
                foreach (JsonProperty property in progress.EnumerateObject())
                {
                    if (!string.Equals(property.Name, taskType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (property.Value.ValueKind == JsonValueKind.Object)
                        return GetNumber(property.Value, "value", "progress", "seconds") ?? 0;

                    if (property.Value.ValueKind == JsonValueKind.Number)
                        return property.Value.GetDouble();
                }
            }

            return GetNumber(progress, "value", "progress", "seconds") ?? 0;
        }

        private static JsonElement? FindElement(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (string name in names)
            {
                if (element.TryGetProperty(name, out JsonElement value))
                    return value;
            }

            return null;
        }

        private static JsonElement? GetObject(JsonElement element, params string[] names)
        {
            JsonElement? value = FindElement(element, names);
            return value.HasValue && value.Value.ValueKind == JsonValueKind.Object ? value : null;
        }

        private static string? GetString(JsonElement element, params string[] names)
        {
            JsonElement? value = FindElement(element, names);
            return value.HasValue && value.Value.ValueKind == JsonValueKind.String ? value.Value.GetString() : null;
        }

        private static DateTimeOffset? GetDate(JsonElement element, params string[] names)
        {
            string? value = GetString(element, names);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : null;
        }

        private static double? GetNumber(JsonElement element, params string[] names)
        {
            JsonElement? value = FindElement(element, names);
            if (!value.HasValue)
                return null;

            if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDouble(out double number))
                return number;

            if (value.Value.ValueKind == JsonValueKind.String
                && double.TryParse(value.Value.GetString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out number))
            {
                return number;
            }

            return null;
        }
    }

    internal class QuestApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string? ResponseBody { get; }

        public QuestApiException(string message, HttpStatusCode? statusCode = null, string? responseBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    internal sealed class QuestAuthException : QuestApiException
    {
        public QuestAuthException(string message, HttpStatusCode? statusCode = null, string? responseBody = null)
            : base(message, statusCode, responseBody)
        {
        }
    }

    internal sealed class QuestRateLimitException : QuestApiException
    {
        public TimeSpan RetryAfter { get; }

        public QuestRateLimitException(string message, TimeSpan retryAfter, string? responseBody = null)
            : base(message, HttpStatusCode.TooManyRequests, responseBody)
        {
            RetryAfter = retryAfter;
        }
    }

    internal sealed class DiscordQuestClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _token;

        public DiscordQuestClient(string token)
        {
            _token = token.Trim();
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _token);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Config.discordUserAgent);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://discord.com");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://discord.com/channels/@me");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Discord-Locale", Config.discordLocale);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Discord-Timezone", GetIanaTimeZone());
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Super-Properties", BuildSuperProperties());
        }

        public async Task<DiscordUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            string json = await SendAsync(HttpMethod.Get, "/users/@me", null, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            return new DiscordUser
            {
                Id = GetString(root, "id") ?? "",
                Username = GetString(root, "username") ?? "",
                GlobalName = GetString(root, "global_name", "globalName") ?? "",
            };
        }

        public async Task<List<DiscordQuest>> GetQuestsAsync(CancellationToken cancellationToken = default)
        {
            string json = await SendAsync(HttpMethod.Get, "/quests/@me", null, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
                return new List<DiscordQuest>();

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            JsonElement quests = root;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("quests", out JsonElement nested))
            {
                quests = nested;
            }

            if (quests.ValueKind != JsonValueKind.Array)
                return new List<DiscordQuest>();

            List<DiscordQuest> result = new List<DiscordQuest>();
            foreach (JsonElement element in quests.EnumerateArray())
            {
                try
                {
                    result.Add(DiscordQuest.Parse(element));
                }
                catch
                {
                    // A single malformed quest should not hide the rest.
                }
            }

            return result
                .OrderByDescending(q => q.IsActive)
                .ThenBy(q => q.Completed)
                .ThenBy(q => q.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public Task EnrollAsync(
            string questId,
            string? trafficMetadataRaw,
            string? trafficMetadataSealed,
            CancellationToken cancellationToken = default)
        {
            // Matches the desktop client: it only sends location plus the sealed
            // ad-decision metadata, and omits the sealed field when absent.
            Dictionary<string, object?> body = new Dictionary<string, object?>
            {
                ["location"] = 11,
            };

            if (!string.IsNullOrWhiteSpace(trafficMetadataSealed))
                body["traffic_metadata_sealed"] = trafficMetadataSealed;

            return SendAsync(HttpMethod.Post, $"/quests/{Uri.EscapeDataString(questId)}/enroll", body, cancellationToken);
        }

        public Task SendVideoProgressAsync(string questId, double timestampSeconds, CancellationToken cancellationToken = default)
        {
            Dictionary<string, object?> body = new Dictionary<string, object?>
            {
                ["timestamp"] = Math.Round(timestampSeconds, 3),
            };

            return SendAsync(
                HttpMethod.Post,
                $"/quests/{Uri.EscapeDataString(questId)}/video-progress",
                body,
                cancellationToken);
        }

        public Task ClaimAsync(string questId, CancellationToken cancellationToken = default)
        {
            Dictionary<string, object?> body = new Dictionary<string, object?>
            {
                ["location"] = 11,
                ["platform"] = 0,
            };

            return SendAsync(
                HttpMethod.Post,
                $"/quests/{Uri.EscapeDataString(questId)}/claim-reward",
                body,
                cancellationToken);
        }

        public Task SendScienceAsync(string jsonBody, CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethod.Post, "/science", null, cancellationToken, jsonBody);
        }

        private async Task<string> SendAsync(
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken,
            string? rawBody = null)
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, Config.discordApiBase + path);

            if (rawBody != null)
            {
                request.Content = new StringContent(rawBody, Encoding.UTF8, "application/json");
            }
            else if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");
            }

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return responseBody;

            string safeBody = responseBody.Length > 400 ? responseBody[..400] : responseBody;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                TimeSpan retryAfter = TimeSpan.FromSeconds(5);
                try
                {
                    using JsonDocument document = JsonDocument.Parse(responseBody);
                    if (document.RootElement.TryGetProperty("retry_after", out JsonElement retry)
                        && retry.TryGetDouble(out double seconds))
                    {
                        retryAfter = TimeSpan.FromSeconds(Math.Clamp(seconds, 0.5, 120));
                    }
                }
                catch
                {
                }

                throw new QuestRateLimitException(
                    $"Discord rate limited the request. Retrying in {retryAfter.TotalSeconds:0.#}s.",
                    retryAfter,
                    safeBody);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new QuestAuthException(
                    "Discord rejected the token. It may have expired or been logged out.",
                    response.StatusCode,
                    safeBody);
            }

            string detail = DescribeError(safeBody);
            throw new QuestApiException(
                $"Discord returned {(int)response.StatusCode} {response.ReasonPhrase}{detail}.",
                response.StatusCode,
                safeBody);
        }

        private static string DescribeError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "";

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    string? message = null;
                    string? code = null;

                    if (root.TryGetProperty("message", out JsonElement messageElement)
                        && messageElement.ValueKind == JsonValueKind.String)
                    {
                        message = messageElement.GetString();
                    }

                    if (root.TryGetProperty("code", out JsonElement codeElement))
                    {
                        if (codeElement.ValueKind == JsonValueKind.Number
                            || codeElement.ValueKind == JsonValueKind.String)
                        {
                            code = codeElement.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return string.IsNullOrWhiteSpace(code) || code == "0"
                            ? $" ({message})"
                            : $" ({message}, code {code})";
                    }
                }
            }
            catch (JsonException)
            {
            }

            string compact = body.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (compact.Length > 200)
                compact = compact[..200] + "...";

            return $" ({compact})";
        }

        private static string GetIanaTimeZone()
        {
            try
            {
                string id = TimeZoneInfo.Local.Id;
                if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out string? iana) && !string.IsNullOrWhiteSpace(iana))
                    return iana;
            }
            catch
            {
            }

            return "UTC";
        }

        private static string BuildSuperProperties()
        {
            Dictionary<string, object?> properties = new Dictionary<string, object?>
            {
                ["os"] = "Windows",
                ["browser"] = "Discord Client",
                ["release_channel"] = "stable",
                ["client_version"] = Config.discordClientVersion,
                ["os_version"] = Environment.OSVersion.Version.ToString(),
                ["os_arch"] = "x64",
                ["app_arch"] = "x64",
                ["system_locale"] = Config.discordLocale,
                ["client_build_number"] = Config.discordBuildNumber,
                ["native_build_number"] = Config.discordBuildNumber,
                ["client_event_source"] = null,
                ["has_client_mods"] = false,
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(properties)));
        }

        private static string? GetString(JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (element.TryGetProperty(name, out JsonElement value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
