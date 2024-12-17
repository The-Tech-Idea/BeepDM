using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.ETL
{
    public static class BatchExtensions
    {
        /// <summary>
        /// Splits a collection into batches of a specified size.
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="source">The collection to split into batches.</param>
        /// <param name="batchSize">The size of each batch.</param>
        /// <returns>An enumerable of batches, where each batch is a collection of items.</returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));

            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
                yield return batch;
        }
    }
}
