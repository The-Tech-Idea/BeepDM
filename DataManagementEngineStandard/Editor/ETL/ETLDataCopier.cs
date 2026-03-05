using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Mapping.Helpers;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.ETL
{
    public partial class ETLDataCopier
    {
        private IDMEEditor DMEEditor { get; }
        private EntityStructure SourceEntityStructure;
        private EntityStructure DestEntityStructure;
        private const int MaxParallelBatches = 4;
        private const int MinBatchSize = 1;
        private const int MaxBatchSize = 5000;
        private const int MaxRetriesBound = 10;

        public ETLDataCopier(IDMEEditor editor)
        {
            DMEEditor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Copies data from a source entity to a destination entity asynchronously with batch processing and optional parallelism.
        /// </summary>
        public async Task<IErrorsInfo> CopyEntityDataAsync(
            IDataSource sourceDs,
            IDataSource destDs,
            string srcEntity,
            string destEntity,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            EntityDataMap_DTL map_DTL = null,
            Func<object, object> customTransformation = null,
            int batchSize = 100,
            bool enableParallel = true,
            int maxRetries = 3)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                var effectiveBatchSize = Math.Max(MinBatchSize, Math.Min(MaxBatchSize, batchSize));
                var effectiveMaxRetries = Math.Max(0, Math.Min(MaxRetriesBound, maxRetries));
                var effectiveParallel = enableParallel && destDs?.Category == DatasourceCategory.RDBMS;

                // Step 0: Get entity structures
                SourceEntityStructure = sourceDs.GetEntityStructure(srcEntity,false);
                DestEntityStructure = destDs.GetEntityStructure(destEntity, false);
                // Step 1: Fetch source data
                var sourceData = await FetchSourceDataAsync(sourceDs, srcEntity, token);
                if (sourceData == null)
                {
                    DMEEditor.AddLogMessage("ETL", $"Source data for entity {srcEntity} is null.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                // Step 2: Transform data
                var transformedData = TransformData(sourceData, destDs.DatasourceName, map_DTL, customTransformation);

                // Step 3: Insert data with batch processing
                if (effectiveParallel)
                {
                    await ParallelInsertDataAsync(destDs, destEntity, transformedData, effectiveBatchSize, progress, token, effectiveMaxRetries);
                }
                else
                {
                    await BatchInsertDataAsync(destDs, destEntity, transformedData, effectiveBatchSize, progress, token, effectiveMaxRetries);
                }

                stopwatch.Stop();
                DMEEditor.AddLogMessage("ETL", $"Successfully copied data from {srcEntity} to {destEntity} in {stopwatch.Elapsed.TotalSeconds:F2} seconds (batch={effectiveBatchSize}, retries={effectiveMaxRetries}, parallel={effectiveParallel}).", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (OperationCanceledException)
            {
                DMEEditor.AddLogMessage("ETL", "Operation was cancelled.", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL", $"Error during data copy: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Fetches data from the source entity.
        /// </summary>
        private Task<object> FetchSourceDataAsync(IDataSource sourceDs, string srcEntity, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult<object>(sourceDs.GetEntity(srcEntity, null));
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL", $"Error fetching source data for entity {srcEntity}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Task.FromResult<object>(null);
            }
        }

        /// <summary>
        /// Transforms data based on mapping and/or custom transformation logic.
        /// </summary>
        private IEnumerable<object> TransformData(object sourceData, string destDataSourceName, EntityDataMap_DTL map_DTL, Func<object, object> customTransformation)
        {
            try
            {
                var transformedList = new List<object>();
                var sourceList = NormalizeSourceRows(sourceData);

                if (sourceList != null)
                {
                    foreach (var record in sourceList)
                    {
                        object transformedRecord = record;

                        // Apply mapping transformation
                        if (map_DTL != null)
                        {
                            transformedRecord = DMEEditor.Utilfunction.MapObjectToAnother(DMEEditor, destDataSourceName, map_DTL, record);
                        }

                        // Apply Defaults using centralized helper (only fills null/default fields)
                        try
                        {
                            MappingDefaultsHelper.ApplyDefaultsToObject(
                                DMEEditor,
                                destDataSourceName,
                                DestEntityStructure?.EntityName,
                                transformedRecord,
                                DestEntityStructure?.Fields);
                        }
                        catch { /* non-fatal */ }

                        // Apply custom transformation
                        if (customTransformation != null)
                        {
                            transformedRecord = customTransformation(transformedRecord);
                        }

                        transformedList.Add(transformedRecord);
                    }
                }

                return transformedList;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL", $"Error during data transformation: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Enumerable.Empty<object>();
            }
        }
        /// <summary>
        /// Normalizes source payloads into an object sequence for transformation.
        /// Handles DataTable, binding lists, generic/non-generic enumerables and single objects.
        /// </summary>
        private IEnumerable<object> NormalizeSourceRows(object sourceData)
        {
            if (sourceData == null)
            {
                return Enumerable.Empty<object>();
            }

            if (sourceData is DataTable table)
            {
                DMTypeBuilder.CreateNewObject(DMEEditor, null, SourceEntityStructure?.EntityName, SourceEntityStructure?.Fields);
                return DMEEditor.Utilfunction.GetListByDataTable(table, DMTypeBuilder.MyType, SourceEntityStructure);
            }

            if (sourceData is IBindingListView listView)
            {
                return listView.Cast<object>().ToList();
            }

            if (sourceData is IEnumerable<object> typed)
            {
                return typed;
            }

            if (sourceData is System.Collections.IEnumerable nonGeneric)
            {
                return nonGeneric.Cast<object>().ToList();
            }

            return new[] { sourceData };
        }

        /// <summary>
        /// Inserts data into the destination in parallel batches.
        /// </summary>
        private async Task ParallelInsertDataAsync(
            IDataSource destDs,
            string destEntity,
            IEnumerable<object> data,
            int batchSize,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            int maxRetries)
        {
            var batches = data.Batch(batchSize);
            var tasks = new List<Task>();
            using var gate = new SemaphoreSlim(MaxParallelBatches, MaxParallelBatches);

            foreach (var batch in batches)
            {
                await gate.WaitAsync(token);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await InsertBatchAsync(destDs, destEntity, batch, progress, token, maxRetries);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Inserts data into the destination in sequential batches.
        /// </summary>
        private async Task BatchInsertDataAsync(
            IDataSource destDs,
            string destEntity,
            IEnumerable<object> data,
            int batchSize,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            int maxRetries)
        {
            var batches = data.Batch(batchSize);

            foreach (var batch in batches)
            {
                await InsertBatchAsync(destDs, destEntity, batch, progress, token, maxRetries);
            }
        }

        /// <summary>
        /// Inserts a single batch of data with retries for failed records.
        /// </summary>
        private async Task InsertBatchAsync(
            IDataSource destDs,
            string destEntity,
            IEnumerable<object> batch,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            int maxRetries)
        {
            var recordsToProcess = batch as IList<object> ?? batch.ToList();
            var attempt = 0;
            var totalInserted = 0;

            while (recordsToProcess.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var failedRecords = new List<object>();
                attempt++;

                foreach (var record in recordsToProcess)
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        destDs.InsertEntity(destEntity, record);
                        totalInserted++;
                        if (progress != null && totalInserted % 100 == 0)
                        {
                            progress.Report(new PassedArgs
                            {
                                Messege = $"Inserted {totalInserted} records into {destEntity} across retry attempts",
                                ParameterInt1 = totalInserted
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        failedRecords.Add(record);
                        DMEEditor.AddLogMessage("ETL", $"Error inserting record (attempt {attempt}): {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }

                if (failedRecords.Count == 0)
                {
                    break;
                }

                if (attempt > maxRetries)
                {
                    DMEEditor.AddLogMessage("ETL", $"Exhausted retries for {failedRecords.Count} records in {destEntity}.", DateTime.Now, -1, null, Errors.Failed);
                    break;
                }

                DMEEditor.AddLogMessage("ETL", $"Retrying {failedRecords.Count} failed records (attempt {attempt}/{maxRetries}).", DateTime.Now, -1, null, Errors.Ok);
                recordsToProcess = failedRecords;
                await Task.Delay(Math.Min(1000, 150 * attempt), token);
            }
        }
    }

    
}
