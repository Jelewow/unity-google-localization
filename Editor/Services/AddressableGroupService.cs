using System.IO;
using System.Linq;
using SheetsLocalization.Editor.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Localization;
using UnityEngine;
using System.Collections.Generic;

namespace SheetsLocalization.Editor.Services
{
    /// <summary>
    /// Assigns generated localization tables and audio clips to an existing Addressables group and label.
    /// </summary>
    public class AddressableGroupService
    {
        public void AssignGroupToTables(string groupName, StringTableCollection stringTableCollection, AssetTableCollection assetTableCollection)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Failed to get Addressables settings.");
                return;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogWarning($"Addressables group '{groupName}' not found. Tables were not added to the group.");
                return;
            }

            if (!settings.GetLabels().Contains(groupName))
            {
                settings.AddLabel(groupName);
            }

            int assignedCount = 0;

            if (stringTableCollection != null)
            {
                assignedCount += AssignGroupToAsset(settings, group, stringTableCollection);
                assignedCount += AssignGroupToAsset(settings, group, stringTableCollection.SharedData);

                foreach (var stringTable in stringTableCollection.StringTables)
                {
                    if (stringTable != null)
                        assignedCount += AssignGroupToAsset(settings, group, stringTable);
                }
            }

            if (assetTableCollection != null)
            {
                assignedCount += AssignGroupToAsset(settings, group, assetTableCollection);
                assignedCount += AssignGroupToAsset(settings, group, assetTableCollection.SharedData);

                foreach (var assetTable in assetTableCollection.AssetTables)
                {
                    if (assetTable != null)
                        assignedCount += AssignGroupToAsset(settings, group, assetTable);
                }
            }

            if (assignedCount > 0)
                Debug.Log($"Addressables: assigned group '{groupName}' to {assignedCount} localization tables");
        }

        public void ValidateAndAssignGroupToAllAssets(string groupName, string audioDirectory,
            StringTableCollection stringTableCollection, AssetTableCollection assetTableCollection)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            Debug.Log($"Addressables: validating and assigning group '{groupName}' to all assets");

            AssignGroupToAudioFiles(groupName, audioDirectory);
            AssignGroupToTables(groupName, stringTableCollection, assetTableCollection);

            AddressableAssetSettingsDefaultObject.GetSettings(true)
                .SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);

            Debug.Log("Addressables: validation and assignment completed");
        }

        private void AssignGroupToAudioFiles(string groupName, string audioDirectory)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            if (string.IsNullOrEmpty(audioDirectory) || !Directory.Exists(audioDirectory))
            {
                Debug.LogError($"Addressables: audio directory not found or empty: {audioDirectory}");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Failed to get Addressables settings.");
                return;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.Log($"Addressables group '{groupName}' not found. Creating a new group.");
                group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            if (!settings.GetLabels().Contains(groupName))
            {
                settings.AddLabel(groupName);
            }

            int assignedCount = 0;

            foreach (var pattern in AudioFileTypes.SearchPatterns)
            {
                var files = Directory.GetFiles(audioDirectory, pattern, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var assetPath = file.Replace('\\', '/');
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (string.IsNullOrEmpty(guid)) continue;

                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    if (entry != null)
                    {
                        entry.address = Path.GetFileNameWithoutExtension(assetPath);
                        UpdateEntryLabels(entry, groupName);
                        assignedCount++;
                    }
                }
            }

            if (assignedCount > 0)
                Debug.Log($"Addressables: assigned group '{groupName}' to {assignedCount} audio files");
        }

        private int AssignGroupToAsset(AddressableAssetSettings settings, AddressableAssetGroup group, Object asset)
        {
            if (asset == null) return 0;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return 0;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return 0;

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
            {
                entry.address = asset.name;
                UpdateEntryLabels(entry, group.Name);
                return 1;
            }
            return 0;
        }

        private void UpdateEntryLabels(AddressableAssetEntry entry, string newLabel)
        {
            var labelsToRemove = new List<string>();
            foreach (var label in entry.labels)
            {
                if (label != newLabel && !label.StartsWith("Locale-"))
                    labelsToRemove.Add(label);
            }

            foreach (var label in labelsToRemove)
                entry.SetLabel(label, false);

            entry.SetLabel(newLabel, true);
        }
    }
}
