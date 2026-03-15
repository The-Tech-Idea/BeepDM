using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Bounded <see cref="Channel{T}"/> wrapper that decouples an async source
    /// producer from a downstream consumer.  A capacity of 1 000 records is the
    /// default; tune via the constructor when many parallel steps are running.
    /// </summary>
    public sealed class PipelineChannelBridge
    {
        private readonly Channel<PipelineRecord> _channel;

        public ChannelWriter<PipelineRecord>  Writer => _channel.Writer;
        public ChannelReader<PipelineRecord>  Reader => _channel.Reader;

        public PipelineChannelBridge(int capacity = 1000)
        {
            _channel = Channel.CreateBounded<PipelineRecord>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode        = BoundedChannelFullMode.Wait,
                    SingleWriter    = true,
                    SingleReader    = true
                });
        }

        /// <summary>
        /// Drains <paramref name="source"/> into the channel, then completes the writer.
        /// Intended to run as a background <see cref="Task"/> via <c>Task.Run</c>.
        /// </summary>
        public async Task PumpAsync(
            IAsyncEnumerable<PipelineRecord> source,
            CancellationToken token = default)
        {
            try
            {
                await foreach (var record in source.WithCancellation(token))
                    await _channel.Writer.WriteAsync(record, token);

                _channel.Writer.Complete();
            }
            catch (System.Exception ex)
            {
                _channel.Writer.Complete(ex);
            }
        }

        /// <summary>Expose reader as <see cref="IAsyncEnumerable{T}"/> for <c>await foreach</c>.</summary>
        public IAsyncEnumerable<PipelineRecord> ReadAllAsync(CancellationToken token = default)
            => _channel.Reader.ReadAllAsync(token);
    }
}
