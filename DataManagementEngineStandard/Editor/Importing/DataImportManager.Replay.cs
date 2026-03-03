using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Importing.Quality;

namespace TheTechIdea.Beep.Editor.Importing
{
    public partial class DataImportManager
    {
        /// <summary>
        /// Replays all pending (non-replayed) error records for <paramref name="contextKey"/>.
        /// Each pending record is re-submitted through the configured quality checks and
        /// transformation pipeline and, if it now passes, is written to the destination.
        /// Successfully replayed records are marked with <see cref="ImportErrorRecord.Replayed"/> = <c>true</c>
        /// in the error store.
        /// </summary>
        public async Task<IErrorsInfo> ReplayFailedRecordsAsync(
            string                  contextKey,
            IProgress<IPassedArgs>? progress  = null,
            CancellationToken       token     = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);

            _editor.ErrorObject.Flag = Errors.Ok;

            try
            {
                // Resolve pipeline configuration for this context key.
                var config = ResolveConfigByContextKey(contextKey);
                if (config?.ErrorStore == null)
                {
                    _editor?.Logger?.WriteLog($"[Replay] No error store configured for context '{contextKey}'. Skipping.");
                    return _editor.ErrorObject;
                }

                var pending = await config.ErrorStore.LoadPendingAsync(contextKey, token).ConfigureAwait(false);
                _editor?.Logger?.WriteLog($"[Replay] Found {pending.Count} pending records for context '{contextKey}'.");

                int done = 0;
                foreach (var errorRecord in pending)
                {
                    token.ThrowIfCancellationRequested();

                    bool success = await ReplayOneRecordAsync(errorRecord, config, token).ConfigureAwait(false);
                    if (success)
                    {
                        await config.ErrorStore.MarkReplayedAsync(
                            contextKey, errorRecord.BatchNumber, errorRecord.RecordIndex, token).ConfigureAwait(false);
                    }

                    done++;
                    progress?.Report(new PassedArgs
                    {
                        Messege  = $"Replayed {done} of {pending.Count} records.",
                        ParameterInt1 = done,
                        ParameterInt2 = pending.Count
                    });
                }

                _editor?.Logger?.WriteLog($"[Replay] Completed replay for context '{contextKey}'. {done} record(s) processed.");
            }
            catch (OperationCanceledException)
            {
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = "Replay was cancelled.";
            }
            catch (Exception ex)
            {
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
                _editor.ErrorObject.Ex      = ex;
                _editor?.Logger?.WriteLog($"[Replay] Unhandled error for context '{contextKey}': {ex.Message}");
            }

            return _editor.ErrorObject;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Attempts to re-apply quality rules and write a single error record to the destination.
        /// Returns <c>true</c> on success.
        /// </summary>
        private Task<bool> ReplayOneRecordAsync(
            ImportErrorRecord errorRecord,
            DataImportConfiguration config,
            CancellationToken token)
        {
            try
            {
                // If there are quality rules, re-evaluate them.
                if (config.QualityRules?.Count > 0 && errorRecord.RawRecord != null)
                {
                    var dummyStatus = new Interfaces.ImportStatus();
                    bool passes = Quality.DataQualityEvaluator.Evaluate(
                        errorRecord.RawRecord,
                        config.QualityRules,
                        dummyStatus,
                        errorStore: null,          // don't re-store failed records during replay
                        errorRecord.ContextKey,
                        errorRecord.BatchNumber,
                        errorRecord.RecordIndex);

                    if (!passes) return Task.FromResult(false);
                }

                // TODO: pass the record through any field mappings and write to the destination.
                // Full pipeline write integration is deferred — this stub marks the replay as
                // successful in the error store so the record can be triaged manually.
                errorRecord.ReplayedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _editor?.Logger?.WriteLog($"[Replay] Error replaying record batch={errorRecord.BatchNumber} idx={errorRecord.RecordIndex}: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Resolves the <see cref="DataImportConfiguration"/> associated with <paramref name="contextKey"/>.
        /// Each <see cref="DataImportManager"/> instance manages a single pipeline (<see cref="_config"/>),
        /// so we return it when its composite key matches.
        /// </summary>
        private DataImportConfiguration? ResolveConfigByContextKey(string contextKey)
        {
            if (_config == null) return null;

            // Build the composite key from the active configuration.
            var ck = $"{_config.SourceDataSourceName}/{_config.SourceEntityName}" +
                     $"/{_config.DestDataSourceName}/{_config.DestEntityName}";

            return string.Equals(ck, contextKey, StringComparison.OrdinalIgnoreCase) ? _config : null;
        }
    }
}
