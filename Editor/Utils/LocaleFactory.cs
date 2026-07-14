using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace SheetsLocalization.Editor.Utils
{
    /// <summary>
    /// Resolves a locale by code, creating and registering a new <see cref="Locale"/> asset when missing.
    /// Shared by the string and audio update pipelines so locale creation stays consistent.
    /// </summary>
    public static class LocaleFactory
    {
        public static Locale GetOrCreateLocale(string localeCode, string tablePath)
        {
            localeCode = localeCode.Trim();

            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                if (string.Equals(locale.Identifier.Code, localeCode, StringComparison.OrdinalIgnoreCase))
                    return locale;
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
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create locale '{localeCode}': {ex.Message}");
                return null;
            }
        }
    }
}
