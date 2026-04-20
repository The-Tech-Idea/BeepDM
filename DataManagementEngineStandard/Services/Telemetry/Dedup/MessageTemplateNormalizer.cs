using System.Text;

namespace TheTechIdea.Beep.Services.Telemetry.Dedup
{
    /// <summary>
    /// Reduces a free-form log message to a stable template by replacing
    /// the parts that vary per call (numbers, GUIDs, quoted literals) with
    /// placeholder tokens. Two messages whose only difference is variable
    /// content normalize to the same template, which is what
    /// <see cref="WindowedDeduper"/> uses as its identity key.
    /// </summary>
    /// <remarks>
    /// Single-pass over the input string with no allocations beyond one
    /// <see cref="StringBuilder"/> per call. Designed for the producer hot
    /// path: roughly two pointer compares and a copy per character.
    /// </remarks>
    internal static class MessageTemplateNormalizer
    {
        public const string NumberToken = "{n}";
        public const string GuidToken = "{g}";
        public const string QuotedToken = "{s}";

        /// <summary>Returns the normalized template, or the original on null/empty.</summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return raw;
            }

            var sb = new StringBuilder(raw.Length);
            int i = 0;
            while (i < raw.Length)
            {
                char c = raw[i];

                if (c == '\'' || c == '"')
                {
                    int end = FindQuoteEnd(raw, i + 1, c);
                    sb.Append(QuotedToken);
                    i = (end < 0) ? raw.Length : end + 1;
                    continue;
                }

                if (TryReadGuid(raw, i, out int guidLen))
                {
                    sb.Append(GuidToken);
                    i += guidLen;
                    continue;
                }

                if (IsDigit(c) || (c == '-' && i + 1 < raw.Length && IsDigit(raw[i + 1])))
                {
                    int numLen = ReadNumberLength(raw, i);
                    sb.Append(NumberToken);
                    i += numLen;
                    continue;
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        private static int FindQuoteEnd(string raw, int start, char quote)
        {
            for (int i = start; i < raw.Length; i++)
            {
                if (raw[i] == quote)
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool TryReadGuid(string raw, int start, out int length)
        {
            length = 0;
            int remaining = raw.Length - start;
            if (remaining >= 36 && IsGuidShape(raw, start, true))
            {
                length = 36;
                return true;
            }
            if (remaining >= 32 && IsGuidShape(raw, start, false))
            {
                length = 32;
                return true;
            }
            return false;
        }

        private static bool IsGuidShape(string raw, int start, bool dashed)
        {
            if (dashed)
            {
                if (!IsHexRun(raw, start, 8) || raw[start + 8] != '-' ||
                    !IsHexRun(raw, start + 9, 4) || raw[start + 13] != '-' ||
                    !IsHexRun(raw, start + 14, 4) || raw[start + 18] != '-' ||
                    !IsHexRun(raw, start + 19, 4) || raw[start + 23] != '-' ||
                    !IsHexRun(raw, start + 24, 12))
                {
                    return false;
                }
                return true;
            }
            return IsHexRun(raw, start, 32);
        }

        private static bool IsHexRun(string raw, int start, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (!IsHexDigit(raw[start + i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static int ReadNumberLength(string raw, int start)
        {
            int i = start;
            if (raw[i] == '-')
            {
                i++;
            }
            while (i < raw.Length && IsDigit(raw[i]))
            {
                i++;
            }
            if (i < raw.Length && raw[i] == '.' && i + 1 < raw.Length && IsDigit(raw[i + 1]))
            {
                i++;
                while (i < raw.Length && IsDigit(raw[i]))
                {
                    i++;
                }
            }
            return i - start;
        }

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}
