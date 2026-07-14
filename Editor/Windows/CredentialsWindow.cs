using SheetsLocalization.Editor.Credentials;
using UnityEditor;
using UnityEngine;

namespace SheetsLocalization.Editor.Windows
{
    /// <summary>
    /// Editor window for entering Google credentials. Values are persisted per-project via
    /// <see cref="CredentialsStore"/> (EditorPrefs), so they never end up in code or source control.
    /// </summary>
    public class CredentialsWindow : EditorWindow
    {
        private GoogleCredentials _credentials;

        [MenuItem("Tools/Sheets Localization/Credentials")]
        public static void Open()
        {
            var window = GetWindow<CredentialsWindow>("Localization Credentials");
            window.minSize = new Vector2(440f, 240f);
            window.Show();
        }

        private void OnEnable()
        {
            _credentials = CredentialsStore.Load();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Google Credentials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Stored per-project in EditorPrefs. Never committed to source control.",
                MessageType.None);
            EditorGUILayout.Space();

            var useApiKey = _credentials.AuthType == GoogleAuthType.ApiKey;
            useApiKey = EditorGUILayout.Toggle("Use API key", useApiKey);
            _credentials.AuthType = useApiKey ? GoogleAuthType.ApiKey : GoogleAuthType.ServiceAccount;

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!useApiKey))
            {
                _credentials.ApiKey = EditorGUILayout.PasswordField("API key", _credentials.ApiKey);
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(useApiKey))
            {
                _credentials.ServiceAccountEmail =
                    EditorGUILayout.TextField("Service account email", _credentials.ServiceAccountEmail);
                DrawKeyPathField();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_credentials.GetActiveCredentialsInfo(), MessageType.Info);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save"))
                {
                    CredentialsStore.Save(_credentials);
                    ShowNotification(new GUIContent("Credentials saved"));
                }

                if (GUILayout.Button("Clear"))
                {
                    CredentialsStore.Clear();
                    _credentials = CredentialsStore.Load();
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawKeyPathField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _credentials.ServiceAccountKeyPath =
                    EditorGUILayout.TextField("Service account JSON", _credentials.ServiceAccountKeyPath);

                if (GUILayout.Button("Browse…", GUILayout.Width(80f)))
                {
                    var path = EditorUtility.OpenFilePanel("Select service account JSON key", string.Empty, "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _credentials.ServiceAccountKeyPath = path;
                        GUI.FocusControl(null);
                    }
                }
            }
        }
    }
}
