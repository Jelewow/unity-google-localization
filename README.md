# Sheets Localization Sync

A Unity **Editor** tool that imports and synchronizes localized **strings** (from Google Sheets) and **audio** (from Google Drive) directly into [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@latest) tables, with optional [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) group/label assignment.

It turns a spreadsheet-driven localization workflow (translators edit Google Sheets, voice-over lands in a Drive folder) into a one-click sync inside the Unity Editor.

> This package contains Editor tooling only. At runtime you use Unity Localization as usual — this tool just fills the tables for you.

## Features

- Import localized strings from **Google Sheets** into `StringTableCollection`s.
- Sync localized audio from **Google Drive** folders into `AssetTableCollection`s (with MD5 change detection and safe-delete guards).
- **Incremental** updates: add new keys, update changed values, optionally remove obsolete entries.
- **Pluggable parsing**: derive from `GoogleSheetsConfigurator` (a `ScriptableObject`) to support any sheet layout and run custom post-processing.
- Two built-in configurators: `StandardColumnConfigurator` and `ScenarioColumnConfigurator`.
- Two authentication modes: **API key** (public documents) or **service account** (private documents, RS256 JWT flow implemented without external crypto libraries).
- Optional **Addressables** group and label assignment for the generated tables and audio clips.
- Auto-creates missing `Locale` assets from ISO-like locale codes.
- **No paid dependencies** — no Odin Inspector; the inspector UI is built with Unity's built-in IMGUI.

## Requirements

- Unity **2022.3** or newer (works with Unity 6).
- Packages (installed automatically as dependencies):
  - `com.unity.localization`
  - `com.unity.addressables`
  - `com.unity.nuget.newtonsoft-json`

## Installation

Add it via the Package Manager using a Git URL:

1. `Window > Package Manager > + > Add package from git URL...`
2. Paste:

```
https://github.com/Jelewow/unity-google-localization.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jelewow.unity-sheets-localization": "https://github.com/Jelewow/unity-google-localization.git"
  }
}
```

## Quick start

1. Create a settings asset: `Assets > Create > Sheets Localization > Localization Settings`.
2. Create a configurator asset: `Assets > Create > Sheets Localization > Configurators > Standard Column Configurator`.
3. In the settings asset:
   - Fill in **credentials** (API key or service account — see below).
   - Set **Google Sheets Link** (and optionally **Google Drive Folder Link** for audio).
   - Assign the **configurator**.
   - Set **Local Table Folder Path** (where the generated tables live, e.g. `Assets/Localization/MyScene`).
   - (Optional) Set **Bundle Name** to an existing Addressables group name to auto-assign tables/audio.
4. Click **Test authentication**, then **Update texts** and/or **Update audio files**.

## Sheet layout

The built-in configurators expect the first row to be a header.

`StandardColumnConfigurator` / `ScenarioColumnConfigurator`:

| id        | en           | de            | ru          |
|-----------|--------------|---------------|-------------|
| greeting  | Hello        | Hallo         | Привет      |
| farewell  | Goodbye      | Auf Wiedersehen | Пока      |

- Column 1 is the **entry key**.
- Every other column header is a **locale code** (`en`, `de`, `ru`, `zh-cn`, ...).
- `ScenarioColumnConfigurator` additionally prefixes keys with the table key prefix and skips rows whose id starts with `#` (comment rows).

### Audio naming

Audio files in the Drive folder must be named `key_locale`, e.g. `greeting_en.mp3`, `greeting_de.mp3`. They are grouped by key into an `AssetTableCollection`.

## Authentication

Credentials are stored on the settings asset (no secrets are hardcoded in the package).

- **API key** — works for publicly shared spreadsheets/folders. Set `Auth Type = ApiKey` and paste your key.
- **Service account** — required for private documents. Set `Auth Type = ServiceAccount`, fill the service account email and either the path to the JSON key file or the inline JSON. Share the spreadsheet/folder with the service account email.

> Security: never commit real API keys or service account JSON to source control. Keep key files outside the repo (the sample `.gitignore` already excludes common key file names). Grant the service account the minimum required scopes (`spreadsheets.readonly`, `drive.readonly`).

## Extending: custom configurator

Derive from `GoogleSheetsConfigurator` to parse any layout. See the **Custom Configurator Example** sample (import it from the Package Manager) for a complete, commented template.

```csharp
using System.Collections.Generic;
using SheetsLocalization.Editor.Configurators;
using SheetsLocalization.Editor.Types;
using UnityEngine;

[CreateAssetMenu(menuName = "Sheets Localization/Configurators/My Configurator")]
public class MyConfigurator : GoogleSheetsConfigurator
{
    public override string SchemeHint => "id | en | de";

    public override bool ValidateData(RawSheetData rawData) => rawData.Rows != null && rawData.Rows.Count >= 2;

    public override SheetData ParseSheetData(RawSheetData rawData)
    {
        var table = new Dictionary<string, Dictionary<string, string>>();
        var headers = rawData.Headers;
        foreach (var row in rawData.DataRows)
        {
            if (row.Count == 0 || string.IsNullOrEmpty(row[0])) continue;
            var perLocale = new Dictionary<string, string>();
            for (int c = 1; c < headers.Count && c < row.Count; c++)
                if (!string.IsNullOrEmpty(row[c])) perLocale[headers[c].Trim()] = row[c];
            table[row[0]] = perLocale;
        }
        return new SheetData { Table = table, TableName = $"{rawData.TableKeyPrefix}_{rawData.SheetName}" };
    }
}
```

## Assembly / namespaces

- Assembly: `SheetsLocalization.Editor` (Editor platform only).
- Root namespace: `SheetsLocalization.Editor` (`.Services`, `.Configurators`, `.Types`, `.Settings`, `.Credentials`, `.Inspectors`).

## License

[MIT](LICENSE.md)
