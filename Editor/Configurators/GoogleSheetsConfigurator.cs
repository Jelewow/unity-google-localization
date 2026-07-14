using System;
using SheetsLocalization.Editor.Types;
using UnityEditor.Localization;

namespace SheetsLocalization.Editor.Configurators
{
    /// <summary>
    /// Base class for pluggable Google Sheets parsers. Derive from it to support custom sheet
    /// layouts and post-processing. It is a plain <c>[Serializable]</c> class (not a ScriptableObject),
    /// selected inline on the settings asset via <c>[SerializeReference]</c> — no separate asset to manage.
    /// </summary>
    [Serializable]
    public abstract class GoogleSheetsConfigurator
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
        }
    }
}
