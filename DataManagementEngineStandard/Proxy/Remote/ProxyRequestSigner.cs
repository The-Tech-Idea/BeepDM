using System;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Proxy.Remote
{
    /// <summary>
    /// Shared HMAC-SHA256 signing and verification helper for the proxy wire protocol.
    ///
    /// <para>
    /// <strong>Canonical message format</strong>:
    /// <c>{CorrelationId}|{Operation}|{RequestTimestamp:O}</c>
    /// </para>
    ///
    /// <para>
    /// The message covers the fields that uniquely identify this request
    /// (correlation ID), what it does (operation), and when it was sent
    /// (timestamp).  Per-field coverage prevents field-swap attacks while
    /// keeping the surface small and deterministic.
    /// </para>
    ///
    /// <para>
    /// <strong>Replay protection</strong>: the worker rejects requests where
    /// <c>|UtcNow − RequestTimestamp| &gt; <see cref="ClockSkewTolerance"/></c>.
    /// </para>
    /// </summary>
    public static class ProxyRequestSigner
    {
        /// <summary>
        /// Maximum allowed difference between the coordinator's clock and the worker's
        /// clock.  Requests outside this window are rejected as potential replays.
        /// </summary>
        public static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(5);

        // ── Signing ──────────────────────────────────────────────────────────

        /// <summary>
        /// Signs <paramref name="request"/> in-place:
        /// sets <c>RequestTimestamp</c> to <c>UtcNow</c> and computes a fresh
        /// HMAC-SHA256 <c>Signature</c>.
        /// </summary>
        /// <param name="request">Request to sign (mutated in-place).</param>
        /// <param name="hmacSecret">
        /// Raw UTF-8 bytes of the shared secret.
        /// Must be at least 32 bytes for adequate security.
        /// Never log or transmit this value in clear text.
        /// </param>
        public static void Sign(ProxyRemoteRequest request, byte[] hmacSecret)
        {
            if (request  is null) throw new ArgumentNullException(nameof(request));
            if (hmacSecret is null || hmacSecret.Length == 0) throw new ArgumentNullException(nameof(hmacSecret));

            request.RequestTimestamp = DateTimeOffset.UtcNow;
            request.Signature        = ComputeSignature(request, hmacSecret);
        }

        // ── Verification ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns a <see cref="SignatureVerificationResult"/> indicating whether the
        /// request's signature and timestamp are valid.
        /// </summary>
        /// <param name="request">Inbound request from the wire.</param>
        /// <param name="hmacSecret">The server's copy of the shared secret.</param>
        public static SignatureVerificationResult Verify(ProxyRemoteRequest request, byte[] hmacSecret)
        {
            if (request is null)    return SignatureVerificationResult.MissingSignature;
            if (hmacSecret is null) return SignatureVerificationResult.MissingSignature;

            if (string.IsNullOrEmpty(request.Signature))
                return SignatureVerificationResult.MissingSignature;

            // Replay check
            var age = DateTimeOffset.UtcNow - request.RequestTimestamp;
            if (age < -ClockSkewTolerance || age > ClockSkewTolerance)
                return SignatureVerificationResult.ReplayAttack;

            // Constant-time comparison (prevents timing-based secret recovery)
            string expected = ComputeSignature(request, hmacSecret);
            if (!CryptographicEquals(expected, request.Signature))
                return SignatureVerificationResult.InvalidSignature;

            return SignatureVerificationResult.Valid;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static string ComputeSignature(ProxyRemoteRequest req, byte[] key)
        {
            string message = $"{req.CorrelationId}|{req.Operation}|{req.RequestTimestamp:O}";
            byte[] msgBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(key);
            byte[] hash = hmac.ComputeHash(msgBytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>Constant-time string equality to prevent timing attacks.</summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a is null || b is null) return false;
            if (a.Length != b.Length)   return false;

            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SignatureVerificationResult
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The outcome of <see cref="ProxyRequestSigner.Verify"/>.
    /// </summary>
    public enum SignatureVerificationResult
    {
        /// <summary>Signature is present and valid; timestamp is within tolerance.</summary>
        Valid,

        /// <summary>The request carries no <c>Signature</c> or <c>RequestTimestamp</c>.</summary>
        MissingSignature,

        /// <summary>The HMAC does not match the shared secret (tampered or wrong key).</summary>
        InvalidSignature,

        /// <summary>
        /// The <c>RequestTimestamp</c> is outside the ±5-minute tolerance window —
        /// likely a replayed or very-delayed request.
        /// </summary>
        ReplayAttack
    }
}
