namespace SheetsLocalization.Editor.Credentials
{
    /// <summary>
    /// Per-project default output folders for generated string tables and audio.
    /// A settings asset uses these unless it overrides the path explicitly.
    /// </summary>
    public static class DefaultPathsStore
    {
        public const string DefaultTextPath = "Assets/Localization/Text";
        public const string DefaultAudioPath = "Assets/Localization/Audio";

        public static string LoadTextPath() => ProjectPrefs.GetString("DefaultTextPath", DefaultTextPath);
        public static string LoadAudioPath() => ProjectPrefs.GetString("DefaultAudioPath", DefaultAudioPath);

        public static void SaveTextPath(string value) => ProjectPrefs.SetString("DefaultTextPath", value);
        public static void SaveAudioPath(string value) => ProjectPrefs.SetString("DefaultAudioPath", value);
    }
}
