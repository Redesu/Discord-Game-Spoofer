using System.Drawing;
using System.Windows.Forms;

namespace DiscordGameSpoofer
{
    /// <summary>
    /// Small visible window used by a spoofed game process. Discord ignores processes
    /// without a visible top-level window, so this form must stay visible while the
    /// spoof is running.
    /// </summary>
    internal sealed class SpoofHostForm : Form
    {
        public SpoofHostForm(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
                gameName = "Game";

            Text = gameName;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(10, 14, 23);
            ForeColor = Color.FromArgb(226, 232, 240);
            ClientSize = new Size(480, 200);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            Label titleLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(24, 22),
                Size = new Size(432, 32),
                Text = "Now spoofing",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Label gameLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(24, 62),
                Size = new Size(432, 44),
                Text = gameName,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Label hintLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(24, 122),
                Size = new Size(432, 54),
                Text = "Keep this window open while the quest is active.\nClosing it stops the spoof.",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Controls.Add(titleLabel);
            Controls.Add(gameLabel);
            Controls.Add(hintLabel);
        }
    }
}
