using System;
using UnityEditor;

namespace SheetsLocalization.Editor.Credentials
{
    /// <summary>
    /// Persists <see cref="GoogleCredentials"/> in per-project <see cref="EditorPrefs"/>.
    /// EditorPrefs lives outside the project folder, so secrets can never be committed to source control.
    /// Keys are scoped by <see cref="PlayerSettings.productGUID"/> so different projects on the same machine don't collide.
    /// </summary>
    public static class CredentialsStore
    {
        private const string Prefix = "SheetsLocalization";

        public static GoogleCredentials Load()
        {
            var credentials = new GoogleCredentials
            {
                ApiKey = EditorPrefs.GetString(Key("ApiKey"), string.Empty),
                ServiceAccountEmail = EditorPrefs.GetString(Key("ServiceAccountEmail"), string.Empty),
                ServiceAccountKeyPath = EditorPrefs.GetString(Key("ServiceAccountKeyPath"), string.Empty)
            };

            var authTypeRaw = EditorPrefs.GetString(Key("AuthType"), GoogleAuthType.ApiKey.ToString());
            credentials.AuthType = Enum.TryParse(authTypeRaw, out GoogleAuthType authType)
                ? authType
                : GoogleAuthType.ApiKey;

            return credentials;
        }

        public static void Save(GoogleCredentials credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            EditorPrefs.SetString(Key("AuthType"), credentials.AuthType.ToString());
            EditorPrefs.SetString(Key("ApiKey"), credentials.ApiKey ?? string.Empty);
            EditorPrefs.SetString(Key("ServiceAccountEmail"), credentials.ServiceAccountEmail ?? string.Empty);
            EditorPrefs.SetString(Key("ServiceAccountKeyPath"), credentials.ServiceAccountKeyPath ?? string.Empty);
        }

        public static void Clear()
        {
            EditorPrefs.DeleteKey(Key("AuthType"));
            EditorPrefs.DeleteKey(Key("ApiKey"));
            EditorPrefs.DeleteKey(Key("ServiceAccountEmail"));
            EditorPrefs.DeleteKey(Key("ServiceAccountKeyPath"));
        }

        // ponytail: values are stored as plaintext in the OS registry/plist (EditorPrefs).
        // Good enough for local editor secrets; upgrade path is an OS keychain/credential-manager backend.
        private static string Key(string name) => $"{Prefix}.{PlayerSettings.productGUID}.{name}";
    }
}
