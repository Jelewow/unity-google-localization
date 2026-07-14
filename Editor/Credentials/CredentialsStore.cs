using System;

namespace SheetsLocalization.Editor.Credentials
{
    /// <summary>
    /// Persists <see cref="GoogleCredentials"/> in per-project <see cref="ProjectPrefs"/> (EditorPrefs).
    /// Secrets live outside the project folder and can never be committed to source control.
    /// </summary>
    public static class CredentialsStore
    {
        public static GoogleCredentials Load()
        {
            var credentials = new GoogleCredentials
            {
                ApiKey = ProjectPrefs.GetString("ApiKey", string.Empty),
                ServiceAccountEmail = ProjectPrefs.GetString("ServiceAccountEmail", string.Empty),
                ServiceAccountKeyPath = ProjectPrefs.GetString("ServiceAccountKeyPath", string.Empty)
            };

            var authTypeRaw = ProjectPrefs.GetString("AuthType", GoogleAuthType.ApiKey.ToString());
            credentials.AuthType = Enum.TryParse(authTypeRaw, out GoogleAuthType authType)
                ? authType
                : GoogleAuthType.ApiKey;

            return credentials;
        }

        public static void Save(GoogleCredentials credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            ProjectPrefs.SetString("AuthType", credentials.AuthType.ToString());
            ProjectPrefs.SetString("ApiKey", credentials.ApiKey);
            ProjectPrefs.SetString("ServiceAccountEmail", credentials.ServiceAccountEmail);
            ProjectPrefs.SetString("ServiceAccountKeyPath", credentials.ServiceAccountKeyPath);
        }

        public static void Clear()
        {
            ProjectPrefs.DeleteKey("AuthType");
            ProjectPrefs.DeleteKey("ApiKey");
            ProjectPrefs.DeleteKey("ServiceAccountEmail");
            ProjectPrefs.DeleteKey("ServiceAccountKeyPath");
        }
    }
}
