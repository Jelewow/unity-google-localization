using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SheetsLocalization.Editor.Credentials;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SheetsLocalization.Editor.Services
{
    public class GoogleAuthService
    {
        private readonly GoogleCredentials _credentials;

        private string _cachedAccessToken;
        private DateTime _tokenExpiryTime;

        public GoogleAuthService(GoogleCredentials credentials)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        public GoogleAuthType CurrentAuthType => _credentials.AuthType;
        public string ActiveCredentialsInfo => _credentials.GetActiveCredentialsInfo();

        public async Task<string> GetAccessTokenAsync()
        {
            switch (CurrentAuthType)
            {
                case GoogleAuthType.ServiceAccount:
                    return await GetServiceAccountAccessTokenAsync();

                case GoogleAuthType.ApiKey:
                    return null; // API key is passed directly in the URL

                default:
                    throw new Exception("Authentication type is not configured");
            }
        }

        public string BuildAuthenticatedUrl(string baseUrl, string accessToken = null)
        {
            switch (_credentials.AuthType)
            {
                case GoogleAuthType.ApiKey:
                    var separator = baseUrl.Contains("?") ? "&" : "?";
                    return $"{baseUrl}{separator}key={_credentials.ApiKey}";

                case GoogleAuthType.ServiceAccount:
                    return baseUrl; // Token is sent in the Authorization header

                default:
                    throw new Exception("Authentication type is not configured");
            }
        }

        public Dictionary<string, string> GetAuthenticationHeaders(string accessToken = null)
        {
            var headers = new Dictionary<string, string>();

            if (_credentials.AuthType == GoogleAuthType.ServiceAccount && !string.IsNullOrEmpty(accessToken))
            {
                headers["Authorization"] = $"Bearer {accessToken}";
            }

            return headers;
        }

        public bool WasTokenExpire()
        {
            var cache = string.IsNullOrEmpty(_cachedAccessToken);
            var wasExpire = DateTime.UtcNow > _tokenExpiryTime;

            return cache && wasExpire;
        }

        public void ClearTokenCache()
        {
            _cachedAccessToken = null;
            _tokenExpiryTime = DateTime.MinValue;
        }

        private async Task<string> GetServiceAccountAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiryTime)
            {
                return _cachedAccessToken;
            }

            if (!IsServiceAccountConfigured())
            {
                throw new Exception("Service account is not configured");
            }

            try
            {
                var serviceAccountKey = GetServiceAccountKey();
                var jwt = CreateJWT(serviceAccountKey);
                var tokenEndpoint = string.IsNullOrWhiteSpace(serviceAccountKey.token_uri)
                    ? "https://oauth2.googleapis.com/token"
                    : serviceAccountKey.token_uri;
                var accessToken = await ExchangeJwtForAccessTokenAsync(jwt, tokenEndpoint);

                _cachedAccessToken = accessToken;
                _tokenExpiryTime = DateTime.UtcNow.AddMinutes(55); // Google tokens last an hour, keep a margin

                return accessToken;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to obtain service account token: {ex.Message}");
                throw;
            }
        }

        private bool IsServiceAccountConfigured()
        {
            return !string.IsNullOrEmpty(_credentials.ServiceAccountEmail) &&
                   (!string.IsNullOrEmpty(_credentials.ServiceAccountKeyPath) || !string.IsNullOrEmpty(_credentials.ServiceAccountKeyJson));
        }

        private ServiceAccountKey GetServiceAccountKey()
        {
            string keyJson;

            if (!string.IsNullOrEmpty(_credentials.ServiceAccountKeyJson))
            {
                keyJson = _credentials.ServiceAccountKeyJson;
            }
            else if (!string.IsNullOrEmpty(_credentials.ServiceAccountKeyPath))
            {
                if (!File.Exists(_credentials.ServiceAccountKeyPath))
                {
                    throw new Exception($"Service account key file not found: {_credentials.ServiceAccountKeyPath}");
                }
                keyJson = File.ReadAllText(_credentials.ServiceAccountKeyPath);
            }
            else
            {
                throw new Exception("No service account key path or inline key JSON provided");
            }

            try
            {
                return JsonConvert.DeserializeObject<ServiceAccountKey>(keyJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse service account key JSON: {ex.Message}");
            }
        }

        private string CreateJWT(ServiceAccountKey serviceAccountKey)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiry = now + 3600; // 1 hour

            var header = new Dictionary<string, object>
            {
                { "alg", "RS256" },
                { "typ", "JWT" }
            };
            if (!string.IsNullOrWhiteSpace(serviceAccountKey.private_key_id))
            {
                header["kid"] = serviceAccountKey.private_key_id;
            }

            var payload = new
            {
                iss = serviceAccountKey.client_email,
                scope = "https://www.googleapis.com/auth/drive.readonly https://www.googleapis.com/auth/spreadsheets.readonly",
                aud = string.IsNullOrWhiteSpace(serviceAccountKey.token_uri) ? "https://oauth2.googleapis.com/token" : serviceAccountKey.token_uri,
                exp = expiry,
                iat = now
            };

            var headerJson = JsonConvert.SerializeObject(header);
            var payloadJson = JsonConvert.SerializeObject(payload);

            var headerEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            var stringToSign = $"{headerEncoded}.{payloadEncoded}";
            var signature = SignWithRsa(stringToSign, serviceAccountKey.private_key);

            return $"{stringToSign}.{signature}";
        }

        private string SignWithRsa(string data, string privateKeyPem)
        {
            if (string.IsNullOrWhiteSpace(privateKeyPem))
            {
                throw new Exception("Private key is empty or missing");
            }

            using var rsa = RSA.Create();

            ImportPrivateKeyFromPem(rsa, privateKeyPem);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Base64UrlEncode(signature);
        }

        private void ImportPrivateKeyFromPem(RSA rsa, string pem)
        {
            var trimmed = pem.Trim();

            if (trimmed.Contains("BEGIN ENCRYPTED PRIVATE KEY"))
            {
                throw new Exception("Encrypted private keys are not supported. Generate an unencrypted service account JSON key in the Google Cloud Console.");
            }

            if (trimmed.Contains("BEGIN PRIVATE KEY"))
            {
                var pkcs8 = ExtractDerFromPem(trimmed, "PRIVATE KEY");
                try
                {
                    var rsaParams = DecodePkcs8ToRsaParameters(pkcs8);
                    rsa.ImportParameters(rsaParams);
                    return;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to import PKCS#8 private key: {ex.Message}");
                }
            }

            if (trimmed.Contains("BEGIN RSA PRIVATE KEY"))
            {
                var pkcs1 = ExtractDerFromPem(trimmed, "RSA PRIVATE KEY");
                try
                {
                    var rsaParams = DecodePkcs1ToRsaParameters(pkcs1);
                    rsa.ImportParameters(rsaParams);
                    return;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to import PKCS#1 RSA private key: {ex.Message}");
                }
            }

            throw new Exception("Unknown PEM private key format. Expected 'BEGIN PRIVATE KEY' or 'BEGIN RSA PRIVATE KEY'.");
        }

        private byte[] ExtractDerFromPem(string pem, string label)
        {
            var header = $"-----BEGIN {label}-----";
            var footer = $"-----END {label}-----";

            var start = pem.IndexOf(header, StringComparison.Ordinal);
            var end = pem.IndexOf(footer, StringComparison.Ordinal);
            if (start < 0 || end < 0)
            {
                throw new Exception($"PEM does not contain a {label} block");
            }

            start += header.Length;
            var base64 = pem.Substring(start, end - start)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty)
                .Trim();

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid Base64 data in PEM: {ex.Message}");
            }
        }

        /// <summary>
        /// Decodes PKCS#8 (PrivateKeyInfo) and extracts RSAParameters by unwrapping the inner PKCS#1 RSAPrivateKey.
        /// </summary>
        private RSAParameters DecodePkcs8ToRsaParameters(byte[] pkcs8)
        {
            var offset = 0;
            ReadAsnTag(pkcs8, ref offset, 0x30); // SEQUENCE
            ReadAsnLength(pkcs8, ref offset);    // length of SEQUENCE

            // version INTEGER
            ReadAsnTag(pkcs8, ref offset, 0x02);
            SkipAsnValue(pkcs8, ref offset);

            // algorithm AlgorithmIdentifier (SEQUENCE)
            ReadAsnTag(pkcs8, ref offset, 0x30);
            var algLen = ReadAsnLength(pkcs8, ref offset);
            offset += algLen; // skip algorithm identifier entirely

            // privateKey OCTET STRING -> contains PKCS#1 RSAPrivateKey
            ReadAsnTag(pkcs8, ref offset, 0x04);
            var octetLen = ReadAsnLength(pkcs8, ref offset);
            var pkcs1 = new byte[octetLen];
            Buffer.BlockCopy(pkcs8, offset, pkcs1, 0, octetLen);

            return DecodePkcs1ToRsaParameters(pkcs1);
        }

        /// <summary>
        /// Decodes PKCS#1 RSAPrivateKey into RSAParameters.
        /// </summary>
        private RSAParameters DecodePkcs1ToRsaParameters(byte[] pkcs1)
        {
            int offset = 0;
            ReadAsnTag(pkcs1, ref offset, 0x30); // SEQUENCE
            ReadAsnLength(pkcs1, ref offset);

            // version INTEGER
            ReadAsnTag(pkcs1, ref offset, 0x02);
            SkipAsnValue(pkcs1, ref offset);

            var n  = ReadAsnInteger(pkcs1, ref offset);
            var e  = ReadAsnInteger(pkcs1, ref offset);
            var d  = ReadAsnInteger(pkcs1, ref offset);
            var p  = ReadAsnInteger(pkcs1, ref offset);
            var q  = ReadAsnInteger(pkcs1, ref offset);
            var dp = ReadAsnInteger(pkcs1, ref offset);
            var dq = ReadAsnInteger(pkcs1, ref offset);
            var iq = ReadAsnInteger(pkcs1, ref offset);

            return new RSAParameters
            {
                Modulus = n,
                Exponent = e,
                D = d,
                P = p,
                Q = q,
                DP = dp,
                DQ = dq,
                InverseQ = iq
            };
        }

        private void ReadAsnTag(byte[] data, ref int offset, int expectedTag)
        {
            if (offset >= data.Length)
            {
                throw new Exception("ASN.1: Unexpected end of data while reading tag");
            }
            int tag = data[offset++];
            if (tag != expectedTag)
            {
                throw new Exception($"ASN.1: Expected tag 0x{expectedTag:X2}, got 0x{tag:X2}");
            }
        }

        private int ReadAsnLength(byte[] data, ref int offset)
        {
            if (offset >= data.Length)
            {
                throw new Exception("ASN.1: Unexpected end of data while reading length");
            }

            var length = data[offset++];
            if ((length & 0x80) == 0)
            {
                return length;
            }

            var numBytes = length & 0x7F;
            if (numBytes is 0 or > 4)
            {
                throw new Exception("ASN.1: Invalid length");
            }
            if (offset + numBytes > data.Length)
            {
                throw new Exception("ASN.1: Length runs past end of data");
            }

            var value = 0;
            for (int i = 0; i < numBytes; i++)
            {
                value = (value << 8) | data[offset++];
            }
            return value;
        }

        private void SkipAsnValue(byte[] data, ref int offset)
        {
            var len = ReadAsnLength(data, ref offset);
            offset += len;
            if (offset > data.Length)
            {
                throw new Exception("ASN.1: Value runs past end of data");
            }
        }

        private byte[] ReadAsnInteger(byte[] data, ref int offset)
        {
            ReadAsnTag(data, ref offset, 0x02);
            var len = ReadAsnLength(data, ref offset);
            if (offset + len > data.Length)
            {
                throw new Exception("ASN.1: INTEGER runs past end of data");
            }

            var start = offset;
            var end = offset + len;
            if (len > 0 && data[start] == 0x00)
            {
                start++;
            }
            var result = new byte[end - start];
            Buffer.BlockCopy(data, start, result, 0, result.Length);
            offset = end;
            return result;
        }

        private string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private async Task<string> ExchangeJwtForAccessTokenAsync(string jwt, string tokenEndpoint)
        {
            var form = new WWWForm();
            form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
            form.AddField("assertion", jwt);

            using var request = UnityWebRequest.Post(tokenEndpoint, form);
            request.timeout = 30;

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"Failed to obtain access token: {request.responseCode} - {request.downloadHandler.text}");
            }

            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(request.downloadHandler.text);
            return tokenResponse.access_token;
        }
    }

    [Serializable]
    public class ServiceAccountKey
    {
        public string type { get; set; }
        public string project_id { get; set; }
        public string private_key_id { get; set; }
        public string private_key { get; set; }
        public string client_email { get; set; }
        public string client_id { get; set; }
        public string auth_uri { get; set; }
        public string token_uri { get; set; }
        public string auth_provider_x509_cert_url { get; set; }
        public string client_x509_cert_url { get; set; }
    }

    [Serializable]
    public class TokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
}
