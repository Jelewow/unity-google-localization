using SheetsLocalization.Editor.Configurators;
using UnityEditor;
using UnityEngine;

namespace SheetsLocalization.Editor.Inspectors
{
    [CustomEditor(typeof(GoogleSheetsConfigurator), true)]
    public class GoogleSheetsConfiguratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var configurator = (GoogleSheetsConfigurator)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Expected sheet layout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(configurator.SchemeHint, MessageType.None);
        }
    }
}
