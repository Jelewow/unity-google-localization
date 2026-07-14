using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SheetsLocalization.Editor.Types;
using Exception = System.Exception;

namespace SheetsLocalization.Editor.Configurators
{
    [CreateAssetMenu(menuName = "Sheets Localization/Configurators/Standard Column Configurator")]
    public class StandardColumnConfigurator : GoogleSheetsConfigurator
    {
        public override string SchemeHint => "Standard parsing with columns: id | de | en | ...\n" +
                                             "The first column is the entry key, the remaining columns are locale codes.";

        public override SheetData ParseSheetData(RawSheetData rawData)
        {
            if (rawData.Rows == null || rawData.Rows.Count < 2)
                throw new Exception("The sheet must contain a header and at least one data row");

            var headers = rawData.Headers;
            var result = new Dictionary<string, Dictionary<string, string>>();

            foreach (var row in rawData.DataRows)
            {
                if (row.Count == 0 || string.IsNullOrEmpty(row[0]))
                    continue;

                var key = row[0];
                var localeMap = new Dictionary<string, string>();

                for (int c = 1; c < headers.Count; c++)
                {
                    var locale = headers[c].Trim();
                    var value = c < row.Count ? row[c] : string.Empty;

                    if (!string.IsNullOrEmpty(value))
                    {
                        localeMap[locale] = value;
                    }
                }

                if (localeMap.Any())
                {
                    result[key] = localeMap;
                }
            }

            return new SheetData
            {
                Table = result,
                TableName = string.Concat(rawData.TableKeyPrefix, "_", rawData.SheetName)
            };
        }

        public override bool ValidateData(RawSheetData rawData)
        {
            if (rawData.Rows == null || rawData.Rows.Count == 0)
                throw new Exception("Sheet data is missing");

            if (rawData.Rows.Count < 2)
                throw new Exception("The sheet must contain a header and at least one data row");

            var headers = rawData.Headers;
            if (headers.Count < 2)
                throw new Exception("The sheet must contain at least 2 columns (ID + at least one locale)");

            var hasValidData = rawData.DataRows.Any(row =>
                row.Count > 0 &&
                !string.IsNullOrEmpty(row[0]) &&
                row.Skip(1).Any(cell => !string.IsNullOrEmpty(cell))
            );

            if (!hasValidData)
                throw new Exception("No rows with valid data found (ID + at least one localized value)");

            return true;
        }
    }
}
