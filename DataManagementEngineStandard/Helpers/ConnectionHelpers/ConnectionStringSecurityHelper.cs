using System;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Helpers.ConnectionHelpers
{
    /// <summary>
    /// Helper class for securing connection strings by masking sensitive information.
    /// </summary>
    public static class ConnectionStringSecurityHelper
    {
        /// <summary>
        /// Creates a secure version of a connection string by masking sensitive information.
        /// </summary>
        /// <param name="connectionString">The connection string to secure.</param>
        /// <returns>A secure version of the connection string with sensitive data masked.</returns>
        public static string SecureConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            string secureConnectionString = connectionString;

            // Mask passwords
            secureConnectionString = MaskPasswords(secureConnectionString);

            // Mask API keys
            secureConnectionString = MaskApiKeys(secureConnectionString);

            // Mask access keys (AWS, Azure, etc.)
            secureConnectionString = MaskAccessKeys(secureConnectionString);

            // Mask tokens
            secureConnectionString = MaskTokens(secureConnectionString);

            // Mask secrets
            secureConnectionString = MaskSecrets(secureConnectionString);

            return secureConnectionString;
        }

        /// <summary>
        /// Masks password-related parameters in a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <returns>The connection string with passwords masked.</returns>
        private static string MaskPasswords(string connectionString)
        {
            // Common password parameter names
            string[] passwordPatterns = {
                @"(Password|Pwd|Pass)\s*=\s*([^;]*)",
                @"(User\s*Password|UserPassword)\s*=\s*([^;]*)",
                @"(Auth\s*Password|AuthPassword)\s*=\s*([^;]*)"
            };

            string result = connectionString;
            foreach (string pattern in passwordPatterns)
            {
                result = Regex.Replace(result, pattern, "$1=********", RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Masks API key-related parameters in a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <returns>The connection string with API keys masked.</returns>
        private static string MaskApiKeys(string connectionString)
        {
            // Common API key parameter names
            string[] apiKeyPatterns = {
                @"(ApiKey|Api[-_]?Key)\s*=\s*([^;]*)",
                @"(ApplicationKey|Application[-_]?Key)\s*=\s*([^;]*)",
                @"(ClientKey|Client[-_]?Key)\s*=\s*([^;]*)",
                @"(SubscriptionKey|Subscription[-_]?Key)\s*=\s*([^;]*)"
            };

            string result = connectionString;
            foreach (string pattern in apiKeyPatterns)
            {
                result = Regex.Replace(result, pattern, "$1=********", RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Masks access key-related parameters in a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <returns>The connection string with access keys masked.</returns>
        private static string MaskAccessKeys(string connectionString)
        {
            // Common access key parameter names
            string[] accessKeyPatterns = {
                @"(AccessKey|Access[-_]?Key)\s*=\s*([^;]*)",
                @"(SecretKey|Secret[-_]?Key)\s*=\s*([^;]*)",
                @"(AwsAccessKey|Aws[-_]?Access[-_]?Key)\s*=\s*([^;]*)",
                @"(AwsSecretKey|Aws[-_]?Secret[-_]?Key)\s*=\s*([^;]*)",
                @"(AccountKey|Account[-_]?Key)\s*=\s*([^;]*)"
            };

            string result = connectionString;
            foreach (string pattern in accessKeyPatterns)
            {
                result = Regex.Replace(result, pattern, "$1=********", RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Masks token-related parameters in a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <returns>The connection string with tokens masked.</returns>
        private static string MaskTokens(string connectionString)
        {
            // Common token parameter names
            string[] tokenPatterns = {
                @"(Token|Access[-_]?Token)\s*=\s*([^;]*)",
                @"(BearerToken|Bearer[-_]?Token)\s*=\s*([^;]*)",
                @"(AuthToken|Auth[-_]?Token)\s*=\s*([^;]*)",
                @"(RefreshToken|Refresh[-_]?Token)\s*=\s*([^;]*)",
                @"(JwtToken|Jwt[-_]?Token)\s*=\s*([^;]*)"
            };

            string result = connectionString;
            foreach (string pattern in tokenPatterns)
            {
                result = Regex.Replace(result, pattern, "$1=********", RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Masks secret-related parameters in a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <returns>The connection string with secrets masked.</returns>
        private static string MaskSecrets(string connectionString)
        {
            // Common secret parameter names
            string[] secretPatterns = {
                @"(Secret|Client[-_]?Secret)\s*=\s*([^;]*)",
                @"(SharedSecret|Shared[-_]?Secret)\s*=\s*([^;]*)",
                @"(AppSecret|App[-_]?Secret)\s*=\s*([^;]*)",
                @"(PrivateKey|Private[-_]?Key)\s*=\s*([^;]*)"
            };

            string result = connectionString;
            foreach (string pattern in secretPatterns)
            {
                result = Regex.Replace(result, pattern, "$1=********", RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Checks if a connection string contains potentially sensitive information.
        /// </summary>
        /// <param name="connectionString">The connection string to check.</param>
        /// <returns>True if sensitive information is detected, false otherwise.</returns>
        public static bool ContainsSensitiveInformation(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return false;

            // Check for common sensitive parameter patterns
            string[] sensitivePatterns = {
                @"\b(Password|Pwd|Pass)\s*=",
                @"\b(ApiKey|Api[-_]?Key)\s*=",
                @"\b(AccessKey|Access[-_]?Key)\s*=",
                @"\b(SecretKey|Secret[-_]?Key)\s*=",
                @"\b(Token|Access[-_]?Token)\s*=",
                @"\b(Secret|Client[-_]?Secret)\s*=",
                @"\b(PrivateKey|Private[-_]?Key)\s*="
            };

            foreach (string pattern in sensitivePatterns)
            {
                if (Regex.IsMatch(connectionString, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts and masks only the sensitive parts of a connection string, leaving non-sensitive parts intact.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <param name="maskChar">The character to use for masking (default is '*').</param>
        /// <param name="visibleChars">Number of characters to keep visible at the start and end (default is 2).</param>
        /// <returns>The connection string with selective masking applied.</returns>
        public static string SelectiveMask(string connectionString, char maskChar = '*', int visibleChars = 2)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            // Patterns for sensitive information with capture groups
            var sensitivePatterns = new[]
            {
                (@"(\b(?:Password|Pwd|Pass)\s*=\s*)([^;]*)", "$1{0}"),
                (@"(\b(?:ApiKey|Api[-_]?Key)\s*=\s*)([^;]*)", "$1{0}"),
                (@"(\b(?:AccessKey|Access[-_]?Key)\s*=\s*)([^;]*)", "$1{0}"),
                (@"(\b(?:SecretKey|Secret[-_]?Key)\s*=\s*)([^;]*)", "$1{0}"),
                (@"(\b(?:Token|Access[-_]?Token)\s*=\s*)([^;]*)", "$1{0}"),
                (@"(\b(?:Secret|Client[-_]?Secret)\s*=\s*)([^;]*)", "$1{0}"),
                (@"(\b(?:PrivateKey|Private[-_]?Key)\s*=\s*)([^;]*)", "$1{0}")
            };

            string result = connectionString;
            foreach (var (pattern, replacement) in sensitivePatterns)
            {
                result = Regex.Replace(result, pattern, match =>
                {
                    string prefix = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    string maskedValue = MaskValue(value, maskChar, visibleChars);
                    return prefix + maskedValue;
                }, RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Masks a value while keeping some characters visible.
        /// </summary>
        /// <param name="value">The value to mask.</param>
        /// <param name="maskChar">The character to use for masking.</param>
        /// <param name="visibleChars">Number of characters to keep visible at start and end.</param>
        /// <returns>The masked value.</returns>
        private static string MaskValue(string value, char maskChar, int visibleChars)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= visibleChars * 2)
                return new string(maskChar, Math.Max(4, value.Length));

            int maskedLength = value.Length - (visibleChars * 2);
            string start = value.Substring(0, visibleChars);
            string end = value.Substring(value.Length - visibleChars);
            string middle = new string(maskChar, Math.Max(4, maskedLength));

            return start + middle + end;
        }

        /// <summary>
        /// Gets a list of parameter names that are considered sensitive.
        /// </summary>
        /// <returns>An array of sensitive parameter names.</returns>
        public static string[] GetSensitiveParameterNames()
        {
            return new[]
            {
                "Password", "Pwd", "Pass", "UserPassword", "AuthPassword",
                "ApiKey", "Api-Key", "Api_Key", "ApplicationKey", "Application-Key", "Application_Key",
                "ClientKey", "Client-Key", "Client_Key", "SubscriptionKey", "Subscription-Key", "Subscription_Key",
                "AccessKey", "Access-Key", "Access_Key", "SecretKey", "Secret-Key", "Secret_Key",
                "AwsAccessKey", "Aws-Access-Key", "Aws_Access_Key", "AwsSecretKey", "Aws-Secret-Key", "Aws_Secret_Key",
                "AccountKey", "Account-Key", "Account_Key",
                "Token", "AccessToken", "Access-Token", "Access_Token", "BearerToken", "Bearer-Token", "Bearer_Token",
                "AuthToken", "Auth-Token", "Auth_Token", "RefreshToken", "Refresh-Token", "Refresh_Token",
                "JwtToken", "Jwt-Token", "Jwt_Token",
                "Secret", "ClientSecret", "Client-Secret", "Client_Secret", "SharedSecret", "Shared-Secret", "Shared_Secret",
                "AppSecret", "App-Secret", "App_Secret", "PrivateKey", "Private-Key", "Private_Key"
            };
        }

        /// <summary>
        /// Validates that a connection string has been properly secured (no plain text sensitive information).
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if the connection string appears to be secured, false otherwise.</returns>
        public static bool IsConnectionStringSecured(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return true;

            // Check if sensitive parameters contain only masked values
            var sensitiveValuePattern = @"\b(?:Password|Pwd|Pass|ApiKey|Api[-_]?Key|AccessKey|Access[-_]?Key|SecretKey|Secret[-_]?Key|Token|Access[-_]?Token|Secret|Client[-_]?Secret|PrivateKey|Private[-_]?Key)\s*=\s*([^;]+)";

            var matches = Regex.Matches(connectionString, sensitiveValuePattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                string value = match.Groups[1].Value.Trim();
                // Check if the value is likely a masked value (contains only asterisks or similar patterns)
                if (!IsMaskedValue(value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if a value appears to be masked.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value appears to be masked, false otherwise.</returns>
        private static bool IsMaskedValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            // Check if value consists primarily of masking characters
            int maskCharCount = 0;
            foreach (char c in value)
            {
                if (c == '*' || c == 'X' || c == '#')
                    maskCharCount++;
            }

            // Consider it masked if more than 70% are masking characters
            return (double)maskCharCount / value.Length > 0.7;
        }
    }
}