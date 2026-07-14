using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SheetsLocalization.Editor.Types;
using SheetsLocalization.Editor.Credentials;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SheetsLocalization.Editor.Services
{
    public class GoogleSheetsParseService
    {
        // Sheets cells store line breaks inconsistently depending on how the text was entered
        // (manual Alt+Enter, paste from Word/browser, paste from Google Docs). These constants
        // cover every variant so the import can unify them to a single LF.
        private const char LineFeed = '\n';
        private const char CarriageReturn = '\r';
        private const char LineSeparator = (char)0x2028;      // Unicode LINE SEPARATOR
        private const char ParagraphSeparator = (char)0x2029; // Unicode PARAGRAPH SEPARATOR
        private const char NextLine = (char)0x0085;           // Unicode NEXT LINE (NEL)

        private readonly GoogleAuthService _authService;

        public GoogleSheetsParseService(GoogleAuthService authService)
        {
            _authService = authService;
        }

        public async Task<RawSheetData> GetRawSheetDataAsync(string sheetUrl, string sheetName = null)
        {
            try
            {
                var spreadsheetId = ExtractSpreadsheetId(sheetUrl);

                Debug.Log($"Fetching raw data from Google Sheets: {spreadsheetId}");
                Debug.Log($"Using authentication: {_authService.ActiveCredentialsInfo}");

                var spreadsheetTitle = await FetchSpreadsheetTitleAsync(spreadsheetId);

                if (string.IsNullOrEmpty(sheetName))
                {
                    sheetName = await ExtractSheetNameFromUrlAsync(sheetUrl, spreadsheetId);

                    if (string.IsNullOrEmpty(sheetName))
                    {
                        sheetName = await FetchSheetNameAsync(spreadsheetId);
                    }
                }

                Debug.Log($"Using sheet: {sheetName}");
                var rows = await FetchSheetValuesAsync(spreadsheetId, sheetName);

                return new RawSheetData
                {
                    TableKeyPrefix = spreadsheetTitle,
                    SheetName = sheetName,
                    Rows = rows
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to fetch raw data from Google Sheets: {ex.Message}");
                throw;
            }
        }

        private string ExtractSpreadsheetId(string url)
        {
            var match = Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
            if (!match.Success)
                throw new ArgumentException($"Invalid Google Sheets URL format: {url}");

            return match.Groups[1].Value;
        }

        private async Task<string> ExtractSheetNameFromUrlAsync(string url, string spreadsheetId)
        {
            try
            {
                var gidMatch = Regex.Match(url, @"[#&]gid=([0-9]+)");
                if (!gidMatch.Success)
                    return null;

                var gid = gidMatch.Groups[1].Value;
                Debug.Log($"Found gid in URL: {gid}");

                var baseUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets(properties(title,sheetId))";
                return await MakeAuthenticatedRequestAsync(baseUrl, response =>
                {
                    var json = JObject.Parse(response);
                    var sheets = json["sheets"] as JArray;

                    if (sheets == null)
                        return null;

                    foreach (var sheet in sheets)
                    {
                        var sheetId = sheet["properties"]?["sheetId"]?.ToString();
                        if (sheetId == gid)
                        {
                            var sheetName = sheet["properties"]?["title"]?.ToString();
                            Debug.Log($"Resolved sheet by gid {gid}: {sheetName}");
                            return sheetName;
                        }
                    }

                    Debug.LogWarning($"Sheet with gid {gid} not found");
                    return null;
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to extract sheet name from URL: {ex.Message}");
                return null;
            }
        }

        private async Task<string> FetchSpreadsheetTitleAsync(string spreadsheetId)
        {
            var baseUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=properties.title";
            return await MakeAuthenticatedRequestAsync(baseUrl, response =>
            {
                var json = JObject.Parse(response);
                return json["properties"]?["title"]?.ToString()
                       ?? throw new Exception("Spreadsheet title not found in response");
            });
        }

        private async Task<string> FetchSheetNameAsync(string spreadsheetId)
        {
            var baseUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets.properties.title";
            return await MakeAuthenticatedRequestAsync(baseUrl, response =>
            {
                var json = JObject.Parse(response);
                return json["sheets"]?[0]?["properties"]?["title"]?.ToString()
                       ?? throw new Exception("Sheet title not found in response");
            });
        }

        private async Task<List<List<string>>> FetchSheetValuesAsync(string spreadsheetId, string sheetName)
        {
            var escapedSheetName = Uri.EscapeDataString(sheetName);
            var baseUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{escapedSheetName}?valueRenderOption=FORMATTED_VALUE";

            return await MakeAuthenticatedRequestAsync(baseUrl, response =>
            {
                var json = JObject.Parse(response);
                var values = json["values"] as JArray;

                if (values == null || values.Count < 2)
                    throw new Exception("The sheet must contain a header and at least one data row");

                return values
                    .Select(row => row.Select(cell => NormalizeNewlines(cell.ToString())).ToList())
                    .ToList();
            });
        }

        // Unify every newline variant a Sheets cell may contain (CRLF, CR, LS, PS, NEL) to a single LF,
        // so imported strings are consistent regardless of how the text was originally entered.
        private static string NormalizeNewlines(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace(CarriageReturn.ToString() + LineFeed, LineFeed.ToString())
                .Replace(CarriageReturn, LineFeed)
                .Replace(LineSeparator, LineFeed)
                .Replace(ParagraphSeparator, LineFeed)
                .Replace(NextLine, LineFeed);
        }

        private async Task<T> MakeAuthenticatedRequestAsync<T>(string baseUrl, Func<string, T> responseParser)
        {
            string accessToken = null;
            if (_authService.CurrentAuthType == GoogleAuthType.ServiceAccount)
            {
                try
                {
                    accessToken = await _authService.GetAccessTokenAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to obtain access token: {ex.Message}");
                    throw new Exception($"Failed to obtain access token: {ex.Message}");
                }
            }

            var url = _authService.BuildAuthenticatedUrl(baseUrl, accessToken);
            using var request = UnityWebRequest.Get(url);
            request.timeout = 30;

            var headers = _authService.GetAuthenticationHeaders(accessToken);
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                var errorMessage = GetDetailedErrorMessage(request.error, request.responseCode, request.downloadHandler.text);
                throw new Exception($"Google API request failed: {errorMessage}");
            }

            try
            {
                return responseParser(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse Google API response: {ex.Message}");
            }
        }

        private string GetDetailedErrorMessage(string error, long responseCode, string responseText)
        {
            var baseMessage = $"{error} (HTTP {responseCode})";
            var authType = _authService.CurrentAuthType == GoogleAuthType.ServiceAccount ? "the service account" : "the API key";

            var hint = responseCode switch
            {
                401 => "ERROR 401 UNAUTHORIZED - authentication problem:\n" +
                       "- Check the authentication settings\n" +
                       "- Service account: make sure the JSON key is valid\n" +
                       "- API key: make sure the key is correct",
                403 => $"ERROR 403 FORBIDDEN - access denied for {authType}:\n" +
                       "1. For API key auth the document must be shared as 'Anyone with the link'\n" +
                       "2. For service account auth share the document with the service account email\n" +
                       "3. Enable the Google Sheets API in the Google Cloud Console for the key's project\n" +
                       "4. Remove HTTP referrer / IP restrictions from the API key, or allow the Sheets API",
                404 => "ERROR 404 NOT FOUND:\n" +
                       "- Check that the document URL is correct\n" +
                       "- Make sure the document exists and is accessible",
                429 => "ERROR 429 TOO MANY REQUESTS:\n" +
                       "- API request limit exceeded\n" +
                       "- Try again later",
                _ => string.Empty
            };

            var details = string.IsNullOrEmpty(responseText) ? string.Empty : $"\nServer response: {responseText}";
            return string.IsNullOrEmpty(hint) ? $"{baseMessage}{details}" : $"{baseMessage}\n{hint}{details}";
        }
    }
}
