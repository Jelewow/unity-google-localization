using SheetsLocalization.Editor.Settings;
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
            EditorGUILayout.HelpBox(settings.CredentialsInfo, MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("Test authentication"))
            {
                settings.TestAuthentication();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);
            if (GUILayout.Button("Update texts"))
            {
                settings.UpdateLocales();
            }
            if (GUILayout.Button("Update audio files"))
            {
                settings.UpdateAudioFiles();
            }
            if (GUILayout.Button("Validate Addressables group for all files"))
            {
                settings.ValidateAddressableGroupAssignment();
            }
        }
    }
}
