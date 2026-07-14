using SheetsLocalization.Editor.Configurators;
using SheetsLocalization.Editor.Credentials;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace SheetsLocalization.Editor.Settings
{
    /// <summary>
    /// Serializable configuration for a localization sync target: source links, generated table
    /// references, output folders and the parsing configurator. Credentials and default output paths
    /// live per-user in the Sheets Localization settings window and never get committed.
    /// </summary>
    [CreateAssetMenu(menuName = "Sheets Localization/Localization Settings")]
    public class GoogleLocalizationSettings : ScriptableObject
    {
        [SerializeField] private string googleSheetsLink;
        [SerializeField] private string googleDriveFolderLink;

        [SerializeField] private StringTableCollection localTable;
        [SerializeField] private AssetTableCollection localAudioTable;

        [SerializeField] private string addressableGroup;

        [SerializeField] private bool overrideTextPath;
        [SerializeField] private string textPath = DefaultPathsStore.DefaultTextPath;
        [SerializeField] private bool overrideAudioPath;
        [SerializeField] private string audioPath = DefaultPathsStore.DefaultAudioPath;

        [SerializeReference] private GoogleSheetsConfigurator configurator = new DefaultColumnConfigurator();

        public string SheetsLink => googleSheetsLink;
        public string DriveFolderLink => googleDriveFolderLink;
        public StringTableCollection StringTable => localTable;
        public AssetTableCollection AudioTable => localAudioTable;
        public string AddressableGroup => addressableGroup;
        public GoogleSheetsConfigurator Configurator => configurator;

        public string StringTableFolderPath =>
            overrideTextPath && !string.IsNullOrEmpty(textPath) ? textPath : DefaultPathsStore.LoadTextPath();

        public string AudioFolderPath =>
            overrideAudioPath && !string.IsNullOrEmpty(audioPath) ? audioPath : DefaultPathsStore.LoadAudioPath();

        internal void SetStringTable(StringTableCollection value)
        {
            localTable = value;
            EditorUtility.SetDirty(this);
        }

        internal void SetAudioTable(AssetTableCollection value)
        {
            localAudioTable = value;
            EditorUtility.SetDirty(this);
        }
    }
}
