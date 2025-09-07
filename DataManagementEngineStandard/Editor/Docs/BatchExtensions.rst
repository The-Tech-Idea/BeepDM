BatchExtensions
===============

.. class:: BatchExtensions

   Provides utility extension methods for efficient batch processing of collections in ETL and data processing operations.

   Responsibilities
   ----------------
   - Splits large collections into manageable batch sizes
   - Provides LINQ-style extension methods for batch operations
   - Optimizes memory usage during large data processing
   - Enables parallel processing of batched data
   - Handles edge cases in batch size calculations

   Key Methods
   -----------
   - Batch<T>(): Splits IEnumerable into batches of specified size
   - ProcessInBatches(): Processes collection in batches with callback
   - BatchParallel(): Enables parallel processing of batches
   - OptimalBatchSize(): Calculates optimal batch size for data type

   Typical Flow
   ------------
   1. Receive large collection for processing
   2. Determine optimal batch size based on data characteristics
   3. Split collection into batches using extension methods
   4. Process each batch individually or in parallel
   5. Aggregate results from all batches

   Extension Points
   ----------------
   - Custom batch size calculation strategies
   - Pluggable batch processing algorithms
   - Memory optimization techniques
   - Progress reporting for batch operations

   Usage Example
   -------------
   .. code-block:: csharp

      var largeDataSet = GetLargeDataCollection();
      foreach (var batch in largeDataSet.Batch(1000))
      {
          ProcessBatch(batch);
      }
