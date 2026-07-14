using System;
using System.Threading.Tasks;
using SheetsLocalization.Editor.Credentials;
using SheetsLocalization.Editor.Settings;
using SheetsLocalization.Editor.Types;
using UnityEditor;
using UnityEngine;

namespace SheetsLocalization.Editor.Services
{
    /// <summary>
    /// Orchestrates the localization sync flow (texts, audio, Addressables validation) using a set of
    /// <see cref="GoogleCredentials"/> and a <see cref="GoogleLocalizationSettings"/> config asset.
    /// Keeping the flow here leaves the settings asset as pure, serializable configuration.
    /// </summary>
    public class LocalizationSyncService
    {
        private readonly GoogleCredentials _credentials;

        public LocalizationSyncService(GoogleCredentials credentials)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        public async Task<bool> TestAuthenticationAsync()
        {
            Debug.Log("Testing authentication...");

            if (_credentials.AuthType == GoogleAuthType.ServiceAccount)
            {
                var authService = new GoogleAuthService(_credentials);
                var token = await authService.GetAccessTokenAsync();
                var success = !string.IsNullOrEmpty(token);
                Debug.Log(success
                    ? "Service account authenticated successfully."
                    : "Failed to obtain an access token.");
                return success;
            }

            var configured = _credentials.IsApiKeyConfigured();
            Debug.Log(configured ? "API key is configured." : "API key is not configured.");
            return configured;
        }

        public async Task UpdateTextsAsync(GoogleLocalizationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (settings.Configurator == null)
                throw new Exception("No parsing configurator selected");

            var authService = new GoogleAuthService(_credentials);
            var sheetsParser = new GoogleSheetsParseService(authService);
            var localeUpdater = new LocalizationUpdateService();

            Debug.Log("Starting text update...");
            Debug.Log($"Using configurator: {settings.Configurator.SchemeHint}");

            var rawData = await sheetsParser.GetRawSheetDataAsync(
                settings.SheetsLink, null, settings.TableKeyPrefix, settings.UseSpreadsheetTitleAsPrefix);

            if (settings.UseSpreadsheetTitleAsPrefix || string.IsNullOrEmpty(settings.TableKeyPrefix))
                settings.SetResolvedTableKeyPrefix(rawData.TableKeyPrefix);

            if (!settings.Configurator.ValidateData(rawData))
                throw new Exception("Data validation failed");

            var sheetData = settings.Configurator.ParseSheetData(rawData);
            var stringTableData = new StringTableData { Table = settings.StringTable, Data = sheetData };
            var updatedData = localeUpdater.UpdateProjectData(
                stringTableData, settings.StringTableFolderPath, settings.BundleName, true);

            if (settings.StringTable == null && updatedData.Table != null)
            {
                settings.SetStringTable(updatedData.Table);
                Debug.Log($"Updated StringTableCollection reference: {updatedData.Table.TableCollectionName}");
            }

            new AssetBundleAssignmentService().AssignAddressableGroupToTables(settings.BundleName, settings.StringTable, null);

            AssetDatabase.SaveAssets();

            settings.Configurator.UpdateAfterTableCreated(settings.StringTable);

            Debug.Log("Text update completed.");
        }

        public async Task UpdateAudioAsync(GoogleLocalizationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var authService = new GoogleAuthService(_credentials);
            var audioSyncService = new GoogleDriveAudioSyncService(authService);
            var audioUpdateService = new AudioLocalizationUpdateService();

            var targetAudioTableName = settings.AudioTable != null ? settings.AudioTable.TableCollectionName : null;
            if (string.IsNullOrEmpty(targetAudioTableName))
            {
                var folderName = await audioSyncService.GetFolderNameAsync(settings.DriveFolderLink);
                targetAudioTableName = !string.IsNullOrWhiteSpace(folderName) ? folderName : settings.name;
            }

            if (settings.AudioTable == null)
            {
                var ensured = audioUpdateService.EnsureAudioAssetTableCollection(
                    null, targetAudioTableName, settings.AudioFolderPath);
                if (ensured != null)
                    settings.SetAudioTable(ensured);
            }

            await audioSyncService.SyncAudioFilesAsync(settings.DriveFolderLink, settings.AudioFolderPath, targetAudioTableName);
            AssetDatabase.Refresh();

            var assetTableData = new AssetTableData
            {
                Table = settings.AudioTable,
                Data = new SheetData { TableName = targetAudioTableName }
            };

            var audioClips = audioUpdateService.LoadAudioClipsFromDirectory(settings.AudioFolderPath);
            var updatedAssetData = audioUpdateService.UpdateAudioTableData(
                assetTableData, audioClips, settings.StringTableFolderPath, settings.BundleName, true);

            if (settings.AudioTable == null && updatedAssetData.Table != null)
            {
                settings.SetAudioTable(updatedAssetData.Table);
                Debug.Log($"Updated AssetTableCollection reference: {updatedAssetData.Table.TableCollectionName}");
            }

            new AssetBundleAssignmentService().ValidateAndAssignGroupToAllAssets(
                settings.BundleName, settings.AudioFolderPath, settings.StringTable, settings.AudioTable);

            AssetDatabase.SaveAssets();
        }

        public void ValidateGroups(GoogleLocalizationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrEmpty(settings.BundleName))
            {
                Debug.LogWarning("BundleName is empty. Set the Addressables group name before running validation.");
                return;
            }

            Debug.Log($"Validating and assigning group '{settings.BundleName}' to all localization files...");

            new AssetBundleAssignmentService().ValidateAndAssignGroupToAllAssets(
                settings.BundleName, settings.AudioFolderPath, settings.StringTable, settings.AudioTable);

            AssetDatabase.SaveAssets();
            Debug.Log("Group validation completed.");
        }
    }
}
