using System;
using SheetsLocalization.Editor.Types;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SheetsLocalization.Editor.Services
{
    public class LocalizationUpdateService
    {
        public StringTableData UpdateProjectData(StringTableData stringTableData, string tablePath, string bundleName = null, bool removeObsoleteEntries = false)
        {
            var updatedData = UpdateLocalizedTable(stringTableData, tablePath, bundleName, removeObsoleteEntries);
            Debug.Log("Localization updated successfully.");
            return updatedData;
        }

        private StringTableData UpdateLocalizedTable(StringTableData stringTableData, string tablePath, string bundleName = null, bool removeObsoleteEntries = false)
        {
            if (stringTableData.Data.Table == null)
                return stringTableData;

            var sheetData = stringTableData.Data.Table;
            var stringTableCollection = stringTableData.Table;

            if (stringTableCollection == null)
            {
                stringTableCollection = CreateNewStringTableCollection(tablePath, stringTableData.Data.TableName);
                if (stringTableCollection == null)
                {
                    throw new Exception("Failed to create a new StringTableCollection");
                }

                Debug.Log($"Created new StringTableCollection: {stringTableCollection.TableCollectionName}");
            }

            var updateResults = UpdateCollectionIncrementally(stringTableCollection, sheetData, tablePath, removeObsoleteEntries);
            Debug.Log($"Update finished: {updateResults.AddedEntries} added, {updateResults.UpdatedEntries} updated, {updateResults.RemovedEntries} removed");

            EditorUtility.SetDirty(stringTableCollection.SharedData);
            foreach (var table in stringTableCollection.StringTables)
            {
                EditorUtility.SetDirty(table);
            }

            if (!string.IsNullOrEmpty(bundleName))
            {
                var bundleAssignmentService = new AssetBundleAssignmentService();
                bundleAssignmentService.AssignAddressableGroupToTables(bundleName, stringTableCollection, null);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new StringTableData
            {
                Table = stringTableCollection,
                Data = stringTableData.Data
            };
        }

        private EntryUpdateResult UpdateCollectionIncrementally(StringTableCollection collection, Dictionary<string, Dictionary<string, string>> newData, string tablePath, bool removeObsoleteEntries = false)
        {
            var results = new EntryUpdateResult();
            var existingKeys = new HashSet<string>();
            if (collection.SharedData.Entries != null)
            {
                foreach (var entry in collection.SharedData.Entries)
                {
                    existingKeys.Add(entry.Key);
                }
            }

            var newKeys = new HashSet<string>(newData.Keys);
            foreach (var sheetEntry in newData)
            {
                var entryKey = sheetEntry.Key;
                var localeData = sheetEntry.Value;
                var isNewEntry = !existingKeys.Contains(entryKey);
                var wasEntryUpdated = false;

                foreach (var localeEntry in localeData)
                {
                    var localeCode = localeEntry.Key;
                    var newValue = localeEntry.Value;

                    if (string.IsNullOrEmpty(newValue))
                        continue;

                    var stringTable = GetOrCreateStringTableForLocale(collection, localeCode, tablePath);
                    if (stringTable == null)
                    {
                        Debug.LogWarning($"Failed to get or create StringTable for locale '{localeCode}'");
                        continue;
                    }

                    var entry = stringTable.GetEntry(entryKey);
                    if (entry == null)
                    {
                        stringTable.AddEntry(entryKey, newValue);
                        if (isNewEntry)
                        {
                            wasEntryUpdated = true;
                        }
                    }
                    else
                    {
                        if (entry.Value != newValue)
                        {
                            var oldValue = entry.Value;
                            entry.Value = newValue;
                            wasEntryUpdated = true;
                            Debug.Log($"Updated entry '{entryKey}' for locale '{localeCode}': '{oldValue}' -> '{newValue}'");
                        }
                    }
                }

                if (isNewEntry && wasEntryUpdated)
                {
                    results.AddedEntries++;
                    Debug.Log($"Added new entry: '{entryKey}'");
                }
                else if (!isNewEntry && wasEntryUpdated)
                {
                    results.UpdatedEntries++;
                }
            }

            if (removeObsoleteEntries)
            {
                var keysToRemove = existingKeys.Except(newKeys).ToList();
                foreach (var keyToRemove in keysToRemove)
                {
                    RemoveEntryFromCollection(collection, keyToRemove);
                    results.RemovedEntries++;
                    Debug.Log($"Removed obsolete entry: '{keyToRemove}'");
                }
            }
            else if (existingKeys.Except(newKeys).Any())
            {
                var obsoleteKeys = existingKeys.Except(newKeys).ToList();
                Debug.LogWarning($"Found {obsoleteKeys.Count} obsolete entries, but removal is disabled.");
            }

            return results;
        }

        private void RemoveEntryFromCollection(StringTableCollection collection, string key)
        {
            if (collection == null || string.IsNullOrEmpty(key))
                return;

            collection.RemoveEntry(key);
            foreach (var stringTable in collection.StringTables)
            {
                var entry = stringTable.GetEntry(key);
                if (entry != null)
                {
                    stringTable.RemoveEntry(key);
                    EditorUtility.SetDirty(stringTable);
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
        }

        private StringTable GetOrCreateStringTableForLocale(StringTableCollection collection, string localeCode, string tablePath)
        {
            foreach (var stringTable in collection.StringTables)
            {
                if (string.Equals(stringTable.LocaleIdentifier.Code.Trim(), localeCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return stringTable;
                }
            }

            var locale = GetOrCreateLocale(localeCode.Trim(), tablePath);
            if (locale == null)
            {
                Debug.LogError($"Failed to create locale for code '{localeCode}'");
                return null;
            }

            var newTable = collection.AddNewTable(locale.Identifier) as StringTable;
            if (newTable != null)
            {
                Debug.Log($"Created new StringTable for locale '{localeCode}' in collection '{collection.TableCollectionName}'");
                EditorUtility.SetDirty(newTable);
                EditorUtility.SetDirty(collection.SharedData);
            }

            return newTable;
        }

        private Locale GetOrCreateLocale(string localeCode, string tablePath)
        {
            localeCode = localeCode.Trim();
            var existingLocales = LocalizationEditorSettings.GetLocales();
            foreach (var locale in existingLocales)
            {
                if (string.Equals(locale.Identifier.Code, localeCode, StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            try
            {
                var systemLanguage = LocaleCodeMapper.ToSystemLanguage(localeCode);
                var newLocale = Locale.CreateLocale(systemLanguage);

                var localesPath = Path.Combine(tablePath, "../Locales");
                var localePath = $"{localesPath}/{localeCode}.asset";
                Directory.CreateDirectory(Path.GetDirectoryName(localePath));
                AssetDatabase.CreateAsset(newLocale, localePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LocalizationEditorSettings.AddLocale(newLocale);

                Debug.Log($"Created new locale: {localeCode} ({newLocale.LocaleName})");
                return newLocale;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create locale '{localeCode}': {ex.Message}");
                return null;
            }
        }

        private StringTableCollection CreateNewStringTableCollection(string tablePath, string tableName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(tablePath) && !Path.IsPathRooted(tablePath))
                {
                    Debug.LogError($"Failed to create table, the path is empty or invalid - {tablePath}");
                    return null;
                }
                var collectionName = string.IsNullOrEmpty(tableName) ? "AutoGeneratedLocalizationTable" : tableName;
                Directory.CreateDirectory(tablePath);
                var collection = LocalizationEditorSettings.CreateStringTableCollection(collectionName, tablePath);

                if (collection != null)
                {
                    Debug.Log($"Created new StringTableCollection: {collectionName} in {tablePath}");
                    EditorUtility.SetDirty(collection);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                return collection;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create StringTableCollection: {ex.Message}");
                return null;
            }
        }
    }
}
