using System.Text.Json;

namespace DiscordGameSpoofer.Quests
{
    /// <summary>
    /// Discord's client also reports video quest progress through /science. The
    /// request in the wild uses a short analytics token that is not the account
    /// token and is not stored locally, so this is a best-effort copy of the event.
    /// The authoritative progress call is still /quests/{id}/video-progress.
    /// </summary>
    internal static class VideoScience
    {
        public static string BuildProgressEvent(
            string questId,
            string taskType,
            double progressSeconds,
            long targetSeconds,
            string? token)
        {
            Dictionary<string, object?> properties = new Dictionary<string, object?>
            {
                ["quest_id"] = questId,
                ["task_type"] = taskType,
                ["progress_seconds"] = Math.Round(progressSeconds, 3),
                ["seconds"] = Math.Round(progressSeconds, 3),
                ["target_seconds"] = targetSeconds,
                ["client_track_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            // The real analytics token is a two-part value that Discord does not
            // expose to us. Sending the account token is harmless here and is the
            // closest available substitute.
            if (!string.IsNullOrWhiteSpace(token))
                properties["token"] = token;

            Dictionary<string, object?> scienceEvent = new Dictionary<string, object?>
            {
                ["type"] = "quest_video_progress",
                ["properties"] = properties,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            };

            Dictionary<string, object?> body = new Dictionary<string, object?>
            {
                ["events"] = new[] { scienceEvent },
            };

            return JsonSerializer.Serialize(body);
        }
    }
}
