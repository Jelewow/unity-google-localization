using System;
using System.Collections.Generic;
using System.Linq;
using SheetsLocalization.Editor.Configurators;
using SheetsLocalization.Editor.Types;
using UnityEngine;

namespace SheetsLocalization.Samples
{
    /// <summary>
    /// Example configurator that reads columns named "<locale>-<suffix>" (e.g. "en-name", "de-name",
    /// "en-description") and produces entry keys "<id>-<suffix>". Useful when one spreadsheet row
    /// describes an object with several localized fields.
    ///
    /// Import this sample from the Package Manager and adapt it to your own sheet layout.
    /// </summary>
    [CreateAssetMenu(menuName = "Sheets Localization/Configurators/Samples/Suffix Column Configurator")]
    public class SuffixColumnConfigurator : GoogleSheetsConfigurator
    {
        [SerializeField] private string[] locales = { "en", "de" };

        public override string SchemeHint =>
            "Columns: id | en-name | de-name | en-description | de-description | ...\n" +
            "Produces keys '<id>-<suffix>' per locale. Rows whose id starts with '#' are skipped.";

        public override bool ValidateData(RawSheetData rawData)
        {
            if (rawData.Rows == null || rawData.Rows.Count < 2)
                throw new Exception("The sheet must contain a header and at least one data row");

            if (!rawData.Headers.Any(h => h.Trim().Equals("id", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Required column 'id' not found");

            if (!rawData.Headers.Any(h => locales.Any(l => h.Trim().StartsWith(l + "-"))))
                throw new Exception("No localized columns found (expected '<locale>-<suffix>')");

            return true;
        }

        public override SheetData ParseSheetData(RawSheetData rawData)
        {
            var headers = rawData.Headers;
            var idColumn = headers.FindIndex(h => h.Trim().Equals("id", StringComparison.OrdinalIgnoreCase));

            var localeColumns = new List<(int index, string locale, string suffix)>();
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].Trim();
                foreach (var locale in locales)
                {
                    var prefix = locale + "-";
                    if (header.StartsWith(prefix))
                    {
                        localeColumns.Add((i, locale, header.Substring(prefix.Length)));
                    }
                }
            }

            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var row in rawData.DataRows)
            {
                if (row.Count <= idColumn || string.IsNullOrEmpty(row[idColumn]) || row[idColumn][0] == '#')
                    continue;

                var baseId = row[idColumn];
                foreach (var (index, locale, suffix) in localeColumns)
                {
                    if (index >= row.Count || string.IsNullOrEmpty(row[index]))
                        continue;

                    var key = $"{baseId}-{suffix}";
                    if (!result.TryGetValue(key, out var perLocale))
                    {
                        perLocale = new Dictionary<string, string>();
                        result[key] = perLocale;
                    }
                    perLocale[locale] = row[index];
                }
            }

            return new SheetData
            {
                Table = result,
                TableName = string.Concat(rawData.TableKeyPrefix, "_", rawData.SheetName)
            };
        }
    }
}
