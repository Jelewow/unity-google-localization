using System.Collections.Generic;

namespace SheetsLocalization.Editor.Types
{
    public struct RawSheetData
    {
        public string TableKeyPrefix;
        public string SheetName;
        public List<List<string>> Rows;
        public List<string> Headers => Rows?.Count > 0 ? Rows[0] : new List<string>();
        public List<List<string>> DataRows => Rows?.Count > 1 ? Rows.GetRange(1, Rows.Count - 1) : new List<List<string>>();
    }
}
