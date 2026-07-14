# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
