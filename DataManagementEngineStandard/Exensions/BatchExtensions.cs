using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        /// <summary>
        /// Processes items in batches with bounded parallelism.
        /// </summary>
        public static async Task ProcessInBatchesAsync<T>(
            this IEnumerable<T> source,
            int batchSize,
            int degreeOfParallelism,
            Func<IReadOnlyList<T>, CancellationToken, Task> processor,
            bool stopOnFirstError = true,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(processor);
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (degreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism));

            using var gate = new SemaphoreSlim(degreeOfParallelism, degreeOfParallelism);
            var tasks = new List<Task>();
            var errors = new List<Exception>();

            foreach (var batch in source.BatchReadOnly(batchSize))
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    gate.Release();
                    break;
                }

                var localBatch = batch;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await processor(localBatch, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                        {
                            errors.Add(ex);
                        }
                        if (stopOnFirstError)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        gate.Release();
                    }
                }, cancellationToken));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch when (stopOnFirstError)
            {
                if (errors.Count > 1)
                {
                    throw new AggregateException(errors);
                }
                throw;
            }

            if (errors.Count > 0 && !stopOnFirstError)
            {
                throw new AggregateException(errors);
            }
        }

        /// <summary>
        /// Produces sliding windows of a given size with optional overlap.
        /// </summary>
        public static IEnumerable<IReadOnlyList<T>> Window<T>(this IEnumerable<T> source, int windowSize, int overlap = 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (windowSize <= 0) throw new ArgumentOutOfRangeException(nameof(windowSize));
            if (overlap < 0 || overlap >= windowSize) throw new ArgumentOutOfRangeException(nameof(overlap));

            var buffer = new List<T>(windowSize);
            foreach (var item in source)
            {
                buffer.Add(item);
                if (buffer.Count == windowSize)
                {
                    yield return new ReadOnlyCollection<T>(buffer);
                    var skip = windowSize - overlap;
                    if (skip >= buffer.Count)
                    {
                        buffer.Clear();
                    }
                    else
                    {
                        buffer = buffer.Skip(skip).ToList();
                    }
                }
            }

            if (buffer.Count > 0 && buffer.Count == windowSize)
            {
                yield return new ReadOnlyCollection<T>(buffer);
            }
        }

        /// <summary>
        /// Buffers an async sequence with a soft cap to limit in-memory items and yields batches as they fill.
        /// </summary>
        public static async IAsyncEnumerable<IReadOnlyList<T>> BufferAsync<T>(
            this IAsyncEnumerable<T> source,
            int batchSize,
            int maxBuffer,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (maxBuffer < batchSize) throw new ArgumentOutOfRangeException(nameof(maxBuffer));

            var buffer = new List<T>(maxBuffer);

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                buffer.Add(item);

                while (buffer.Count >= batchSize)
                {
                    var segment = buffer.Take(batchSize).ToList();
                    buffer.RemoveRange(0, batchSize);
                    yield return new ReadOnlyCollection<T>(segment);
                }

                if (buffer.Count > maxBuffer)
                {
                    var overflow = buffer.Take(batchSize).ToList();
                    buffer.RemoveRange(0, Math.Min(batchSize, buffer.Count));
                    yield return new ReadOnlyCollection<T>(overflow);
                }
            }

            if (buffer.Count > 0)
            {
                yield return new ReadOnlyCollection<T>(buffer.ToList());
            }
        }

        /// <summary>
        /// Executes a callback per page with page index and batch content.
        /// </summary>
        public static async Task ForEachPageAsync<T>(
            this IEnumerable<T> source,
            int pageSize,
            Func<int, IReadOnlyList<T>, Task> onPage,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(onPage);
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var pageIndex = 0;
            foreach (var batch in source.BatchReadOnly(pageSize))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await onPage(pageIndex++, batch).ConfigureAwait(false);
            }
        }
    }
}
