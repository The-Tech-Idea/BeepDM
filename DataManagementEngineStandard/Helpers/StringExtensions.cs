using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace TheTechIdea.Beep.Helpers
{
    public static partial class StringExtensions
    {
        /// <summary>
        /// Converts a string to a list of strings, splitting by commas.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>A list of strings.</returns>
        public static List<string> ToList(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new List<string>();
            }
            return input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
        }

        /// <summary>
        /// Converts a snake_case string to PascalCase
        /// </summary>
        /// <param name="str">The snake_case string to convert</param>
        /// <returns>The string in PascalCase format</returns>
        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var parts = str.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }

            return string.Join("", parts);
        }

        /// <summary>
        /// Converts a string to camelCase
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>The string in camelCase format</returns>
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            str = ToPascalCase(str); // First convert to PascalCase if it's snake_case

            if (str.Length == 1)
                return str.ToLowerInvariant();

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Converts a PascalCase or camelCase string to snake_case
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>The string in snake_case format</returns>
        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Add underscore before each capital letter and convert to lowercase
            var result = Regex.Replace(str, "(?<=.)([A-Z])", "_$1").ToLowerInvariant();
            return result;
        }

        /// <summary>
        /// Converts a PascalCase or camelCase string to kebab-case
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>The string in kebab-case format</returns>
        public static string ToKebabCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Add hyphen before each capital letter and convert to lowercase
            var result = Regex.Replace(str, "(?<=.)([A-Z])", "-$1").ToLowerInvariant();
            return result;
        }

        /// <summary>
        /// Truncates a string to the specified maximum length
        /// </summary>
        /// <param name="str">The string to truncate</param>
        /// <param name="maxLength">Maximum length</param>
        /// <param name="appendEllipsis">Whether to append "..." if truncated</param>
        /// <returns>The truncated string</returns>
        public static string Truncate(this string str, int maxLength, bool appendEllipsis = false)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;

            if (appendEllipsis && maxLength > 3)
                return str.Substring(0, maxLength - 3) + "...";
            else
                return str.Substring(0, maxLength);
        }

        /// <summary>
        /// Removes diacritics (accents) from a string
        /// </summary>
        /// <param name="text">The string to normalize</param>
        /// <returns>String without diacritics</returns>
        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Checks if a string is a valid email address
        /// </summary>
        /// <param name="email">String to validate</param>
        /// <returns>True if valid email format, otherwise false</returns>
        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use a simple regex pattern for basic validation
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a string contains only numeric characters
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>True if string contains only numbers</returns>
        public static bool IsNumeric(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            foreach (char c in str)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a string is a valid date according to the specified format
        /// </summary>
        /// <param name="dateString">The date string to validate</param>
        /// <param name="format">The expected date format (null for culture default)</param>
        /// <param name="culture">The culture to use (null for current culture)</param>
        /// <returns>True if valid date, otherwise false</returns>
        public static bool IsValidDate(this string dateString, string? format = null, CultureInfo? culture = null)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            culture ??= CultureInfo.CurrentCulture;

            try
            {
                if (format != null)
                {
                    // Parse with specific format
                    DateTime.ParseExact(dateString, format, culture, DateTimeStyles.None);
                }
                else
                {
                    // Parse with culture's default formats
                    DateTime.Parse(dateString, culture);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts all numeric digits from a string
        /// </summary>
        /// <param name="str">The input string</param>
        /// <returns>A string containing only the numeric digits</returns>
        public static string ExtractNumbers(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            return new string(str.Where(c => char.IsDigit(c)).ToArray());
        }

        /// <summary>
        /// Converts first character of the string to uppercase
        /// </summary>
        /// <param name="str">The string to capitalize</param>
        /// <returns>String with first letter capitalized</returns>
        public static string Capitalize(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length == 1)
                return str.ToUpper();

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Splits a string by capital letters (useful for breaking apart identifiers)
        /// </summary>
        /// <param name="str">The string to split</param>
        /// <returns>A string with spaces before capital letters</returns>
        public static string SplitCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return Regex.Replace(str, "(?<=[a-z])([A-Z])", " $1");
        }

        /// <summary>
        /// Checks if a string is valid JSON
        /// </summary>
        /// <param name="strInput">String to check</param>
        /// <returns>True if valid JSON, otherwise false</returns>
        public static bool IsValidJson(this string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput))
                return false;

            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) ||
                (strInput.StartsWith("[") && strInput.EndsWith("]")))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(strInput);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts a delimited string to a list of strings
        /// </summary>
        /// <param name="input">The delimited string</param>
        /// <param name="delimiter">The delimiter character</param>
        /// <returns>A list of strings</returns>
        public static List<string> ToList(this string input, char delimiter)
        {
            if (string.IsNullOrEmpty(input))
                return new List<string>();

            return input.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .ToList();
        }

        /// <summary>
        /// Checks if a string contains any of the specified substrings
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="values">The substrings to look for</param>
        /// <returns>True if string contains any of the specified substrings</returns>
        public static bool ContainsAny(this string str, params string[] values)
        {
            if (string.IsNullOrEmpty(str) || values == null || values.Length == 0)
                return false;

            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value) && str.Contains(value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a string contains all the specified substrings
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="values">The substrings to look for</param>
        /// <returns>True if string contains all the specified substrings</returns>
        public static bool ContainsAll(this string str, params string[] values)
        {
            if (string.IsNullOrEmpty(str) || values == null || values.Length == 0)
                return false;

            foreach (string value in values)
            {
                if (string.IsNullOrEmpty(value) || !str.Contains(value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the string or a default value if the string is null or empty
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="defaultValue">The default value to return if string is null or empty</param>
        /// <returns>The original string or the default value</returns>
        public static string DefaultIfEmpty(this string str, string defaultValue)
        {
            return string.IsNullOrEmpty(str) ? defaultValue : str;
        }

        /// <summary>
        /// Checks if a string is a valid file path
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <returns>True if valid file path, otherwise false</returns>
        public static bool IsValidFilePath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // Get full path to check for formatting errors
                var fullPath = Path.GetFullPath(path);

                // Check for invalid characters
                var invalidChars = Path.GetInvalidPathChars();
                if (path.IndexOfAny(invalidChars) >= 0)
                    return false;

                // Additional validation can be added here
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts a substring between two specified strings
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="start">The starting string</param>
        /// <param name="end">The ending string</param>
        /// <param name="includeStartEnd">Whether to include the start and end strings</param>
        /// <returns>The extracted substring or empty if not found</returns>
        public static string Between(this string source, string start, string end, bool includeStartEnd = false)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
                return string.Empty;

            int startIdx = source.IndexOf(start);
            if (startIdx < 0)
                return string.Empty;

            startIdx = includeStartEnd ? startIdx : startIdx + start.Length;
            int endIdx = source.IndexOf(end, startIdx);
            if (endIdx < 0)
                return string.Empty;

            endIdx = includeStartEnd ? endIdx + end.Length : endIdx;
            int length = endIdx - startIdx;

            return length <= 0 ? string.Empty : source.Substring(startIdx, length);
        }

        /// <summary>
        /// Reverses a string
        /// </summary>
        /// <param name="str">The string to reverse</param>
        /// <returns>The reversed string</returns>
        public static string Reverse(this string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= 1)
                return str;

            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

            #region SQL and Database Helpers

            /// <summary>
            /// Properly escapes a string for SQL to prevent SQL injection
            /// </summary>
            /// <param name="str">String to be escaped</param>
            /// <param name="quoteChar">Quote character (default is single quote)</param>
            /// <returns>SQL-safe string</returns>
            public static string EscapeSql(this string str, char quoteChar = '\'')
            {
                if (string.IsNullOrEmpty(str))
                    return str;

                return str.Replace(quoteChar.ToString(), new string(quoteChar, 2));
            }

            /// <summary>
            /// Creates a delimited SQL string (IN clause) from a list of strings
            /// </summary>
            /// <param name="values">Collection of string values</param>
            /// <returns>Delimited string for SQL IN clause</returns>
            public static string ToSqlInClause(this IEnumerable<string> values)
            {
                if (values == null || !values.Any())
                    return "('')";

                return "(" + string.Join(",", values.Select(x => $"'{x.EscapeSql()}'")) + ")";
            }

            /// <summary>
            /// Convert a string to a valid SQL identifier by removing invalid characters
            /// </summary>
            /// <param name="str">String to convert</param>
            /// <returns>Valid SQL identifier</returns>
            public static string ToSqlIdentifier(this string str)
            {
                if (string.IsNullOrEmpty(str))
                    return "Column";

                // Remove invalid characters
                string identifier = Regex.Replace(str, @"[^\w\d_]", "");

                // Ensure it starts with a letter or underscore
                if (identifier.Length > 0 && !char.IsLetter(identifier[0]) && identifier[0] != '_')
                {
                    identifier = "_" + identifier;
                }

                // If empty after sanitizing, return a default
                return string.IsNullOrEmpty(identifier) ? "Column" : identifier;
            }
            #endregion

            #region Formatting Helpers

            /// <summary>
            /// Format a string as a phone number
            /// </summary>
            /// <param name="phoneNumber">String to format</param>
            /// <returns>Formatted phone number</returns>
            public static string FormatAsPhoneNumber(this string phoneNumber)
            {
                if (string.IsNullOrEmpty(phoneNumber))
                    return phoneNumber;

                // Extract digits only
                string digits = Regex.Replace(phoneNumber, @"[^\d]", "");

                if (digits.Length == 10)
                {
                    return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";
                }
                else if (digits.Length > 10)
                {
                    return $"+{digits.Substring(0, digits.Length - 10)} ({digits.Substring(digits.Length - 10, 3)}) {digits.Substring(digits.Length - 7, 3)}-{digits.Substring(digits.Length - 4)}";
                }

                // Return original if can't format
                return phoneNumber;
            }

            /// <summary>
            /// Format a string as a social security number (XXX-XX-XXXX)
            /// </summary>
            /// <param name="ssn">String to format as SSN</param>
            /// <returns>Formatted SSN</returns>
            public static string FormatAsSSN(this string ssn)
            {
                if (string.IsNullOrEmpty(ssn))
                    return ssn;

                string digits = Regex.Replace(ssn, @"[^\d]", "");

                if (digits.Length == 9)
                {
                    return $"{digits.Substring(0, 3)}-{digits.Substring(3, 2)}-{digits.Substring(5)}";
                }

                // Return original if can't format
                return ssn;
            }

            /// <summary>
            /// Format a credit card number with dashes and mask middle digits
            /// </summary>
            /// <param name="cardNumber">Card number to format</param>
            /// <param name="maskChar">Character to use for masking (default '*')</param>
            /// <returns>Masked credit card number</returns>
            public static string FormatAsCreditCard(this string cardNumber, char maskChar = '*')
            {
                if (string.IsNullOrEmpty(cardNumber))
                    return cardNumber;

                string digits = Regex.Replace(cardNumber, @"[^\d]", "");

                if (digits.Length >= 13) // Most cards are between 13-19 digits
                {
                    // Keep first 4 and last 4 digits visible, mask the rest
                    string masked = digits.Substring(0, 4) + new string(maskChar, digits.Length - 8) + digits.Substring(digits.Length - 4);

                    // Add dashes for readability
                    StringBuilder result = new StringBuilder();
                    for (int i = 0; i < masked.Length; i++)
                    {
                        if (i > 0 && i % 4 == 0)
                            result.Append('-');
                        result.Append(masked[i]);
                    }

                    return result.ToString();
                }

                // Return original if can't format
                return cardNumber;
            }
            #endregion

            #region Validation Helpers

            /// <summary>
            /// Checks if a string is a valid URL
            /// </summary>
            /// <param name="url">URL to validate</param>
            /// <returns>True if valid URL</returns>
            public static bool IsValidUrl(this string url)
            {
                if (string.IsNullOrEmpty(url))
                    return false;

                return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) &&
                       (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }

            /// <summary>
            /// Validates if a string follows the GUID format
            /// </summary>
            /// <param name="str">The string to check</param>
            /// <returns>True if the string is a valid GUID format</returns>
            public static bool IsGuid(this string str)
            {
                if (string.IsNullOrEmpty(str))
                    return false;

                return Guid.TryParse(str, out _);
            }

            /// <summary>
            /// Checks if a string contains only letters (no numbers or special characters)
            /// </summary>
            /// <param name="str">String to check</param>
            /// <returns>True if contains only letters</returns>
            public static bool IsAlphaOnly(this string str)
            {
                if (string.IsNullOrEmpty(str))
                    return false;

                return str.All(char.IsLetter);
            }

            /// <summary>
            /// Checks if a string is a valid IPv4 or IPv6 address
            /// </summary>
            /// <param name="ipAddress">String to validate</param>
            /// <returns>True if valid IP address</returns>
            public static bool IsValidIpAddress(this string ipAddress)
            {
                if (string.IsNullOrEmpty(ipAddress))
                    return false;

                return System.Net.IPAddress.TryParse(ipAddress, out _);
            }

            /// <summary>
            /// Checks if a string is a valid credit card number (using Luhn algorithm)
            /// </summary>
            /// <param name="cardNumber">Card number to validate</param>
            /// <returns>True if valid credit card number</returns>
            public static bool IsValidCreditCardNumber(this string cardNumber)
            {
                if (string.IsNullOrEmpty(cardNumber))
                    return false;

                // Remove spaces and hyphens
                string digits = Regex.Replace(cardNumber, @"[\s-]", "");

                if (!digits.All(char.IsDigit))
                    return false;

                // Luhn algorithm for card validation
                int sum = 0;
                bool alternate = false;
                for (int i = digits.Length - 1; i >= 0; i--)
                {
                    int n = int.Parse(digits[i].ToString());
                    if (alternate)
                    {
                        n *= 2;
                        if (n > 9)
                            n -= 9;
                    }
                    sum += n;
                    alternate = !alternate;
                }
                return (sum % 10 == 0);
            }
            #endregion

            #region Conversion Helpers

            /// <summary>
            /// Tries to parse a string to a specific enum type
            /// </summary>
            /// <typeparam name="TEnum">Type of enum</typeparam>
            /// <param name="value">String value to parse</param>
            /// <param name="ignoreCase">Whether to ignore case</param>
            /// <param name="defaultValue">Default value if parsing fails</param>
            /// <returns>Parsed enum value or default</returns>
            public static TEnum ToEnum<TEnum>(this string value, bool ignoreCase = true, TEnum defaultValue = default) where TEnum : struct
            {
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                return Enum.TryParse<TEnum>(value, ignoreCase, out TEnum result) ? result : defaultValue;
            }

            /// <summary>
            /// Converts a base64 string to bytes
            /// </summary>
            /// <param name="base64String">Base64 encoded string</param>
            /// <returns>Decoded byte array or null if invalid</returns>
            public static byte[] FromBase64(this string base64String)
            {
                if (string.IsNullOrEmpty(base64String))
                    return null;

                try
                {
                    return Convert.FromBase64String(base64String);
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// Converts a string to bytes using the specified encoding
            /// </summary>
            /// <param name="str">String to convert</param>
            /// <param name="encoding">Text encoding to use (default UTF8)</param>
            /// <returns>Byte array</returns>
            public static byte[] ToBytes(this string str, Encoding encoding = null)
            {
                if (string.IsNullOrEmpty(str))
                    return new byte[0];

                encoding ??= Encoding.UTF8;
                return encoding.GetBytes(str);
            }

            /// <summary>
            /// Attempts to convert a string to a DateTime with specified format and culture
            /// </summary>
            /// <param name="dateString">Date string to parse</param>
            /// <param name="format">Format specifier</param>
            /// <param name="defaultValue">Default value if parsing fails</param>
            /// <param name="provider">Format provider (culture)</param>
            /// <returns>Parsed DateTime or default</returns>
            public static DateTime ToDateTime(this string dateString, string format = null,
                DateTime defaultValue = default, IFormatProvider provider = null)
            {
                if (string.IsNullOrEmpty(dateString))
                    return defaultValue;

                provider ??= CultureInfo.CurrentCulture;

                if (!string.IsNullOrEmpty(format))
                {
                    if (DateTime.TryParseExact(dateString, format, provider, DateTimeStyles.None, out DateTime result))
                        return result;
                }
                else if (DateTime.TryParse(dateString, provider, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }

                return defaultValue;
            }
            #endregion

            #region Security Helpers

            /// <summary>
            /// Computes MD5 hash of a string (for checksums, not for passwords)
            /// </summary>
            /// <param name="input">Input string</param>
            /// <returns>MD5 hash as a hex string</returns>
            public static string ToMD5Hash(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }

            /// <summary>
            /// Computes SHA256 hash of a string
            /// </summary>
            /// <param name="input">Input string</param>
            /// <returns>SHA256 hash as a hex string</returns>
            public static string ToSHA256Hash(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            #endregion

          
        }
    


}
