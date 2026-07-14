using System;
using UnityEngine;

namespace SheetsLocalization.Editor.Credentials
{
    public enum GoogleAuthType
    {
        ApiKey,
        ServiceAccount
    }

    [Serializable]
    public class GoogleCredentials
    {
        [Tooltip("Authentication method used for Google Sheets/Drive requests.")]
        public GoogleAuthType AuthType = GoogleAuthType.ApiKey;

        [Tooltip("Google API key. Required when AuthType is ApiKey. Never commit real keys to source control.")]
        public string ApiKey = "";

        [Tooltip("Service account email. Required when AuthType is ServiceAccount.")]
        public string ServiceAccountEmail = "";

        [Tooltip("Path to the service account JSON key file (relative to the project or absolute). Keep this file out of source control.")]
        public string ServiceAccountKeyPath = "";

        [Tooltip("Inline service account JSON key. Takes precedence over the key path. Keep out of source control.")]
        [TextArea]
        public string ServiceAccountKeyJson = "";

        public bool IsServiceAccountConfigured()
        {
            return !string.IsNullOrEmpty(ServiceAccountEmail) &&
                   (!string.IsNullOrEmpty(ServiceAccountKeyPath) || !string.IsNullOrEmpty(ServiceAccountKeyJson));
        }

        public bool IsApiKeyConfigured()
        {
            return !string.IsNullOrEmpty(ApiKey);
        }

        public string GetActiveCredentialsInfo()
        {
            switch (AuthType)
            {
                case GoogleAuthType.ServiceAccount:
                    return IsServiceAccountConfigured()
                        ? $"Service Account: {ServiceAccountEmail}"
                        : "Service Account: NOT CONFIGURED";

                case GoogleAuthType.ApiKey:
                    return IsApiKeyConfigured()
                        ? "API Key: configured"
                        : "API Key: NOT CONFIGURED";

                default:
                    return "NOT CONFIGURED";
            }
        }
    }
}
