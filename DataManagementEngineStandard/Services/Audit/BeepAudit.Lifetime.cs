using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Audit
{
    /// <summary>
    /// Lifetime / shutdown half of <see cref="BeepAudit"/>. Delegates to the
    /// shared <see cref="TheTechIdea.Beep.Services.Telemetry.TelemetryPipeline"/>
    /// so audit and log events drain together.
    /// </summary>
    public sealed partial class BeepAudit
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
