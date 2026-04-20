using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Opt-in <see cref="IHostedService"/> that periodically captures a
    /// <see cref="MetricsSnapshot"/> and either emits it as a self-event,
    /// writes it to a single rolling file, or both. The service is kept
    /// off by default so production hosts pay nothing until an operator
    /// opts in to long-running observability.
    /// </summary>
    /// <remarks>
    /// Notes:
    /// <list type="bullet">
    /// <item><description>The service ticks on a single timer; an
    /// in-flight snapshot is never re-entered.</description></item>
    /// <item><description>Disk writes go through a temp+rename so a
    /// crash mid-write never produces a half-written snapshot.</description></item>
    /// <item><description>Self-event emission is rate-controlled by
    /// <see cref="SelfEventEmitter"/> so flooding cannot occur even with
    /// a 1-second interval.</description></item>
    /// </list>
    /// </remarks>
    public sealed class PeriodicMetricsSnapshotHostedService : IHostedService, IAsyncDisposable
    {
        private readonly TelemetryPipeline _pipeline;
        private readonly SelfEventEmitter _selfEvents;
        private readonly TimeSpan _interval;
        private readonly bool _emitSelfEvents;
        private readonly string _outputPath;
        private readonly MetricsSnapshotFormat _format;

        private Timer _timer;
        private int _running;
        private int _disposed;

        public PeriodicMetricsSnapshotHostedService(
            TelemetryPipeline pipeline,
            SelfEventEmitter selfEvents,
            TimeSpan interval,
            bool emitSelfEvents = true,
            string outputPath = null,
            MetricsSnapshotFormat format = MetricsSnapshotFormat.Text)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _selfEvents = selfEvents;
            _interval = interval <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : interval;
            _emitSelfEvents = emitSelfEvents;
            _outputPath = string.IsNullOrWhiteSpace(outputPath) ? null : outputPath;
            _format = format;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return Task.CompletedTask;
            }
            _timer = new Timer(OnTick, state: null, dueTime: _interval, period: _interval);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await DisposeAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            Timer t = _timer;
            _timer = null;
            if (t is not null)
            {
                await t.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void OnTick(object state)
        {
            if (Interlocked.Exchange(ref _running, 1) == 1)
            {
                return; // skip overlapping tick
            }
            try
            {
                MetricsSnapshot snapshot = _pipeline.Metrics?.Snapshot();
                if (snapshot is null)
                {
                    return;
                }

                if (_outputPath is not null)
                {
                    TryWriteToDisk(snapshot);
                }
                if (_emitSelfEvents && _selfEvents is not null)
                {
                    EmitSelfEvent(snapshot);
                }
            }
            catch
            {
                // Diagnostics must never escape into the host.
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        private void EmitSelfEvent(MetricsSnapshot snapshot)
        {
            try
            {
                string body = MetricsSnapshotRenderer.Render(snapshot, MetricsSnapshotFormat.Json);
                _selfEvents.Emit(
                    category: SelfEventCategory.Snapshot,
                    dedupKey: snapshot.PipelineName,
                    level: BeepLogLevel.Information,
                    message: "metrics-snapshot",
                    properties: new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["snapshot.json"] = body
                    });
            }
            catch
            {
                // best-effort
            }
        }

        private void TryWriteToDisk(MetricsSnapshot snapshot)
        {
            try
            {
                string text = MetricsSnapshotRenderer.Render(snapshot, _format);
                string dir = Path.GetDirectoryName(_outputPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string tmp = _outputPath + ".tmp";
                File.WriteAllText(tmp, text);
                if (File.Exists(_outputPath))
                {
                    File.Delete(_outputPath);
                }
                File.Move(tmp, _outputPath);
            }
            catch
            {
                // best-effort - never throw from the timer
            }
        }
    }
}
