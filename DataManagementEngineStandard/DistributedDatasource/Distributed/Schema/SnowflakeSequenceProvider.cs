using System;
using System.Threading;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Snowflake-style <see cref="IDistributedSequenceProvider"/>.
    /// Produces 63-bit ids composed of:
    /// <list type="bullet">
    ///   <item>41 bits — milliseconds since <see cref="Epoch"/>.</item>
    ///   <item>10 bits — node id (supplied at construction, 0..1023).</item>
    ///   <item>12 bits — per-ms sequence counter (0..4095).</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Thread-safe. Ids are monotonically increasing within a single
    /// process / node id pair. When two threads race for the same
    /// millisecond the counter increments; when the counter exhausts
    /// the generator spins until the next millisecond boundary.
    /// </remarks>
    public sealed class SnowflakeSequenceProvider : IDistributedSequenceProvider
    {
        /// <summary>Custom Snowflake epoch: 2024-01-01T00:00:00Z.</summary>
        public static readonly DateTime Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const int  NodeBits     = 10;
        private const int  SequenceBits = 12;
        private const long NodeMax      = (1L << NodeBits)     - 1;
        private const long SequenceMax  = (1L << SequenceBits) - 1;
        private const int  NodeShift    = SequenceBits;
        private const int  TimeShift    = SequenceBits + NodeBits;

        private readonly long _nodeId;
        private readonly object _gate = new object();
        private long _lastTimestamp = -1L;
        private long _sequence;

        /// <summary>Initialises a new provider bound to the given node id.</summary>
        /// <param name="nodeId">Node identifier in the range [0, 1023]. Must be globally unique across all producers.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="nodeId"/> is out of range.</exception>
        public SnowflakeSequenceProvider(long nodeId)
        {
            if (nodeId < 0 || nodeId > NodeMax)
                throw new ArgumentOutOfRangeException(
                    nameof(nodeId), nodeId,
                    $"Node id must be in range [0, {NodeMax}].");
            _nodeId = nodeId;
        }

        /// <inheritdoc/>
        public long NextId(string entityName, string columnName)
        {
            // entityName / columnName are ignored — one global id space.
            lock (_gate)
            {
                var now = CurrentMillis();
                if (now == _lastTimestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMax;
                    if (_sequence == 0)
                    {
                        now = WaitForNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }

                if (now < _lastTimestamp)
                {
                    // Clock moved backwards — refuse rather than risk duplicates.
                    throw new InvalidOperationException(
                        "Snowflake sequence detected backwards clock movement. " +
                        $"Last timestamp={_lastTimestamp}, now={now}.");
                }

                _lastTimestamp = now;
                return (now << TimeShift) | (_nodeId << NodeShift) | _sequence;
            }
        }

        private static long CurrentMillis()
            => (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;

        private static long WaitForNextMillis(long lastTimestamp)
        {
            long now;
            do
            {
                Thread.SpinWait(16);
                now = CurrentMillis();
            } while (now <= lastTimestamp);
            return now;
        }
    }
}
