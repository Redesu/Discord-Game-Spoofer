namespace DiscordGameSpoofer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            gamesList = new ListBox();
            searchBox = new TextBox();
            runningGamesList = new ListBox();
            label1 = new Label();
            stopButton = new Button();
            startButton = new Button();
            detailsBox = new RichTextBox();
            label2 = new Label();
            runningGamesTime = new ListBox();
            logBox = new RichTextBox();
            toggleLogButton = new Button();
            versionLabel = new Label();
            autoStopCheckBox = new DarkCheckBox();
            addToQueueButton = new Button();
            customAppButton = new Button();
            queueLabel = new Label();
            queueList = new ListBox();
            startQueueButton = new Button();
            removeQueueButton = new Button();
            queueUpButton = new Button();
            queueDownButton = new Button();
            clearQueueButton = new Button();
            setInstallFolderButton = new Button();
            defaultInstallFolderButton = new Button();
            steamManifestCheckBox = new DarkCheckBox();
            navGamesButton = new Button();
            navVideoButton = new Button();
            gamesHost = new Panel();
            videoHost = new Panel();
            videoQuestsPanel = new VideoQuestsPanel();
            SuspendLayout();
            //
            // gamesList
            //
            gamesList.BackColor = Color.FromArgb(17, 24, 39);
            gamesList.BorderStyle = BorderStyle.None;
            gamesList.ForeColor = Color.FromArgb(226, 232, 240);
            gamesList.FormattingEnabled = true;
            gamesList.IntegralHeight = false;
            gamesList.Location = new Point(22, 94);
            gamesList.Margin = new Padding(6, 6, 6, 6);
            gamesList.Name = "gamesList";
            gamesList.Size = new Size(316, 780);
            gamesList.TabIndex = 0;
            gamesList.SelectedIndexChanged += gamesList_SelectedIndexChanged;
            //
            // searchBox
            //
            searchBox.BackColor = Color.FromArgb(24, 32, 50);
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.ForeColor = Color.FromArgb(226, 232, 240);
            searchBox.Location = new Point(22, 26);
            searchBox.Margin = new Padding(6, 6, 6, 6);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Search...";
            searchBox.Size = new Size(314, 39);
            searchBox.TabIndex = 1;
            searchBox.TextChanged += searchBox_TextChanged;
            //
            // runningGamesList
            //
            runningGamesList.BackColor = Color.FromArgb(17, 24, 39);
            runningGamesList.BorderStyle = BorderStyle.None;
            runningGamesList.ForeColor = Color.FromArgb(226, 232, 240);
            runningGamesList.FormattingEnabled = true;
            runningGamesList.Location = new Point(349, 94);
            runningGamesList.Margin = new Padding(6, 6, 6, 6);
            runningGamesList.Name = "runningGamesList";
            runningGamesList.Size = new Size(964, 384);
            runningGamesList.TabIndex = 2;
            runningGamesList.SelectedIndexChanged += runningGamesList_SelectedIndexChanged;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.BackColor = Color.FromArgb(10, 14, 23);
            label1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(56, 189, 248);
            label1.Location = new Point(349, 32);
            label1.Margin = new Padding(6, 0, 6, 0);
            label1.Name = "label1";
            label1.Size = new Size(186, 32);
            label1.TabIndex = 3;
            label1.Text = "Running Games";
            //
            // stopButton
            //
            stopButton.BackColor = Color.FromArgb(244, 63, 94);
            stopButton.Cursor = Cursors.Hand;
            stopButton.FlatAppearance.BorderSize = 0;
            stopButton.FlatStyle = FlatStyle.Flat;
            stopButton.ForeColor = Color.White;
            stopButton.Location = new Point(1324, 26);
            stopButton.Margin = new Padding(6, 6, 6, 6);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(139, 49);
            stopButton.TabIndex = 4;
            stopButton.Text = "Stop";
            stopButton.UseVisualStyleBackColor = false;
            stopButton.Click += stopButton_Click;
            //
            // startButton
            //
            startButton.BackColor = Color.FromArgb(14, 165, 233);
            startButton.Cursor = Cursors.Hand;
            startButton.FlatAppearance.BorderSize = 0;
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.ForeColor = Color.White;
            startButton.Location = new Point(1174, 26);
            startButton.Margin = new Padding(6, 6, 6, 6);
            startButton.Name = "startButton";
            startButton.Size = new Size(139, 49);
            startButton.TabIndex = 5;
            startButton.Text = "Start";
            startButton.UseVisualStyleBackColor = false;
            startButton.Click += startButton_Click;
            //
            // detailsBox
            //
            detailsBox.BackColor = Color.FromArgb(17, 24, 39);
            detailsBox.BorderStyle = BorderStyle.None;
            detailsBox.Font = new Font("Consolas", 9F);
            detailsBox.ForeColor = Color.FromArgb(226, 232, 240);
            detailsBox.Location = new Point(770, 555);
            detailsBox.Margin = new Padding(6, 6, 6, 6);
            detailsBox.Name = "detailsBox";
            detailsBox.ReadOnly = true;
            detailsBox.Size = new Size(693, 373);
            detailsBox.TabIndex = 6;
            detailsBox.Text = "";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.BackColor = Color.FromArgb(10, 14, 23);
            label2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(56, 189, 248);
            label2.Location = new Point(770, 499);
            label2.Margin = new Padding(6, 0, 6, 0);
            label2.Name = "label2";
            label2.Size = new Size(88, 32);
            label2.TabIndex = 7;
            label2.Text = "Details";
            //
            // runningGamesTime
            //
            runningGamesTime.BackColor = Color.FromArgb(17, 24, 39);
            runningGamesTime.BorderStyle = BorderStyle.None;
            runningGamesTime.ForeColor = Color.FromArgb(226, 232, 240);
            runningGamesTime.FormattingEnabled = true;
            runningGamesTime.Location = new Point(1324, 94);
            runningGamesTime.Margin = new Padding(6, 6, 6, 6);
            runningGamesTime.Name = "runningGamesTime";
            runningGamesTime.Size = new Size(139, 384);
            runningGamesTime.TabIndex = 8;
            runningGamesTime.SelectedIndexChanged += runningGamesTime_SelectedIndexChanged;
            //
            // logBox
            //
            logBox.BackColor = Color.FromArgb(17, 24, 39);
            logBox.BorderStyle = BorderStyle.None;
            logBox.Font = new Font("Consolas", 9F);
            logBox.ForeColor = Color.FromArgb(226, 232, 240);
            logBox.Location = new Point(770, 555);
            logBox.Margin = new Padding(6, 6, 6, 6);
            logBox.Name = "logBox";
            logBox.ReadOnly = true;
            logBox.Size = new Size(693, 373);
            logBox.TabIndex = 9;
            logBox.Text = "";
            //
            // toggleLogButton
            //
            toggleLogButton.BackColor = Color.FromArgb(0, 192, 0);
            toggleLogButton.Cursor = Cursors.Hand;
            toggleLogButton.FlatAppearance.BorderSize = 0;
            toggleLogButton.FlatStyle = FlatStyle.Flat;
            toggleLogButton.ForeColor = Color.White;
            toggleLogButton.Location = new Point(1023, 26);
            toggleLogButton.Margin = new Padding(6, 6, 6, 6);
            toggleLogButton.Name = "toggleLogButton";
            toggleLogButton.Size = new Size(139, 49);
            toggleLogButton.TabIndex = 10;
            toggleLogButton.Text = "Toggle Log";
            toggleLogButton.UseVisualStyleBackColor = false;
            toggleLogButton.Click += toggleLogButton_Click;
            //
            // versionLabel
            //
            versionLabel.AutoSize = true;
            versionLabel.BackColor = Color.FromArgb(10, 14, 23);
            versionLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            versionLabel.ForeColor = Color.FromArgb(56, 189, 248);
            versionLabel.Location = new Point(938, 34);
            versionLabel.Margin = new Padding(6, 0, 6, 0);
            versionLabel.Name = "versionLabel";
            versionLabel.Size = new Size(77, 32);
            versionLabel.TabIndex = 11;
            versionLabel.Text = "v0.0.0";
            //
            // autoStopCheckBox
            //
            autoStopCheckBox.BackColor = Color.FromArgb(10, 14, 23);
            autoStopCheckBox.ForeColor = Color.FromArgb(226, 232, 240);
            autoStopCheckBox.Location = new Point(615, 26);
            autoStopCheckBox.Margin = new Padding(6, 6, 6, 6);
            autoStopCheckBox.Name = "autoStopCheckBox";
            autoStopCheckBox.Size = new Size(311, 49);
            autoStopCheckBox.TabIndex = 12;
            autoStopCheckBox.Text = "Auto stop after 20 min";
            autoStopCheckBox.UseVisualStyleBackColor = false;
            //
            // addToQueueButton
            //
            addToQueueButton.BackColor = Color.FromArgb(71, 85, 105);
            addToQueueButton.Cursor = Cursors.Hand;
            addToQueueButton.FlatAppearance.BorderSize = 0;
            addToQueueButton.FlatStyle = FlatStyle.Flat;
            addToQueueButton.ForeColor = Color.White;
            addToQueueButton.Location = new Point(22, 884);
            addToQueueButton.Margin = new Padding(6, 6, 6, 6);
            addToQueueButton.Name = "addToQueueButton";
            addToQueueButton.Size = new Size(155, 44);
            addToQueueButton.TabIndex = 13;
            addToQueueButton.Text = "Add to Queue";
            addToQueueButton.UseVisualStyleBackColor = false;
            addToQueueButton.Click += addToQueueButton_Click;
            //
            // customAppButton
            //
            customAppButton.BackColor = Color.FromArgb(71, 85, 105);
            customAppButton.Cursor = Cursors.Hand;
            customAppButton.FlatAppearance.BorderSize = 0;
            customAppButton.FlatStyle = FlatStyle.Flat;
            customAppButton.ForeColor = Color.White;
            customAppButton.Location = new Point(183, 884);
            customAppButton.Margin = new Padding(6, 6, 6, 6);
            customAppButton.Name = "customAppButton";
            customAppButton.Size = new Size(155, 44);
            customAppButton.TabIndex = 14;
            customAppButton.Text = "Custom App...";
            customAppButton.UseVisualStyleBackColor = false;
            customAppButton.Click += customAppButton_Click;
            //
            // queueLabel
            //
            queueLabel.AutoSize = true;
            queueLabel.BackColor = Color.FromArgb(10, 14, 23);
            queueLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            queueLabel.ForeColor = Color.FromArgb(56, 189, 248);
            queueLabel.Location = new Point(349, 499);
            queueLabel.Margin = new Padding(6, 0, 6, 0);
            queueLabel.Name = "queueLabel";
            queueLabel.Size = new Size(88, 32);
            queueLabel.TabIndex = 15;
            queueLabel.Text = "Queue";
            //
            // queueList
            //
            queueList.BackColor = Color.FromArgb(17, 24, 39);
            queueList.BorderStyle = BorderStyle.None;
            queueList.ForeColor = Color.FromArgb(226, 232, 240);
            queueList.FormattingEnabled = true;
            queueList.IntegralHeight = false;
            queueList.Location = new Point(349, 555);
            queueList.Margin = new Padding(6, 6, 6, 6);
            queueList.Name = "queueList";
            queueList.Size = new Size(300, 373);
            queueList.TabIndex = 16;
            queueList.SelectedIndexChanged += queueList_SelectedIndexChanged;
            //
            // startQueueButton
            //
            startQueueButton.BackColor = Color.FromArgb(14, 165, 233);
            startQueueButton.Cursor = Cursors.Hand;
            startQueueButton.FlatAppearance.BorderSize = 0;
            startQueueButton.FlatStyle = FlatStyle.Flat;
            startQueueButton.ForeColor = Color.White;
            startQueueButton.Location = new Point(660, 555);
            startQueueButton.Margin = new Padding(6, 6, 6, 6);
            startQueueButton.Name = "startQueueButton";
            startQueueButton.Size = new Size(100, 50);
            startQueueButton.TabIndex = 17;
            startQueueButton.Text = "Start Queue";
            startQueueButton.UseVisualStyleBackColor = false;
            startQueueButton.Click += startQueueButton_Click;
            //
            // removeQueueButton
            //
            removeQueueButton.BackColor = Color.FromArgb(71, 85, 105);
            removeQueueButton.Cursor = Cursors.Hand;
            removeQueueButton.FlatAppearance.BorderSize = 0;
            removeQueueButton.FlatStyle = FlatStyle.Flat;
            removeQueueButton.ForeColor = Color.White;
            removeQueueButton.Location = new Point(660, 611);
            removeQueueButton.Margin = new Padding(6, 6, 6, 6);
            removeQueueButton.Name = "removeQueueButton";
            removeQueueButton.Size = new Size(100, 50);
            removeQueueButton.TabIndex = 18;
            removeQueueButton.Text = "Remove";
            removeQueueButton.UseVisualStyleBackColor = false;
            removeQueueButton.Click += removeQueueButton_Click;
            //
            // queueUpButton
            //
            queueUpButton.BackColor = Color.FromArgb(71, 85, 105);
            queueUpButton.Cursor = Cursors.Hand;
            queueUpButton.FlatAppearance.BorderSize = 0;
            queueUpButton.FlatStyle = FlatStyle.Flat;
            queueUpButton.ForeColor = Color.White;
            queueUpButton.Location = new Point(660, 667);
            queueUpButton.Margin = new Padding(6, 6, 6, 6);
            queueUpButton.Name = "queueUpButton";
            queueUpButton.Size = new Size(100, 50);
            queueUpButton.TabIndex = 19;
            queueUpButton.Text = "Move Up";
            queueUpButton.UseVisualStyleBackColor = false;
            queueUpButton.Click += queueUpButton_Click;
            //
            // queueDownButton
            //
            queueDownButton.BackColor = Color.FromArgb(71, 85, 105);
            queueDownButton.Cursor = Cursors.Hand;
            queueDownButton.FlatAppearance.BorderSize = 0;
            queueDownButton.FlatStyle = FlatStyle.Flat;
            queueDownButton.ForeColor = Color.White;
            queueDownButton.Location = new Point(660, 723);
            queueDownButton.Margin = new Padding(6, 6, 6, 6);
            queueDownButton.Name = "queueDownButton";
            queueDownButton.Size = new Size(100, 50);
            queueDownButton.TabIndex = 20;
            queueDownButton.Text = "Move Down";
            queueDownButton.UseVisualStyleBackColor = false;
            queueDownButton.Click += queueDownButton_Click;
            //
            // clearQueueButton
            //
            clearQueueButton.BackColor = Color.FromArgb(244, 63, 94);
            clearQueueButton.Cursor = Cursors.Hand;
            clearQueueButton.FlatAppearance.BorderSize = 0;
            clearQueueButton.FlatStyle = FlatStyle.Flat;
            clearQueueButton.ForeColor = Color.White;
            clearQueueButton.Location = new Point(660, 779);
            clearQueueButton.Margin = new Padding(6, 6, 6, 6);
            clearQueueButton.Name = "clearQueueButton";
            clearQueueButton.Size = new Size(100, 50);
            clearQueueButton.TabIndex = 21;
            clearQueueButton.Text = "Clear";
            clearQueueButton.UseVisualStyleBackColor = false;
            clearQueueButton.Click += clearQueueButton_Click;
            //
            // setInstallFolderButton
            //
            setInstallFolderButton.BackColor = Color.FromArgb(71, 85, 105);
            setInstallFolderButton.Cursor = Cursors.Hand;
            setInstallFolderButton.FlatAppearance.BorderSize = 0;
            setInstallFolderButton.FlatStyle = FlatStyle.Flat;
            setInstallFolderButton.ForeColor = Color.White;
            setInstallFolderButton.Location = new Point(1000, 492);
            setInstallFolderButton.Margin = new Padding(6, 6, 6, 6);
            setInstallFolderButton.Name = "setInstallFolderButton";
            setInstallFolderButton.Size = new Size(215, 42);
            setInstallFolderButton.TabIndex = 22;
            setInstallFolderButton.Text = "Set Install Folder...";
            setInstallFolderButton.UseVisualStyleBackColor = false;
            setInstallFolderButton.Click += setInstallFolderButton_Click;
            //
            // defaultInstallFolderButton
            //
            defaultInstallFolderButton.BackColor = Color.FromArgb(71, 85, 105);
            defaultInstallFolderButton.Cursor = Cursors.Hand;
            defaultInstallFolderButton.FlatAppearance.BorderSize = 0;
            defaultInstallFolderButton.FlatStyle = FlatStyle.Flat;
            defaultInstallFolderButton.ForeColor = Color.White;
            defaultInstallFolderButton.Location = new Point(1225, 492);
            defaultInstallFolderButton.Margin = new Padding(6, 6, 6, 6);
            defaultInstallFolderButton.Name = "defaultInstallFolderButton";
            defaultInstallFolderButton.Size = new Size(238, 42);
            defaultInstallFolderButton.TabIndex = 23;
            defaultInstallFolderButton.Text = "Use Default Folder";
            defaultInstallFolderButton.UseVisualStyleBackColor = false;
            defaultInstallFolderButton.Click += defaultInstallFolderButton_Click;
            //
            // steamManifestCheckBox
            //
            steamManifestCheckBox.BackColor = Color.FromArgb(10, 14, 23);
            steamManifestCheckBox.Checked = true;
            steamManifestCheckBox.CheckState = CheckState.Checked;
            steamManifestCheckBox.Enabled = false;
            steamManifestCheckBox.ForeColor = Color.FromArgb(226, 232, 240);
            steamManifestCheckBox.Location = new Point(770, 492);
            steamManifestCheckBox.Margin = new Padding(6, 6, 6, 6);
            steamManifestCheckBox.Name = "steamManifestCheckBox";
            steamManifestCheckBox.Size = new Size(218, 42);
            steamManifestCheckBox.TabIndex = 24;
            steamManifestCheckBox.Text = "Steam .acf manifest";
            steamManifestCheckBox.UseVisualStyleBackColor = false;
            steamManifestCheckBox.CheckedChanged += steamManifestCheckBox_CheckedChanged;
            //
            // navGamesButton
            //
            navGamesButton.BackColor = Color.FromArgb(14, 165, 233);
            navGamesButton.Cursor = Cursors.Hand;
            navGamesButton.FlatAppearance.BorderSize = 0;
            navGamesButton.FlatStyle = FlatStyle.Flat;
            navGamesButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            navGamesButton.ForeColor = Color.White;
            navGamesButton.Location = new Point(22, 8);
            navGamesButton.Name = "navGamesButton";
            navGamesButton.Size = new Size(170, 40);
            navGamesButton.TabIndex = 25;
            navGamesButton.Text = "Game Spoofing";
            navGamesButton.UseVisualStyleBackColor = false;
            navGamesButton.Click += navGamesButton_Click;
            //
            // navVideoButton
            //
            navVideoButton.BackColor = Color.FromArgb(24, 32, 50);
            navVideoButton.Cursor = Cursors.Hand;
            navVideoButton.FlatAppearance.BorderSize = 0;
            navVideoButton.FlatStyle = FlatStyle.Flat;
            navVideoButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            navVideoButton.ForeColor = Color.White;
            navVideoButton.Location = new Point(200, 8);
            navVideoButton.Name = "navVideoButton";
            navVideoButton.Size = new Size(170, 40);
            navVideoButton.TabIndex = 26;
            navVideoButton.Text = "Video Quests";
            navVideoButton.UseVisualStyleBackColor = false;
            navVideoButton.Click += navVideoButton_Click;
            //
            // gamesHost
            //
            gamesHost.BackColor = Color.FromArgb(10, 14, 23);
            gamesHost.Location = new Point(0, 56);
            gamesHost.Name = "gamesHost";
            gamesHost.Size = new Size(1486, 936);
            gamesHost.TabIndex = 27;
            //
            // videoHost
            //
            videoHost.BackColor = Color.FromArgb(10, 14, 23);
            videoHost.Location = new Point(0, 56);
            videoHost.Name = "videoHost";
            videoHost.Size = new Size(1486, 936);
            videoHost.TabIndex = 28;
            videoHost.Visible = false;
            videoHost.Controls.Add(videoQuestsPanel);
            //
            // videoQuestsPanel
            //
            videoQuestsPanel.Dock = DockStyle.Fill;
            videoQuestsPanel.Location = new Point(0, 0);
            videoQuestsPanel.Name = "videoQuestsPanel";
            videoQuestsPanel.Size = new Size(1486, 936);
            videoQuestsPanel.TabIndex = 0;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(10, 14, 23);
            ClientSize = new Size(1486, 992);
            Controls.Add(navVideoButton);
            Controls.Add(navGamesButton);
            Controls.Add(versionLabel);
            Controls.Add(videoHost);
            Controls.Add(gamesHost);
            gamesHost.Controls.Add(steamManifestCheckBox);
            gamesHost.Controls.Add(defaultInstallFolderButton);
            gamesHost.Controls.Add(setInstallFolderButton);
            gamesHost.Controls.Add(clearQueueButton);
            gamesHost.Controls.Add(queueDownButton);
            gamesHost.Controls.Add(queueUpButton);
            gamesHost.Controls.Add(removeQueueButton);
            gamesHost.Controls.Add(startQueueButton);
            gamesHost.Controls.Add(queueList);
            gamesHost.Controls.Add(queueLabel);
            gamesHost.Controls.Add(customAppButton);
            gamesHost.Controls.Add(addToQueueButton);
            gamesHost.Controls.Add(autoStopCheckBox);
            gamesHost.Controls.Add(toggleLogButton);
            gamesHost.Controls.Add(logBox);
            gamesHost.Controls.Add(runningGamesTime);
            gamesHost.Controls.Add(label2);
            gamesHost.Controls.Add(detailsBox);
            gamesHost.Controls.Add(startButton);
            gamesHost.Controls.Add(stopButton);
            gamesHost.Controls.Add(label1);
            gamesHost.Controls.Add(runningGamesList);
            gamesHost.Controls.Add(searchBox);
            gamesHost.Controls.Add(gamesList);
            FormBorderStyle = FormBorderStyle.Sizable;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(6, 6, 6, 6);
            MaximizeBox = true;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Discord Game Spoofer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox gamesList;
        private TextBox searchBox;
        private ListBox runningGamesList;
        private Label label1;
        private Button stopButton;
        private Button startButton;
        private RichTextBox detailsBox;
        private Label label2;
        private ListBox runningGamesTime;
        private RichTextBox logBox;
        private Button toggleLogButton;
        private Label versionLabel;
        private DarkCheckBox autoStopCheckBox;
        private Button addToQueueButton;
        private Button customAppButton;
        private Label queueLabel;
        private ListBox queueList;
        private Button startQueueButton;
        private Button removeQueueButton;
        private Button queueUpButton;
        private Button queueDownButton;
        private Button clearQueueButton;
        private Button setInstallFolderButton;
        private Button defaultInstallFolderButton;
        private DarkCheckBox steamManifestCheckBox;
        private Button navGamesButton;
        private Button navVideoButton;
        private Panel gamesHost;
        private Panel videoHost;
        private VideoQuestsPanel videoQuestsPanel;
    }
}
