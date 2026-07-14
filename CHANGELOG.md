# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.1] - 2026-07-14

### Fixed

- Settings inspector no longer clears the Addressables group or swaps the configurator on repaint when the stored value isn't in the dropdown (writes only on an explicit change).
- Guard against a null configurator when opening the Parsing section.
- Migrate legacy assets: the old `bundleName` and folder paths are preserved via `[FormerlySerializedAs]`.

## [0.2.0] - 2026-07-14

### Changed

- Configurators are now plain `[Serializable]` classes selected inline on the settings asset (`[SerializeReference]`) instead of ScriptableObject assets.
- Renamed the built-in configurator to `DefaultColumnConfigurator`.
- The Addressables group is chosen from a dropdown of existing groups instead of a free-text field.
- Default output paths and credentials moved to the unified `Tools > Sheets Localization > Settings` window; each asset can override paths individually.

### Removed

- `StandardColumnConfigurator` (use `DefaultColumnConfigurator`).
- Manual table key prefix options — the spreadsheet title is always used as the prefix.
- AssetBundle terminology: `AssetBundleAssignmentService` is now `AddressableGroupService`.

### Added

- Per-project default output folders (string tables / audio) with per-asset override toggles.

## [0.1.1] - 2026-07-14

### Added

- `Tools > Sheets Localization > Credentials` window to enter API key / service account without hardcoding.
- Per-project credential storage in `EditorPrefs` (`CredentialsStore`); secrets never touch source control.

### Changed

- Moved credentials out of the settings asset; sync orchestration extracted into `LocalizationSyncService`.
- Google API errors now include the raw server response so 403/40x causes are visible.

### Fixed

- Escape the sheet name in the Sheets API URL (sheets with spaces failed).
- Widen the audio locale suffix parser to accept region codes (e.g. `zh-cn`).

## [0.1.0] - 2026-07-14

### Added

- Import localized strings from Google Sheets into Unity Localization `StringTableCollection`s.
- Sync localized audio from Google Drive folders into `AssetTableCollection`s.
- Google authentication via API key or service account (RS256 JWT flow).
- Pluggable, ScriptableObject-based parsing pipeline (`GoogleSheetsConfigurator`).
- Built-in configurators: `StandardColumnConfigurator`, `ScenarioColumnConfigurator`.
- Optional Addressables group/label assignment for generated tables and audio.
- Incremental updates (add / update / remove obsolete entries).
