using System.Text;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Bit-for-bit port of the 32-bit x86 MurmurHash3 implementation
    /// originally embedded in <c>Proxy/ProxyCluster.NodeRouting.cs</c>'s
    /// <c>ConsistentHashRouter</c>. Extracted into a shared helper so
    /// the Phase 04 partition functions and the existing proxy-tier
    /// router both call the same algorithm — required for the
    /// "ConsistentHash tests still pass after refactor" verification
    /// criterion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MurmurHash3 is a non-cryptographic hash; do NOT use it for
    /// security-sensitive scenarios. It is chosen here for its strong
    /// distribution properties and identical behaviour across .NET
    /// runtimes and target frameworks.
    /// </para>
    /// <para>
    /// Constants and finalisation steps mirror the original
    /// implementation exactly:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>c1 = 0xcc9e2d51</c>, <c>c2 = 0x1b873593</c>, <c>seed = 0</c>.</item>
    ///   <item>UTF-8 encoding for string inputs, little-endian block reads.</item>
    ///   <item>Tail processing for trailing 1–3 bytes.</item>
    ///   <item>fmix32 finaliser with constants <c>0x85ebca6b</c> and <c>0xc2b2ae35</c>.</item>
    /// </list>
    /// </remarks>
    public static class MurmurHash3Helper
    {
        /// <summary>Default seed used by the 32-bit hash; matches the legacy proxy implementation.</summary>
        public const uint DefaultSeed = 0u;

        /// <summary>
        /// Computes the 32-bit MurmurHash3 of the UTF-8 encoding of
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Input string. <c>null</c> hashes as empty.</param>
        /// <param name="seed">Seed value. Defaults to <see cref="DefaultSeed"/>.</param>
        /// <returns>32-bit unsigned hash code.</returns>
        public static uint Hash(string value, uint seed = DefaultSeed)
        {
            if (string.IsNullOrEmpty(value))
                return Hash(System.Array.Empty<byte>(), seed);

            return Hash(Encoding.UTF8.GetBytes(value), seed);
        }

        /// <summary>
        /// Computes the 32-bit MurmurHash3 of <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">Input buffer. <c>null</c> is treated as empty.</param>
        /// <param name="seed">Seed value. Defaults to <see cref="DefaultSeed"/>.</param>
        /// <returns>32-bit unsigned hash code.</returns>
        public static uint Hash(byte[] bytes, uint seed = DefaultSeed)
        {
            const uint c1 = 0xcc9e2d51u;
            const uint c2 = 0x1b873593u;

            bytes = bytes ?? System.Array.Empty<byte>();
            int  len = bytes.Length;
            uint h1  = seed;
            int  i   = 0;

            // Body — process 4-byte blocks.
            while (i + 4 <= len)
            {
                uint k1 = (uint)(bytes[i]
                    | (bytes[i + 1] << 8)
                    | (bytes[i + 2] << 16)
                    | (bytes[i + 3] << 24));

                k1 *= c1;
                k1  = RotL(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1  = RotL(h1, 13);
                h1  = h1 * 5 + 0xe6546b64u;

                i += 4;
            }

            // Tail — 1..3 trailing bytes.
            uint tail = 0;
            int  rem  = len & 3;
            if (rem == 3) tail ^= (uint)bytes[i + 2] << 16;
            if (rem >= 2) tail ^= (uint)bytes[i + 1] << 8;
            if (rem >= 1)
            {
                tail ^= bytes[i];
                tail *= c1;
                tail  = RotL(tail, 15);
                tail *= c2;
                h1   ^= tail;
            }

            // Finalisation (fmix32).
            h1 ^= (uint)len;
            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6bu;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35u;
            h1 ^= h1 >> 16;

            return h1;
        }

        private static uint RotL(uint x, int r) => (x << r) | (x >> (32 - r));
    }
}
