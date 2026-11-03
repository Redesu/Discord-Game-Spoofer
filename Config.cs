namespace DiscordGameSpoofer
{
    internal class Config
    {
        public static string version = "0.0.1";
        public static int autoStopMinutes = 20;
        public static string versionCheckUrl = "https://api.github.com/repos/devAxri/Discord-Game-Spoofer/releases/latest";
        public static string blankDownloadUrl = "https://github.com/devAxri/blank_window/releases/latest/download/blank_window.exe";
        public static string blankAPIUrl = "https://api.github.com/repos/devAxri/blank_window/releases/latest";
        public static string discordAPIUrl = "https://discord.com/api/v10/applications/detectable";
        public static string githubUrl = "https://github.com/devAxri/Discord-Game-Spoofer";
        public static string githubIssuesUrl = "https://github.com/devAxri/Discord-Game-Spoofer/issues";

        // Video quest support
        public static string discordApiBase = "https://discord.com/api/v9";
        public static string discordClientVersion = "1.0.9266";
        public static string discordElectronVersion = "44.9.3";
        public static string discordChromeVersion = "152.0.7977.167";
        public static int discordBuildNumber = 657850;
        public static string discordLocale = "en-US";
        public static string discordUserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) discord/{discordClientVersion} Chrome/{discordChromeVersion} Electron/{discordElectronVersion} Safari/537.36";
        public static string tokenFileName = "discord_token.dat";
        public static int questPollSeconds = 20;
        public static int videoProgressMinMs = 7000;
        public static int videoProgressMaxMs = 9500;
    }
}
