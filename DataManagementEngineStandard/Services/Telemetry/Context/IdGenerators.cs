using System;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// W3C TraceContext-compatible id generators. Trace ids are 16 random
    /// bytes encoded as 32 lowercase hex characters; span ids are 8 random
    /// bytes encoded as 16 lowercase hex characters. Both are non-zero by
    /// construction so they round-trip cleanly through OTel exporters that
    /// reject the all-zero sentinel.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="RandomNumberGenerator"/> rather than <see cref="Guid.NewGuid"/>
    /// because GUIDs do not match the W3C byte length and version bits would
    /// have to be stripped anyway.
    /// </remarks>
    internal static class IdGenerators
    {
        public const int TraceIdLengthBytes = 16;
        public const int SpanIdLengthBytes = 8;

        /// <summary>Returns a fresh 32-char lowercase hex trace id.</summary>
        public static string NewTraceId()
        {
            return NewHexId(TraceIdLengthBytes);
        }

        /// <summary>Returns a fresh 16-char lowercase hex span id.</summary>
        public static string NewSpanId()
        {
            return NewHexId(SpanIdLengthBytes);
        }

        /// <summary>
        /// Returns a fresh GUID-N (32 hex chars, no dashes). Used as a
        /// human-friendly correlation id when no scope is active.
        /// </summary>
        public static string NewCorrelationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string NewHexId(int byteLength)
        {
            byte[] buffer = new byte[byteLength];
            do
            {
                RandomNumberGenerator.Fill(buffer);
            }
            while (IsAllZero(buffer));
            var sb = new StringBuilder(byteLength * 2);
            for (int i = 0; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private static bool IsAllZero(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
