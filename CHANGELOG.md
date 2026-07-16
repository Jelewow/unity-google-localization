# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2026-07-16

### Added

- GitHub Actions release workflow: signed `.tgz` attached to GitHub Releases for OpenUPM `githubRelease` tracking.

## [1.0.1] - 2026-07-14

### Fixed

- `Validate Addressables group` now assigns the group to audio clips both referenced by the table (by GUID) and physically present in the audio folder, so it works even when the table is out of sync with the folder.

## [1.0.0] - 2026-07-14

First stable release.

### Fixed

- `Validate Addressables group` now moves the audio clips referenced by the asset table to the group by GUID, so it works regardless of where the clips are stored on disk.
- Audio clips are no longer removed from Addressables and re-added when a group is selected.

### Changed

- Operations UI: primary `Update` buttons on top, `Validate` in a neutral color, `Test authentication` moved to the bottom.

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
