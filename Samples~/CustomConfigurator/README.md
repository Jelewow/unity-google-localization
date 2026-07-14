# Custom Configurator Example

`SuffixColumnConfigurator` shows how to extend `GoogleSheetsConfigurator` for a non-trivial sheet
layout where a single row describes an object with several localized fields.

## Expected sheet

| id     | en-name | de-name | en-description | de-description |
|--------|---------|---------|----------------|----------------|
| drone1 | Drone   | Drohne  | A small UAV    | Ein kleines UAV |

This produces the localization keys:

- `drone1-name` (en / de)
- `drone1-description` (en / de)

## Usage

1. Import this sample via the Package Manager.
2. Create the asset: `Assets > Create > Sheets Localization > Configurators > Samples > Suffix Column Configurator`.
3. Assign it to your `Localization Settings` asset and run **Update texts**.

Adjust the `locales` array and the parsing rules to match your own spreadsheet.
