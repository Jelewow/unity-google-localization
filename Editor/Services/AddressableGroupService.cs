using System.Collections.Generic;
using System.IO;
using System.Linq;
using SheetsLocalization.Editor.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;

namespace SheetsLocalization.Editor.Services
{
    /// <summary>
    /// Assigns generated localization tables and their audio clips to an existing Addressables group and label.
    /// </summary>
    public class AddressableGroupService
    {
        public void AssignGroupToTables(string groupName, StringTableCollection stringTableCollection, AssetTableCollection assetTableCollection)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = ResolveGroup(settings, groupName);
            if (group == null)
                return;

            int assignedCount = 0;

            if (stringTableCollection != null)
            {
                assignedCount += AssignGroupToAsset(settings, group, stringTableCollection);
                assignedCount += AssignGroupToAsset(settings, group, stringTableCollection.SharedData);

                foreach (var stringTable in stringTableCollection.StringTables)
                    assignedCount += AssignGroupToAsset(settings, group, stringTable);
            }

            if (assetTableCollection != null)
            {
                assignedCount += AssignGroupToAsset(settings, group, assetTableCollection);
                assignedCount += AssignGroupToAsset(settings, group, assetTableCollection.SharedData);

                foreach (var assetTable in assetTableCollection.AssetTables)
                    assignedCount += AssignGroupToAsset(settings, group, assetTable);
            }

            if (assignedCount > 0)
                Debug.Log($"Addressables: assigned group '{groupName}' to {assignedCount} localization tables");
        }

        public void ValidateAndAssignGroupToAllAssets(string groupName, string audioDirectory,
            StringTableCollection stringTableCollection, AssetTableCollection assetTableCollection)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = ResolveGroup(settings, groupName);
            if (group == null)
                return;

            Debug.Log($"Addressables: validating and assigning group '{groupName}' to all assets");

            AssignGroupToTables(groupName, stringTableCollection, assetTableCollection);

            // Assign the group to the audio clips both referenced by the table (by GUID) and physically
            // present in the audio folder, so the assignment is robust to a stale table or an out-of-sync folder.
            var assignedClipGuids = new HashSet<string>();
            AssignGroupToTableClips(settings, group, groupName, assetTableCollection, assignedClipGuids);
            AssignGroupToFolderClips(settings, group, groupName, audioDirectory, assignedClipGuids);

            if (assignedClipGuids.Count > 0)
                Debug.Log($"Addressables: assigned group '{groupName}' to {assignedClipGuids.Count} audio clips");

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);

            Debug.Log("Addressables: validation and assignment completed");
        }

        private void AssignGroupToTableClips(AddressableAssetSettings settings, AddressableAssetGroup group,
            string groupName, AssetTableCollection assetTableCollection, HashSet<string> assignedGuids)
        {
            if (assetTableCollection == null)
                return;

            foreach (var assetTable in assetTableCollection.AssetTables)
            {
                if (assetTable == null)
                    continue;

                foreach (var entry in assetTable.Values)
                    AssignGroupToClipGuid(settings, group, groupName, entry?.Guid, assignedGuids);
            }
        }

        private void AssignGroupToFolderClips(AddressableAssetSettings settings, AddressableAssetGroup group,
            string groupName, string audioDirectory, HashSet<string> assignedGuids)
        {
            if (string.IsNullOrEmpty(audioDirectory) || !Directory.Exists(audioDirectory))
                return;

            foreach (var pattern in AudioFileTypes.SearchPatterns)
            {
                foreach (var file in Directory.GetFiles(audioDirectory, pattern, SearchOption.AllDirectories))
                {
                    var guid = AssetDatabase.AssetPathToGUID(file.Replace('\\', '/'));
                    AssignGroupToClipGuid(settings, group, groupName, guid, assignedGuids);
                }
            }
        }

        private void AssignGroupToClipGuid(AddressableAssetSettings settings, AddressableAssetGroup group,
            string groupName, string guid, HashSet<string> assignedGuids)
        {
            if (string.IsNullOrEmpty(guid) || !assignedGuids.Add(guid))
                return;

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
                return;

            entry.address = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            UpdateEntryLabels(entry, groupName);
        }

        private AddressableAssetGroup ResolveGroup(AddressableAssetSettings settings, string groupName)
        {
            if (settings == null)
            {
                Debug.LogError("Failed to get Addressables settings.");
                return null;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogWarning($"Addressables group '{groupName}' not found. Assignment skipped.");
                return null;
            }

            if (!settings.GetLabels().Contains(groupName))
                settings.AddLabel(groupName);

            return group;
        }

        private int AssignGroupToAsset(AddressableAssetSettings settings, AddressableAssetGroup group, Object asset)
        {
            if (asset == null) return 0;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return 0;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return 0;

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null) return 0;

            entry.address = asset.name;
            UpdateEntryLabels(entry, group.Name);
            return 1;
        }

        private void UpdateEntryLabels(AddressableAssetEntry entry, string newLabel)
        {
            var labelsToRemove = entry.labels
                .Where(label => label != newLabel && !label.StartsWith("Locale-"))
                .ToList();

            foreach (var label in labelsToRemove)
                entry.SetLabel(label, false);

            entry.SetLabel(newLabel, true);
        }
    }
}
