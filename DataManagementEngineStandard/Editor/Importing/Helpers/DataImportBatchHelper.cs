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
        {
            if (batch == null || !batch.Any())
                return CreateErrorsInfo(Errors.Ok, "No records in batch to process");

            if (config?.DestData == null)
                return CreateErrorsInfo(Errors.Failed, "Destination data source not configured");

            try
            {
                var batchList = batch.ToList();
                int recordsProcessed = 0;
                var errors = new List<string>();

                foreach (var record in batchList)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        // Apply transformation pipeline
                        var transformedRecord = _transformationHelper.ApplyTransformationPipeline(record, config);

                        if (transformedRecord == null)
                        {
                            errors.Add($"Record {recordsProcessed + 1}: Transformation resulted in null record");
                            continue;
                        }

                        // Insert the transformed record
                        await Task.Run(() => 
                        {
                            var insertResult = config.DestData.InsertEntity(config.DestEntityName, transformedRecord);
                            if (insertResult?.Flag == Errors.Failed)
                            {
                                errors.Add($"Record {recordsProcessed + 1}: {insertResult.Message}");
                            }
                        }, token);

                        recordsProcessed++;

                        // Report progress for this record
                        _progressHelper.ReportProgress(progress, 
                            $"Processed {recordsProcessed} records in current batch", 
                            recordsProcessed);
                    }
                    catch (Exception recordEx)
                    {
                        var errorMsg = $"Record {recordsProcessed + 1}: {recordEx.Message}";
                        errors.Add(errorMsg);
                        _progressHelper.LogError("Batch Processing", recordEx);
                    }
                }

                // Log batch completion
                var batchMessage = $"Batch completed: {recordsProcessed} records processed";
                if (errors.Any())
                {
                    batchMessage += $", {errors.Count} errors";
                }

                _progressHelper.LogImport(batchMessage, recordsProcessed);

                // Return result based on success rate
                if (errors.Any() && recordsProcessed == 0)
                {
                    return CreateErrorsInfo(Errors.Failed, 
                        $"Batch processing failed completely. Errors: {string.Join("; ", errors)}");
                }
                else if (errors.Any())
                {
                    return CreateErrorsInfo(Errors.Ok, 
                        $"Batch processing completed with {errors.Count} errors out of {batchList.Count} records. " +
                        $"First error: {errors.First()}");
                }
                else
                {
                    return CreateErrorsInfo(Errors.Ok, 
                        $"Batch processing completed successfully. {recordsProcessed} records processed.");
                }
            }
            catch (OperationCanceledException)
            {
                _progressHelper.LogImport("Batch processing was cancelled", 0);
                throw; // Re-throw to let caller handle cancellation
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Batch Processing", ex);
                return CreateErrorsInfo(Errors.Failed, $"Batch processing error: {ex.Message}");
            }
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
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount <= maxRetries)
            {
                try
                {
                    return await ProcessBatchAsync(batch, config, progress, token);
                }
                catch (OperationCanceledException)
                {
                    throw; // Don't retry on cancellation
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;

                    if (retryCount <= maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
                        _progressHelper.LogImport($"Batch processing failed, retrying in {delay.TotalSeconds} seconds (attempt {retryCount + 1}/{maxRetries + 1})", 0);
                        
                        await Task.Delay(delay, token);
                    }
                }
            }

            return CreateErrorsInfo(Errors.Failed, 
                $"Batch processing failed after {maxRetries + 1} attempts. Last error: {lastException?.Message}");
        }

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