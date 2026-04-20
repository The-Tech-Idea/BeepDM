using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Context;
using TheTechIdea.Beep.Services.Telemetry.Sinks.Platform;

namespace TheTechIdea.Beep.Services.Examples
{
    /// <summary>
    /// End-to-end sample of <c>AddBeepLoggingForBlazor</c> for a Blazor
    /// WebAssembly host. The host project supplies an
    /// <see cref="IIndexedDbBridge"/> implementation backed by
    /// <c>Microsoft.JSInterop</c>; the sample uses an in-memory stub
    /// so the file builds inside the engine assembly.
    /// </summary>
    /// <remarks>
    /// Real Blazor wiring:
    /// <code>
    /// var builder = WebAssemblyHostBuilder.CreateDefault(args);
    /// builder.Services.AddSingleton&lt;IIndexedDbBridge, JsIndexedDbBridge&gt;();
    /// builder.Services.AddBeepLoggingForBlazor(
    ///     builder.Services.BuildServiceProvider().GetRequiredService&lt;IIndexedDbBridge&gt;());
    /// </code>
    /// </remarks>
    public static class LoggingBlazorExample
    {
        /// <summary>
        /// Builds a configured <see cref="IServiceProvider"/> with the
        /// Blazor logging preset, runs <see cref="ExecuteAsync"/>, and
        /// performs a clean shutdown.
        /// </summary>
        public static async Task RunAsync(CancellationToken cancellationToken = default)
        {
            ServiceCollection services = new ServiceCollection();
            InMemoryIndexedDbBridge bridge = new InMemoryIndexedDbBridge();
            services.AddSingleton<IIndexedDbBridge>(bridge);
            services.AddBeepLoggingForBlazor(bridge, opt =>
            {
                opt.MinLevel = BeepLogLevel.Information;
            });

            ServiceProvider provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(false))
            {
                IBeepLog log = provider.GetRequiredService<IBeepLog>();
                await ExecuteAsync(log, cancellationToken).ConfigureAwait(false);
                await log.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emits a Blazor-typical workload: a page-render scope, a
        /// click handler scope, and a property bag containing values
        /// the redactor would normally mask.
        /// </summary>
        public static async Task ExecuteAsync(IBeepLog log, CancellationToken cancellationToken = default)
        {
            if (log is null) { throw new ArgumentNullException(nameof(log)); }

            using (BeepActivityScope.Begin("Page.Render", new Dictionary<string, object>
            {
                ["page"] = "/orders"
            }))
            {
                log.Info("Page render started");
                await Task.Delay(5, cancellationToken).ConfigureAwait(false);

                using (BeepActivityScope.Begin("Button.Click", new Dictionary<string, object>
                {
                    ["component"] = "SubmitButton"
                }))
                {
                    log.Info("Submit clicked", new { itemId = "ITEM-7" });
                }

                log.Info("Page render finished");
            }
        }

        /// <summary>
        /// Minimal in-memory stand-in for the JS-backed
        /// <see cref="IIndexedDbBridge"/>. Only used by the sample so it
        /// can run without a browser.
        /// </summary>
        public sealed class InMemoryIndexedDbBridge : IIndexedDbBridge
        {
            private readonly List<string> _store = new List<string>();
            private long _bytes;

            /// <summary>Number of entries currently in the in-memory store.</summary>
            public int Count
            {
                get { lock (_store) { return _store.Count; } }
            }

            /// <inheritdoc />
            public Task PutBatchAsync(IReadOnlyList<string> lines, CancellationToken cancellationToken)
            {
                if (lines is null) { return Task.CompletedTask; }
                lock (_store)
                {
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line)) { continue; }
                        _store.Add(line);
                        _bytes += line.Length;
                    }
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task<long> EstimateUsedBytesAsync(CancellationToken cancellationToken)
            {
                lock (_store) { return Task.FromResult(_bytes); }
            }

            /// <inheritdoc />
            public Task PruneOldestAsync(long bytesToFree, CancellationToken cancellationToken)
            {
                lock (_store)
                {
                    long freed = 0;
                    while (freed < bytesToFree && _store.Count > 0)
                    {
                        freed += _store[0].Length;
                        _store.RemoveAt(0);
                    }
                    _bytes -= freed;
                    if (_bytes < 0) { _bytes = 0; }
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
