using SheetsLocalization.Editor.Configurators;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace SheetsLocalization.Editor.Settings
{
    /// <summary>
    /// Serializable configuration for a localization sync target: source links, generated table
    /// references, output folders and the parsing configurator. Credentials are intentionally
    /// NOT stored here — they live per-user in <c>CredentialsStore</c> and never get committed.
    /// </summary>
    [CreateAssetMenu(menuName = "Sheets Localization/Localization Settings")]
    public class GoogleLocalizationSettings : ScriptableObject
    {
        [Tooltip("Addressables group name to assign the generated tables and audio to. Leave empty to skip Addressables assignment.")]
        [SerializeField] private string bundleName;

        [SerializeField] private string googleSheetsLink;
        [SerializeField] private string googleDriveFolderLink;

        [SerializeField] private StringTableCollection localTable;
        [SerializeField] private AssetTableCollection localAudioTable;

        [SerializeField] private string localTableFolderPath;
        [SerializeField] private string localAudioFolderPath;

        [Tooltip("Use the spreadsheet title as the table key prefix instead of the value below.")]
        [SerializeField] private bool dontUseTableKeyPrefix;

        [SerializeField] private string tableKeyPrefix;

        [SerializeField] private GoogleSheetsConfigurator sheetsConfigurator;

        public string BundleName => bundleName;
        public string SheetsLink => googleSheetsLink;
        public string DriveFolderLink => googleDriveFolderLink;
        public StringTableCollection StringTable => localTable;
        public AssetTableCollection AudioTable => localAudioTable;
        public string StringTableFolderPath => localTableFolderPath;
        public string AudioFolderPath => localAudioFolderPath;
        public bool UseSpreadsheetTitleAsPrefix => dontUseTableKeyPrefix;
        public string TableKeyPrefix => tableKeyPrefix;
        public GoogleSheetsConfigurator Configurator => sheetsConfigurator;

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

        internal void SetResolvedTableKeyPrefix(string value)
        {
            tableKeyPrefix = value;
            EditorUtility.SetDirty(this);
        }
    }
}
