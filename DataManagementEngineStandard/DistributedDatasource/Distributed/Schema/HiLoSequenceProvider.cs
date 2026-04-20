using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// HiLo <see cref="IDistributedSequenceProvider"/> that allocates
    /// ids in blocks from a supplied <see cref="HiLoBlockAllocator"/>.
    /// Each (<paramref name="entityName"/>, <paramref name="columnName"/>)
    /// pair gets its own <c>lo</c> counter in-process so callers can
    /// maintain independent sequences per column when required.
    /// </summary>
    /// <remarks>
    /// Thread-safe. When the in-process <c>lo</c> counter reaches
    /// <see cref="HiLoBlock.Size"/> the provider requests the next
    /// block via the allocator delegate. The allocator is responsible
    /// for the persistent <c>hi</c> state (e.g. a single-row counter
    /// table on a central shard).
    /// </remarks>
    public sealed class HiLoSequenceProvider : IDistributedSequenceProvider
    {
        /// <summary>
        /// Signature for a <c>hi</c>-block allocator. Invoked when the
        /// in-process counter exhausts the current block. Must return
        /// a fresh <see cref="HiLoBlock"/> whose range does not overlap
        /// with any previously returned block.
        /// </summary>
        public delegate HiLoBlock HiLoBlockAllocator(string entityName, string columnName, int blockSize);

        private readonly HiLoBlockAllocator _allocator;
        private readonly int _blockSize;
        private readonly ConcurrentDictionary<string, SequenceSlot> _slots
            = new ConcurrentDictionary<string, SequenceSlot>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Initialises a new HiLo provider.</summary>
        /// <param name="allocator">Delegate that mints fresh <see cref="HiLoBlock"/>s; must be collision-free across all producers.</param>
        /// <param name="blockSize">Block size to request from the allocator. Defaults to <c>1000</c>. Must be positive.</param>
        public HiLoSequenceProvider(HiLoBlockAllocator allocator, int blockSize = 1000)
        {
            if (allocator == null)  throw new ArgumentNullException(nameof(allocator));
            if (blockSize <= 0)     throw new ArgumentOutOfRangeException(nameof(blockSize));
            _allocator = allocator;
            _blockSize = blockSize;
        }

        /// <inheritdoc/>
        public long NextId(string entityName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));

            var key  = entityName + "::" + columnName;
            var slot = _slots.GetOrAdd(key, _ => new SequenceSlot());
            return slot.NextId(this, entityName, columnName);
        }

        /// <summary>
        /// Single contiguous range of ids minted by the allocator.
        /// <see cref="StartInclusive"/> is the first usable id;
        /// <see cref="Size"/> is the block length.
        /// </summary>
        public readonly struct HiLoBlock
        {
            /// <summary>First usable id.</summary>
            public long StartInclusive { get; }
            /// <summary>Block size (count of ids).</summary>
            public int Size { get; }

            /// <summary>Initialises a new block.</summary>
            public HiLoBlock(long startInclusive, int size)
            {
                if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
                StartInclusive = startInclusive;
                Size           = size;
            }

            /// <summary>Last usable id in this block (inclusive).</summary>
            public long EndInclusive => StartInclusive + Size - 1;
        }

        private sealed class SequenceSlot
        {
            private readonly object _gate = new object();
            private long _next;
            private long _endExclusive;

            internal long NextId(HiLoSequenceProvider owner, string entityName, string columnName)
            {
                lock (_gate)
                {
                    if (_next >= _endExclusive)
                    {
                        var block = owner._allocator(entityName, columnName, owner._blockSize);
                        if (block.Size <= 0)
                            throw new InvalidOperationException(
                                $"HiLo allocator returned an empty block for {entityName}.{columnName}.");
                        _next         = block.StartInclusive;
                        _endExclusive = block.StartInclusive + block.Size;
                    }
                    return _next++;
                }
            }
        }

        // Ensures the Interlocked include is not pruned by linkers in
        // trimming/AOT scenarios; the class uses a lock but callers may
        // subclass and use Interlocked — keep the reference available.
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance", "CA1822",
            Justification = "Anchor to retain Interlocked reference for trimming.")]
        internal static long InterlockedAnchor(ref long l) => Interlocked.Read(ref l);
    }
}
