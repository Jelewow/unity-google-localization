using SheetsLocalization.Editor.Credentials;
using UnityEditor;
using UnityEngine;

namespace SheetsLocalization.Editor.Windows
{
    /// <summary>
    /// Per-project settings window: default output folders and Google credentials.
    /// Everything here is stored in <see cref="ProjectPrefs"/> (EditorPrefs) and never committed.
    /// </summary>
    public class SheetsLocalizationWindow : EditorWindow
    {
        private GoogleCredentials _credentials;
        private string _textPath;
        private string _audioPath;

        [MenuItem("Tools/Sheets Localization/Settings")]
        public static void Open()
        {
            var window = GetWindow<SheetsLocalizationWindow>("Sheets Localization");
            window.minSize = new Vector2(460f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void Reload()
        {
            _credentials = CredentialsStore.Load();
            _textPath = DefaultPathsStore.LoadTextPath();
            _audioPath = DefaultPathsStore.LoadAudioPath();
        }

        private void OnGUI()
        {
            DrawDefaultPaths();
            EditorGUILayout.Space(8f);
            DrawCredentials();

            EditorGUILayout.Space(12f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", GUILayout.Height(26f)))
                    Save();

                if (GUILayout.Button("Clear credentials", GUILayout.Height(26f)))
                {
                    CredentialsStore.Clear();
                    _credentials = CredentialsStore.Load();
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawDefaultPaths()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Default Output Paths", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Used by settings assets that don't override paths.", EditorStyles.miniLabel);

                _textPath = DrawFolderField("String tables", _textPath);
                _audioPath = DrawFolderField("Audio clips", _audioPath);
            }
        }

        private void DrawCredentials()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Credentials", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Stored per-project in EditorPrefs. Never committed.", EditorStyles.miniLabel);

                var useApiKey = _credentials.AuthType == GoogleAuthType.ApiKey;
                useApiKey = EditorGUILayout.Toggle("Use API key", useApiKey);
                _credentials.AuthType = useApiKey ? GoogleAuthType.ApiKey : GoogleAuthType.ServiceAccount;

                EditorGUILayout.Space(4f);

                using (new EditorGUI.DisabledScope(!useApiKey))
                    _credentials.ApiKey = EditorGUILayout.PasswordField("API key", _credentials.ApiKey);

                EditorGUILayout.Space(4f);

                using (new EditorGUI.DisabledScope(useApiKey))
                {
                    _credentials.ServiceAccountEmail =
                        EditorGUILayout.TextField("Service account email", _credentials.ServiceAccountEmail);
                    _credentials.ServiceAccountKeyPath =
                        DrawFileField("Service account JSON", _credentials.ServiceAccountKeyPath, "json");
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(_credentials.GetActiveCredentialsInfo(), MessageType.Info);
            }
        }

        private void Save()
        {
            DefaultPathsStore.SaveTextPath(_textPath);
            DefaultPathsStore.SaveAudioPath(_audioPath);
            CredentialsStore.Save(_credentials);
            ShowNotification(new GUIContent("Settings saved"));
        }

        private string DrawFolderField(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.TextField(label, value);
                if (GUILayout.Button("Browse…", GUILayout.Width(80f)))
                {
                    var start = System.IO.Directory.Exists(value) ? value : "Assets";
                    var picked = EditorUtility.OpenFolderPanel($"Select {label} folder", start, string.Empty);
                    if (!string.IsNullOrEmpty(picked))
                    {
                        value = ToProjectRelative(picked);
                        GUI.FocusControl(null);
                    }
                }
            }
            return value;
        }

        private string DrawFileField(string label, string value, string extension)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.TextField(label, value);
                if (GUILayout.Button("Browse…", GUILayout.Width(80f)))
                {
                    var picked = EditorUtility.OpenFilePanel($"Select {label}", string.Empty, extension);
                    if (!string.IsNullOrEmpty(picked))
                    {
                        value = picked;
                        GUI.FocusControl(null);
                    }
                }
            }
            return value;
        }

        private static string ToProjectRelative(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;

            var normalized = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');

            if (normalized == dataPath)
                return "Assets";
            if (normalized.StartsWith(dataPath + "/"))
                return "Assets" + normalized.Substring(dataPath.Length);

            return normalized;
        }
    }
}
