using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DiscordGameSpoofer
{
    public partial class Form1 : Form
    {
        private List<DiscordDetectableApplication> allGames = new List<DiscordDetectableApplication>();
        private List<DiscordDetectableApplication> customGames = new List<DiscordDetectableApplication>();
        private List<DiscordDetectableApplication> gameQueue = new List<DiscordDetectableApplication>();
        private DiscordDetectableApplication? selectedGame = null;
        private List<RunningGame> runningGames = new List<RunningGame>();
        private System.Windows.Forms.Timer gameTimer = new System.Windows.Forms.Timer();
        private RunningGame? activeQueueGame = null;
        private bool queueRunning = false;

        string appPath = AppContext.BaseDirectory;
        string gamePath = "";
        string blankWindowPath = "";
        string customAppsPath = "";
        string gameFoldersPath = "";
        string steamAppsCachePath = "";
        string steamManifestsPath = "";

        private Dictionary<string, string> gameFolders = new(StringComparer.OrdinalIgnoreCase);
        private List<SteamLibrary> steamLibraries = new();
        private Dictionary<string, string> steamInstallDirs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> steamManifests = new(StringComparer.OrdinalIgnoreCase);
        private bool startInProgress = false;

        // Appended to every generated spoof host so we can tell our files apart from
        // real game executables and refuse to overwrite anything we did not create.
        private static readonly byte[] HostTrailer = Encoding.ASCII.GetBytes("DGSHOST1");

        bool logShown = false;

        // Layout is based on the designer's original surface. WinForms scales the
        // controls for the current DPI/font settings before OnLoad, so this factor
        // lets us rebuild the same layout at any window size without tiny fonts.
        private float uiScale = 1f;
        private bool layoutReady = false;
        private bool applyingLayout = false;

        private class SteamLibrary
        {
            public string RootPath { get; set; } = "";
            public string SteamAppsPath => Path.Combine(RootPath, "steamapps");
            public string CommonPath => Path.Combine(SteamAppsPath, "common");
        }

        private class RunningGame
        {
            public Process Process { get; set; } = null!;
            public string Name { get; set; } = "";
            public string GameId { get; set; } = "";
            public DateTime StartTime { get; set; }
            public bool IsQueueGame { get; set; }

            public override string ToString()
            {
                return Name ?? "";
            }
        }

        // Move these to interfaces folder
        public class DiscordDetectableApplication
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("executables")]
            public List<DiscordExecutable> Executables { get; set; } = new();

            [JsonPropertyName("third_party_skus")]
            public List<DiscordThirdPartySku> ThirdPartySkus { get; set; } = new();

            public override string ToString()
            {
                return Name ?? "";
            }
        }

        public class DiscordThirdPartySku
        {
            [JsonPropertyName("distributor")]
            public string Distributor { get; set; } = "";

            [JsonPropertyName("id")]
            public string Id { get; set; } = "";
        }

        public class DiscordExecutable
        {

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("os")]
            public string Os { get; set; } = "";
        }

        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public Form1()
        {
            InitializeComponent();

            versionLabel.Text = $"v{Config.version}";
            videoQuestsPanel.SetExternalLog(log);
            ShowTab(showVideo: false);

            gamesList.Items.Add("Loading...");
            gamesList.Items.Add("This can");
            gamesList.Items.Add("take a second.");
            gamesList.Items.Add("UI may lag.");

            addToQueueButton.Enabled = false;
            startQueueButton.Enabled = false;
            setInstallFolderButton.Enabled = false;
            defaultInstallFolderButton.Enabled = false;
            UpdateQueueButtons();

            log("Welcome to Discord Game Spoofer");
            log(Config.githubUrl);
            logBox.Hide();

            detailsBox.AppendText("Make a selection on the left to display details.\nInterface may lag while filtering, it has to check through 23.000 games.");

            gameTimer.Interval = 1000;
            gameTimer.Tick += gameTimer_Tick;
            gameTimer.Start();

            FormClosing += Form1_FormClosing;

            _ = InitializeAsync();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDarkTitleBar();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            uiScale = GetUiScale();
            FitWindowToScreen();

            layoutReady = true;
            ApplyResponsiveLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!layoutReady || applyingLayout || WindowState == FormWindowState.Minimized)
                return;

            ApplyResponsiveLayout();
        }

        private float GetUiScale()
        {
            SizeF design = AutoScaleDimensions;
            SizeF current = CurrentAutoScaleDimensions;

            if (design.Width > 0 && current.Width > 0)
                return Math.Max(0.5f, current.Width / design.Width);

            return Math.Max(0.5f, DeviceDpi / 96f);
        }

        private int ScaleValue(float value)
        {
            return Math.Max(1, (int)Math.Round(value * uiScale));
        }

        /// <summary>
        /// Opens the window centered at a size that fits the current screen and
        /// never grows past the designer size.
        /// </summary>
        private void FitWindowToScreen()
        {
            Rectangle area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;

            int chromeWidth = Math.Max(0, Width - ClientSize.Width);
            int chromeHeight = Math.Max(0, Height - ClientSize.Height);
            int margin = ScaleValue(24);

            int maxClientWidth = Math.Max(ScaleValue(900), area.Width - chromeWidth - margin * 2);
            int maxClientHeight = Math.Max(ScaleValue(520), area.Height - chromeHeight - margin * 2);

            int clientWidth = Math.Min(ScaleValue(1366), maxClientWidth);
            int clientHeight = Math.Min(ScaleValue(860), maxClientHeight);

            // Minimum usable size in device pixels. The layout below is designed to
            // stay readable all the way down to this size. On screens that cannot
            // show even that, the window is capped to the available work area.
            MinimumSize = new Size(
                Math.Min(ScaleValue(1000), maxClientWidth) + chromeWidth,
                Math.Min(ScaleValue(600), maxClientHeight) + chromeHeight);

            clientWidth = Math.Max(MinimumSize.Width - chromeWidth, clientWidth);
            clientHeight = Math.Max(MinimumSize.Height - chromeHeight, clientHeight);

            applyingLayout = true;
            try
            {
                ClientSize = new Size(clientWidth, clientHeight);
            }
            finally
            {
                applyingLayout = false;
            }

            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;

            if (Top < area.Top)
                Top = area.Top;
        }

        /// <summary>
        /// Rebuilds the game tab layout for the current client size. Lists and text
        /// boxes grow with the window while fonts stay at their normal size.
        /// </summary>
        private void ApplyResponsiveLayout()
        {
            if (!layoutReady || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            applyingLayout = true;
            SuspendLayout();

            try
            {
                int pad = ScaleValue(22);
                int navHeight = ScaleValue(56);
                int navButtonWidth = ScaleValue(170);
                int navButtonHeight = ScaleValue(40);
                int navButtonGap = ScaleValue(8);

                navGamesButton.SetBounds(pad, ScaleValue(8), navButtonWidth, navButtonHeight);
                navVideoButton.SetBounds(
                    pad + navButtonWidth + navButtonGap,
                    ScaleValue(8),
                    navButtonWidth,
                    navButtonHeight);

                versionLabel.Location = new Point(
                    Math.Max(pad, ClientSize.Width - pad - versionLabel.Width),
                    ScaleValue(16));

                int contentHeight = Math.Max(ScaleValue(200), ClientSize.Height - navHeight);

                gamesHost.SetBounds(0, navHeight, ClientSize.Width, contentHeight);
                videoHost.SetBounds(0, navHeight, ClientSize.Width, contentHeight);

                LayoutGamesTab(ClientSize.Width, contentHeight);
                videoQuestsPanel.ApplyResponsiveLayout(uiScale);
            }
            finally
            {
                ResumeLayout(true);
                applyingLayout = false;
            }
        }

        private void LayoutGamesTab(int width, int height)
        {
            int pad = ScaleValue(22);
            int gap = ScaleValue(11);
            int listTop = ScaleValue(94);
            int leftWidth = ScaleValue(316);
            int rightX = ScaleValue(349);
            int rightEdge = width - pad;

            // Left column: search, games list, and the two action buttons.
            searchBox.SetBounds(pad, ScaleValue(26), Math.Max(ScaleValue(120), leftWidth - ScaleValue(2)), ScaleValue(39));

            int leftButtonHeight = ScaleValue(44);
            int leftButtonTop = height - ScaleValue(8) - leftButtonHeight;
            int autoStopHeight = ScaleValue(26);
            int autoStopTop = leftButtonTop - ScaleValue(8) - autoStopHeight;
            autoStopCheckBox.SetBounds(pad, autoStopTop, leftWidth, autoStopHeight);

            int gamesListHeight = Math.Max(
                ScaleValue(80),
                autoStopTop - ScaleValue(10) - listTop);
            gamesList.SetBounds(pad, listTop, leftWidth, gamesListHeight);

            int halfButtonWidth = Math.Max(ScaleValue(70), (leftWidth - gap) / 2);
            addToQueueButton.SetBounds(pad, leftButtonTop, halfButtonWidth, leftButtonHeight);
            customAppButton.SetBounds(
                pad + halfButtonWidth + gap,
                leftButtonTop,
                halfButtonWidth,
                leftButtonHeight);

            // Top header: running games title on the left, action buttons on the right.
            int headerButtonHeight = ScaleValue(49);
            int headerButtonTop = ScaleValue(26);
            int maxHeaderButtonWidth = Math.Max(
                ScaleValue(80),
                (rightEdge - (rightX + label1.Width + gap) - gap * 2) / 3);
            int headerButtonWidth = Math.Min(ScaleValue(130), maxHeaderButtonWidth);

            stopButton.SetBounds(
                rightEdge - headerButtonWidth,
                headerButtonTop,
                headerButtonWidth,
                headerButtonHeight);
            startButton.SetBounds(
                stopButton.Left - gap - headerButtonWidth,
                headerButtonTop,
                headerButtonWidth,
                headerButtonHeight);
            toggleLogButton.SetBounds(
                startButton.Left - gap - headerButtonWidth,
                headerButtonTop,
                headerButtonWidth,
                headerButtonHeight);

            label1.Location = new Point(rightX, ScaleValue(32));

            // Two equal list rows. The row heights are recomputed on every resize.
            int toolsHeight = ScaleValue(42);
            int listGap = ScaleValue(10);
            int labelHeight = ScaleValue(32);
            int labelGap = ScaleValue(24);
            int bottomMargin = ScaleValue(8);

            int fixedHeight = listGap + toolsHeight + listGap + labelHeight + labelGap;
            int flexibleHeight = Math.Max(
                ScaleValue(180),
                height - listTop - fixedHeight - bottomMargin);
            int topListHeight = flexibleHeight / 2;

            int toolsTop = listTop + topListHeight + listGap;
            int bottomHeaderTop = toolsTop + toolsHeight + listGap;
            int bottomListTop = bottomHeaderTop + labelHeight + labelGap;
            int bottomListHeight = Math.Max(
                ScaleValue(90),
                height - bottomMargin - bottomListTop);

            int timeWidth = ScaleValue(139);
            runningGamesTime.SetBounds(rightEdge - timeWidth, listTop, timeWidth, topListHeight);
            runningGamesList.SetBounds(
                rightX,
                listTop,
                Math.Max(ScaleValue(120), runningGamesTime.Left - gap - rightX),
                topListHeight);

            // Install folder and Steam options sit on their own row below the running list.
            int toolsGap = ScaleValue(11);
            int steamWidth = ScaleValue(200);
            int installWidth = ScaleValue(190);
            int defaultWidth = ScaleValue(210);
            int toolsWidth = steamWidth + installWidth + defaultWidth + toolsGap * 2;

            // On very narrow displays shrink the three controls so they stay on screen.
            int toolsAvailable = Math.Max(ScaleValue(200), rightEdge - rightX);
            if (toolsWidth > toolsAvailable)
            {
                float toolFit = (float)toolsAvailable / toolsWidth;
                steamWidth = Math.Max(ScaleValue(110), (int)Math.Round(steamWidth * toolFit));
                installWidth = Math.Max(ScaleValue(110), (int)Math.Round(installWidth * toolFit));
                defaultWidth = Math.Max(ScaleValue(110), (int)Math.Round(defaultWidth * toolFit));
                toolsWidth = steamWidth + installWidth + defaultWidth + toolsGap * 2;
            }

            int toolsX = Math.Max(rightX, rightEdge - toolsWidth);

            steamManifestCheckBox.SetBounds(toolsX, toolsTop, steamWidth, toolsHeight);
            setInstallFolderButton.SetBounds(
                steamManifestCheckBox.Right + toolsGap,
                toolsTop,
                installWidth,
                toolsHeight);
            defaultInstallFolderButton.SetBounds(
                setInstallFolderButton.Right + toolsGap,
                toolsTop,
                defaultWidth,
                toolsHeight);

            // Bottom row: queue on the left, details/log on the right.
            int rightWidth = Math.Max(ScaleValue(320), rightEdge - rightX);
            int queueWidth = Math.Max(ScaleValue(160), (int)Math.Round(rightWidth * 0.27));
            int singleButtonWidth = ScaleValue(100);
            int queueButtonGap = ScaleValue(6);

            // With very little height, two button columns keep every queue action
            // visible instead of overflowing below the window.
            int buttonColumns = bottomListHeight < ScaleValue(154) ? 2 : 1;
            int buttonAreaWidth = singleButtonWidth * buttonColumns
                + (buttonColumns > 1 ? queueButtonGap : 0);
            int queueButtonX = rightX + queueWidth + gap;
            int detailsX = queueButtonX + buttonAreaWidth + ScaleValue(10);
            int detailsWidth = Math.Max(ScaleValue(120), rightEdge - detailsX);

            queueLabel.Location = new Point(rightX, bottomHeaderTop);
            label2.Location = new Point(detailsX, bottomHeaderTop);

            queueList.SetBounds(rightX, bottomListTop, queueWidth, bottomListHeight);

            detailsBox.SetBounds(detailsX, bottomListTop, detailsWidth, bottomListHeight);
            logBox.Bounds = detailsBox.Bounds;

            int buttonRows = (5 + buttonColumns - 1) / buttonColumns;
            int queueButtonHeight = Math.Min(
                ScaleValue(50),
                Math.Max(
                    ScaleValue(24),
                    (bottomListHeight - queueButtonGap * (buttonRows - 1)) / buttonRows));

            Button[] queueButtons =
            {
                startQueueButton,
                removeQueueButton,
                queueUpButton,
                queueDownButton,
                clearQueueButton,
            };

            for (int i = 0; i < queueButtons.Length; i++)
            {
                int column = i % buttonColumns;
                int row = i / buttonColumns;

                queueButtons[i].SetBounds(
                    queueButtonX + column * (singleButtonWidth + queueButtonGap),
                    bottomListTop + row * (queueButtonHeight + queueButtonGap),
                    singleButtonWidth,
                    queueButtonHeight);
            }
        }

        private void ApplyDarkTitleBar()
        {
            try
            {
                int enabled = 1;

                // Windows 11 and Windows 10 2004+. Older Windows 10 builds use
                // attribute 19 instead.
                if (DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(
                        Handle,
                        DwmwaUseImmersiveDarkModeBefore20H1,
                        ref enabled,
                        sizeof(int));
                }
            }
            catch
            {
                // Dark title bars are cosmetic and unavailable on some Windows versions.
            }
        }

        private async Task InitializeAsync()
        {
            // This is just incase
            startButton.Enabled = false;
            startQueueButton.Enabled = false;
            addToQueueButton.Enabled = false;

            await CheckVersion();
            await SetupApplicationAsync();
            await LoadDetectableGamesAsync();
            LoadCustomApps();
            RefreshGamesList();

            bool canStart = IsSpoofHostAvailable();
            startButton.Enabled = canStart;
            UpdateQueueButtons();

            if (!canStart)
            {
                log("Start is disabled because the spoof host could not be located. Restart the app or check the log for details.");
            }
        }

        private async void gameTimer_Tick(object? sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            TimeSpan autoStopAfter = TimeSpan.FromMinutes(Config.autoStopMinutes);

            for (int i = runningGames.Count - 1; i >= 0; i--)
            {
                if (i >= runningGamesTime.Items.Count)
                    continue;

                RunningGame game = runningGames[i];
                TimeSpan elapsed = DateTime.Now - game.StartTime;
                runningGamesTime.Items[i] = elapsed.ToString(@"hh\:mm\:ss");

                bool exited;
                try
                {
                    exited = game.Process.HasExited;
                }
                catch
                {
                    exited = true;
                }

                // Queue games always auto stop so the queue can move on.
                bool autoStop = (game.IsQueueGame || autoStopCheckBox.Checked) && elapsed >= autoStopAfter;

                if (!exited && !autoStop)
                    continue;

                bool wasActiveQueueGame = ReferenceEquals(game, activeQueueGame);
                string gameName = game.Name;

                RemoveRunningGame(i, kill: autoStop && !exited);

                if (autoStop && !exited)
                {
                    log($"Auto-stopped {gameName} after {Config.autoStopMinutes} minutes.");
                }
                else
                {
                    log($"Spoof process exited for {gameName}.");
                }

                if (wasActiveQueueGame)
                {
                    activeQueueGame = null;

                    if (queueRunning)
                    {
                        await StartNextQueuedGameAsync();
                    }
                }
            }
        }

        private void log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logBox.AppendText($"[{timestamp}] {message}\n");

            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        private async Task CheckVersion()
        {
            try
            {
                log("Checking version.");
                using HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordGameSpoofer"); // Got HTTP 403 without a header
                using JsonDocument jsonDoc = JsonDocument.Parse(await client.GetStringAsync(Config.versionCheckUrl));
                string latestVersion = jsonDoc.RootElement.GetProperty("tag_name").ToString();

                if (latestVersion != Config.version)
                {
                    log("A new version of Discord Game Spoofer is available, please update to the latest version.\nThis version will keep working.");
                    MessageBox.Show("A new version of Discord Game Spoofer is available, please update to the latest version.\nThis version will keep working.");
                }
                else
                {
                    log("Latest version of Discord Game Spoofer in use.");
                }
            }
            catch (Exception ex)
            {
                log($"Failed to check version: {ex.Message}");
                MessageBox.Show("Failed to check version: " + ex.Message);
            }
        }

        private async Task LoadDetectableGamesAsync()
        {
            try
            {
                log("Loading Discord detectable games...");

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordGameSpoofer");

                string json = await client.GetStringAsync(Config.discordAPIUrl);

                List<DiscordDetectableApplication>? games =
                    JsonSerializer.Deserialize<List<DiscordDetectableApplication>>(json);

                gamesList.Items.Clear();
                allGames.Clear();
                selectedGame = null;

                if (games == null)
                {
                    log("No games were returned from the Discord API.");
                    RefreshGamesList();
                    return;
                }

                games.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

                foreach (DiscordDetectableApplication game in games)
                {
                    if (game.Executables != null && game.Executables.Count != 0)
                    {
                        allGames.Add(game);
                    }
                }

                log($"Loaded {allGames.Count} detectable games.");
                RefreshGamesList();
            }
            catch (Exception ex)
            {
                log($"Failed to load games: {ex.Message}");
                RefreshGamesList();
                MessageBox.Show("Failed to load games: " + ex.Message);
            }
        }

        private async Task SetupApplicationAsync()
        {
            try
            {
                log("Setting up application folders...");

                // Setup the bin path
                string binPath = Path.Combine(appPath, "bin");
                gamePath = Path.Combine(binPath, "games");
                customAppsPath = Path.Combine(binPath, "custom_apps.json");
                gameFoldersPath = Path.Combine(binPath, "game_folders.json");
                steamAppsCachePath = Path.Combine(binPath, "steam_apps.json");
                steamManifestsPath = Path.Combine(binPath, "steam_manifests.json");

                if (!Directory.Exists(binPath))
                {
                    Directory.CreateDirectory(binPath);
                    log($"Created bin folder: {binPath}");
                }

                if (!Directory.Exists(gamePath))
                {
                    Directory.CreateDirectory(gamePath);
                    log($"Created games folder: {gamePath}");
                }

                LoadGameFolders();
                LoadSteamAppCache();
                LoadSteamManifests();
                LoadSteamLibraries();

                blankWindowPath = Path.Combine(binPath, "blank.exe");

                if (!File.Exists(blankWindowPath))
                {
                    try
                    {
                        log("blank.exe not found. Downloading...");

                        // Last time I used C#, we used a WebClient but apparently it's deprecated
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordGameSpoofer"); // Got HTTP 403 without a header

                        await using (var input = await client.GetStreamAsync(Config.blankDownloadUrl))
                        await using (var output = File.Create(blankWindowPath))
                        {
                            await input.CopyToAsync(output);
                        }

                        using JsonDocument jsonDoc = JsonDocument.Parse(await client.GetStringAsync(Config.blankAPIUrl));
                        JsonElement asset = jsonDoc.RootElement.GetProperty("assets").EnumerateArray()
                            .First(a => a.GetProperty("name").ToString() == "blank_window.exe");
                        string sha256 = asset.GetProperty("digest").ToString().Replace("sha256:", "");

                        string downloadedBlankHash;
                        using (FileStream stream = File.OpenRead(blankWindowPath))
                        {
                            byte[] hashBytes = SHA256.HashData(stream);
                            downloadedBlankHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                        }

                        if (downloadedBlankHash != sha256)
                        {
                            log("SHA256 check on blank failed, deleting file.");

                            try
                            {
                                if (File.Exists(blankWindowPath))
                                    File.Delete(blankWindowPath);
                            }
                            catch (Exception ex)
                            {
                                log($"Failed to delete invalid blank.exe: {ex.Message}");
                            }

                            MessageBox.Show("blank.exe failed the SHA256 security check.\n\nThe downloaded file did not match the expected Github release hash, so it was deleted.\n\nPlease try again later. If this keeps happening, check the GitHub release or open an issue.");
                            return;
                        }

                        log("blank.exe downloaded and verified successfully.");
                    }
                    catch
                    {
                        // Do not leave a broken download behind.
                        try
                        {
                            if (File.Exists(blankWindowPath))
                                File.Delete(blankWindowPath);
                        }
                        catch
                        {
                        }

                        throw;
                    }
                }
                else
                {
                    log("blank.exe found.");
                }

                log("Application setup complete.");
            }
            catch (Exception ex)
            {
                log($"Failed during setup: {ex.Message}");
                MessageBox.Show("Failed during setup: " + ex.Message);
            }
        }

        private void LoadCustomApps()
        {
            try
            {
                if (string.IsNullOrEmpty(customAppsPath) || !File.Exists(customAppsPath))
                    return;

                string json = File.ReadAllText(customAppsPath);
                List<DiscordDetectableApplication>? apps =
                    JsonSerializer.Deserialize<List<DiscordDetectableApplication>>(json);

                if (apps == null)
                    return;

                foreach (DiscordDetectableApplication app in apps)
                {
                    if (app.Executables == null || app.Executables.Count == 0)
                        continue;

                    if (string.IsNullOrWhiteSpace(app.Id))
                        app.Id = "custom-" + Guid.NewGuid().ToString("N");

                    if (string.IsNullOrWhiteSpace(app.Name))
                        app.Name = "Custom application";

                    if (allGames.Any(g => g.Id == app.Id))
                        continue;

                    customGames.Add(app);
                    allGames.Add(app);
                }

                log($"Loaded {customGames.Count} custom application(s).");
            }
            catch (Exception ex)
            {
                log($"Failed to load custom apps: {ex.Message}");
            }
        }

        private void SaveCustomApps()
        {
            try
            {
                if (string.IsNullOrEmpty(customAppsPath))
                    return;

                string json = JsonSerializer.Serialize(customGames, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(customAppsPath, json);
            }
            catch (Exception ex)
            {
                log($"Failed to save custom apps: {ex.Message}");
            }
        }

        private void LoadGameFolders()
        {
            gameFolders.Clear();

            try
            {
                if (string.IsNullOrEmpty(gameFoldersPath) || !File.Exists(gameFoldersPath))
                    return;

                string json = File.ReadAllText(gameFoldersPath);
                Dictionary<string, string>? folders =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (folders == null)
                    return;

                foreach (KeyValuePair<string, string> pair in folders)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                        continue;

                    gameFolders[pair.Key] = pair.Value;
                }

                log($"Loaded {gameFolders.Count} custom install folder(s).");
            }
            catch (Exception ex)
            {
                log($"Failed to load install folders: {ex.Message}");
            }
        }

        private void SaveGameFolders()
        {
            try
            {
                if (string.IsNullOrEmpty(gameFoldersPath))
                    return;

                string json = JsonSerializer.Serialize(gameFolders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(gameFoldersPath, json);
            }
            catch (Exception ex)
            {
                log($"Failed to save install folders: {ex.Message}");
            }
        }

        private bool HasCustomFolder(DiscordDetectableApplication game)
        {
            return game != null
                && !string.IsNullOrWhiteSpace(game.Id)
                && gameFolders.ContainsKey(game.Id);
        }

        private string GetGameBaseFolder(DiscordDetectableApplication game)
        {
            if (game != null
                && !string.IsNullOrWhiteSpace(game.Id)
                && gameFolders.TryGetValue(game.Id, out string? folder)
                && !string.IsNullOrWhiteSpace(folder))
            {
                return folder;
            }

            return gamePath;
        }

        private void LoadSteamLibraries()
        {
            steamLibraries.Clear();

            string? root = FindSteamRoot();
            if (root == null)
            {
                log("Steam was not detected. Steam manifests are disabled.");
                return;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddSteamLibrary(root, seen);

            string libraryFoldersPath = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFoldersPath))
            {
                try
                {
                    string vdf = File.ReadAllText(libraryFoldersPath);
                    foreach (Match match in Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\""))
                    {
                        string path = match.Groups[1].Value.Replace("\\\\", "\\");
                        AddSteamLibrary(path, seen);
                    }
                }
                catch (Exception ex)
                {
                    log($"Failed to read Steam library folders: {ex.Message}");
                }
            }

            if (steamLibraries.Count == 0)
            {
                log("Steam was not detected. Steam manifests are disabled.");
                return;
            }

            foreach (SteamLibrary library in steamLibraries)
            {
                log($"Steam library found: {library.RootPath}");
            }
        }

        private static string? FindSteamRoot()
        {
            List<string> candidates = new List<string>();

            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key?.GetValue("SteamPath") is string steamPath && !string.IsNullOrWhiteSpace(steamPath))
                    candidates.Add(steamPath);
            }
            catch
            {
            }

            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                if (key?.GetValue("InstallPath") is string installPath && !string.IsNullOrWhiteSpace(installPath))
                    candidates.Add(installPath);
            }
            catch
            {
            }

            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
                if (key?.GetValue("InstallPath") is string installPath && !string.IsNullOrWhiteSpace(installPath))
                    candidates.Add(installPath);
            }
            catch
            {
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrWhiteSpace(programFilesX86))
                candidates.Add(Path.Combine(programFilesX86, "Steam"));

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
                candidates.Add(Path.Combine(programFiles, "Steam"));

            foreach (string candidate in candidates)
            {
                try
                {
                    string fullPath = Path.GetFullPath(candidate.Replace('/', '\\'));
                    if (Directory.Exists(Path.Combine(fullPath, "steamapps")))
                        return fullPath;
                }
                catch
                {
                }
            }

            return null;
        }

        private void AddSteamLibrary(string path, HashSet<string> seen)
        {
            try
            {
                string fullPath = Path.GetFullPath(path.Trim().TrimEnd('\\', '/'));
                if (!Directory.Exists(Path.Combine(fullPath, "steamapps")))
                    return;

                if (!seen.Add(fullPath))
                    return;

                steamLibraries.Add(new SteamLibrary { RootPath = fullPath });
            }
            catch
            {
            }
        }

        private static bool CanWriteToDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string probe = Path.Combine(directory, ".dgs_write_test_" + Guid.NewGuid().ToString("N"));

                using (File.Create(probe))
                {
                }

                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? GetSteamAppId(DiscordDetectableApplication game)
        {
            if (game?.ThirdPartySkus == null)
                return null;

            foreach (DiscordThirdPartySku sku in game.ThirdPartySkus)
            {
                if (string.Equals(sku.Distributor, "steam", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(sku.Id)
                    && sku.Id.Trim().All(char.IsDigit))
                {
                    return sku.Id.Trim();
                }
            }

            return null;
        }

        private SteamLibrary? FindSteamLibraryForApp(string appId, out string? existingManifestPath)
        {
            existingManifestPath = null;

            // Prefer a library that already has a manifest for this app. This avoids
            // moving an existing install to another drive.
            foreach (SteamLibrary library in steamLibraries)
            {
                string manifestPath = Path.Combine(library.SteamAppsPath, $"appmanifest_{appId}.acf");
                if (File.Exists(manifestPath))
                {
                    existingManifestPath = manifestPath;
                    return library;
                }
            }

            // Otherwise use the first library we can write to.
            foreach (SteamLibrary library in steamLibraries)
            {
                if (CanWriteToDirectory(library.SteamAppsPath))
                    return library;
            }

            return null;
        }

        private async Task<string> GetSteamInstallDirAsync(string appId, string gameName, string? existingManifestPath)
        {
            if (!string.IsNullOrEmpty(existingManifestPath) && File.Exists(existingManifestPath))
            {
                string? existing = ReadAcfValue(existingManifestPath, "installdir");
                if (IsSafeSteamInstallDir(existing))
                    return existing!.Trim();
            }

            if (steamInstallDirs.TryGetValue(appId, out string? cached) && IsSafeSteamInstallDir(cached))
                return cached!.Trim();

            try
            {
                using HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(6),
                };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordGameSpoofer");

                string json = await client.GetStringAsync($"https://api.steamcmd.net/v1/info/{appId}");
                using JsonDocument document = JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty("data", out JsonElement data)
                    && data.TryGetProperty(appId, out JsonElement app)
                    && app.TryGetProperty("config", out JsonElement config)
                    && config.TryGetProperty("installdir", out JsonElement installDirElement)
                    && installDirElement.ValueKind == JsonValueKind.String)
                {
                    string? installDir = installDirElement.GetString();
                    if (IsSafeSteamInstallDir(installDir))
                    {
                        string safeInstallDir = installDir!.Trim();
                        steamInstallDirs[appId] = safeInstallDir;
                        SaveSteamAppCache();
                        return safeInstallDir;
                    }
                }
            }
            catch (Exception ex)
            {
                log($"Could not fetch Steam install info for {gameName}: {ex.Message}");
            }

            string fallback = SanitizeSteamFolderName(gameName);
            steamInstallDirs[appId] = fallback;
            SaveSteamAppCache();
            return fallback;
        }

        private static string? ReadAcfValue(string path, string key)
        {
            try
            {
                string text = File.ReadAllText(path);
                Match match = Regex.Match(
                    text,
                    "\"" + Regex.Escape(key) + "\"\\s+\"([^\"]*)\"",
                    RegexOptions.IgnoreCase);

                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSafeSteamInstallDir(string? installDir)
        {
            if (string.IsNullOrWhiteSpace(installDir))
                return false;

            string value = installDir.Trim();

            if (value.Length == 0 || value == "." || value == "..")
                return false;

            if (value.Contains(".."))
                return false;

            if (value.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                return false;

            return value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static string SanitizeSteamFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Game";

            string invalidChars = new string(Path.GetInvalidFileNameChars());
            StringBuilder builder = new StringBuilder(name.Length);

            foreach (char c in name)
            {
                if (invalidChars.IndexOf(c) >= 0 || c == '/' || c == '\\')
                    builder.Append('_');
                else
                    builder.Append(c);
            }

            string result = builder.ToString().Trim().TrimEnd('.');
            return result.Length == 0 ? "Game" : result;
        }

        private static string EscapeVdf(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private string CreateSteamManifest(string steamAppsPath, string appId, string gameName, string installDir)
        {
            string manifestPath = Path.Combine(steamAppsPath, $"appmanifest_{appId}.acf");

            // Never overwrite a manifest. It may belong to a real Steam install.
            if (File.Exists(manifestPath))
                return manifestPath;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string content =
                "\"AppState\"\n" +
                "{\n" +
                $"\t\"appid\"\t\t\"{EscapeVdf(appId)}\"\n" +
                $"\t\"Universe\"\t\t\"1\"\n" +
                $"\t\"name\"\t\t\"{EscapeVdf(gameName)}\"\n" +
                $"\t\"StateFlags\"\t\t\"4\"\n" +
                $"\t\"installdir\"\t\t\"{EscapeVdf(installDir)}\"\n" +
                $"\t\"LastUpdated\"\t\t\"{now}\"\n" +
                $"\t\"LastPlayed\"\t\t\"{now}\"\n" +
                $"\t\"SizeOnDisk\"\t\t\"1000000\"\n" +
                $"\t\"buildid\"\t\t\"12345678\"\n" +
                $"\t\"UpdateResult\"\t\t\"0\"\n" +
                "}\n";

            File.WriteAllText(manifestPath, content, new UTF8Encoding(false));
            steamManifests[manifestPath] = ComputeFileHash(manifestPath);
            SaveSteamManifests();

            log($"Created Steam manifest: {manifestPath}");
            return manifestPath;
        }

        private static string ComputeFileHash(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private void LoadSteamAppCache()
        {
            steamInstallDirs.Clear();

            try
            {
                if (string.IsNullOrEmpty(steamAppsCachePath) || !File.Exists(steamAppsCachePath))
                    return;

                Dictionary<string, string>? cache =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(steamAppsCachePath));

                if (cache == null)
                    return;

                foreach (KeyValuePair<string, string> pair in cache)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                        steamInstallDirs[pair.Key] = pair.Value;
                }
            }
            catch (Exception ex)
            {
                log($"Failed to load Steam app cache: {ex.Message}");
            }
        }

        private void SaveSteamAppCache()
        {
            try
            {
                if (string.IsNullOrEmpty(steamAppsCachePath))
                    return;

                string json = JsonSerializer.Serialize(steamInstallDirs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(steamAppsCachePath, json);
            }
            catch (Exception ex)
            {
                log($"Failed to save Steam app cache: {ex.Message}");
            }
        }

        private void LoadSteamManifests()
        {
            steamManifests.Clear();

            try
            {
                if (string.IsNullOrEmpty(steamManifestsPath) || !File.Exists(steamManifestsPath))
                    return;

                Dictionary<string, string>? manifests =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(steamManifestsPath));

                if (manifests == null)
                    return;

                foreach (KeyValuePair<string, string> pair in manifests)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                        steamManifests[pair.Key] = pair.Value;
                }
            }
            catch (Exception ex)
            {
                log($"Failed to load Steam manifest list: {ex.Message}");
            }
        }

        private void SaveSteamManifests()
        {
            try
            {
                if (string.IsNullOrEmpty(steamManifestsPath))
                    return;

                string json = JsonSerializer.Serialize(steamManifests, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(steamManifestsPath, json);
            }
            catch (Exception ex)
            {
                log($"Failed to save Steam manifest list: {ex.Message}");
            }
        }

        private void CleanupSteamManifests()
        {
            if (steamManifests.Count == 0)
                return;

            foreach (KeyValuePair<string, string> pair in steamManifests.ToList())
            {
                try
                {
                    if (!File.Exists(pair.Key))
                    {
                        steamManifests.Remove(pair.Key);
                        continue;
                    }

                    if (string.Equals(ComputeFileHash(pair.Key), pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(pair.Key);
                        log($"Removed Steam manifest: {pair.Key}");
                    }
                    else
                    {
                        log($"Kept Steam manifest because it was changed: {pair.Key}");
                    }

                    steamManifests.Remove(pair.Key);
                }
                catch (Exception ex)
                {
                    log($"Failed to remove Steam manifest {pair.Key}: {ex.Message}");
                }
            }

            SaveSteamManifests();
        }

        private void RefreshGamesList()
        {
            DiscordDetectableApplication? previous = selectedGame;
            string search = searchBox.Text.Trim().ToLower();

            gamesList.BeginUpdate();
            gamesList.Items.Clear();

            foreach (DiscordDetectableApplication game in allGames)
            {
                string gameName = game.Name ?? "";
                if (search.Length == 0 || gameName.ToLower().Contains(search))
                {
                    gamesList.Items.Add(game);
                }
            }

            gamesList.EndUpdate();

            if (previous != null)
            {
                int index = gamesList.Items.IndexOf(previous);
                if (index >= 0)
                {
                    gamesList.SelectedIndex = index;
                }
            }
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            RefreshGamesList();
        }

        private void gamesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedGame = gamesList.SelectedItem as DiscordDetectableApplication;
            addToQueueButton.Enabled = selectedGame != null;
            setInstallFolderButton.Enabled = selectedGame != null;
            defaultInstallFolderButton.Enabled = selectedGame != null && HasCustomFolder(selectedGame);

            bool hasSteamSku = selectedGame != null && GetSteamAppId(selectedGame) != null;
            steamManifestCheckBox.Enabled = hasSteamSku && steamLibraries.Count > 0;

            if (selectedGame == null)
            {
                detailsBox.Clear();
                return;
            }

            log($"Selected game: {selectedGame.Name}");
            ShowGameDetails();
        }

        private void ShowGameDetails()
        {
            if (selectedGame == null)
            {
                detailsBox.Clear();
                return;
            }

            detailsBox.Clear();

            if (selectedGame.Id?.StartsWith("custom-", StringComparison.OrdinalIgnoreCase) == true)
            {
                detailsBox.AppendText("Custom application\n\n");
            }

            List<String> winGames = new List<String>();

            if (selectedGame.Executables == null || selectedGame.Executables.Count == 0)
            {
                detailsBox.Text = "No executable details found.";
                log($"No Windows executables found for {selectedGame.Name}.");
                return;
            }

            foreach (DiscordExecutable exe in selectedGame.Executables)
            {
                if (exe.Os == "win32" && !string.IsNullOrEmpty(exe.Name))
                {
                    winGames.Add(exe.Name);
                }
            }

            detailsBox.AppendText("Executable paths:\n");
            foreach (String exe in winGames)
            {
                detailsBox.AppendText($" - {exe}\n");
            }

            string baseFolder = GetGameBaseFolder(selectedGame);
            detailsBox.AppendText($"\nInstall folder: {baseFolder}\n");
            detailsBox.AppendText(HasCustomFolder(selectedGame)
                ? " (custom folder)\n"
                : " (default bin\\games folder)\n");

            string? steamAppId = GetSteamAppId(selectedGame);
            if (steamAppId != null)
            {
                detailsBox.AppendText($"\nSteam app ID: {steamAppId}\n");

                if (steamLibraries.Count == 0)
                {
                    detailsBox.AppendText("Steam was not detected, so a manifest cannot be created.\n");
                }
                else if (steamManifestCheckBox.Checked)
                {
                    detailsBox.AppendText("Steam mode: ON (exe is placed in steamapps\\common and an appmanifest is created).\n");
                }
                else
                {
                    detailsBox.AppendText("Steam mode: OFF (tick the box above to use the Steam library layout).\n");
                }
            }

            log($"Found {winGames.Count} Windows executable path(s) for {selectedGame.Name}.");
        }

        private void steamManifestCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            ShowGameDetails();
        }

        private void setInstallFolderButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;

            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = $"Select the folder where the spoofed executable for {selectedGame.Name} should be placed. "
                    + "Use the game's install folder if the quest checks the executable path.",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
            };

            string current = GetGameBaseFolder(selectedGame);
            if (Directory.Exists(current))
            {
                dialog.SelectedPath = current;
            }

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string folder = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(folder))
                return;

            try
            {
                folder = Path.GetFullPath(folder);
            }
            catch (Exception ex)
            {
                log($"Invalid install folder: {ex.Message}");
                MessageBox.Show("That folder path is not valid. View the log for more details.", "Install Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            gameFolders[selectedGame.Id] = folder;
            SaveGameFolders();

            log($"Install folder for {selectedGame.Name} set to: {folder}");
            gamesList_SelectedIndexChanged(sender, e);
        }

        private void defaultInstallFolderButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null || !HasCustomFolder(selectedGame))
                return;

            gameFolders.Remove(selectedGame.Id);
            SaveGameFolders();

            log($"Install folder for {selectedGame.Name} reset to the default bin\\games folder.");
            gamesList_SelectedIndexChanged(sender, e);
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
            {
                log("Start clicked, but no game is selected.");
                return;
            }

            await StartGameAsync(selectedGame, isQueueGame: false);
        }

        private async Task<bool> StartGameAsync(DiscordDetectableApplication game, bool isQueueGame)
        {
            if (startInProgress)
            {
                log("A game is already being prepared. Wait a moment and try again.");
                return false;
            }

            startInProgress = true;
            try
            {
                return await StartGameCoreAsync(game, isQueueGame);
            }
            finally
            {
                startInProgress = false;
            }
        }

        private async Task<bool> StartGameCoreAsync(DiscordDetectableApplication game, bool isQueueGame)
        {
            if (queueRunning && !isQueueGame)
            {
                log("A queue is running. Pause or clear the queue before starting another game.");
                return false;
            }

            if (game.Executables == null || game.Executables.Count == 0)
            {
                log($"Cannot start {game.Name}: no executable list found.");
                return false;
            }

            if (runningGames.Any(r => r.GameId == game.Id))
            {
                log($"{game.Name} is already running.");
                return false;
            }

            List<String> winGames = new List<String>();
            foreach (DiscordExecutable exe in game.Executables)
            {
                if (exe.Os == "win32" && !string.IsNullOrEmpty(exe.Name))
                {
                    winGames.Add(exe.Name);
                }
            }

            if (winGames.Count == 0)
            {
                log($"Cannot start {game.Name}: no Windows executable found.");
                return false;
            }

            // We've validated its a valid game
            string[] disallowedChars = new string[] { "<", ">", "\"", "\\", "|", "*", "?", "~", ":" };

            string? pathGame = null;
            foreach (String exe in winGames)
            {
                bool isInvalid = exe.Any(c => disallowedChars.Contains(c.ToString()));
                bool hasPathUp = exe.Contains("..");
                if (!isInvalid && !hasPathUp)
                {
                    pathGame = exe;
                    break;
                }
            }

            if (pathGame == null)
            {
                log($"No valid executable path found for {game.Name}.");
                DialogResult result = MessageBox.Show("No valid game paths were detected.\n\nPlease open a Github issue if this is unexpected.\nOpen Github issues page?", "No Valid Game Path", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Config.githubIssuesUrl,
                        UseShellExecute = true,
                    });
                }
                return false;
            }

            string baseFolder = GetGameBaseFolder(game);
            string? steamAppId = null;
            SteamLibrary? steamLibrary = null;
            string? steamInstallDir = null;
            bool useSteam = steamManifestCheckBox.Checked && steamLibraries.Count > 0;

            if (useSteam)
            {
                steamAppId = GetSteamAppId(game);

                if (steamAppId == null)
                {
                    log($"{game.Name} has no Steam app ID. Using the selected install folder instead.");
                    useSteam = false;
                }
            }

            if (useSteam && steamAppId != null)
            {
                SteamLibrary? library = FindSteamLibraryForApp(steamAppId, out string? existingManifestPath);

                if (library == null)
                {
                    log("No writable Steam library was found. Using the selected install folder instead.");
                    useSteam = false;
                }
                else
                {
                    string installDir = await GetSteamInstallDirAsync(steamAppId, game.Name, existingManifestPath);

                    steamLibrary = library;
                    steamInstallDir = installDir;
                    baseFolder = Path.Combine(library.CommonPath, installDir);

                    log($"Steam mode enabled: app {steamAppId}, install folder {baseFolder}");
                }
            }

            string[] splitPath = pathGame.Split("/");
            string full_dir = Path.Combine(baseFolder, string.Join("\\", splitPath.Take(splitPath.Length - 1).ToArray()));
            string exe_path = Path.Combine(full_dir, splitPath.Last());

            // Security check - this is an edge case. Normally, the return from Discord's API should be safe,
            // but this ensures the spoof executables can only be created inside the selected install folder.
            string fullGamePath = Path.GetFullPath(baseFolder);
            string fullExePath = Path.GetFullPath(exe_path);

            if (!fullGamePath.EndsWith(Path.DirectorySeparatorChar))
                fullGamePath += Path.DirectorySeparatorChar;

            if (!fullExePath.StartsWith(fullGamePath, StringComparison.OrdinalIgnoreCase))
            {
                log($"Blocked unsafe executable path. Game: {game.Name}, Path: {fullExePath}, Allowed root: {fullGamePath}");
                MessageBox.Show("Game launch aborted because last minute safety check failed. View log for more details.");
                return false;
            }

            string? safeGameDir = Path.GetDirectoryName(fullExePath);
            if (safeGameDir == null)
            {
                log($"Could not determine game's folder for {game.Name}.");
                MessageBox.Show("An unexpected error occurred. View logs for more details.");
                return false;
            }

            if (!IsSpoofHostAvailable())
            {
                log("The spoof host executable could not be located. Cannot create the spoof executable.");
                MessageBox.Show("The spoof host could not be found. Restart the app and try again.");
                return false;
            }

            try
            {
                if (!Directory.Exists(safeGameDir))
                {
                    Directory.CreateDirectory(safeGameDir);
                    log($"Created spoof folder: {safeGameDir}");
                }

                CreateSpoofExecutable(fullExePath, game.Name);

                if (useSteam && steamLibrary != null && steamAppId != null && steamInstallDir != null)
                {
                    try
                    {
                        CreateSteamManifest(steamLibrary.SteamAppsPath, steamAppId, game.Name, steamInstallDir);
                    }
                    catch (Exception ex)
                    {
                        log($"Failed to create the Steam manifest for {game.Name}: {ex.Message}");
                        log("The game will still start, but quests that check Steam installation may not progress.");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                log($"Windows denied access to the install folder for {game.Name}: {ex.Message}");
                MessageBox.Show(
                    "Windows denied access to the selected install folder.\n\n"
                    + "Choose a folder you can write to, or run Discord Game Spoofer as administrator.",
                    "Install Folder Permission",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            catch (IOException ex)
            {
                log($"Failed to prepare spoof executable for {game.Name}: {ex.Message}");
                MessageBox.Show(ex.Message, "Spoof Executable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch (Exception ex)
            {
                log($"Failed to prepare spoof executable for {game.Name}: {ex.Message}");
                MessageBox.Show("Failed to create the spoof executable. View logs for more details.");
                return false;
            }

            // The spoof host is a visible GUI app. Discord ignores hidden and console
            // processes, so the window must stay visible for the quest to count.
            ProcessStartInfo startInfo = new ProcessStartInfo(fullExePath)
            {
                UseShellExecute = false,
                WorkingDirectory = safeGameDir,
            };

            startInfo.ArgumentList.Add("--spoof-host");
            startInfo.ArgumentList.Add(game.Name);

            Process? proc;
            try
            {
                proc = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                log($"Failed to start spoof process for {game.Name}: {ex.Message}");
                return false;
            }

            if (proc == null)
            {
                log($"Failed to start spoof process for {game.Name}.");
                return false;
            }

            RunningGame runningGame = new RunningGame
            {
                Process = proc,
                Name = game.Name,
                GameId = game.Id,
                StartTime = DateTime.Now,
                IsQueueGame = isQueueGame,
            };

            runningGames.Add(runningGame);
            runningGamesList.Items.Add(runningGame);
            runningGamesTime.Items.Add("00:00:00");

            log(isQueueGame ? $"Started spoofing {game.Name} (queue)." : $"Started spoofing {game.Name}.");
            log("A visible spoof window was opened. Keep it open while the quest is active.");

            UpdateQueueButtons();
            return true;
        }

        private string? GetHostSource()
        {
            string? processPath = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(processPath)
                && File.Exists(processPath)
                && !string.Equals(Path.GetFileName(processPath), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return processPath;
            }

            // Fallback for `dotnet run`: use the apphost next to the managed dll.
            string candidate = Path.Combine(appPath, "DiscordGameSpoofer.exe");
            return File.Exists(candidate) ? candidate : null;
        }

        private bool IsSpoofHostAvailable()
        {
            return GetHostSource() != null;
        }

        private static bool IsSpoofHostFile(string path)
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                if (stream.Length < HostTrailer.Length)
                    return false;

                stream.Seek(-HostTrailer.Length, SeekOrigin.End);
                byte[] buffer = new byte[HostTrailer.Length];
                int read = stream.Read(buffer, 0, buffer.Length);

                return read == buffer.Length && buffer.SequenceEqual(HostTrailer);
            }
            catch
            {
                return false;
            }
        }

        private void CopyHostCompanion(string source, string targetDirectory)
        {
            if (!File.Exists(source))
                return;

            string target = Path.Combine(targetDirectory, Path.GetFileName(source));

            if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(target))
            {
                FileInfo sourceInfo = new FileInfo(source);
                FileInfo targetInfo = new FileInfo(target);

                if (sourceInfo.Length == targetInfo.Length
                    && sourceInfo.LastWriteTimeUtc == targetInfo.LastWriteTimeUtc)
                {
                    return;
                }
            }

            try
            {
                File.Copy(source, target, overwrite: true);
            }
            catch (IOException) when (File.Exists(target))
            {
                // A just-stopped spoof host may still be releasing the file. The
                // existing copy is from the same app version, so it is safe to use.
                log($"Could not update {target}; using the existing file.");
            }
            catch (UnauthorizedAccessException) when (File.Exists(target))
            {
                log($"Could not update {target}; using the existing file.");
            }
        }

        private void CreateSpoofExecutable(string destination, string gameName)
        {
            string? hostSource = GetHostSource();
            if (hostSource == null)
                throw new InvalidOperationException("The spoof host executable could not be located.");

            if (string.Equals(Path.GetFullPath(hostSource), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                throw new IOException("The spoof executable cannot overwrite the running application.");

            bool rewriteHost = true;

            // Try to regenerate the file on every start so every generated exe has a new random size.
            if (File.Exists(destination))
            {
                if (!IsSpoofHostFile(destination))
                {
                    throw new IOException(
                        $"A file already exists at:\n{destination}\n\n"
                        + "It was not created by Discord Game Spoofer, so it was not overwritten. "
                        + "Choose a different install folder or remove that file manually.");
                }

                try
                {
                    File.Delete(destination);
                }
                catch (IOException)
                {
                    log($"Spoof executable is in use, keeping the existing file: {destination}");
                    rewriteHost = false;
                }
                catch (UnauthorizedAccessException)
                {
                    log($"Spoof executable is locked, keeping the existing file: {destination}");
                    rewriteHost = false;
                }
            }

            string? destinationDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
                log($"Created spoof folder: {destinationDir}");
            }

            if (rewriteHost)
            {
                File.Copy(hostSource, destination, overwrite: true);

                int paddingSize = GetRandomPaddingSize(destination);
                byte[] padding = RandomNumberGenerator.GetBytes(paddingSize);

                // Extra bytes after the PE image are ignored by the Windows loader, but they
                // make every generated exe a different size. The trailer lets us recognise
                // our own files so real game executables are never overwritten.
                using (FileStream stream = new FileStream(destination, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    stream.Write(padding, 0, padding.Length);
                    stream.Write(HostTrailer, 0, HostTrailer.Length);
                }

                log($"Created spoof host for {gameName}: {destination} ({new FileInfo(destination).Length} bytes)");
            }

            // A framework-dependent apphost needs the managed assembly and config files
            // next to it. Single-file builds only need the exe itself.
            string? entryDll = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entryDll) && File.Exists(entryDll))
            {
                CopyHostCompanion(entryDll, destinationDir!);
                CopyHostCompanion(Path.ChangeExtension(entryDll, ".deps.json"), destinationDir!);
                CopyHostCompanion(Path.ChangeExtension(entryDll, ".runtimeconfig.json"), destinationDir!);
            }
        }

        private int GetRandomPaddingSize(string destination)
        {
            HashSet<long> existingSizes = new HashSet<long>();

            void AddSizes(string folder, SearchOption searchOption)
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(folder, "*.exe", searchOption))
                    {
                        if (string.Equals(file, destination, StringComparison.OrdinalIgnoreCase))
                            continue;

                        existingSizes.Add(new FileInfo(file).Length);
                    }
                }
                catch
                {
                    // Not critical - fall back to a plain random size.
                }
            }

            // The default games folder is small, so scan it fully. Custom install
            // folders can be large real game folders, so only check siblings.
            if (!string.IsNullOrWhiteSpace(gamePath) && Directory.Exists(gamePath))
            {
                AddSizes(gamePath, SearchOption.AllDirectories);
            }

            string? destinationDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDir)
                && Directory.Exists(destinationDir)
                && !string.Equals(Path.GetFullPath(destinationDir), Path.GetFullPath(gamePath), StringComparison.OrdinalIgnoreCase))
            {
                AddSizes(destinationDir, SearchOption.TopDirectoryOnly);
            }

            string? hostSource = GetHostSource();
            long baseSize = hostSource != null ? new FileInfo(hostSource).Length + HostTrailer.Length : 0;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int padding = RandomNumberGenerator.GetInt32(1024, (64 * 1024) + 1);
                if (!existingSizes.Contains(baseSize + padding))
                {
                    return padding;
                }
            }

            return RandomNumberGenerator.GetInt32(1024, (64 * 1024) + 1);
        }

        private void RemoveRunningGame(int index, bool kill)
        {
            if (index < 0 || index >= runningGames.Count)
                return;

            RunningGame game = runningGames[index];

            if (kill)
            {
                try
                {
                    if (!game.Process.HasExited)
                        game.Process.Kill();
                }
                catch (Exception ex)
                {
                    log($"Failed to stop {game.Name}: {ex.Message}");
                }
            }

            try
            {
                game.Process.Dispose();
            }
            catch
            {
            }

            runningGames.RemoveAt(index);
            runningGamesList.Items.RemoveAt(index);
            runningGamesTime.Items.RemoveAt(index);

            UpdateQueueButtons();
        }

        private void runningGamesList_SelectedIndexChanged(object sender, EventArgs e) => runningGamesTime.SelectedIndex = runningGamesList.SelectedIndex;

        private void runningGamesTime_SelectedIndexChanged(object sender, EventArgs e) => runningGamesList.SelectedIndex = runningGamesTime.SelectedIndex;

        private void stopButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = runningGamesList.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= runningGames.Count)
            {
                log("Stop clicked, but no running game is selected.");
                return;
            }

            RunningGame game = runningGames[selectedIndex];
            bool wasActiveQueueGame = ReferenceEquals(game, activeQueueGame);
            string gameName = game.Name;

            RemoveRunningGame(selectedIndex, kill: true);
            log($"Stopped spoofing {gameName}.");

            if (wasActiveQueueGame)
            {
                activeQueueGame = null;
                queueRunning = false;
                log("Queue paused. Press Start Queue to continue with the remaining games.");
                UpdateQueueButtons();
            }
        }

        private void addToQueueButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
            {
                log("Add to Queue clicked, but no game is selected.");
                return;
            }

            if (gameQueue.Any(g => g.Id == selectedGame.Id))
            {
                log($"{selectedGame.Name} is already in the queue.");
                return;
            }

            gameQueue.Add(selectedGame);
            RefreshQueueList(gameQueue.Count - 1);
            log($"Added {selectedGame.Name} to the queue.");
        }

        private void queueList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateQueueButtons();
        }

        private async void startQueueButton_Click(object sender, EventArgs e)
        {
            if (queueRunning)
            {
                log("The queue is already running.");
                return;
            }

            if (gameQueue.Count == 0)
            {
                log("The queue is empty. Add some games first.");
                return;
            }

            if (!IsSpoofHostAvailable())
            {
                log("Cannot start the queue because the spoof host could not be located.");
                return;
            }

            if (runningGames.Count > 0)
            {
                log("Stop the running game(s) before starting the queue.");
                return;
            }

            queueRunning = true;
            log("Starting queue.");
            UpdateQueueButtons();
            await StartNextQueuedGameAsync();
        }

        private async Task StartNextQueuedGameAsync()
        {
            if (!queueRunning || startInProgress)
                return;

            if (gameQueue.Count == 0)
            {
                queueRunning = false;
                activeQueueGame = null;
                log("Queue finished.");
                RefreshQueueList();
                return;
            }

            while (gameQueue.Count > 0)
            {
                DiscordDetectableApplication game = gameQueue[0];
                gameQueue.RemoveAt(0);
                RefreshQueueList(0);

                if (await StartGameAsync(game, isQueueGame: true))
                {
                    activeQueueGame = runningGames.LastOrDefault();
                    return;
                }

                log($"Skipping {game.Name} because it could not be started.");
            }

            queueRunning = false;
            activeQueueGame = null;
            log("Queue finished.");
            RefreshQueueList();
        }

        private void removeQueueButton_Click(object sender, EventArgs e)
        {
            int index = queueList.SelectedIndex;
            if (index < 0 || index >= gameQueue.Count)
                return;

            string gameName = gameQueue[index].Name;
            gameQueue.RemoveAt(index);
            RefreshQueueList(Math.Min(index, gameQueue.Count - 1));
            log($"Removed {gameName} from the queue.");
        }

        private void queueUpButton_Click(object sender, EventArgs e)
        {
            int index = queueList.SelectedIndex;
            if (index <= 0 || index >= gameQueue.Count)
                return;

            (gameQueue[index - 1], gameQueue[index]) = (gameQueue[index], gameQueue[index - 1]);
            RefreshQueueList(index - 1);
        }

        private void queueDownButton_Click(object sender, EventArgs e)
        {
            int index = queueList.SelectedIndex;
            if (index < 0 || index >= gameQueue.Count - 1)
                return;

            (gameQueue[index + 1], gameQueue[index]) = (gameQueue[index], gameQueue[index + 1]);
            RefreshQueueList(index + 1);
        }

        private void clearQueueButton_Click(object sender, EventArgs e)
        {
            int count = gameQueue.Count;
            gameQueue.Clear();

            // Stop auto-advancing. If a queued game is still running, it finishes normally.
            queueRunning = false;

            RefreshQueueList();
            log($"Cleared {count} game(s) from the queue.");
        }

        private void RefreshQueueList(int selectIndex = -1)
        {
            queueList.BeginUpdate();
            queueList.Items.Clear();

            for (int i = 0; i < gameQueue.Count; i++)
            {
                queueList.Items.Add($"{i + 1}. {gameQueue[i].Name}");
            }

            queueList.EndUpdate();

            if (selectIndex >= 0 && selectIndex < queueList.Items.Count)
            {
                queueList.SelectedIndex = selectIndex;
            }
            else
            {
                queueList.SelectedIndex = -1;
            }

            UpdateQueueButtons();
        }

        private void UpdateQueueButtons()
        {
            int index = queueList.SelectedIndex;
            bool hasSelection = index >= 0 && index < gameQueue.Count;

            bool canStart = IsSpoofHostAvailable();

            startButton.Enabled = canStart && !queueRunning;
            removeQueueButton.Enabled = hasSelection;
            queueUpButton.Enabled = hasSelection && index > 0;
            queueDownButton.Enabled = hasSelection && index < gameQueue.Count - 1;
            clearQueueButton.Enabled = gameQueue.Count > 0;
            startQueueButton.Enabled = gameQueue.Count > 0
                && !queueRunning
                && runningGames.Count == 0
                && canStart;
        }

        private void customAppButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(gamePath))
            {
                log("Cannot add a custom app before setup is complete.");
                return;
            }

            using CustomApplicationForm dialog = new CustomApplicationForm(gamePath);

            DialogResult dialogResult = dialog.ShowDialog(this);
            DiscordDetectableApplication? result = dialog.Result;

            if (dialogResult != DialogResult.OK || result == null)
                return;

            DiscordDetectableApplication app = result;

            if (customGames.Any(g => g.Id == app.Id))
                return;

            customGames.Add(app);
            allGames.Add(app);
            SaveCustomApps();

            searchBox.Clear();
            RefreshGamesList();

            int index = gamesList.Items.IndexOf(app);
            if (index >= 0)
            {
                gamesList.SelectedIndex = index;
            }

            log($"Added custom application: {app.Name} ({app.Executables[0].Name}).");
        }

        private void toggleLogButton_Click(object sender, EventArgs e)
        {
            if (!logShown)
            {
                logBox.Show();
                logShown = true;
            }
            else
            {
                logBox.Hide();
                logShown = false;
            }
        }

        private void navGamesButton_Click(object? sender, EventArgs e)
        {
            ShowTab(showVideo: false);
        }

        private void navVideoButton_Click(object? sender, EventArgs e)
        {
            ShowTab(showVideo: true);
        }

        private void ShowTab(bool showVideo)
        {
            if (gamesHost.Visible == !showVideo && videoHost.Visible == showVideo)
                return;

            gamesHost.Visible = !showVideo;
            videoHost.Visible = showVideo;

            navGamesButton.BackColor = showVideo ? Color.FromArgb(24, 32, 50) : Color.FromArgb(14, 165, 233);
            navVideoButton.BackColor = showVideo ? Color.FromArgb(14, 165, 233) : Color.FromArgb(24, 32, 50);

            if (showVideo)
                log("Opened the Video Quests tab.");
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            gameTimer.Stop();
            videoQuestsPanel.Shutdown();

            foreach (RunningGame game in runningGames)
            {
                try
                {
                    if (!game.Process.HasExited)
                        game.Process.Kill();
                }
                catch
                {
                }

                try
                {
                    game.Process.Dispose();
                }
                catch
                {
                }
            }

            runningGames.Clear();

            CleanupSteamManifests();
        }
    }
}
