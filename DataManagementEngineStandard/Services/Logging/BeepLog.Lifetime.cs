using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Lifetime / shutdown half of <see cref="BeepLog"/>. Delegates to the
    /// shared <see cref="TheTechIdea.Beep.Services.Telemetry.TelemetryPipeline"/>
    /// so logs and audit events drain together.
    /// </summary>
    public sealed partial class BeepLog
    {
        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return Task.CompletedTask;
            }
            return _pipeline.FlushAsync(_options.ShutdownTimeout, cancellationToken);
        }
    }
}
