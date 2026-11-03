using System.Runtime.InteropServices;
using DiscordGameSpoofer.Quests;

namespace DiscordGameSpoofer
{
    /// <summary>
    /// Dark themed tab that watches Discord video quests and advances their progress
    /// while Discord itself is closed.
    /// </summary>
    public sealed class VideoQuestsPanel : UserControl
    {
        private static readonly Color BackgroundColor = Color.FromArgb(10, 14, 23);
        private static readonly Color SurfaceColor = Color.FromArgb(17, 24, 39);
        private static readonly Color InputColor = Color.FromArgb(24, 32, 50);
        private static readonly Color TextColor = Color.FromArgb(226, 232, 240);
        private static readonly Color MutedColor = Color.FromArgb(148, 163, 184);
        private static readonly Color AccentColor = Color.FromArgb(56, 189, 248);
        private static readonly Color PrimaryColor = Color.FromArgb(14, 165, 233);
        private static readonly Color SecondaryColor = Color.FromArgb(71, 85, 105);
        private static readonly Color DangerColor = Color.FromArgb(244, 63, 94);
        private static readonly Color SuccessColor = Color.FromArgb(34, 197, 94);
        private static readonly Color WarningColor = Color.FromArgb(251, 191, 36);
        private static readonly Color HeaderColor = Color.FromArgb(24, 32, 50);
        private static readonly Color AlternateRowColor = Color.FromArgb(14, 20, 32);
        private static readonly Color SelectionColor = Color.FromArgb(30, 58, 95);
        private static readonly Color GridLineColor = Color.FromArgb(38, 49, 68);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string? subIdList);

        private readonly VideoQuestManager _manager;
        private readonly string _tokenPath;

        private readonly TextBox _tokenBox = new TextBox();
        private readonly Button _detectButton = new Button();
        private readonly Button _saveButton = new Button();
        private readonly Button _forgetButton = new Button();
        private readonly Button _startButton = new Button();
        private readonly Button _stopButton = new Button();
        private readonly DarkCheckBox _rememberCheckBox = new DarkCheckBox();
        private readonly DarkCheckBox _autoAcceptCheckBox = new DarkCheckBox();
        private readonly DarkCheckBox _autoClaimCheckBox = new DarkCheckBox();
        private readonly DarkCheckBox _scienceCheckBox = new DarkCheckBox();
        private readonly DarkCheckBox _showTokenCheckBox = new DarkCheckBox();
        private readonly Label _statusLabel = new Label();
        private readonly Label _titleLabel = new Label();
        private readonly Label _subtitleLabel = new Label();
        private readonly Label _tokenLabel = new Label();
        private readonly Label _warningLabel = new Label();
        private readonly Label _questsLabel = new Label();
        private readonly Label _logLabel = new Label();
        private readonly QuestListView _questList = new QuestListView();
        private readonly ImageList _questRowImages = new ImageList();
        private readonly Font _questHeaderFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        private readonly RichTextBox _logBox = new RichTextBox();

        private float _uiScale = 1f;

        private bool _shutdown;
        private bool _loadedSavedToken;
        private bool _updatingTokenBox;
        private bool _applyingLayout;

        private Action<string>? ExternalLog { get; set; }

        public void SetExternalLog(Action<string>? logger)
        {
            ExternalLog = logger;
        }

        public VideoQuestsPanel()
        {
            _tokenPath = Path.Combine(AppContext.BaseDirectory, "bin", Config.tokenFileName);
            _manager = new VideoQuestManager(AppendLog);

            BuildUi();

            _manager.QuestsUpdated += OnQuestsUpdated;
            _manager.StatusChanged += status => RunOnUi(() => _statusLabel.Text = status);
            _manager.RunningChanged += running => RunOnUi(() => ApplyRunningState(running));

            ApplyRunningState(false);
        }

        private void BuildUi()
        {
            SuspendLayout();

            BackColor = BackgroundColor;
            ForeColor = TextColor;
            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9F);
            Name = "videoQuestsPanel";

            _titleLabel.AutoSize = true;
            _titleLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            _titleLabel.ForeColor = AccentColor;
            _titleLabel.Location = new Point(22, 16);
            _titleLabel.Text = "Video Quests";

            _subtitleLabel.AutoSize = true;
            _subtitleLabel.ForeColor = MutedColor;
            _subtitleLabel.Location = new Point(22, 56);
            _subtitleLabel.Text = "Start a Discord video quest and this tab completes it, even when Discord is closed.";

            _tokenLabel.AutoSize = true;
            _tokenLabel.ForeColor = TextColor;
            _tokenLabel.Location = new Point(22, 100);
            _tokenLabel.Text = "Discord token";

            _tokenBox.BackColor = InputColor;
            _tokenBox.BorderStyle = BorderStyle.FixedSingle;
            _tokenBox.ForeColor = TextColor;
            _tokenBox.Location = new Point(22, 126);
            _tokenBox.Name = "tokenBox";
            _tokenBox.PlaceholderText = "Paste a user token, or click Auto-detect";
            _tokenBox.Size = new Size(700, 36);
            _tokenBox.TabIndex = 0;
            _tokenBox.UseSystemPasswordChar = true;
            _tokenBox.TextChanged += (_, _) =>
            {
                if (_updatingTokenBox)
                    return;

                if (_manager.IsRunning)
                    return;

                SetButtonEnabled(_startButton, true);
            };

            StyleButton(_detectButton, "Auto-detect", SecondaryColor, new Point(736, 124), new Size(150, 40));
            _detectButton.Click += DetectButton_Click;

            StyleButton(_saveButton, "Save", SecondaryColor, new Point(896, 124), new Size(100, 40));
            _saveButton.Click += SaveButton_Click;

            StyleButton(_forgetButton, "Forget", SecondaryColor, new Point(1006, 124), new Size(100, 40));
            _forgetButton.Click += ForgetButton_Click;

            StyleButton(_startButton, "Start", PrimaryColor, new Point(1120, 124), new Size(140, 40));
            SetButtonEnabled(_startButton, false);
            _startButton.Click += StartButton_Click;

            StyleButton(_stopButton, "Stop", DangerColor, new Point(1272, 124), new Size(140, 40));
            _stopButton.Click += StopButton_Click;

            StyleCheckBox(_rememberCheckBox, "Remember token", new Point(22, 176), new Size(220, 30));
            _rememberCheckBox.Checked = true;

            StyleCheckBox(_autoAcceptCheckBox, "Auto-accept video quests", new Point(250, 176), new Size(260, 30));

            StyleCheckBox(_autoClaimCheckBox, "Auto-claim rewards", new Point(520, 176), new Size(220, 30));

            StyleCheckBox(_scienceCheckBox, "Send science telemetry", new Point(750, 176), new Size(300, 30));

            StyleCheckBox(_showTokenCheckBox, "Show token", new Point(1080, 176), new Size(160, 30));
            _showTokenCheckBox.CheckedChanged += (_, _) =>
            {
                _tokenBox.UseSystemPasswordChar = !_showTokenCheckBox.Checked;
            };

            _statusLabel.AutoSize = true;
            _statusLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            _statusLabel.ForeColor = AccentColor;
            _statusLabel.Location = new Point(22, 216);
            _statusLabel.MaximumSize = new Size(1440, 0);
            _statusLabel.Text = "Stopped.";

            _warningLabel.AutoSize = false;
            _warningLabel.ForeColor = WarningColor;
            _warningLabel.Location = new Point(22, 252);
            _warningLabel.Size = new Size(1440, 42);
            _warningLabel.Text = "Warning: Discord may suspend accounts for automated quest progress. "
                + "Only use an account you are willing to lose. Science telemetry is optional and best-effort.";

            _questsLabel.AutoSize = true;
            _questsLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            _questsLabel.ForeColor = AccentColor;
            _questsLabel.Location = new Point(22, 306);
            _questsLabel.Text = "Video quests";

            _questList.BackColor = SurfaceColor;
            _questList.BorderStyle = BorderStyle.None;
            _questList.ForeColor = TextColor;
            _questList.FullRowSelect = true;
            _questList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _questList.HideSelection = false;
            _questList.Location = new Point(22, 338);
            _questList.MultiSelect = false;
            _questList.Name = "questList";
            _questList.OwnerDraw = true;
            _questList.SetEmptyAreaColor(SurfaceColor);
            _questList.Size = new Size(1440, 300);
            _questList.TabIndex = 14;
            _questList.UseCompatibleStateImageBehavior = false;
            _questList.View = View.Details;
            _questList.DrawColumnHeader += QuestList_DrawColumnHeader;
            _questList.DrawItem += QuestList_DrawItem;
            _questList.DrawSubItem += QuestList_DrawSubItem;
            _questList.ClientSizeChanged += QuestList_ClientSizeChanged;

            // A tiny transparent image makes the detail rows taller and easier
            // to scan. ListView has no RowHeight property in Details view.
            _questRowImages.ColorDepth = ColorDepth.Depth32Bit;
            _questRowImages.ImageSize = new Size(1, 26);
            _questRowImages.Images.Add(new Bitmap(1, 26));
            _questList.SmallImageList = _questRowImages;
            _questList.Columns.Add("Quest", 380);
            _questList.Columns.Add("Application", 300);
            _questList.Columns.Add("Progress", 220);
            _questList.Columns.Add("Status", 230);
            _questList.Columns.Add("Expires", 300);
            FillQuestColumns();

            _logLabel.AutoSize = true;
            _logLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            _logLabel.ForeColor = AccentColor;
            _logLabel.Location = new Point(22, 652);
            _logLabel.Text = "Video quest log";

            _logBox.BackColor = SurfaceColor;
            _logBox.BorderStyle = BorderStyle.None;
            _logBox.Font = new Font("Consolas", 9F);
            _logBox.ForeColor = TextColor;
            _logBox.Location = new Point(22, 682);
            _logBox.Name = "videoLogBox";
            _logBox.ReadOnly = true;
            _logBox.Size = new Size(1440, 250);
            _logBox.TabIndex = 15;
            _logBox.Text = "";

            Controls.Add(_titleLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_tokenLabel);
            Controls.Add(_tokenBox);
            Controls.Add(_detectButton);
            Controls.Add(_saveButton);
            Controls.Add(_forgetButton);
            Controls.Add(_startButton);
            Controls.Add(_stopButton);
            Controls.Add(_rememberCheckBox);
            Controls.Add(_autoAcceptCheckBox);
            Controls.Add(_autoClaimCheckBox);
            Controls.Add(_scienceCheckBox);
            Controls.Add(_showTokenCheckBox);
            Controls.Add(_statusLabel);
            Controls.Add(_warningLabel);
            Controls.Add(_questsLabel);
            Controls.Add(_questList);
            Controls.Add(_logLabel);
            Controls.Add(_logBox);

            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ApplyDarkWindowTheme(_questList);
            ApplyDarkWindowTheme(_logBox);
            FillQuestColumns();

            if (_loadedSavedToken)
                return;

            _loadedSavedToken = true;
            string? saved = TokenStore.Load(_tokenPath);

            if (string.IsNullOrWhiteSpace(saved))
                return;

            _updatingTokenBox = true;
            _tokenBox.Text = saved;
            _updatingTokenBox = false;
            SetButtonEnabled(_startButton, true);

            AppendLog("Loaded the saved Discord token.");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (IsHandleCreated && ClientSize.Width > 0 && !_applyingLayout)
                ApplyResponsiveLayout(_uiScale);
        }

        /// <summary>
        /// Places every control from the panel's current size. The panel is docked,
        /// so this runs when the main window is resized or maximized.
        /// </summary>
        public void ApplyResponsiveLayout(float uiScale)
        {
            _uiScale = Math.Max(0.5f, uiScale);

            int ScaleValue(float value) => Math.Max(1, (int)Math.Round(value * _uiScale));

            int width = ClientSize.Width;
            int height = ClientSize.Height;

            if (_applyingLayout || width <= 0 || height <= 0)
                return;

            _applyingLayout = true;
            SuspendLayout();
            bool autoScrollChanged = false;

            try
            {
                int pad = ScaleValue(22);
                int gap = ScaleValue(10);
                int right = width - pad;

                _titleLabel.Location = new Point(pad, ScaleValue(14));
                _subtitleLabel.Location = new Point(pad, ScaleValue(48));
                _tokenLabel.Location = new Point(pad, ScaleValue(84));

                // Token row: the token box takes the remaining width, the buttons stay fixed.
                int rowTop = ScaleValue(110);
                int rowHeight = ScaleValue(40);
                int detectWidth = ScaleValue(150);
                int saveWidth = ScaleValue(100);
                int forgetWidth = ScaleValue(100);
                int startWidth = ScaleValue(140);
                int stopWidth = ScaleValue(140);
                int checkTop;

                int tokenButtonsWidth = detectWidth + saveWidth + forgetWidth + startWidth + stopWidth + gap * 4;
                bool wrapTokenButtons =
                    tokenButtonsWidth + ScaleValue(200) + gap > width - pad * 2;

                if (wrapTokenButtons)
                {
                    // Not enough width for the token box and all buttons: give the
                    // token box a full row and put the buttons on the row below.
                    _tokenBox.SetBounds(pad, rowTop + ScaleValue(2), width - pad * 2, ScaleValue(36));

                    int buttonsTop = rowTop + rowHeight + gap;
                    _stopButton.SetBounds(right - stopWidth, buttonsTop, stopWidth, rowHeight);
                    _startButton.SetBounds(
                        _stopButton.Left - gap - startWidth,
                        buttonsTop,
                        startWidth,
                        rowHeight);
                    _forgetButton.SetBounds(
                        _startButton.Left - gap - forgetWidth,
                        buttonsTop,
                        forgetWidth,
                        rowHeight);
                    _saveButton.SetBounds(
                        _forgetButton.Left - gap - saveWidth,
                        buttonsTop,
                        saveWidth,
                        rowHeight);
                    _detectButton.SetBounds(
                        _saveButton.Left - gap - detectWidth,
                        buttonsTop,
                        detectWidth,
                        rowHeight);

                    checkTop = buttonsTop + rowHeight + ScaleValue(12);
                }
                else
                {
                    _stopButton.SetBounds(right - stopWidth, rowTop, stopWidth, rowHeight);
                    _startButton.SetBounds(
                        _stopButton.Left - gap - startWidth,
                        rowTop,
                        startWidth,
                        rowHeight);
                    _forgetButton.SetBounds(
                        _startButton.Left - gap - forgetWidth,
                        rowTop,
                        forgetWidth,
                        rowHeight);
                    _saveButton.SetBounds(
                        _forgetButton.Left - gap - saveWidth,
                        rowTop,
                        saveWidth,
                        rowHeight);
                    _detectButton.SetBounds(
                        _saveButton.Left - gap - detectWidth,
                        rowTop,
                        detectWidth,
                        rowHeight);

                    int tokenWidth = Math.Max(ScaleValue(140), _detectButton.Left - gap - pad);
                    _tokenBox.SetBounds(pad, rowTop + ScaleValue(2), tokenWidth, ScaleValue(36));
                    checkTop = rowTop + rowHeight + ScaleValue(12);
                }

                // Options wrap onto a second row on narrow windows.
                int checkHeight = ScaleValue(28);
                int checkGap = ScaleValue(10);
                int rememberWidth = ScaleValue(205);
                int acceptWidth = ScaleValue(245);
                int claimWidth = ScaleValue(200);
                int scienceWidth = ScaleValue(215);
                int showTokenWidth = ScaleValue(140);

                bool singleCheckRow =
                    rememberWidth + acceptWidth + claimWidth + scienceWidth + showTokenWidth + checkGap * 4
                    <= width - pad * 2;

                int checkX = pad;
                _rememberCheckBox.SetBounds(checkX, checkTop, rememberWidth, checkHeight);
                checkX += rememberWidth + checkGap;
                _autoAcceptCheckBox.SetBounds(checkX, checkTop, acceptWidth, checkHeight);
                checkX += acceptWidth + checkGap;
                _autoClaimCheckBox.SetBounds(checkX, checkTop, claimWidth, checkHeight);

                if (singleCheckRow)
                {
                    checkX += claimWidth + checkGap;
                    _scienceCheckBox.SetBounds(checkX, checkTop, scienceWidth, checkHeight);
                    checkX += scienceWidth + checkGap;
                    _showTokenCheckBox.SetBounds(checkX, checkTop, showTokenWidth, checkHeight);
                }
                else
                {
                    int secondRowTop = checkTop + checkHeight + ScaleValue(4);
                    _scienceCheckBox.SetBounds(pad, secondRowTop, scienceWidth, checkHeight);
                    _showTokenCheckBox.SetBounds(
                        pad + scienceWidth + checkGap,
                        secondRowTop,
                        showTokenWidth,
                        checkHeight);
                }

                int checkRowsHeight = singleCheckRow
                    ? checkHeight
                    : checkHeight * 2 + ScaleValue(4);

                int statusTop = checkTop + checkRowsHeight + ScaleValue(10);
                _statusLabel.Location = new Point(pad, statusTop);
                _statusLabel.MaximumSize = new Size(Math.Max(ScaleValue(200), width - pad * 2), 0);

                int warningTop = statusTop + ScaleValue(34);
                int warningHeight = ScaleValue(38);
                _warningLabel.SetBounds(pad, warningTop, width - pad * 2, warningHeight);

                int questsLabelTop = warningTop + warningHeight + ScaleValue(8);
                _questsLabel.Location = new Point(pad, questsLabelTop);

                int listTop = questsLabelTop + ScaleValue(32);

                // If the window is too short for the list and log, allow the tab to
                // scroll instead of letting the two sections overlap.
                int minimumContentHeight = listTop
                    + ScaleValue(80)
                    + ScaleValue(10)
                    + ScaleValue(26)
                    + ScaleValue(70)
                    + ScaleValue(8);

                bool needsScroll = minimumContentHeight > height;
                autoScrollChanged = AutoScroll != needsScroll;
                AutoScrollMinSize = needsScroll ? new Size(0, minimumContentHeight) : Size.Empty;
                AutoScroll = needsScroll;

                int layoutHeight = Math.Max(height, minimumContentHeight);
                int bottom = layoutHeight - ScaleValue(8);
                int available = Math.Max(ScaleValue(150), bottom - listTop);

                int logHeight = Math.Max(ScaleValue(70), (int)Math.Round(available * 0.36));
                int logTop = bottom - logHeight;
                int logLabelTop = logTop - ScaleValue(26);
                int listHeight = Math.Max(ScaleValue(80), logLabelTop - ScaleValue(10) - listTop);

                int contentWidth = Math.Max(ScaleValue(200), width - pad * 2);
                _questList.SetBounds(pad, listTop, contentWidth, listHeight);
                _logLabel.Location = new Point(pad, logLabelTop);
                _logBox.SetBounds(pad, logTop, contentWidth, logHeight);

                // Scale the quest columns with the window and let the last column fill.
                int[] baseWidths = { 380, 300, 220, 230, 300 };
                int baseTotal = 0;
                foreach (int value in baseWidths)
                    baseTotal += value;

                float fit = Math.Min(1f, (float)contentWidth / (baseTotal * _uiScale));

                for (int i = 0; i < _questList.Columns.Count && i < baseWidths.Length; i++)
                {
                    _questList.Columns[i].Width = Math.Max(
                        ScaleValue(50),
                        (int)Math.Round(baseWidths[i] * _uiScale * fit));
                }

                FillQuestColumns();
            }
            finally
            {
                ResumeLayout(true);
                _applyingLayout = false;
            }

            // Showing or hiding a scrollbar changes the usable width. Lay out once
            // more with the final width so nothing sits under the scrollbar.
            if (autoScrollChanged && IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() => ApplyResponsiveLayout(_uiScale)));
        }

        private static void StyleButton(Button button, string text, Color backColor, Point location, Size size)
        {
            button.BackColor = backColor;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.12f);
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.16f);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.ForeColor = Color.White;
            button.Location = location;
            button.Size = size;
            button.Tag = backColor;
            button.Text = text;
            button.UseVisualStyleBackColor = false;
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            button.Enabled = enabled;

            if (button.Tag is not Color baseColor)
                return;

            button.BackColor = enabled ? baseColor : SecondaryColor;
            button.ForeColor = enabled ? Color.White : MutedColor;
        }

        private static void StyleCheckBox(DarkCheckBox checkBox, string text, Point location, Size size)
        {
            checkBox.BackColor = BackgroundColor;
            checkBox.ForeColor = TextColor;
            checkBox.Location = location;
            checkBox.Size = size;
            checkBox.Text = text;
        }

        private void ApplyRunningState(bool running)
        {
            _tokenBox.Enabled = !running;
            SetButtonEnabled(_detectButton, !running);
            SetButtonEnabled(_saveButton, !running);
            SetButtonEnabled(_forgetButton, !running);
            _rememberCheckBox.Enabled = !running;
            _autoAcceptCheckBox.Enabled = !running;
            _autoClaimCheckBox.Enabled = !running;
            _scienceCheckBox.Enabled = !running;
            _showTokenCheckBox.Enabled = !running;
            SetButtonEnabled(_stopButton, running);
            SetButtonEnabled(_startButton, !running);
        }

        private async void DetectButton_Click(object? sender, EventArgs e)
        {
            if (_manager.IsRunning)
                return;

            SetButtonEnabled(_detectButton, false);
            _detectButton.Text = "Searching...";

            try
            {
                await DetectTokenAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"Token detection failed: {ex.Message}");
            }
            finally
            {
                _detectButton.Text = "Auto-detect";
                ApplyRunningState(_manager.IsRunning);
            }
        }

        private async Task<(string Token, DiscordUser User)?> DetectTokenAsync()
        {
            List<DiscordTokenCandidate> candidates = await Task.Run(
                () => DiscordTokenReader.ReadTokens(AppendLog));

            if (candidates.Count == 0)
            {
                AppendLog("No token was found. Make sure Discord is installed and was opened at least once.");
                return null;
            }

            AppendLog($"Found {candidates.Count} candidate token(s). Checking them with Discord...");

            foreach (DiscordTokenCandidate candidate in candidates)
            {
                try
                {
                    DiscordUser user = await _manager.ValidateTokenAsync(candidate.Token);

                    _updatingTokenBox = true;
                    _tokenBox.Text = candidate.Token;
                    _updatingTokenBox = false;
                    SetButtonEnabled(_startButton, true);

                    if (_rememberCheckBox.Checked && TokenStore.Save(_tokenPath, candidate.Token))
                        AppendLog("Saved the token for this Windows user.");

                    AppendLog($"Using the token for {user.DisplayName} from {candidate.Source}.");
                    return (candidate.Token, user);
                }
                catch (QuestAuthException)
                {
                    AppendLog($"Discord rejected a token from {candidate.Source}. Trying the next one.");
                }
                catch (Exception ex)
                {
                    AppendLog($"Could not check a token from {candidate.Source}: {ex.Message}");
                }
            }

            AppendLog("None of the local tokens are valid. Open Discord, then try Auto-detect again.");
            return null;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            string token = _tokenBox.Text.Trim();
            if (!DiscordTokenReader.IsLikelyToken(token))
            {
                AppendLog("That does not look like a Discord token.");
                return;
            }

            if (TokenStore.Save(_tokenPath, token))
                AppendLog("Token saved. It is encrypted with Windows DPAPI for this user.");
            else
                AppendLog("Could not save the token.");
        }

        private void ForgetButton_Click(object? sender, EventArgs e)
        {
            TokenStore.Delete(_tokenPath);
            AppendLog("Removed the saved token.");
        }

        private async void StartButton_Click(object? sender, EventArgs e)
        {
            if (_manager.IsRunning)
                return;

            SetButtonEnabled(_startButton, false);
            SetButtonEnabled(_detectButton, false);
            _statusLabel.Text = "Checking the token...";

            try
            {
                string token = _tokenBox.Text.Trim();
                DiscordUser? user = null;

                if (!DiscordTokenReader.IsLikelyToken(token))
                {
                    AppendLog("No usable token in the box. Searching Discord's local storage...");
                    (string Token, DiscordUser User)? detected = await DetectTokenAsync();

                    if (detected == null)
                    {
                        _statusLabel.Text = "No Discord token was found.";
                        return;
                    }

                    token = detected.Value.Token;
                    user = detected.Value.User;
                }

                user ??= await _manager.ValidateTokenAsync(token);

                if (_rememberCheckBox.Checked)
                {
                    if (TokenStore.Save(_tokenPath, token))
                        AppendLog("Saved the token for this Windows user.");
                }
                else
                {
                    TokenStore.Delete(_tokenPath);
                }

                _manager.AutoAccept = _autoAcceptCheckBox.Checked;
                _manager.AutoClaim = _autoClaimCheckBox.Checked;
                _manager.SendScience = _scienceCheckBox.Checked;

                _manager.Start(token, user);
            }
            catch (QuestAuthException)
            {
                AppendLog("Discord rejected that token. Copy a fresh token and try again.");
                MessageBox.Show(
                    this,
                    "Discord rejected that token. It may have expired or been logged out.",
                    "Video Quests",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppendLog($"Could not start video quests: {ex.Message}");
                MessageBox.Show(
                    this,
                    "Could not connect to Discord.\n\n" + ex.Message,
                    "Video Quests",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                ApplyRunningState(_manager.IsRunning);
            }
        }

        private async void StopButton_Click(object? sender, EventArgs e)
        {
            SetButtonEnabled(_stopButton, false);
            _statusLabel.Text = "Stopping...";
            await _manager.StopAsync();
        }

        private void OnQuestsUpdated(IReadOnlyList<DiscordQuest> quests)
        {
            RunOnUi(() => RefreshQuests(quests));
        }

        private void RefreshQuests(IReadOnlyList<DiscordQuest> quests)
        {
            if (IsDisposed)
                return;

            List<DiscordQuest> videoQuests = quests
                .Where(q => q.IsVideoQuest)
                .OrderByDescending(q => q.IsActive)
                .ThenBy(q => q.Claimed)
                .ThenBy(q => q.Completed)
                .ThenBy(q => q.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _questList.BeginUpdate();
            _questList.Items.Clear();

            foreach (DiscordQuest quest in videoQuests)
            {
                ListViewItem item = new ListViewItem(quest.Name);
                item.SubItems.Add(string.IsNullOrWhiteSpace(quest.ApplicationName) ? "-" : quest.ApplicationName);
                item.SubItems.Add(FormatProgress(quest));
                item.SubItems.Add(DescribeStatus(quest));
                item.SubItems.Add(quest.ExpiresAt.HasValue
                    ? quest.ExpiresAt.Value.ToLocalTime().ToString("g")
                    : "-");

                item.ForeColor = quest.Claimed || quest.Completed
                    ? SuccessColor
                    : quest.Expired
                        ? MutedColor
                        : quest.IsActive
                            ? AccentColor
                            : TextColor;

                item.Tag = quest.Id;
                _questList.Items.Add(item);
            }

            _questList.EndUpdate();
            FillQuestColumns();
        }

        private void QuestList_ClientSizeChanged(object? sender, EventArgs e)
        {
            FillQuestColumns();
        }

        /// <summary>
        /// Stretches the last column to the right edge. Without this the native
        /// header paints an unpainted white strip next to the final column.
        /// </summary>
        private void FillQuestColumns()
        {
            if (_questList.Columns.Count == 0 || _questList.ClientSize.Width <= 0)
                return;

            int usedWidth = 0;
            for (int i = 0; i < _questList.Columns.Count - 1; i++)
                usedWidth += _questList.Columns[i].Width;

            int lastWidth = _questList.ClientSize.Width - usedWidth;

            if (lastWidth <= 0 || _questList.Columns[^1].Width == lastWidth)
                return;

            _questList.Columns[^1].Width = lastWidth;
        }

        private void QuestList_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush background = new SolidBrush(HeaderColor))
                e.Graphics.FillRectangle(background, e.Bounds);

            using (Pen line = new Pen(GridLineColor))
                e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 10,
                e.Bounds.Top,
                Math.Max(0, e.Bounds.Width - 14),
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                e.Header?.Text ?? "",
                _questHeaderFont,
                textBounds,
                TextColor,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);
        }

        private void QuestList_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
            FillQuestRow(e.Graphics, e.Item, e.Bounds);
        }

        private void QuestList_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null)
                return;

            FillQuestRow(e.Graphics, e.Item, e.Bounds);

            Color textColor = e.Item.Selected ? Color.White : e.Item.ForeColor;
            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 8,
                e.Bounds.Top,
                Math.Max(0, e.Bounds.Width - 12),
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem?.Text ?? "",
                _questList.Font,
                textBounds,
                textColor,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);
        }

        private static void FillQuestRow(Graphics graphics, ListViewItem item, Rectangle bounds)
        {
            Color background = item.Selected
                ? SelectionColor
                : item.Index % 2 == 1
                    ? AlternateRowColor
                    : SurfaceColor;

            using SolidBrush brush = new SolidBrush(background);
            graphics.FillRectangle(brush, bounds);
        }

        private static string FormatProgress(DiscordQuest quest)
        {
            string progress = FormatDuration(quest.EffectiveProgress);

            if (quest.TargetSeconds <= 0)
                return progress;

            return $"{progress} / {FormatDuration(quest.TargetSeconds)} ({quest.Percent:0}%)";
        }

        private static string DescribeStatus(DiscordQuest quest)
        {
            if (quest.Claimed)
                return "Claimed";

            if (quest.Completed)
                return "Completed - claim reward";

            if (quest.Expired)
                return "Expired";

            if (quest.Enrolled)
                return "In progress";

            return "Waiting for you to start";
        }

        private static string FormatDuration(double seconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0, seconds));

            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes:00}:{duration.Seconds:00}";
        }

        private static void ApplyDarkWindowTheme(Control control)
        {
            if (!control.IsHandleCreated)
                return;

            try
            {
                // Darkens native scrollbars and list chrome on Windows 10/11.
                SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            catch
            {
                // Cosmetic only; ignore on older Windows versions.
            }
        }

        private void RunOnUi(Action action)
        {
            if (IsDisposed || Disposing)
                return;

            if (!IsHandleCreated)
            {
                try
                {
                    action();
                }
                catch
                {
                }

                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch
                {
                }

                return;
            }

            action();
        }

        private void AppendLog(string message)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(AppendLog), message);
                }
                catch
                {
                }

                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logBox.AppendText($"[{timestamp}] {message}\n");
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();

            try
            {
                ExternalLog?.Invoke($"[Video Quests] {message}");
            }
            catch
            {
            }
        }

        public void Shutdown()
        {
            if (_shutdown)
                return;

            _shutdown = true;
            _manager.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
                _questRowImages.Dispose();
                _questHeaderFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
