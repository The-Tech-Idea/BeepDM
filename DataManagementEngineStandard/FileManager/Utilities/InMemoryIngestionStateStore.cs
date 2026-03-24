using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.FileManager.Contracts;

namespace TheTechIdea.Beep.FileManager.Utilities
{
    public sealed class InMemoryIngestionStateStore : IIngestionStateStore
    {
        private readonly ConcurrentDictionary<string, IFileIngestionDescriptor> _descriptors = new();
        private readonly ConcurrentDictionary<string, IngestionJobStatus> _statuses = new();
        private readonly ConcurrentDictionary<string, IngestionCheckpoint> _checkpoints = new();

        public Task CreateAsync(IFileIngestionDescriptor descriptor, CancellationToken ct = default)
        {
            if (!_descriptors.TryAdd(descriptor.JobId, descriptor))
            {
                throw new InvalidOperationException($"Job '{descriptor.JobId}' already exists.");
            }

            _statuses[descriptor.JobId] = new IngestionJobStatus(
                descriptor.JobId,
                IngestionState.Pending,
                0,
                0,
                DateTimeOffset.UtcNow,
                "Created");

            return Task.CompletedTask;
        }

        public Task TransitionAsync(string jobId, IngestionState newState, string message = null, CancellationToken ct = default)
        {
            if (_statuses.TryGetValue(jobId, out var existing))
            {
                _statuses[jobId] = existing with
                {
                    State = newState,
                    LastUpdatedAt = DateTimeOffset.UtcNow,
                    LastMessage = message ?? newState.ToString()
                };
            }
            return Task.CompletedTask;
        }

        public Task SaveCheckpointAsync(string jobId, long bytesRead, long rowsCommitted, CancellationToken ct = default)
        {
            _checkpoints[jobId] = new IngestionCheckpoint(jobId, bytesRead, rowsCommitted, DateTimeOffset.UtcNow);
            if (_statuses.TryGetValue(jobId, out var existing))
            {
                _statuses[jobId] = existing with { RowsCommitted = rowsCommitted, LastUpdatedAt = DateTimeOffset.UtcNow };
            }
            return Task.CompletedTask;
        }

        public Task<IngestionCheckpoint> GetCheckpointAsync(string jobId, CancellationToken ct = default)
        {
            _checkpoints.TryGetValue(jobId, out var checkpoint);
            return Task.FromResult(checkpoint);
        }

        public Task<IngestionJobStatus> GetStatusAsync(string jobId, CancellationToken ct = default)
        {
            _statuses.TryGetValue(jobId, out var status);
            return Task.FromResult(status);
        }

        public Task<string> FindByChecksumAsync(string sha256Checksum, string targetEntityName, CancellationToken ct = default)
        {
            foreach (KeyValuePair<string, IFileIngestionDescriptor> item in _descriptors)
            {
                if (string.Equals(item.Value.FileChecksum, sha256Checksum, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Value.TargetEntityName, targetEntityName, StringComparison.OrdinalIgnoreCase)
                    && _statuses.TryGetValue(item.Key, out var status)
                    && status.State == IngestionState.Complete)
                {
                    return Task.FromResult(item.Key);
                }
            }
            return Task.FromResult<string>(null);
        }

        public Task<IFileIngestionDescriptor> GetDescriptorAsync(string jobId, CancellationToken ct = default)
        {
            _descriptors.TryGetValue(jobId, out var descriptor);
            return Task.FromResult(descriptor);
        }
    }
}
