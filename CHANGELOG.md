# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-14

### Added

- Import localized strings from Google Sheets into Unity Localization `StringTableCollection`s.
- Sync localized audio from Google Drive folders into `AssetTableCollection`s.
- Google authentication via API key or service account (RS256 JWT flow).
- Pluggable, ScriptableObject-based parsing pipeline (`GoogleSheetsConfigurator`).
- Built-in configurators: `StandardColumnConfigurator`, `ScenarioColumnConfigurator`.
- Optional Addressables group/label assignment for generated tables and audio.
- Incremental updates (add / update / remove obsolete entries).
