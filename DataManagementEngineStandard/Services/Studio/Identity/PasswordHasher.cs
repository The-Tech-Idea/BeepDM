// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Services.Studio.Identity;

/// <summary>
/// Stage 4: PBKDF2-HMAC-SHA256 password hashing. No external dependency — uses the BCL
/// <see cref="Rfc2898DeriveBytes"/> with 256k iterations (OWASP recommendation as of 2023) and a
/// 128-bit salt. The output format is a self-describing string <c>v1.{iterations}.{saltB64}.{hashB64}</c>
/// so the iteration count can be raised later without invalidating older hashes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Stage 4 implementation note:</b> the codebase had no password-hashing precedent (only
/// HMAC-SHA256 for audit and manifest signing). This is the first password hasher; future stages can
/// swap to Argon2id by changing the <c>v1.</c> prefix and the implementation — the format is
/// version-stamped so callers can detect and rehash on next login.
/// </para>
/// <para>
/// <b>Constant-time verification.</b> <see cref="Verify"/> uses <see cref="CryptographicOperations.FixedTimeEquals"/>
/// after the PBKDF2 computation, so the hash comparison doesn't leak via timing.
/// </para>
/// </remarks>
public static class PasswordHasher
{
    private const int SaltBytes = 16;       // 128-bit salt
    private const int HashBytes = 32;       // 256-bit hash
    private const int DefaultIterations = 256_000;
    private static readonly HashAlgorithmName HashAlg = HashAlgorithmName.SHA256;

    /// <summary>Hash a password, returning the self-describing wire format.</summary>
    public static string Hash(string password, int iterations = DefaultIterations)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlg, HashBytes);
        return $"v1.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verify a password against a stored hash. Returns false on any malformed input (never throws)
    /// so callers can treat "bad hash" the same as "wrong password" — both deny login.
    /// </summary>
    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;
        var parts = stored.Split('.');
        // Format: v1.iterations.saltB64.hashB64
        if (parts.Length != 4 || parts[0] != "v1") return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations < 1000) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch { return false; }
        if (salt.Length == 0 || expected.Length == 0) return false;

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, HashAlg, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>
    /// True when <paramref name="stored"/> is a hash from an older iteration count — caller should
    /// rehash on next successful login. Returns false for malformed input.
    /// </summary>
    public static bool NeedsRehash(string stored, int targetIterations = DefaultIterations)
    {
        var parts = stored?.Split('.') ?? Array.Empty<string>();
        if (parts.Length != 4 || parts[0] != "v1") return false;
        return int.TryParse(parts[1], out var iters) && iters < targetIterations;
    }
}
