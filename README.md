# Sheets Localization Sync

[![openupm](https://img.shields.io/npm/v/com.jelewow.unity-sheets-localization?label=openupm&registry_uri=https://package.openupm.com&color=3068b7)](https://openupm.com/packages/com.jelewow.unity-sheets-localization/)
![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-9.0-239120?logo=csharp&logoColor=white)
![Google](https://img.shields.io/badge/Google-Sheets%20%26%20Drive-4285F4?logo=google&logoColor=white)
![Unity Localization](https://img.shields.io/badge/Unity-Localization-black?logo=unity&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-blue)

A Unity **Editor** tool that imports and synchronizes localized **strings** (from Google Sheets) and **audio** (from Google Drive) directly into [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@latest) tables, with optional [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) group/label assignment.

It turns a spreadsheet-driven localization workflow (translators edit Google Sheets, voice-over lands in a Drive folder) into a one-click sync inside the Unity Editor.

> This package contains Editor tooling only. At runtime you use Unity Localization as usual — this tool just fills the tables for you.

## Features

- Import localized strings from **Google Sheets** into `StringTableCollection`s.
- Sync localized audio from **Google Drive** folders into `AssetTableCollection`s (MD5 change detection and safe-delete guards).
- **Incremental** updates: add new keys, update changed values, optionally remove obsolete entries.
- **Pluggable parsing**: derive from `GoogleSheetsConfigurator` — a plain `[Serializable]` class picked inline on the settings asset (`[SerializeReference]`), no extra asset to manage.
- Two authentication modes: **API key** (public documents) or **service account** (private documents, RS256 JWT flow implemented without external crypto libraries).
- **Secrets stay out of source control**: credentials and default paths live per-project in `EditorPrefs`, never in a committed asset.
- Optional **Addressables** group and label assignment, selected from a dropdown of existing groups.
- Auto-creates missing `Locale` assets from ISO-like locale codes.
- **No paid dependencies** — no Odin Inspector; the UI is built with Unity's IMGUI.

## Requirements

- Unity **2022.3** or newer (works with Unity 6).
- Packages (installed automatically as dependencies):
  - `com.unity.localization`
  - `com.unity.addressables`
  - `com.unity.nuget.newtonsoft-json`

## Installation

### Via OpenUPM (recommended — shows updates in Package Manager)

Installing through the [OpenUPM](https://openupm.com) scoped registry lets Unity's Package Manager notify you about new versions (Git URL installs don't).

With the [openupm-cli](https://openupm.com/docs/getting-started.html):

```
openupm add com.jelewow.unity-sheets-localization
```

Or add the scoped registry to `Packages/manifest.json` manually:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.jelewow"]
    }
  ],
  "dependencies": {
    "com.jelewow.unity-sheets-localization": "1.0.2"
  }
}
```

Versions **1.0.2+** are published as **Unity-signed** tarballs via GitHub Releases (no unsigned-package warning in Unity 6.3+).

### Maintainer release (signed)

1. Add GitHub repository secrets: `UPM_SERVICE_ACCOUNT_KEY_ID`, `UPM_SERVICE_ACCOUNT_KEY_SECRET`, `UPM_ORG_ID` (Unity service account with package signing permission).
2. Bump `package.json` version, commit to `main`.
3. Push a version tag: `git tag v1.0.2 && git push origin v1.0.2`
4. CI packs with `upm pack`, attaches signed `.tgz` to the GitHub Release, then waits for OpenUPM.

### Via Git URL (quick, but no update notifications)

`Window > Package Manager > + > Add package from git URL...` and paste:

```
https://github.com/Jelewow/unity-google-localization.git
```

Pin a specific version by appending a release tag, e.g. `...unity-google-localization.git#v1.0.0`.

## Quick start

1. Open **`Tools > Sheets Localization > Settings`** and fill in:
   - **Credentials** — API key, or service account email + JSON key path (see below).
   - **Default output paths** — where generated string tables and audio go by default.
2. Create a settings asset: `Assets > Create > Sheets Localization > Localization Settings`.
3. Select the asset and set:
   - **Google Sheets link** (and optionally **Google Drive folder link** for audio).
   - **Configurator** — pick one from the dropdown (built-in `Default Column Configurator`, or your own).
   - **Addressable group** — pick an existing group from the dropdown (optional).
   - Path **overrides** — only if this asset needs folders different from the defaults.
4. Click **Test authentication**, then **Update texts** and/or **Update audio**.

## Sheet layout

The built-in `Default Column Configurator` expects the first row to be a header:

| id        | en           | de              | ru      |
|-----------|--------------|-----------------|---------|
| greeting  | Hello        | Hallo           | Привет  |
| farewell  | Goodbye      | Auf Wiedersehen | Пока    |

- Column 1 is the **entry id**.
- Every other column header is a **locale code** (`en`, `de`, `ru`, `zh-cn`, ...).
- Keys are prefixed with the (lowercased) spreadsheet title.
- Rows whose id starts with `#` are treated as comments and skipped.

### Audio naming

Audio files in the Drive folder must be named `key_locale`, e.g. `greeting_en.mp3`, `greeting_de.mp3` (region codes like `greeting_zh-cn.mp3` are supported). They are grouped by key into an `AssetTableCollection`.

## Credentials & paths

Credentials and default output paths are stored per-project in `EditorPrefs` via the **`Tools > Sheets Localization > Settings`** window — nothing is written to a committed asset.

- **API key** — works for **publicly shared** spreadsheets/folders (`Anyone with the link`). Tick *Use API key* and paste the key.
- **Service account** — required for **private** documents. Untick *Use API key*, set the service account email and the path to its JSON key file, then share the spreadsheet/folder with that email.

> Security: never commit real API keys or service account JSON. Keep key files outside the repo. Grant the service account the minimum scopes (`spreadsheets.readonly`, `drive.readonly`).

## Output

- **Addressable group** — choose an existing Addressables group from the dropdown; the generated tables, shared data and audio clips are added to it and labelled. Leave it as `(None)` to skip.
- **Paths** — each settings asset uses the window's default paths unless you tick *Override* and provide a custom folder.

## Extending: custom configurator

Derive from `GoogleSheetsConfigurator`. Because it's a plain `[Serializable]` class, once the file compiles it appears in the **Configurator** dropdown on the settings asset — no asset to create. See the **Custom Configurator Example** sample (import it from the Package Manager) for a complete, commented template.

```csharp
using System;
using System.Collections.Generic;
using SheetsLocalization.Editor.Configurators;
using SheetsLocalization.Editor.Types;
using UnityEngine;

[Serializable]
public class MyConfigurator : GoogleSheetsConfigurator
{
    [SerializeField] private string[] locales = { "en", "de" }; // your own inspector fields

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
- Root namespace: `SheetsLocalization.Editor` (`.Services`, `.Configurators`, `.Types`, `.Settings`, `.Credentials`, `.Inspectors`, `.Windows`).

## License

[MIT](LICENSE.md)
