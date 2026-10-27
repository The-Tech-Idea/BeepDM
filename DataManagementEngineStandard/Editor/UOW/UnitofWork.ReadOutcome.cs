using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T>
    {
        /// <summary>The last read's own acknowledgement, never a later caller's shared last error.</summary>
        public IErrorsInfo ReadOutcome { get; private set; } = new ErrorsInfo
            { Flag = Errors.Unknown, Message = "No datasource read has been acknowledged." };

        private async Task<IEnumerable<object>> ReadWithOutcomeAsync(IDataSource source, string entityName,
            List<AppFilter> filters, CancellationToken cancellationToken)
        {
            ReadOutcome = new ErrorsInfo { Flag = Errors.Failed, Message = "Datasource read was not acknowledged." };
            if (source is IOwnedEntityReadDataSource owned)
            {
                if (!owned.SupportsOwnedEntityReads) throw new NotSupportedException("This provider cannot acknowledge an owned entity read.");
                var response = await owned.ReadEntityAsync(entityName, filters, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Provider returned no owned entity read acknowledgement.");
                ReadOutcome = response.Outcome;
                if (ReadOutcome.Flag != Errors.Ok)
                    throw new InvalidOperationException(ReadOutcome.Message ?? "Datasource read was refused.", ReadOutcome.Ex);
                return response.Rows;
            }
            var rows = await source.GetEntityAsync(entityName, filters).ConfigureAwait(false);
            CaptureLegacyReadOutcome(source);
            if (ReadOutcome.Flag != Errors.Unknown && ReadOutcome.Flag != Errors.Ok)
                throw new InvalidOperationException(ReadOutcome.Message ?? "Datasource read was refused.", ReadOutcome.Ex);
            return rows;
        }
        private void CaptureLegacyReadOutcome(IDataSource source)
        {
            var outcome = source.ErrorObject;
            ReadOutcome = outcome == null ? new ErrorsInfo { Flag = Errors.Unknown, Message = "Legacy provider supplied no read acknowledgement." }
                : new ErrorsInfo { Flag = outcome.Flag, Message = outcome.Message, Ex = outcome.Ex,
                    Errors = outcome.Errors?.ToList() ?? new List<IErrorsInfo>() };
        }
        private void PublishAcknowledgedRead(IUnitofWorkReadStage stage)
        {
            stage.Publish(publish => publish());
            foreach (var notificationError in stage.NotificationFailures)
            {
                // The new buffer is already published. Surface/report observer faults without
                // turning this acknowledged read into a failed operation that callers might retry.
                ReadOutcome.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = "Read completed but a notification failed.", Ex = notificationError });
                try
                {
                    using var diagnostics = (DMEEditor as IScopedEditorErrors)?.BeginOperationErrors(new ErrorsInfo());
                    DMEEditor?.AddLogMessage("UnitOfWorkRead", notificationError.ToString(), DateTime.Now, 0, EntityName, Errors.Warning);
                }
                catch (Exception reportingError)
                {
                    ReadOutcome.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = "Read notification diagnostics could not be recorded.", Ex = reportingError });
                }
            }
        }
    }
}
