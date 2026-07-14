using UnityEngine;

namespace SheetsLocalization.Editor.Utils
{
    /// <summary>
    /// Maps ISO-like locale codes (e.g. "en", "zh-cn") to Unity <see cref="SystemLanguage"/> values.
    /// </summary>
    public static class LocaleCodeMapper
    {
        public static SystemLanguage ToSystemLanguage(string localeCode)
        {
            switch (localeCode.Trim().ToLower())
            {
                case "en": return SystemLanguage.English;
                case "ru": return SystemLanguage.Russian;
                case "de": return SystemLanguage.German;
                case "fr": return SystemLanguage.French;
                case "es": return SystemLanguage.Spanish;
                case "it": return SystemLanguage.Italian;
                case "ja": return SystemLanguage.Japanese;
                case "ko": return SystemLanguage.Korean;
                case "zh":
                case "zh-cn": return SystemLanguage.ChineseSimplified;
                case "zh-tw": return SystemLanguage.ChineseTraditional;
                case "pt": return SystemLanguage.Portuguese;
                case "nl": return SystemLanguage.Dutch;
                case "sv": return SystemLanguage.Swedish;
                case "da": return SystemLanguage.Danish;
                case "no": return SystemLanguage.Norwegian;
                case "fi": return SystemLanguage.Finnish;
                case "pl": return SystemLanguage.Polish;
                case "tr": return SystemLanguage.Turkish;
                case "ar": return SystemLanguage.Arabic;
                case "he": return SystemLanguage.Hebrew;
                case "hi": return SystemLanguage.Hindi;
                case "th": return SystemLanguage.Thai;
                case "vi": return SystemLanguage.Vietnamese;
                case "cs": return SystemLanguage.Czech;
                case "hu": return SystemLanguage.Hungarian;
                case "ro": return SystemLanguage.Romanian;
                case "bg": return SystemLanguage.Bulgarian;
                case "uk": return SystemLanguage.Ukrainian;
                case "ca": return SystemLanguage.Catalan;
                case "sl": return SystemLanguage.Slovenian;
                case "sk": return SystemLanguage.Slovak;
                case "lv": return SystemLanguage.Latvian;
                case "lt": return SystemLanguage.Lithuanian;
                case "et": return SystemLanguage.Estonian;
                default:
                    Debug.LogWarning($"Unknown locale code: {localeCode}. Falling back to English.");
                    return SystemLanguage.English;
            }
        }
    }
}
