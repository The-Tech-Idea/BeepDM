using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Exensions
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
            ArgumentNullException.ThrowIfNull(source);
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

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

        /// <summary>
        /// Splits a collection into batches and applies a projection per batch.
        /// This avoids materializing and keeping intermediate batches alive longer than necessary.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <typeparam name="TResult">Projection result type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="batchSize">Batch size &gt; 0.</param>
        /// <param name="resultSelector">Function applied to each batch.</param>
        /// <returns>Projected results per batch.</returns>
        public static IEnumerable<TResult> Batch<T, TResult>(this IEnumerable<T> source, int batchSize, Func<IReadOnlyList<T>, TResult> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(resultSelector);
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    // Create a read-only view to discourage mutation
                    var ro = new ReadOnlyCollection<T>(batch);
                    yield return resultSelector(ro);
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                var ro = new ReadOnlyCollection<T>(batch);
                yield return resultSelector(ro);
            }
        }

        /// <summary>
        /// Splits a collection into read-only batches of a specified size to avoid external modification of batch contents.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="batchSize">Batch size &gt; 0.</param>
        /// <returns>Batches as read-only lists.</returns>
        public static IEnumerable<IReadOnlyList<T>> BatchReadOnly<T>(this IEnumerable<T> source, int batchSize)
            => Batch(source, batchSize, static b => (IReadOnlyList<T>)b);

        /// <summary>
        /// Splits an async sequence into batches asynchronously.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="source">Async sequence.</param>
        /// <param name="batchSize">Batch size &gt; 0.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>IAsyncEnumerable of batches.</returns>
        public static async IAsyncEnumerable<IReadOnlyList<T>> BatchAsync<T>(
            this IAsyncEnumerable<T> source,
            int batchSize,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

            var batch = new List<T>(batchSize);
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return new ReadOnlyCollection<T>(batch);
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return new ReadOnlyCollection<T>(batch);
            }
        }
    }
}
