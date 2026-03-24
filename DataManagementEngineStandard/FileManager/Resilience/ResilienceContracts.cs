using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager.Resilience
{
    public sealed class CheckpointPolicy
    {
        public int RowsPerCheckpoint { get; init; } = 10000;
        public long BytesPerCheckpoint { get; init; } = 50 * 1024 * 1024;
        public bool CheckpointOnComplete { get; init; } = true;
    }

    public sealed class DeadLetterEntry
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string JobId { get; init; }
        public long SourceRowIndex { get; init; }
        public string RawLine { get; init; }
        public string ErrorCategory { get; init; }
        public string ErrorMessage { get; init; }
        public string ColumnName { get; init; }
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
        public bool IsResolved { get; init; }
        public string ResolvedBy { get; init; }
    }

    public interface IDeadLetterStore
    {
        Task WriteAsync(DeadLetterEntry entry, CancellationToken ct = default);
        Task<IReadOnlyList<DeadLetterEntry>> GetByJobAsync(string jobId, CancellationToken ct = default);
        Task<IReadOnlyList<DeadLetterEntry>> GetRecentAsync(int limit = 100, CancellationToken ct = default);
        Task ResolveAsync(string entryId, string resolvedBy, string notes, CancellationToken ct = default);
    }

    public sealed class InMemoryDeadLetterStore : IDeadLetterStore
    {
        private readonly List<DeadLetterEntry> _entries = new();
        public Task WriteAsync(DeadLetterEntry entry, CancellationToken ct = default)
        {
            lock (_entries) _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DeadLetterEntry>> GetByJobAsync(string jobId, CancellationToken ct = default)
        {
            lock (_entries)
            {
                return Task.FromResult((IReadOnlyList<DeadLetterEntry>)_entries.FindAll(e => e.JobId == jobId));
            }
        }

        public Task<IReadOnlyList<DeadLetterEntry>> GetRecentAsync(int limit = 100, CancellationToken ct = default)
        {
            lock (_entries)
            {
                var ordered = new List<DeadLetterEntry>(_entries);
                ordered.Sort((a, b) => b.OccurredAt.CompareTo(a.OccurredAt));
                if (ordered.Count > limit) ordered = ordered.GetRange(0, limit);
                return Task.FromResult((IReadOnlyList<DeadLetterEntry>)ordered);
            }
        }

        public Task ResolveAsync(string entryId, string resolvedBy, string notes, CancellationToken ct = default)
        {
            lock (_entries)
            {
                var idx = _entries.FindIndex(e => e.Id == entryId);
                if (idx >= 0)
                {
                    var existing = _entries[idx];
                    _entries[idx] = new DeadLetterEntry
                    {
                        Id = existing.Id,
                        JobId = existing.JobId,
                        SourceRowIndex = existing.SourceRowIndex,
                        RawLine = existing.RawLine,
                        ErrorCategory = existing.ErrorCategory,
                        ErrorMessage = existing.ErrorMessage + (string.IsNullOrWhiteSpace(notes) ? "" : $" | {notes}"),
                        ColumnName = existing.ColumnName,
                        OccurredAt = existing.OccurredAt,
                        IsResolved = true,
                        ResolvedBy = resolvedBy
                    };
                }
            }
            return Task.CompletedTask;
        }
    }
}
