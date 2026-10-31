namespace DiscordGameSpoofer
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // A spoofed game is launched as a renamed copy of this app. In that mode we
            // only show a small visible window so Discord can detect the process.
            int hostIndex = Array.FindIndex(args, arg => string.Equals(arg, "--spoof-host", StringComparison.OrdinalIgnoreCase));
            if (hostIndex >= 0)
            {
                string gameName = hostIndex + 1 < args.Length ? args[hostIndex + 1] : "Game";
                Application.Run(new SpoofHostForm(gameName));
                return;
            }

            Application.Run(new Form1());
        }
    }
}
