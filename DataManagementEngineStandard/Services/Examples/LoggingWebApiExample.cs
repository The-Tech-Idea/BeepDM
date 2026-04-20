using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Context;
using TheTechIdea.Beep.Services.Telemetry.Redaction;

namespace TheTechIdea.Beep.Services.Examples
{
    /// <summary>
    /// End-to-end sample of <c>AddBeepLoggingForWeb</c> for an ASP.NET
    /// Core Web API host. Intended to be copy-pasted into a project's
    /// <c>Program.cs</c>; nothing here references
    /// <c>Microsoft.AspNetCore</c> so the file compiles inside the
    /// engine assembly.
    /// </summary>
    /// <remarks>
    /// In a real Web API:
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddBeepLoggingForWeb(builder.Environment.ContentRootPath, "MyApi");
    /// // optionally bridge ILogger to BeepLog:
    /// builder.Logging.AddProvider(new MicrosoftLoggerProvider(provider.GetRequiredService&lt;IBeepLog&gt;()));
    /// </code>
    /// The sample below shows the wiring with a fake content root
    /// directory so it can run as a console executable too.
    /// </remarks>
    public static class LoggingWebApiExample
    {
        /// <summary>Suggested entry-point name for the sample.</summary>
        public const string SampleAppName = "BeepLoggingWebSample";

        /// <summary>
        /// Builds a configured <see cref="IServiceProvider"/> with the
        /// web logging preset and runs <see cref="HandleRequestAsync"/>
        /// inside a per-request correlation scope.
        /// </summary>
        public static async Task RunAsync(string contentRootPath, CancellationToken cancellationToken = default)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddBeepLoggingForWeb(contentRootPath, SampleAppName, opt =>
            {
                opt.MinLevel = BeepLogLevel.Information;
                foreach (IRedactor redactor in DefaultRedactionPresets.LogsBalanced())
                {
                    opt.Redactors.Add(redactor);
                }
            });

            ServiceProvider provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(false))
            {
                IBeepLog log = provider.GetRequiredService<IBeepLog>();
                await HandleRequestAsync(log, "/api/orders", "POST", cancellationToken).ConfigureAwait(false);
                await log.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stand-in for an ASP.NET Core middleware/endpoint. Opens a
        /// correlation scope keyed on the request path + method, emits
        /// representative entries, and surfaces an exception path.
        /// </summary>
        public static async Task HandleRequestAsync(
            IBeepLog log,
            string path,
            string method,
            CancellationToken cancellationToken = default)
        {
            if (log is null) { throw new ArgumentNullException(nameof(log)); }

            using (BeepActivityScope.Begin("Http.Request", new Dictionary<string, object>
            {
                ["http.path"] = path,
                ["http.method"] = method
            }))
            {
                log.Info("Request received", new { path, method });
                await Task.Delay(15, cancellationToken).ConfigureAwait(false);

                Dictionary<string, object> sensitive = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["authorization"] = "Bearer eyJhbGciOiJIUzI1NiJ9.payload.signature",
                    ["userId"] = "u-42"
                };
                log.Info("Authorized", sensitive);

                try
                {
                    if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                    {
                        log.Info("Creating order", new { itemCount = 2 });
                    }
                    else
                    {
                        throw new NotSupportedException($"Method {method} not handled by sample");
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Request failed", ex, new { path, method });
                }

                log.Info("Response sent", new { status = 201 });
            }
        }
    }
}
