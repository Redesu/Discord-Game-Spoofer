using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DiscordGameSpoofer
{
    public partial class CustomApplicationForm : Form
    {
        private readonly string gamesRoot;

        public Form1.DiscordDetectableApplication? Result { get; private set; }

        public CustomApplicationForm(string gamesRoot)
        {
            InitializeComponent();
            this.gamesRoot = Path.GetFullPath(gamesRoot);
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            string appName = appNameBox.Text.Trim();
            string exeName = exeNameBox.Text.Trim();
            string folder = folderBox.Text.Trim();

            if (exeName.Length == 0)
            {
                MessageBox.Show("Enter an executable name, for example game.exe.", "Custom Application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                exeName += ".exe";

            string[] disallowedChars = new string[] { "<", ">", "\"", "|", "*", "?", "~" };

            if (exeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || exeName.Contains('\\')
                || exeName.Contains('/')
                || disallowedChars.Any(exeName.Contains))
            {
                MessageBox.Show("The executable name contains invalid characters.", "Custom Application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            folder = folder.Replace('/', '\\').Trim('\\');

            if (folder.Length > 0)
            {
                if (Path.IsPathRooted(folder) || folder.Split('\\').Any(part => part == ".."))
                {
                    MessageBox.Show("The folder must be a relative path inside bin\\games and cannot contain '..'.", "Custom Application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (folder.IndexOfAny(Path.GetInvalidPathChars()) >= 0
                    || disallowedChars.Any(folder.Contains))
                {
                    MessageBox.Show("The folder path contains invalid characters.", "Custom Application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string relativePath = folder.Length > 0 ? Path.Combine(folder, exeName) : exeName;
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(Path.Combine(gamesRoot, relativePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show("The path is invalid: " + ex.Message, "Custom Application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rootWithSeparator = gamesRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? gamesRoot
                : gamesRoot + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The executable must be created inside the app's bin\\games folder.", "Custom Application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (appName.Length == 0)
                appName = Path.GetFileNameWithoutExtension(exeName);

            Result = new Form1.DiscordDetectableApplication
            {
                Id = "custom-" + Guid.NewGuid().ToString("N"),
                Name = appName,
                Executables = new List<Form1.DiscordExecutable>
                {
                    new Form1.DiscordExecutable
                    {
                        Name = relativePath.Replace('\\', '/'),
                        Os = "win32",
                    }
                }
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
