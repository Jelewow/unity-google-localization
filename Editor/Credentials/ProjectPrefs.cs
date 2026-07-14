using UnityEditor;

namespace SheetsLocalization.Editor.Credentials
{
    /// <summary>
    /// Project-scoped wrapper over <see cref="EditorPrefs"/>. Keys are namespaced by
    /// <see cref="PlayerSettings.productGUID"/> so values don't leak between projects on the same machine,
    /// and they live outside the project folder (never committed to source control).
    /// </summary>
    internal static class ProjectPrefs
    {
        private const string Prefix = "SheetsLocalization";

        public static string GetString(string name, string defaultValue) =>
            EditorPrefs.GetString(Key(name), defaultValue);

        public static void SetString(string name, string value) =>
            EditorPrefs.SetString(Key(name), value ?? string.Empty);

        public static void DeleteKey(string name) =>
            EditorPrefs.DeleteKey(Key(name));

        private static string Key(string name) => $"{Prefix}.{PlayerSettings.productGUID}.{name}";
    }
}
