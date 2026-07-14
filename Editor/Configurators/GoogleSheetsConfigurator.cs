using SheetsLocalization.Editor.Types;
using UnityEditor.Localization;
using UnityEngine;

namespace SheetsLocalization.Editor.Configurators
{
    /// <summary>
    /// Base class for pluggable Google Sheets parsers. Derive from it to support
    /// custom sheet layouts and post-processing after the table is created.
    /// </summary>
    public abstract class GoogleSheetsConfigurator : ScriptableObject
    {
        /// <summary>Human-readable description of the expected sheet layout, shown in the inspector.</summary>
        public abstract string SchemeHint { get; }

        /// <summary>Converts raw sheet rows into a keyed, per-locale table.</summary>
        public abstract SheetData ParseSheetData(RawSheetData rawData);

        /// <summary>Validates the raw sheet before parsing. Should throw with a clear message on failure.</summary>
        public abstract bool ValidateData(RawSheetData rawData);

        /// <summary>Optional hook invoked after the localization table has been created/updated.</summary>
        public virtual void UpdateAfterTableCreated(StringTableCollection stringTableCollection)
        {
            Debug.Log($"{GetType().Name} finished.");
        }
    }
}
