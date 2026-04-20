using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TheTechIdea.Beep.Services.Logging.Bridges
{
    /// <summary>
    /// <see cref="ILoggerProvider"/> that funnels every
    /// <see cref="Microsoft.Extensions.Logging.ILogger"/> call into the
    /// supplied <see cref="IBeepLog"/>. Lets ASP.NET Core, EF Core, gRPC,
    /// hosted services, and any other framework using MEL flow into the
    /// Beep telemetry pipeline without duplicate plumbing.
    /// </summary>
    /// <remarks>
    /// Each unique <c>category</c> string passed to
    /// <see cref="CreateLogger"/> is cached; the wrapper itself is cheap
    /// (one <see cref="MicrosoftLoggerAdapter"/> per category) so the
    /// provider scales to the typical few-hundred-category footprint of
    /// a large application.
    /// Disposal is a no-op because the underlying pipeline is owned by
    /// the DI container, not by the provider.
    /// </remarks>
    public sealed class MicrosoftLoggerProvider : ILoggerProvider
    {
        private readonly IBeepLog _log;
        private readonly ConcurrentDictionary<string, MicrosoftLoggerAdapter> _cache
            = new ConcurrentDictionary<string, MicrosoftLoggerAdapter>(StringComparer.Ordinal);

        /// <summary>Creates a provider that forwards to the supplied beep logger.</summary>
        /// <param name="log">Target beep logger; must not be <c>null</c>.</param>
        public MicrosoftLoggerProvider(IBeepLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            string key = categoryName ?? string.Empty;
            return _cache.GetOrAdd(key, name => new MicrosoftLoggerAdapter(_log, name));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _cache.Clear();
        }
    }
}
