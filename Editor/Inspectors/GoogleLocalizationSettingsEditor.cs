using System;
using System.Threading.Tasks;
using SheetsLocalization.Editor.Credentials;
using SheetsLocalization.Editor.Services;
using SheetsLocalization.Editor.Settings;
using SheetsLocalization.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace SheetsLocalization.Editor.Inspectors
{
    [CustomEditor(typeof(GoogleLocalizationSettings))]
    public class GoogleLocalizationSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (GoogleLocalizationSettings)target;

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Credentials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(CredentialsStore.Load().GetActiveCredentialsInfo(), MessageType.Info);
            if (GUILayout.Button("Open Credentials window"))
                CredentialsWindow.Open();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Test authentication"))
                Run("Testing authentication", _ => new LocalizationSyncService(CredentialsStore.Load()).TestAuthenticationAsync(), settings);

            if (GUILayout.Button("Update texts"))
                Run("Updating texts", s => new LocalizationSyncService(CredentialsStore.Load()).UpdateTextsAsync(s), settings);

            if (GUILayout.Button("Update audio files"))
                Run("Updating audio", s => new LocalizationSyncService(CredentialsStore.Load()).UpdateAudioAsync(s), settings);

            if (GUILayout.Button("Validate Addressables group for all files"))
            {
                try
                {
                    new LocalizationSyncService(CredentialsStore.Load()).ValidateGroups(settings);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Group validation failed: {ex.Message}");
                }
            }
        }

        // async void is the accepted boundary for a fire-and-forget UI button handler.
        private static async void Run(string title, Func<GoogleLocalizationSettings, Task> operation, GoogleLocalizationSettings settings)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Sheets Localization", $"{title}…", 0f);
                await operation(settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{title} failed: {ex.Message}\n{ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
