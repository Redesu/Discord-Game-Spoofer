namespace DiscordGameSpoofer
{
    partial class CustomApplicationForm
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
            appNameLabel = new Label();
            appNameBox = new TextBox();
            exeNameLabel = new Label();
            exeNameBox = new TextBox();
            folderLabel = new Label();
            folderBox = new TextBox();
            hintLabel = new Label();
            addButton = new Button();
            cancelButton = new Button();
            SuspendLayout();
            //
            // appNameLabel
            //
            appNameLabel.AutoSize = true;
            appNameLabel.BackColor = Color.FromArgb(10, 14, 23);
            appNameLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            appNameLabel.ForeColor = Color.FromArgb(56, 189, 248);
            appNameLabel.Location = new Point(24, 20);
            appNameLabel.Name = "appNameLabel";
            appNameLabel.Size = new Size(200, 32);
            appNameLabel.TabIndex = 0;
            appNameLabel.Text = "Application name";
            //
            // appNameBox
            //
            appNameBox.BackColor = Color.FromArgb(24, 32, 50);
            appNameBox.BorderStyle = BorderStyle.FixedSingle;
            appNameBox.ForeColor = Color.FromArgb(226, 232, 240);
            appNameBox.Location = new Point(24, 52);
            appNameBox.Name = "appNameBox";
            appNameBox.PlaceholderText = "My Game";
            appNameBox.Size = new Size(560, 39);
            appNameBox.TabIndex = 1;
            //
            // exeNameLabel
            //
            exeNameLabel.AutoSize = true;
            exeNameLabel.BackColor = Color.FromArgb(10, 14, 23);
            exeNameLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            exeNameLabel.ForeColor = Color.FromArgb(56, 189, 248);
            exeNameLabel.Location = new Point(24, 104);
            exeNameLabel.Name = "exeNameLabel";
            exeNameLabel.Size = new Size(200, 32);
            exeNameLabel.TabIndex = 2;
            exeNameLabel.Text = "Executable name";
            //
            // exeNameBox
            //
            exeNameBox.BackColor = Color.FromArgb(24, 32, 50);
            exeNameBox.BorderStyle = BorderStyle.FixedSingle;
            exeNameBox.ForeColor = Color.FromArgb(226, 232, 240);
            exeNameBox.Location = new Point(24, 136);
            exeNameBox.Name = "exeNameBox";
            exeNameBox.PlaceholderText = "game.exe";
            exeNameBox.Size = new Size(560, 39);
            exeNameBox.TabIndex = 3;
            //
            // folderLabel
            //
            folderLabel.AutoSize = true;
            folderLabel.BackColor = Color.FromArgb(10, 14, 23);
            folderLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            folderLabel.ForeColor = Color.FromArgb(56, 189, 248);
            folderLabel.Location = new Point(24, 188);
            folderLabel.Name = "folderLabel";
            folderLabel.Size = new Size(400, 32);
            folderLabel.TabIndex = 4;
            folderLabel.Text = "Create in folder (under bin\\games)";
            //
            // folderBox
            //
            folderBox.BackColor = Color.FromArgb(24, 32, 50);
            folderBox.BorderStyle = BorderStyle.FixedSingle;
            folderBox.ForeColor = Color.FromArgb(226, 232, 240);
            folderBox.Location = new Point(24, 220);
            folderBox.Name = "folderBox";
            folderBox.PlaceholderText = "Game\\bin";
            folderBox.Size = new Size(560, 39);
            folderBox.TabIndex = 5;
            //
            // hintLabel
            //
            hintLabel.AutoSize = true;
            hintLabel.BackColor = Color.FromArgb(10, 14, 23);
            hintLabel.ForeColor = Color.FromArgb(148, 163, 184);
            hintLabel.Location = new Point(24, 266);
            hintLabel.Name = "hintLabel";
            hintLabel.Size = new Size(500, 32);
            hintLabel.TabIndex = 6;
            hintLabel.Text = "Leave the folder empty to create the exe directly in bin\\games.";
            //
            // addButton
            //
            addButton.BackColor = Color.FromArgb(14, 165, 233);
            addButton.Cursor = Cursors.Hand;
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.ForeColor = Color.White;
            addButton.Location = new Point(376, 304);
            addButton.Name = "addButton";
            addButton.Size = new Size(100, 44);
            addButton.TabIndex = 7;
            addButton.Text = "Add";
            addButton.UseVisualStyleBackColor = false;
            addButton.Click += addButton_Click;
            //
            // cancelButton
            //
            cancelButton.BackColor = Color.FromArgb(71, 85, 105);
            cancelButton.Cursor = Cursors.Hand;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.ForeColor = Color.White;
            cancelButton.Location = new Point(488, 304);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(96, 44);
            cancelButton.TabIndex = 8;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = false;
            //
            // CustomApplicationForm
            //
            AcceptButton = addButton;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(10, 14, 23);
            CancelButton = cancelButton;
            ClientSize = new Size(620, 360);
            Controls.Add(cancelButton);
            Controls.Add(addButton);
            Controls.Add(hintLabel);
            Controls.Add(folderBox);
            Controls.Add(folderLabel);
            Controls.Add(exeNameBox);
            Controls.Add(exeNameLabel);
            Controls.Add(appNameBox);
            Controls.Add(appNameLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CustomApplicationForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Custom Application";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label appNameLabel;
        private TextBox appNameBox;
        private Label exeNameLabel;
        private TextBox exeNameBox;
        private Label folderLabel;
        private TextBox folderBox;
        private Label hintLabel;
        private Button addButton;
        private Button cancelButton;
    }
}
