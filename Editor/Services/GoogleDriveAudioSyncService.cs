using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SheetsLocalization.Editor.Types;
using SheetsLocalization.Editor.Credentials;
using SheetsLocalization.Editor.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SheetsLocalization.Editor.Services
{
    public class GoogleDriveAudioSyncService
    {
        private readonly GoogleAuthService _authService;

        public GoogleDriveAudioSyncService(GoogleAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Synchronizes audio files between a Google Drive folder and a local folder.
        /// </summary>
        public async Task SyncAudioFilesAsync(string googleDriveFolderUrl, string localAudioPath, string tableName)
        {
            try
            {
                Debug.Log($"Starting audio sync for table {tableName}");

                var driveFiles = await GetGoogleDriveFilesAsync(googleDriveFolderUrl);
                Debug.Log($"Found {driveFiles.Count} files in Google Drive");

                var localFiles = GetLocalAudioFiles(localAudioPath);
                Debug.Log($"Found {localFiles.Count} local files");

                await SyncFilesAsync(driveFiles, localFiles, localAudioPath);

                Debug.Log("Audio sync completed successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Audio sync failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns the display name of a Google Drive folder from its URL, or null on failure.
        /// </summary>
        public async Task<string> GetFolderNameAsync(string folderUrl)
        {
            try
            {
                var folderId = ExtractFolderIdFromUrl(folderUrl);

                string accessToken = null;
                if (_authService.CurrentAuthType == GoogleAuthType.ServiceAccount)
                {
                    accessToken = await _authService.GetAccessTokenAsync();
                }

                var baseUrl = $"https://www.googleapis.com/drive/v3/files/{folderId}?fields=name";
                var url = _authService.BuildAuthenticatedUrl(baseUrl, accessToken);

                using var request = UnityWebRequest.Get(url);
                request.timeout = 15;

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
                    Debug.LogWarning($"Failed to fetch Google Drive folder name: {request.error} (HTTP {request.responseCode})");
                    return null;
                }

                var json = JObject.Parse(request.downloadHandler.text);
                return json["name"]?.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to fetch Google Drive folder name: {ex.Message}");
                return null;
            }
        }

        private async Task<List<AudioFileInfo>> GetGoogleDriveFilesAsync(string folderUrl)
        {
            var folderId = ExtractFolderIdFromUrl(folderUrl);
            var files = new List<AudioFileInfo>();
            var nextPageToken = "";

            do
            {
                var filesData = await FetchFilesFromFolderAsync(folderId, nextPageToken);
                files.AddRange(filesData.files);
                nextPageToken = filesData.nextPageToken;
            } while (!string.IsNullOrEmpty(nextPageToken));

            return files;
        }

        private string ExtractFolderIdFromUrl(string url)
        {
            var match = Regex.Match(url, @"/folders/([a-zA-Z0-9-_]+)");
            if (!match.Success)
                throw new ArgumentException($"Invalid Google Drive folder URL: {url}");

            return match.Groups[1].Value;
        }

        private async Task<(List<AudioFileInfo> files, string nextPageToken)> FetchFilesFromFolderAsync(string folderId, string pageToken = "")
        {
            var baseUrl = $"https://www.googleapis.com/drive/v3/files?q='{folderId}'+in+parents&fields=files(id,name,md5Checksum,mimeType),nextPageToken";

            if (!string.IsNullOrEmpty(pageToken))
                baseUrl += $"&pageToken={pageToken}";

            string accessToken = null;
            if (_authService.CurrentAuthType == GoogleAuthType.ServiceAccount)
            {
                accessToken = await _authService.GetAccessTokenAsync();
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
                var detailedError = GetDetailedApiError(request.responseCode, request.error, request.downloadHandler.text, folderId);
                throw new Exception($"Failed to list files: {detailedError}");
            }

            var json = JObject.Parse(request.downloadHandler.text);
            var files = new List<AudioFileInfo>();

            foreach (var file in json["files"])
            {
                var mimeType = file["mimeType"]?.ToString();

                if (IsAudioFile(mimeType, file["name"]?.ToString()))
                {
                    files.Add(new AudioFileInfo
                    {
                        Id = file["id"]?.ToString(),
                        Name = file["name"]?.ToString(),
                        Md5Checksum = file["md5Checksum"]?.ToString()
                    });
                }
            }

            var nextPageToken = json["nextPageToken"]?.ToString() ?? "";
            return (files, nextPageToken);
        }

        private bool IsAudioFile(string mimeType, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            if (!string.IsNullOrEmpty(mimeType) && mimeType.StartsWith("audio/"))
                return true;

            return AudioFileTypes.HasAudioExtension(fileName);
        }

        private Dictionary<string, string> GetLocalAudioFiles(string localPath)
        {
            var localFiles = new Dictionary<string, string>();

            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
                return localFiles;
            }

            foreach (var pattern in AudioFileTypes.SearchPatterns)
            {
                var files = Directory.GetFiles(localPath, pattern, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var md5 = CalculateFileMD5(file);
                    localFiles[Path.GetFileName(file)] = md5;
                }
            }

            return localFiles;
        }

        private string CalculateFileMD5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private async Task SyncFilesAsync(List<AudioFileInfo> driveFiles, Dictionary<string, string> localFiles, string localPath)
        {
            // Guard against accidental deletion: if the remote list is empty,
            // don't delete local files (common when an API key lacks public access).
            if (driveFiles == null || driveFiles.Count == 0)
            {
                Debug.LogWarning("Google Drive file list is empty. Local audio deletion was skipped.");
                return;
            }

            var filesToDownload = driveFiles.Where(df =>
                !localFiles.ContainsKey(df.Name) ||
                localFiles[df.Name] != df.Md5Checksum?.ToLower()).ToList();

            var driveFileNames = driveFiles.Select(f => f.Name).ToHashSet();
            var filesToDelete = localFiles.Keys.Where(name => !driveFileNames.Contains(name)).ToList();

            foreach (var file in filesToDownload)
            {
                try
                {
                    await DownloadFileAsync(file, localPath);
                    Debug.Log($"Downloaded file: {file.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to download file {file.Name}: {e.Message}");
                }
            }

            if (filesToDelete.Count > 0)
            {
                var minimalConfidentRemoteCount = Math.Max(3, localFiles.Count / 2);
                if (driveFiles.Count < minimalConfidentRemoteCount)
                {
                    Debug.LogWarning($"Found {filesToDelete.Count} local files missing from Google Drive, but the remote list looks incomplete (Drive: {driveFiles.Count}, Local: {localFiles.Count}). Deletion skipped.");
                }
                else
                {
                    foreach (var fileName in filesToDelete)
                    {
                        try
                        {
                            var filePath = Path.Combine(localPath, fileName);
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                Debug.Log($"Deleted file: {fileName}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to delete file {fileName}: {e.Message}");
                        }
                    }
                }
            }

            Debug.Log($"Sync completed: downloaded {filesToDownload.Count}, deleted {filesToDelete.Count}");

            if (filesToDownload.Count > 0 || filesToDelete.Count > 0)
            {
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log("AssetDatabase refreshed after file sync");
            }
        }

        private async Task DownloadFileAsync(AudioFileInfo fileInfo, string localPath)
        {
            var baseUrl = $"https://www.googleapis.com/drive/v3/files/{fileInfo.Id}?alt=media";

            string accessToken = null;
            if (_authService.CurrentAuthType == GoogleAuthType.ServiceAccount)
            {
                accessToken = await _authService.GetAccessTokenAsync();
            }

            var url = _authService.BuildAuthenticatedUrl(baseUrl, accessToken);

            using var request = UnityWebRequest.Get(url);
            request.timeout = 60; // 60s timeout for file downloads

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
                var errorDetails = GetDownloadErrorDetails(request.responseCode, request.error);
                throw new Exception($"Failed to download file {fileInfo.Name}: {errorDetails}");
            }

            var filePath = Path.Combine(localPath, fileInfo.Name);

            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, request.downloadHandler.data);

            var downloadedMd5 = CalculateFileMD5(filePath);
            if (!string.IsNullOrEmpty(fileInfo.Md5Checksum) &&
                downloadedMd5 != fileInfo.Md5Checksum.ToLower())
            {
                File.Delete(filePath);
                throw new Exception($"MD5 of downloaded file {fileInfo.Name} does not match the expected checksum");
            }

            UnityEditor.AssetDatabase.ImportAsset("Assets/" + Path.GetRelativePath(Application.dataPath, filePath).Replace('\\', '/'));
        }

        private string GetDownloadErrorDetails(long responseCode, string error)
        {
            switch (responseCode)
            {
                case 403:
                    return $"{error} - Access denied. Make sure the Google Drive folder is publicly accessible";
                case 404:
                    return $"{error} - File not found. It may have been deleted or moved";
                case 429:
                    return $"{error} - Request limit exceeded. Try again later";
                default:
                    return $"{error} (HTTP {responseCode})";
            }
        }

        private string GetDetailedApiError(long responseCode, string error, string responseText, string folderId)
        {
            var baseError = $"{error} (HTTP {responseCode})";

            switch (responseCode)
            {
                case 403:
                    return GetDetailed403Error(responseText, folderId);
                case 404:
                    return GetDetailed404Error(folderId);
                case 400:
                    return GetDetailed400Error(responseText);
                case 429:
                    return GetDetailed429Error();
                default:
                    var additionalInfo = !string.IsNullOrEmpty(responseText) ? $"\nDetails: {responseText}" : "";
                    return $"{baseError}{additionalInfo}";
            }
        }

        private string GetDetailed403Error(string responseText, string folderId)
        {
            var diagnostics = new StringBuilder();
            diagnostics.AppendLine("ERROR 403 FORBIDDEN - ACCESS DENIED");
            diagnostics.AppendLine();
            diagnostics.AppendLine("Possible causes and fixes:");
            diagnostics.AppendLine();
            diagnostics.AppendLine("1) The API key does not have access to the Google Drive API");
            diagnostics.AppendLine("   - Enable the Google Drive API for your project in the Google Cloud Console");
            diagnostics.AppendLine("   - Create a new API key or verify the existing one");
            diagnostics.AppendLine();
            diagnostics.AppendLine("2) The Google Drive folder is not publicly accessible");
            diagnostics.AppendLine("   - Open the folder in Google Drive");
            diagnostics.AppendLine("   - Share it with 'Anyone with the link' as 'Viewer' (for API key auth)");
            diagnostics.AppendLine("   - Or grant the service account access to the folder");
            diagnostics.AppendLine();
            diagnostics.AppendLine("3) The API key has restrictions");
            diagnostics.AppendLine("   - Check IP/domain restrictions in the Google Cloud Console");
            diagnostics.AppendLine();
            diagnostics.AppendLine("4) API quotas exceeded");
            diagnostics.AppendLine("   - Check quota usage in the Google Cloud Console");
            diagnostics.AppendLine();
            diagnostics.AppendLine("Diagnostic info:");
            diagnostics.AppendLine($"   - Folder ID: {folderId}");
            diagnostics.AppendLine($"   - Credentials: {_authService.ActiveCredentialsInfo}");

            if (!string.IsNullOrEmpty(responseText))
            {
                diagnostics.AppendLine($"   - Server response: {responseText}");
            }

            return diagnostics.ToString();
        }

        private string GetDetailed404Error(string folderId)
        {
            return "ERROR 404 NOT FOUND - FOLDER NOT FOUND\n" +
                   "Possible causes:\n" +
                   $"- Wrong folder ID: {folderId}\n" +
                   "- The folder was deleted or moved\n" +
                   "- You do not have access to the folder\n" +
                   "Fix:\n" +
                   "- Check the Google Drive folder URL\n" +
                   "- Make sure the folder exists and is accessible";
        }

        private string GetDetailed400Error(string responseText)
        {
            return "ERROR 400 BAD REQUEST - INVALID REQUEST\n" +
                   "Possible causes:\n" +
                   "- Invalid API key format\n" +
                   "- Incorrect request parameters\n" +
                   "- Invalid folder ID\n" +
                   "Fix:\n" +
                   "- Verify the API key\n" +
                   "- Make sure the folder URL is correct\n" +
                   $"Details: {responseText}";
        }

        private string GetDetailed429Error()
        {
            return "ERROR 429 TOO MANY REQUESTS - REQUEST LIMIT EXCEEDED\n" +
                   "Cause:\n" +
                   "- Too many requests to the Google Drive API\n" +
                   "Fix:\n" +
                   "- Wait a few minutes before retrying\n" +
                   "- Add delays between requests\n" +
                   "- Check quotas in the Google Cloud Console";
        }
    }
}
