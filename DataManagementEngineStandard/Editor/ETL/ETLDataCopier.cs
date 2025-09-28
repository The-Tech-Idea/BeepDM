using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using TheTechIdea.Beep.Exensions;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.ETL
{
    public partial class ETLDataCopier
    {
        private IDMEEditor DMEEditor { get; }
        private EntityStructure SourceEntityStructure;
        private EntityStructure DestEntityStructure;

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
                if (enableParallel)
                {
                    await ParallelInsertDataAsync(destDs, destEntity, transformedData, batchSize, progress, token, maxRetries);
                }
                else
                {
                    await BatchInsertDataAsync(destDs, destEntity, transformedData, batchSize, progress, token, maxRetries);
                }

                stopwatch.Stop();
                DMEEditor.AddLogMessage("ETL", $"Successfully copied data from {srcEntity} to {destEntity} in {stopwatch.Elapsed.TotalSeconds:F2} seconds.", DateTime.Now, -1, null, Errors.Ok);
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
        private async Task<object> FetchSourceDataAsync(IDataSource sourceDs, string srcEntity, CancellationToken token)
        {
            try
            {
                return await Task.Run(() => sourceDs.GetEntity(srcEntity, null), token);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL", $"Error fetching source data for entity {srcEntity}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
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

                if (sourceData is IEnumerable<object> sourceList)
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

            foreach (var batch in batches)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await InsertBatchAsync(destDs, destEntity, batch, progress, token, maxRetries);
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
            var failedRecords = new ConcurrentQueue<object>();

            foreach (var record in batch)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        DMEEditor.AddLogMessage("ETL", "Insert operation was cancelled.", DateTime.Now, -1, null, Errors.Failed);
                        break;
                    }

                    await Task.Run(() => destDs.InsertEntity(destEntity, record), token);
                    progress?.Report(new PassedArgs
                    {
                        Messege = $"Inserted record into {destEntity}",
                        ParameterInt1 = 1
                    });
                }
                catch (Exception ex)
                {
                    failedRecords.Enqueue(record);
                    DMEEditor.AddLogMessage("ETL", $"Error inserting record: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
            }

            // Retry failed records
            if (failedRecords.Count > 0 && maxRetries > 0)
            {
                DMEEditor.AddLogMessage("ETL", $"Retrying {failedRecords.Count} failed records.", DateTime.Now, -1, null, Errors.Ok);
                await InsertBatchAsync(destDs, destEntity, failedRecords, progress, token, maxRetries - 1);
            }
        }
    }

    
}
