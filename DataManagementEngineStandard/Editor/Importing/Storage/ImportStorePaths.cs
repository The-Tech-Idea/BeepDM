using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Editor.Importing.Storage
{
    internal static class ImportStorePaths
    {
        internal static string Resolve(string folder, string contextKey, string suffix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
            // Match the existing in-memory store's case-insensitive context identity.
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contextKey.ToUpperInvariant())));
            var path = Path.Combine(folder, hash + suffix);
            if (!File.Exists(path))
            {
                var legacyName = string.Join("_", contextKey.Split(Path.GetInvalidFileNameChars()));
                var legacy = Path.Combine(folder, legacyName + suffix);
                if (File.Exists(legacy))
                    throw new InvalidDataException($"Legacy import store '{legacy}' requires explicit migration. " +
                        "Preserve the original, validate context identity and convert its data before using the versioned store.");
            }
            return path;
        }
    }
}
