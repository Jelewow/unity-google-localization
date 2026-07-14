using System;
using SheetsLocalization.Editor.Configurators;
using SheetsLocalization.Editor.Credentials;
using SheetsLocalization.Editor.Services;
using SheetsLocalization.Editor.Types;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace SheetsLocalization.Editor.Settings
{
    [CreateAssetMenu(menuName = "Sheets Localization/Localization Settings")]
    public class GoogleLocalizationSettings : ScriptableObject
    {
        [SerializeField] private GoogleCredentials credentials = new GoogleCredentials();

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

        public string CredentialsInfo => credentials.GetActiveCredentialsInfo();

        public async void TestAuthentication()
        {
            try
            {
                var authService = new GoogleAuthService(credentials);

                Debug.Log("Testing authentication...");

                if (credentials.AuthType == GoogleAuthType.ServiceAccount)
                {
                    var token = await authService.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        Debug.Log("Service account authenticated successfully.");
                    }
                    else
                    {
                        Debug.LogError("Failed to obtain an access token.");
                    }
                }
                else
                {
                    if (credentials.IsApiKeyConfigured())
                    {
                        Debug.Log("API key is configured.");
                    }
                    else
                    {
                        Debug.LogError("API key is not configured.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication test failed: {ex.Message}");
            }
        }

        public async void UpdateAudioFiles()
        {
            var authService = new GoogleAuthService(credentials);

            var audioSyncService = new GoogleDriveAudioSyncService(authService);
            var audioUpdateService = new AudioLocalizationUpdateService();

            var targetAudioTableName = localAudioTable != null ? localAudioTable.TableCollectionName : null;
            if (string.IsNullOrEmpty(targetAudioTableName))
            {
                var folderName = await audioSyncService.GetFolderNameAsync(googleDriveFolderLink);
                targetAudioTableName = !string.IsNullOrWhiteSpace(folderName) ? folderName : name;
            }

            if (localAudioTable == null)
            {
                var ensured = audioUpdateService.EnsureAudioAssetTableCollection(null, targetAudioTableName, localAudioFolderPath);
                if (ensured != null)
                {
                    localAudioTable = ensured;
                    EditorUtility.SetDirty(this);
                }
            }

            await audioSyncService.SyncAudioFilesAsync(googleDriveFolderLink, localAudioFolderPath, targetAudioTableName);
            AssetDatabase.Refresh();

            var assetTableData = new AssetTableData
            {
                Table = localAudioTable,
                Data = new SheetData { TableName = targetAudioTableName }
            };

            var audioClips = audioUpdateService.LoadAudioClipsFromDirectory(localAudioFolderPath);
            var updatedAssetData = audioUpdateService.UpdateAudioTableData(assetTableData, audioClips, localTableFolderPath, bundleName, true);

            if (localAudioTable == null && updatedAssetData.Table != null)
            {
                localAudioTable = updatedAssetData.Table;
                EditorUtility.SetDirty(this);
                Debug.Log($"Updated AssetTableCollection reference: {localAudioTable.TableCollectionName}");
            }

            var bundleAssignmentService = new AssetBundleAssignmentService();
            bundleAssignmentService.ValidateAndAssignGroupToAllAssets(bundleName, localAudioFolderPath, localTable, localAudioTable);

            AssetDatabase.SaveAssets();
        }

        public async void UpdateLocales()
        {
            var authService = new GoogleAuthService(credentials);

            try
            {
                var sheetsParser = new GoogleSheetsParseService(authService);
                var localeUpdater = new LocalizationUpdateService();

                Debug.Log("Starting text update...");

                if (sheetsConfigurator == null)
                    throw new Exception("No parsing configurator selected");

                Debug.Log($"Using configurator: {sheetsConfigurator.SchemeHint}");
                var configuratorRawData = await sheetsParser.GetRawSheetDataAsync(googleSheetsLink,
                    null, tableKeyPrefix, dontUseTableKeyPrefix);

                if (dontUseTableKeyPrefix || string.IsNullOrEmpty(tableKeyPrefix))
                {
                    tableKeyPrefix = configuratorRawData.TableKeyPrefix;
                    EditorUtility.SetDirty(this);
                }

                if (!sheetsConfigurator.ValidateData(configuratorRawData))
                    throw new Exception("Data validation failed");

                var sheetData = sheetsConfigurator.ParseSheetData(configuratorRawData);

                var stringTableData = new StringTableData
                {
                    Table = localTable,
                    Data = sheetData
                };

                var updatedData = localeUpdater.UpdateProjectData(stringTableData, localTableFolderPath, bundleName, true);

                if (localTable == null && updatedData.Table != null)
                {
                    localTable = updatedData.Table;
                    EditorUtility.SetDirty(this);
                    Debug.Log($"Updated StringTableCollection reference: {localTable.TableCollectionName}");
                }

                var bundleAssignmentService = new AssetBundleAssignmentService();
                bundleAssignmentService.AssignAddressableGroupToTables(bundleName, localTable, null);

                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(this);

                sheetsConfigurator.UpdateAfterTableCreated(localTable);

                Debug.Log("Text update completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Text update failed: {ex.Message}");
            }
        }

        public void ValidateAddressableGroupAssignment()
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Debug.LogWarning("BundleName is empty. Set the Addressables group name before running validation.");
                return;
            }

            try
            {
                Debug.Log($"Validating and assigning group '{bundleName}' to all localization files...");

                var bundleAssignmentService = new AssetBundleAssignmentService();
                bundleAssignmentService.ValidateAndAssignGroupToAllAssets(bundleName, localAudioFolderPath, localTable, localAudioTable);

                AssetDatabase.SaveAssets();
                Debug.Log("Group validation completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Group validation failed: {ex.Message}");
                throw;
            }
        }
    }
}
