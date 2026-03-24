using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager.Utilities
{
    public static class FileChecksumHelper
    {
        public static async Task<string> ComputeChecksumAsync(string filePath, CancellationToken ct = default)
        {
            using SHA256 sha = SHA256.Create();
            await using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            byte[] hashBytes = await sha.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hashBytes);
        }
    }
}
