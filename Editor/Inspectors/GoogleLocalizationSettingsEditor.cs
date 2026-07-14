using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SheetsLocalization.Editor.Configurators;
using SheetsLocalization.Editor.Credentials;
using SheetsLocalization.Editor.Services;
using SheetsLocalization.Editor.Settings;
using SheetsLocalization.Editor.Windows;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace SheetsLocalization.Editor.Inspectors
{
    [CustomEditor(typeof(GoogleLocalizationSettings))]
    public class GoogleLocalizationSettingsEditor : UnityEditor.Editor
    {
        private const string NoneGroup = "(None)";
        private static readonly Color PrimaryColor = new Color(0.55f, 0.82f, 0.55f);
        private static readonly Color NeutralColor = new Color(0.72f, 0.80f, 0.92f);

        private Type[] _configuratorTypes;
        private string[] _configuratorNames;

        public override void OnInspectorGUI()
        {
            var settings = (GoogleLocalizationSettings)target;
            serializedObject.Update();

            DrawSection("Source", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("googleSheetsLink"), new GUIContent("Google Sheets link"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("googleDriveFolderLink"), new GUIContent("Google Drive folder link"));
            });

            DrawSection("Generated tables", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("localTable"), new GUIContent("String table"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("localAudioTable"), new GUIContent("Audio table"));
            });

            DrawSection("Output", () =>
            {
                DrawGroupDropdown(serializedObject.FindProperty("addressableGroup"));
                EditorGUILayout.Space(4f);
                DrawPathOverride("String tables", serializedObject.FindProperty("overrideTextPath"),
                    serializedObject.FindProperty("textPath"), DefaultPathsStore.LoadTextPath());
                DrawPathOverride("Audio", serializedObject.FindProperty("overrideAudioPath"),
                    serializedObject.FindProperty("audioPath"), DefaultPathsStore.LoadAudioPath());
            });

            DrawSection("Parsing", () => DrawConfigurator(settings, serializedObject.FindProperty("configurator")));

            serializedObject.ApplyModifiedProperties();

            DrawCredentials();
            DrawOperations(settings);
        }

        private void DrawGroupDropdown(SerializedProperty groupProp)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorGUILayout.PropertyField(groupProp, new GUIContent("Addressable group"));
                EditorGUILayout.HelpBox("Addressables settings not found. Create them via Window > Asset Management > Addressables > Groups.", MessageType.None);
                return;
            }

            var options = new List<string> { NoneGroup };
            options.AddRange(settings.groups.Where(g => g != null && !g.ReadOnly).Select(g => g.Name));

            var current = groupProp.stringValue;
            if (!string.IsNullOrEmpty(current) && !options.Contains(current))
                options.Add(current); // preserve a renamed/removed group instead of silently clearing it

            var index = string.IsNullOrEmpty(current) ? 0 : Mathf.Max(0, options.IndexOf(current));

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup("Addressable group", index, options.ToArray());
            if (EditorGUI.EndChangeCheck())
                groupProp.stringValue = newIndex <= 0 ? string.Empty : options[newIndex];
        }

        private void DrawPathOverride(string label, SerializedProperty toggleProp, SerializedProperty pathProp, string defaultPath)
        {
            toggleProp.boolValue = EditorGUILayout.ToggleLeft($"Override {label.ToLower()} path", toggleProp.boolValue);

            EditorGUI.indentLevel++;
            if (toggleProp.boolValue)
            {
                EditorGUILayout.PropertyField(pathProp, new GUIContent(" "));
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(" ", $"{defaultPath}  (default)");
            }
            EditorGUI.indentLevel--;
        }

        private void DrawConfigurator(GoogleLocalizationSettings settings, SerializedProperty configuratorProp)
        {
            EnsureConfiguratorTypes();
            if (_configuratorTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("No GoogleSheetsConfigurator implementations found.", MessageType.Warning);
                return;
            }

            if (configuratorProp.managedReferenceValue == null || settings.Configurator == null)
            {
                configuratorProp.managedReferenceValue = Activator.CreateInstance(_configuratorTypes[0]);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            var currentType = settings.Configurator.GetType();
            var currentIndex = Array.IndexOf(_configuratorTypes, currentType);

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup("Configurator", Mathf.Max(0, currentIndex), _configuratorNames);
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex != currentIndex)
            {
                configuratorProp.managedReferenceValue = Activator.CreateInstance(_configuratorTypes[newIndex]);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            DrawManagedChildren(configuratorProp);

            var hint = settings.Configurator?.SchemeHint;
            if (!string.IsNullOrEmpty(hint))
                EditorGUILayout.HelpBox(hint, MessageType.None);
        }

        private void DrawManagedChildren(SerializedProperty property)
        {
            var iterator = property.Copy();
            var depth = property.depth;
            var enterChildren = true;

            EditorGUI.indentLevel++;
            while (iterator.NextVisible(enterChildren) && iterator.depth > depth)
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawCredentials()
        {
            DrawSection("Credentials", () =>
            {
                EditorGUILayout.HelpBox(CredentialsStore.Load().GetActiveCredentialsInfo(), MessageType.Info);
                if (GUILayout.Button("Open settings window"))
                    SheetsLocalizationWindow.Open();
            });
        }

        private void DrawOperations(GoogleLocalizationSettings settings)
        {
            DrawSection("Operations", () =>
            {
                var previous = GUI.backgroundColor;

                GUI.backgroundColor = PrimaryColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Update texts", "Import strings from Google Sheets"), GUILayout.Height(34f)))
                        Run("Updating texts", s => new LocalizationSyncService(CredentialsStore.Load()).UpdateTextsAsync(s), settings);
                    if (GUILayout.Button(new GUIContent("Update audio", "Sync audio from Google Drive"), GUILayout.Height(34f)))
                        Run("Updating audio", s => new LocalizationSyncService(CredentialsStore.Load()).UpdateAudioAsync(s), settings);
                }

                EditorGUILayout.Space(4f);

                GUI.backgroundColor = NeutralColor;
                if (GUILayout.Button(new GUIContent("Validate Addressables group", "Reassign the group/label to all generated assets"), GUILayout.Height(26f)))
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
                GUI.backgroundColor = previous;

                EditorGUILayout.Space(8f);

                if (GUILayout.Button(new GUIContent("Test authentication", "Verify the current credentials"), GUILayout.Height(22f)))
                    Run("Testing authentication", _ => new LocalizationSyncService(CredentialsStore.Load()).TestAuthenticationAsync(), settings);
            });
        }

        private void EnsureConfiguratorTypes()
        {
            if (_configuratorTypes != null)
                return;

            _configuratorTypes = TypeCache.GetTypesDerivedFrom<GoogleSheetsConfigurator>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToArray();
            _configuratorNames = _configuratorTypes.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        private static void DrawSection(string title, Action body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                body();
            }
            EditorGUILayout.Space(2f);
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
