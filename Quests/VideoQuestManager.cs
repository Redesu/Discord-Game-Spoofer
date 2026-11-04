using System.Net;

namespace DiscordGameSpoofer.Quests
{
    /// <summary>
    /// Polls Discord for quests, optionally enrolls in video quests, and sends
    /// video-progress updates at a human-like pace until the target is reached.
    /// Only one quest is watched at a time.
    /// </summary>
    internal sealed class VideoQuestManager : IDisposable
    {
        private readonly Action<string> _log;
        private readonly SemaphoreSlim _watcherGate = new(1, 1);
        private readonly object _sync = new();

        private readonly Dictionary<string, double> _localProgress = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _localEnrollment = new(StringComparer.Ordinal);
        private readonly HashSet<string> _locallyCompleted = new(StringComparer.Ordinal);
        private readonly HashSet<string> _reportedNoTarget = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _acceptFailures = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _claimFailures = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _progressFailures = new(StringComparer.Ordinal);

        private CancellationTokenSource? _cts;
        private Task? _pollTask;
        private Task? _watcherTask;
        private DiscordQuestClient? _client;
        private string? _token;
        private volatile bool _running;
        private string? _lastStatus;

        public VideoQuestManager(Action<string> log)
        {
            _log = log;
        }

        public event Action<IReadOnlyList<DiscordQuest>>? QuestsUpdated;
        public event Action<string>? StatusChanged;
        public event Action<bool>? RunningChanged;

        public bool AutoAccept { get; set; }
        public bool AutoClaim { get; set; }
        public bool SendScience { get; set; }

        public bool IsRunning => _running;

        public async Task<DiscordUser> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            using DiscordQuestClient client = new DiscordQuestClient(token);
            return await client.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Start(string token, DiscordUser user)
        {
            DiscordQuestClient client;
            CancellationToken cancellationToken;
            CancellationTokenSource? previousCts;

            lock (_sync)
            {
                if (_running)
                    return;

                previousCts = _cts;
                _token = token.Trim();
                client = new DiscordQuestClient(_token);
                _client = client;
                _cts = new CancellationTokenSource();
                cancellationToken = _cts.Token;

                _localProgress.Clear();
                _localEnrollment.Clear();
                _locallyCompleted.Clear();
                _reportedNoTarget.Clear();
                _acceptFailures.Clear();
                _claimFailures.Clear();
                _progressFailures.Clear();
                _running = true;
            }

            previousCts?.Dispose();

            SetStatus($"Signed in as {user.DisplayName}. Watching for video quests...");
            _log($"Connected to Discord as {user.DisplayName}.");
            _log("Waiting for a video quest. Start one in Discord, or enable auto-accept.");

            RunningChanged?.Invoke(true);
            _pollTask = Task.Run(
                () => PollLoopAsync(client, cancellationToken),
                CancellationToken.None);
        }

        public async Task StopAsync()
        {
            CancellationTokenSource? cts;
            DiscordQuestClient? client;
            Task? pollTask;
            Task? watcherTask;

            lock (_sync)
            {
                cts = _cts;
                client = _client;
                pollTask = _pollTask;
                watcherTask = _watcherTask;

                _cts = null;
                _client = null;
                _pollTask = null;
                _watcherTask = null;
                _token = null;
                _running = false;
            }

            cts?.Cancel();

            foreach (Task? task in new[] { pollTask, watcherTask })
            {
                if (task == null)
                    continue;

                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                }
            }

            client?.Dispose();
            cts?.Dispose();

            SetStatus("Stopped.");
            RunningChanged?.Invoke(false);
        }

        public void Stop()
        {
            _ = StopAsync();
        }

        private async Task PollLoopAsync(DiscordQuestClient client, CancellationToken cancellationToken)
        {
            bool reportedPollError = false;
            string? stopReason = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                IReadOnlyList<DiscordQuest>? quests = null;

                try
                {
                    quests = await client.GetQuestsAsync(cancellationToken).ConfigureAwait(false);
                    reportedPollError = false;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (QuestAuthException ex)
                {
                    _log(ex.Message);
                    stopReason = "Discord rejected the token. Sign in again or auto-detect a new token.";
                    break;
                }
                catch (QuestRateLimitException ex)
                {
                    _log(ex.Message);

                    try
                    {
                        await Task.Delay(ex.RetryAfter, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    continue;
                }
                catch (Exception ex)
                {
                    if (!reportedPollError)
                        _log($"Quest polling failed: {ex.Message}");

                    reportedPollError = true;
                }

                if (quests != null)
                {
                    ApplyLocalState(quests);
                    RaiseQuestsUpdated(quests);

                    try
                    {
                        if (AutoAccept)
                            await AcceptEligibleAsync(client, quests, cancellationToken).ConfigureAwait(false);

                        if (AutoClaim)
                            await ClaimCompletedAsync(client, quests, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (QuestAuthException ex)
                    {
                        _log(ex.Message);
                        stopReason = "Discord rejected the token. Sign in again or auto-detect a new token.";
                        break;
                    }
                    catch (QuestRateLimitException ex)
                    {
                        _log(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        _log($"Quest action failed: {ex.Message}");
                    }

                    StartWatcher(client, quests, cancellationToken);
                    UpdateStatus(quests);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Config.questPollSeconds), cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            bool wasRunning = _running;
            _running = false;

            if (stopReason != null)
            {
                lock (_sync)
                {
                    if (ReferenceEquals(_client, client))
                        _client = null;
                }

                try
                {
                    client.Dispose();
                }
                catch
                {
                }

                SetStatus(stopReason);
            }
            else if (!cancellationToken.IsCancellationRequested)
            {
                SetStatus("Stopped watching quests.");
            }

            if (wasRunning)
                RunningChanged?.Invoke(false);
        }

        private void ApplyLocalState(IReadOnlyList<DiscordQuest> quests)
        {
            foreach (DiscordQuest quest in quests)
            {
                if (quest.EnrolledAt.HasValue)
                    _localEnrollment[quest.Id] = quest.EnrolledAt.Value.ToUnixTimeSeconds();

                if (_localEnrollment.TryGetValue(quest.Id, out double enrollment))
                    quest.LocalEnrollmentEpoch = enrollment;

                if (_localProgress.TryGetValue(quest.Id, out double progress))
                    quest.LocalProgressSeconds = Math.Max(quest.ServerProgressSeconds, progress);
                else
                    quest.LocalProgressSeconds = quest.ServerProgressSeconds;

                if (_locallyCompleted.Contains(quest.Id))
                {
                    quest.Completed = true;
                    quest.LocalProgressSeconds = Math.Max(quest.LocalProgressSeconds, quest.TargetSeconds);
                    _localProgress[quest.Id] = quest.LocalProgressSeconds;
                }
            }
        }

        private async Task AcceptEligibleAsync(
            DiscordQuestClient client,
            IReadOnlyList<DiscordQuest> quests,
            CancellationToken cancellationToken)
        {
            foreach (DiscordQuest quest in quests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!quest.IsVideoQuest || quest.Enrolled || quest.Completed || quest.Expired)
                    continue;

                if (_acceptFailures.TryGetValue(quest.Id, out DateTime failedAt)
                    && DateTime.UtcNow - failedAt < TimeSpan.FromMinutes(10))
                {
                    continue;
                }

                try
                {
                    await client.EnrollAsync(
                            quest.Id,
                            quest.TrafficMetadataRaw,
                            quest.TrafficMetadataSealed,
                            cancellationToken)
                        .ConfigureAwait(false);

                    double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    quest.Enrolled = true;
                    quest.EnrolledAt = DateTimeOffset.UtcNow;
                    quest.LocalEnrollmentEpoch = now;
                    _localEnrollment[quest.Id] = now;
                    _acceptFailures.Remove(quest.Id);

                    _log($"Accepted video quest \"{quest.Name}\".");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (QuestAuthException)
                {
                    throw;
                }
                catch (QuestRateLimitException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _acceptFailures[quest.Id] = DateTime.UtcNow;
                    _log($"Could not accept \"{quest.Name}\": {ex.Message}");
                }
            }
        }

        private async Task ClaimCompletedAsync(
            DiscordQuestClient client,
            IReadOnlyList<DiscordQuest> quests,
            CancellationToken cancellationToken)
        {
            foreach (DiscordQuest quest in quests)
            {
                if (!quest.IsVideoQuest || !quest.Completed || quest.Claimed)
                    continue;

                if (_claimFailures.TryGetValue(quest.Id, out DateTime failedAt)
                    && DateTime.UtcNow - failedAt < TimeSpan.FromMinutes(5))
                {
                    continue;
                }

                try
                {
                    await client.ClaimAsync(quest.Id, cancellationToken).ConfigureAwait(false);
                    quest.Claimed = true;
                    quest.ClaimedAt = DateTimeOffset.UtcNow;
                    _claimFailures.Remove(quest.Id);
                    _log($"Claimed the reward for \"{quest.Name}\".");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (QuestAuthException)
                {
                    throw;
                }
                catch (QuestRateLimitException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _claimFailures[quest.Id] = DateTime.UtcNow;
                    _log($"Could not claim \"{quest.Name}\": {ex.Message}");
                }
            }
        }

        private void StartWatcher(
            DiscordQuestClient client,
            IReadOnlyList<DiscordQuest> quests,
            CancellationToken cancellationToken)
        {
            DiscordQuest? quest = quests.FirstOrDefault(
                q => q.IsActive
                    && !_locallyCompleted.Contains(q.Id)
                    && !_reportedNoTarget.Contains(q.Id)
                    && !IsProgressCooldown(q.Id));

            if (quest == null)
                return;

            if (!_watcherGate.Wait(0))
                return;

            Task task = Task.Run(async () =>
            {
                try
                {
                    await WatchQuestAsync(client, quest, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (QuestAuthException ex)
                {
                    _log(ex.Message);
                    SetStatus("Discord rejected the token while sending progress.");
                }
                catch (Exception ex)
                {
                    _log($"Video quest watcher stopped: {ex.Message}");
                }
                finally
                {
                    _watcherGate.Release();
                }
            }, CancellationToken.None);

            lock (_sync)
            {
                _watcherTask = task;
            }
        }

        private async Task WatchQuestAsync(
            DiscordQuestClient client,
            DiscordQuest quest,
            CancellationToken cancellationToken)
        {
            long target = quest.TargetSeconds;
            if (target <= 0)
            {
                if (_reportedNoTarget.Add(quest.Id))
                    _log($"\"{quest.Name}\" does not have a readable video target. Skipping it.");

                return;
            }

            // Discord only accepts incremental progress: the real desktop client
            // reports the media timestamp it has reached, roughly every 10 seconds.
            // Never derive the timestamp from enrolled_at, because a quest accepted
            // a while ago would jump straight to the target and be rejected (400).
            double lastSent = Math.Clamp(
                Math.Floor(Math.Max(quest.ServerProgressSeconds, quest.LocalProgressSeconds)),
                0,
                target);

            _log($"Watching \"{quest.Name}\" ({FormatDuration(target)}).");
            SetStatus($"Sending progress for \"{quest.Name}\"...");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (quest.ExpiresAt.HasValue && quest.ExpiresAt.Value <= DateTimeOffset.UtcNow)
                {
                    _log($"\"{quest.Name}\" expired before it finished.");
                    break;
                }

                // Wait before every report, including the first one, so the very
                // first timestamp is never 0 or a sudden jump to the target.
                int delayMs = Random.Shared.Next(
                    Config.videoProgressMinMs,
                    Config.videoProgressMaxMs + 1);

                try
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Whole seconds, like Discord's client. The step is the time spent
                // since the last accepted report, so progress stays human-like.
                double progress = Math.Min(target, lastSent + Math.Floor(delayMs / 1000.0));
                bool sent = false;

                try
                {
                    await client.SendVideoProgressAsync(quest.Id, progress, cancellationToken)
                        .ConfigureAwait(false);

                    lastSent = progress;
                    quest.LocalProgressSeconds = progress;
                    _localProgress[quest.Id] = progress;
                    ClearProgressFailure(quest.Id);
                    sent = true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (QuestAuthException)
                {
                    throw;
                }
                catch (QuestRateLimitException ex)
                {
                    _log(ex.Message);

                    try
                    {
                        await Task.Delay(ex.RetryAfter, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    continue;
                }
                catch (QuestApiException ex) when (IsTerminalQuestStatus(ex.StatusCode))
                {
                    // 400/403/404/409/410 mean Discord will not accept progress for
                    // this quest, so retrying the same value would loop forever.
                    RecordProgressFailure(quest.Id);
                    _log($"Discord rejected video progress for \"{quest.Name}\" ({(int)ex.StatusCode.GetValueOrDefault()} {ex.StatusCode}).{DescribeResponse(ex)} Skipping it for 10 minutes.");
                    break;
                }
                catch (Exception ex)
                {
                    _log($"Could not send video progress: {ex.Message}");
                }

                if (SendScience)
                {
                    try
                    {
                        string scienceBody = VideoScience.BuildProgressEvent(
                            quest.Id,
                            quest.TaskType,
                            progress,
                            target,
                            _token);

                        await client.SendScienceAsync(scienceBody, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // Science is optional telemetry; video-progress is authoritative.
                    }
                }

                if (sent && progress >= target)
                {
                    _locallyCompleted.Add(quest.Id);
                    quest.Completed = true;
                    quest.LocalProgressSeconds = target;
                    _localProgress[quest.Id] = target;

                    _log($"Reached the video target for \"{quest.Name}\".");
                    break;
                }
            }

            if (_locallyCompleted.Contains(quest.Id) && AutoClaim && !cancellationToken.IsCancellationRequested)
                await ConfirmAndClaimAsync(client, quest, cancellationToken).ConfigureAwait(false);

            SetStatus("Waiting for another video quest...");
        }

        private async Task ConfirmAndClaimAsync(
            DiscordQuestClient client,
            DiscordQuest quest,
            CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                try
                {
                    IReadOnlyList<DiscordQuest> fresh = await client.GetQuestsAsync(cancellationToken)
                        .ConfigureAwait(false);
                    DiscordQuest? current = fresh.FirstOrDefault(q => q.Id == quest.Id);

                    if (current == null)
                        continue;

                    quest.ServerProgressSeconds = current.ServerProgressSeconds;
                    quest.Completed = current.Completed;
                    quest.Claimed = current.Claimed;
                    quest.CompletedAt = current.CompletedAt;
                    quest.ClaimedAt = current.ClaimedAt;

                    if (quest.Completed)
                        break;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (QuestAuthException)
                {
                    throw;
                }
                catch
                {
                }
            }

            if (!quest.Completed)
            {
                _log($"Discord has not confirmed \"{quest.Name}\" yet. The reward will be claimed on a later poll.");
                return;
            }

            if (quest.Claimed)
                return;

            try
            {
                await client.ClaimAsync(quest.Id, cancellationToken).ConfigureAwait(false);
                quest.Claimed = true;
                quest.ClaimedAt = DateTimeOffset.UtcNow;
                _log($"Claimed the reward for \"{quest.Name}\".");
            }
            catch (OperationCanceledException)
            {
            }
            catch (QuestAuthException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _claimFailures[quest.Id] = DateTime.UtcNow;
                _log($"Could not claim \"{quest.Name}\": {ex.Message}");
            }
        }

        private void UpdateStatus(IReadOnlyList<DiscordQuest> quests)
        {
            List<DiscordQuest> videoQuests = quests.Where(q => q.IsVideoQuest).ToList();

            if (videoQuests.Count == 0)
            {
                SetStatus("No video quests found. Start a video quest in Discord to begin.");
                return;
            }

            if (videoQuests.Any(q => q.IsActive))
            {
                DiscordQuest active = videoQuests.First(q => q.IsActive);
                SetStatus($"Watching \"{active.Name}\"...");
                return;
            }

            if (videoQuests.Any(q => q.Completed && !q.Claimed))
            {
                SetStatus("A video quest is complete. Claim the reward in Discord.");
                return;
            }

            if (videoQuests.Any(q => !q.Completed && !q.Expired && !q.Enrolled))
            {
                SetStatus("Waiting for you to start a video quest in Discord...");
                return;
            }

            SetStatus("Waiting for quest updates...");
        }

        private void RaiseQuestsUpdated(IReadOnlyList<DiscordQuest> quests)
        {
            try
            {
                QuestsUpdated?.Invoke(quests);
            }
            catch
            {
            }
        }

        private void SetStatus(string status)
        {
            if (string.Equals(_lastStatus, status, StringComparison.Ordinal))
                return;

            _lastStatus = status;

            try
            {
                StatusChanged?.Invoke(status);
            }
            catch
            {
            }
        }

        private bool IsProgressCooldown(string questId)
        {
            lock (_sync)
            {
                return _progressFailures.TryGetValue(questId, out DateTime failedAt)
                    && DateTime.UtcNow - failedAt < TimeSpan.FromMinutes(10);
            }
        }

        private void RecordProgressFailure(string questId)
        {
            lock (_sync)
            {
                _progressFailures[questId] = DateTime.UtcNow;
            }
        }

        private void ClearProgressFailure(string questId)
        {
            lock (_sync)
            {
                _progressFailures.Remove(questId);
            }
        }

        private static bool IsTerminalQuestStatus(HttpStatusCode? statusCode)
        {
            return statusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.Conflict
                or HttpStatusCode.Gone;
        }

        private static string DescribeResponse(QuestApiException ex)
        {
            if (string.IsNullOrWhiteSpace(ex.ResponseBody))
                return "";

            string body = ex.ResponseBody.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (body.Length > 200)
                body = body[..200] + "...";

            return $" Response: {body}.";
        }

        private static string FormatDuration(long seconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}h {duration.Minutes:00}m"
                : $"{duration.Minutes}m {duration.Seconds:00}s";
        }

        public void Dispose()
        {
            CancellationTokenSource? cts;
            DiscordQuestClient? client;

            lock (_sync)
            {
                cts = _cts;
                client = _client;

                _cts = null;
                _client = null;
                _pollTask = null;
                _watcherTask = null;
                _token = null;
                _running = false;
            }

            cts?.Cancel();
            cts?.Dispose();
            client?.Dispose();
        }
    }
}
