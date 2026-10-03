using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.Importing.Quality;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    /// <summary>
    /// Helper class for batch processing operations in data import
    /// </summary>
    public class DataImportBatchHelper : IDataImportBatchHelper
    {
        private readonly IDMEEditor _editor;
        private readonly IDataImportTransformationHelper _transformationHelper;
        private readonly IDataImportProgressHelper _progressHelper;

        public DataImportBatchHelper(IDMEEditor editor, 
            IDataImportTransformationHelper transformationHelper,
            IDataImportProgressHelper progressHelper)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _transformationHelper = transformationHelper ?? throw new ArgumentNullException(nameof(transformationHelper));
            _progressHelper = progressHelper ?? throw new ArgumentNullException(nameof(progressHelper));
        }

        /// <summary>
        /// Calculates optimal batch size based on data characteristics
        /// </summary>
        public int CalculateOptimalBatchSize(int totalRecords, long estimatedRecordSize, long? availableMemory = null)
        {
            try
            {
                // Default target memory per batch: 10MB
                const long defaultTargetMemory = 10 * 1024 * 1024; // 10MB
                
                long targetBatchMemory = availableMemory.HasValue 
                    ? Math.Min(availableMemory.Value / 4, defaultTargetMemory) // Use 25% of available memory max
                    : defaultTargetMemory;

                // Calculate batch size based on memory target
                int calculatedBatchSize = estimatedRecordSize > 0 
                    ? (int)(targetBatchMemory / estimatedRecordSize)
                    : 100; // Default fallback

                // Apply bounds based on total record count and best practices
                if (totalRecords < 1000)
                {
                    return Math.Max(10, Math.Min(calculatedBatchSize, 100));
                }
                else if (totalRecords < 10000)
                {
                    return Math.Max(50, Math.Min(calculatedBatchSize, 250));
                }
                else if (totalRecords < 100000)
                {
                    return Math.Max(100, Math.Min(calculatedBatchSize, 500));
                }
                else
                {
                    return Math.Max(200, Math.Min(calculatedBatchSize, 1000));
                }
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error calculating optimal batch size: {ex.Message}. Using default size of 100.");
                return 100;
            }
        }

        /// <summary>
        /// Processes a batch of records
        /// </summary>
        public async Task<IErrorsInfo> ProcessBatchAsync(IEnumerable<object> batch, DataImportConfiguration config,
            IProgress<PassedArgs> progress, CancellationToken token)
            => await ProcessBatchDetailedAsync(batch, config, progress, token,
                config?.OnBatchError == BatchErrorStrategy.Retry ? config.MaxRetries : 0).ConfigureAwait(false);

        public async Task<ImportExecutionResult> ProcessBatchDetailedAsync(IEnumerable<object> batch,
            DataImportConfiguration config, IProgress<PassedArgs> progress, CancellationToken token,
            int maxRetries = 0)
        {
            if (config?.DestData == null)
            {
                var failed = new ImportExecutionResult();
                failed.Complete(ImportOutcome.Failed, "Destination data source not configured");
                return failed;
            }
            try
            {
                var defaults = ImportDefaultsAdmission.Capture(_editor, config, token);
                config = defaults.Bind(_editor, config);
                using var resolverScope = defaults.EnterResolutionScope();
                var admission = ImportQualityAdmission.Capture(config, token);
                await admission.ValidateStoreAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                return await ProcessBatchWithAdmissionAsync(batch, config, progress, token, maxRetries, admission).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                var cancelled = new ImportExecutionResult();
                cancelled.Complete(ImportOutcome.Cancelled, "Batch processing was cancelled.");
                return cancelled;
            }
            catch (TheTechIdea.Beep.Editor.Defaults.DefaultCatalogReadException)
            {
                var failed = new ImportExecutionResult { TransformationAdmissionFailed = true };
                failed.Complete(ImportOutcome.Failed, "Required defaults catalog admission failed.");
                return failed;
            }
            catch (ImportQualityAdmissionException)
            {
                var failed = new ImportExecutionResult { QualityAdmissionFailed = true };
                failed.Complete(ImportOutcome.Failed, "Required record-quality admission configuration failed.");
                return failed;
            }
        }

        internal async Task<ImportExecutionResult> ProcessBatchWithAdmissionAsync(IEnumerable<object> batch,
            DataImportConfiguration config, IProgress<PassedArgs> progress, CancellationToken token,
            int maxRetries, ImportQualityAdmission admission)
        {
            var result = new ImportExecutionResult { RunId = admission.RunId };
            if (config?.DestData == null)
            {
                result.Complete(ImportOutcome.Failed, "Destination data source not configured");
                return result;
            }

            try
            {
                token.ThrowIfCancellationRequested();
                foreach (var record in batch ?? Enumerable.Empty<object>())
                {
                    token.ThrowIfCancellationRequested();
                    result.RecordsAttempted++;
                    object transformed;
                    try
                    {
                        if (_transformationHelper is IDataImportTransformationOutcome typed)
                        {
                            var transformation = typed.TransformRecord(record, config, token);
                            token.ThrowIfCancellationRequested();
                            if (transformation?.Succeeded != true)
                            {
                                result.RecordsTransformationFailed++;
                                AddFailure(result, transformation?.Message ?? "Required transformation returned no outcome.", null);
                                continue;
                            }
                            transformed = transformation.Record;
                        }
                        else
                        {
                            transformed = _transformationHelper.ApplyTransformationPipeline(record, config);
                            token.ThrowIfCancellationRequested();
                        }
                        if (transformed == null)
                            throw new InvalidOperationException("Transformation resulted in null record");
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        token.ThrowIfCancellationRequested();
                        result.RecordsTransformationFailed++;
                        AddFailure(result, ex is ImportTransformationException failure
                            ? failure.Message : "Required transformation failed.", null);
                        continue;
                    }

                    if (!await admission.AdmitAsync(transformed, result, token).ConfigureAwait(false)) continue;

                    IErrorsInfo acknowledgement = null;
                    Exception writeException = null;
                    for (int attempt = 0; attempt <= Math.Max(0, maxRetries); attempt++)
                    {
                        token.ThrowIfCancellationRequested();
                        result.WriteAttempts++;
                        try
                        {
                            acknowledgement = await Task.Run(() =>
                                config.DestData.InsertEntity(config.DestEntityName, transformed), token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            result.HasUncertainWrites = true;
                            throw;
                        }
                        catch (Exception ex)
                        {
                            // A provider may throw after applying the write. Never replay it blindly.
                            writeException = ex;
                            result.HasUncertainWrites = true;
                            break;
                        }

                        if (acknowledgement?.Flag == Errors.Ok) break;
                        if (acknowledgement == null || acknowledgement.Flag != Errors.Failed)
                        {
                            result.HasUncertainWrites = true;
                            break;
                        }
                        if (attempt < Math.Max(0, maxRetries))
                            await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), token).ConfigureAwait(false);
                    }

                    if (writeException == null && acknowledgement?.Flag == Errors.Ok)
                    {
                        result.RecordsSucceeded++;
                        _progressHelper.ReportProgress(progress,
                            $"Acknowledged {result.RecordsSucceeded} writes in current batch", result.RecordsSucceeded);
                    }
                    else
                    {
                        AddFailure(result, writeException?.Message ?? acknowledgement?.Message ??
                            "Datasource returned no write acknowledgement", writeException);
                    }
                }
                result.Complete(result.RecordsFailed == 0 ? ImportOutcome.Completed :
                    result.RecordsSucceeded > 0 ? ImportOutcome.Partial : ImportOutcome.Failed,
                    $"Batch completed: {result.RecordsSucceeded} acknowledged writes, {result.RecordsFailed} failed records.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                result.Complete(ImportOutcome.Cancelled, "Batch processing was cancelled.");
            }
            catch (Exception ex)
            {
                result.Complete(ImportOutcome.Failed, $"Batch processing error: {ex.Message}");
                result.Ex = ex;
            }
            _progressHelper.LogImport(result.Message, result.RecordsSucceeded);
            return result;
        }

        private static void AddFailure(ImportExecutionResult result, string message, Exception exception)
        {
            result.RecordsFailed++;
            result.Errors.Add(new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = $"Record {result.RecordsAttempted}: {message}",
                Ex = exception
            });
        }

        /// <summary>
        /// Splits source data into batches
        /// </summary>
        public IEnumerable<IEnumerable<object>> SplitIntoBatches(IEnumerable<object> sourceData, int batchSize)
        {
            if (sourceData == null)
                yield break;

            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));

            var batch = new List<object>(batchSize);

            foreach (var item in sourceData)
            {
                batch.Add(item);

                if (batch.Count >= batchSize)
                {
                    yield return batch.ToList(); // Return a copy
                    batch.Clear();
                }
            }

            // Return remaining items if any
            if (batch.Count > 0)
            {
                yield return batch.ToList();
            }
        }

        /// <summary>
        /// Estimates memory usage for a batch
        /// </summary>
        public long EstimateBatchMemoryUsage(IEnumerable<object> batch, long estimatedRecordSize)
        {
            if (batch == null)
                return 0;

            try
            {
                var batchCount = batch.Count();
                return batchCount * estimatedRecordSize;
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error estimating batch memory usage: {ex.Message}");
                return estimatedRecordSize * 100; // Fallback estimate
            }
        }

        /// <summary>
        /// Validates batch configuration
        /// </summary>
        public IErrorsInfo ValidateBatchConfiguration(DataImportConfiguration config, int batchSize)
        {
            if (config == null)
                return CreateErrorsInfo(Errors.Failed, "Import configuration is null");

            if (batchSize <= 0)
                return CreateErrorsInfo(Errors.Failed, "Batch size must be greater than 0");

            if (batchSize > 10000)
                return CreateErrorsInfo(Errors.Failed, "Batch size too large (max 10,000 recommended)");

            if (config.DestData == null)
                return CreateErrorsInfo(Errors.Failed, "Destination data source not configured");

            if (string.IsNullOrEmpty(config.DestEntityName))
                return CreateErrorsInfo(Errors.Failed, "Destination entity name not specified");

            return CreateErrorsInfo(Errors.Ok, "Batch configuration is valid");
        }

        /// <summary>
        /// Processes batches with retry logic
        /// </summary>
        public async Task<IErrorsInfo> ProcessBatchWithRetryAsync(IEnumerable<object> batch, DataImportConfiguration config,
            IProgress<PassedArgs> progress, CancellationToken token, int maxRetries = 3)
            => await ProcessBatchDetailedAsync(batch, config, progress, token, maxRetries).ConfigureAwait(false);

        /// <summary>
        /// Creates an IErrorsInfo object with the specified flag and message
        /// </summary>
        private IErrorsInfo CreateErrorsInfo(Errors flag, string message)
        {
            return new ErrorsInfo
            {
                Flag = flag,
                Message = message
            };
        }
    }
}
