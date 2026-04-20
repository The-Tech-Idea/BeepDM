using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry.Retention;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Subscribes <see cref="PipelineMetrics"/> + <see cref="SelfEventEmitter"/>
    /// to existing pipeline events:
    /// <list type="bullet">
    ///   <item><see cref="IBudgetEnforcer.Swept"/> → sweeper / budget metrics + self event.</item>
    ///   <item><see cref="TelemetryPipeline.SinkErrored"/> → sink-error metric + self event.</item>
    /// </list>
    /// Hooks are detachable via the returned <see cref="IDisposable"/> so
    /// the pipeline can clean up during shutdown without leaving dangling
    /// subscriptions on the singleton enforcer.
    /// </summary>
    public sealed class PipelineDiagnosticsHooks : IDisposable
    {
        private readonly PipelineMetrics _metrics;
        private readonly SelfEventEmitter _selfEvents;
        private readonly IBudgetEnforcer _enforcer;
        private readonly TelemetryPipeline _pipeline;
        private Action<BudgetSweepResult> _sweptHandler;
        private Action<string, Exception> _sinkErrorHandler;
        private int _disposed;

        /// <summary>Creates a new hook bag and immediately attaches every subscription.</summary>
        public PipelineDiagnosticsHooks(
            PipelineMetrics metrics,
            SelfEventEmitter selfEvents,
            TelemetryPipeline pipeline,
            IBudgetEnforcer enforcer)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _selfEvents = selfEvents;
            _pipeline = pipeline;
            _enforcer = enforcer;

            if (_enforcer is not null)
            {
                _sweptHandler = OnSwept;
                _enforcer.Swept += _sweptHandler;
            }
            if (_pipeline is not null)
            {
                _sinkErrorHandler = OnSinkErrored;
                _pipeline.SinkErrored += _sinkErrorHandler;
            }
        }

        private void OnSwept(BudgetSweepResult result)
        {
            if (result is null)
            {
                return;
            }

            _metrics.AddSweeperDeletes(result.FilesDeleted);
            _metrics.AddSweeperCompress(result.FilesCompressed);

            if (result.BudgetBreachTriggered)
            {
                _metrics.IncrementBudgetBreach();
            }
            _metrics.SetBlockingWrites(result.BlockingNewWrites);

            if (_selfEvents is null)
            {
                return;
            }

            if (result.BudgetBreachTriggered || result.BlockingNewWrites || !string.IsNullOrEmpty(result.LastError))
            {
                BeepLogLevel level = result.BlockingNewWrites
                    ? BeepLogLevel.Error
                    : (result.BudgetBreachTriggered ? BeepLogLevel.Warning : BeepLogLevel.Information);

                Dictionary<string, object> props = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["scope"] = result.ScopeName,
                    ["directory"] = result.Directory,
                    ["bytesBefore"] = result.TotalBytesBefore,
                    ["bytesAfter"] = result.TotalBytesAfter,
                    ["filesDeleted"] = result.FilesDeleted,
                    ["filesCompressed"] = result.FilesCompressed,
                    ["action"] = result.ActionTaken.ToString(),
                    ["blocking"] = result.BlockingNewWrites
                };
                if (!string.IsNullOrEmpty(result.LastError))
                {
                    props["error"] = result.LastError;
                }

                string category = result.BudgetBreachTriggered ? SelfEventCategory.Budget : SelfEventCategory.Retention;
                string dedupKey = string.Concat(result.ScopeName ?? string.Empty, "|", result.Directory ?? string.Empty);
                _selfEvents.Emit(category, dedupKey, level,
                    BuildSweepMessage(result), props);
            }
        }

        private void OnSinkErrored(string sinkName, Exception ex)
        {
            _metrics.IncrementSinkError();

            if (_selfEvents is null)
            {
                return;
            }

            Dictionary<string, object> props = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sink"] = sinkName ?? "(unknown)"
            };
            if (ex is not null)
            {
                props["exception"] = ex.GetType().FullName;
            }

            _selfEvents.Emit(
                SelfEventCategory.Sink,
                sinkName ?? "(unknown)",
                BeepLogLevel.Warning,
                $"Sink '{sinkName}' write failed.",
                props,
                ex);
        }

        private static string BuildSweepMessage(BudgetSweepResult result)
        {
            if (result.BlockingNewWrites)
            {
                return $"Storage budget breached for '{result.ScopeName}' ({result.Directory}); blocking new writes.";
            }
            if (result.BudgetBreachTriggered)
            {
                return $"Storage budget breached for '{result.ScopeName}' ({result.Directory}); action={result.ActionTaken}.";
            }
            if (!string.IsNullOrEmpty(result.LastError))
            {
                return $"Retention sweep error for '{result.ScopeName}': {result.LastError}";
            }
            return $"Retention sweep for '{result.ScopeName}' deleted {result.FilesDeleted} file(s).";
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            try
            {
                if (_enforcer is not null && _sweptHandler is not null)
                {
                    _enforcer.Swept -= _sweptHandler;
                }
            }
            catch
            {
                // best-effort detach
            }
            try
            {
                if (_pipeline is not null && _sinkErrorHandler is not null)
                {
                    _pipeline.SinkErrored -= _sinkErrorHandler;
                }
            }
            catch
            {
                // best-effort detach
            }
        }
    }
}
