using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.SetUp.State;

namespace TheTechIdea.Beep.SetUp.State
{
    /// <summary>
    /// Enterprise <see cref="ISetupStateStore"/>: shared state behind an ETag-versioned transport,
    /// with optimistic concurrency on save and a lease resource for exclusion.
    /// </summary>
    /// <remarks>
    /// The local store's file lease only guards processes on one machine. Here two runners may be
    /// on different machines, so the transport's compare-and-set (ETag / If-Match) is the source of
    /// truth: a stale save is refused rather than silently interleaved.
    /// </remarks>
    public sealed class RemoteSetupStateStore : ISetupStateStore
    {
        private readonly ISetupStateTransport _transport;
        private readonly ILogger _logger;

        // Remembers the ETag last seen per key so SaveAsync can present If-Match. A store instance
        // serves one wizard at a time in practice; ConcurrentDictionary keeps multi-key use safe.
        private readonly ConcurrentDictionary<string, string> _stateETags = new(StringComparer.Ordinal);

        public RemoteSetupStateStore(ISetupStateTransport transport, ILogger logger = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _logger = logger;
        }

        private static string StateResource(SetupStateKey key) => key.ToToken() + ".state";
        private static string LeaseResource(SetupStateKey key) => key.ToToken() + ".lease";

        public async Task<SetupState> LoadAsync(SetupStateKey key, CancellationToken token = default)
        {
            var entry = await _transport.GetAsync(StateResource(key), token).ConfigureAwait(false);
            if (entry == null) return null;

            _stateETags[key.ToToken()] = entry.ETag;
            try
            {
                return JsonSerializer.Deserialize<SetupState>(entry.Body);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex,
                    "Remote setup state for '{Key}' is unreadable; this run will start fresh.", key);
                return null;
            }
        }

        public async Task SaveAsync(SetupStateKey key, SetupState state, ISetupStateLease lease = null,
            CancellationToken token = default)
        {
            // A held lease that has since been lost means another runner took over — don't clobber.
            if (lease is RemoteLease rl && !await rl.IsStillHeldAsync(token).ConfigureAwait(false))
                throw new SetupStateConflictException(
                    $"Lease on '{key}' was lost; refusing to save stale state.");

            state.Revision++;
            _stateETags.TryGetValue(key.ToToken(), out var etag);

            try
            {
                var newETag = await _transport
                    .PutAsync(StateResource(key), JsonSerializer.Serialize(state), etag, token)
                    .ConfigureAwait(false);
                _stateETags[key.ToToken()] = newETag;
            }
            catch (SetupStateConflictException)
            {
                _logger?.LogError(
                    "Concurrent write to setup state '{Key}' detected; another runner advanced it since " +
                    "this run last loaded. Refusing to overwrite.", key);
                throw;
            }
        }

        public async Task<ISetupStateLease> TryAcquireLeaseAsync(SetupStateKey key, TimeSpan ttl,
            CancellationToken token = default)
        {
            var resource = LeaseResource(key);
            var current = await _transport.GetAsync(resource, token).ConfigureAwait(false);

            LeaseRecord existing = null;
            if (current != null)
            {
                try { existing = JsonSerializer.Deserialize<LeaseRecord>(current.Body); }
                catch { existing = null; }
            }

            if (existing != null && existing.ExpiresAt > DateTimeOffset.UtcNow)
            {
                _logger?.LogWarning(
                    "Setup state '{Key}' is leased by run {RunId} until {Expiry:o}; not acquiring.",
                    key, existing.RunId, existing.ExpiresAt);
                return null;
            }

            var record = new LeaseRecord
            {
                RunId = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTimeOffset.UtcNow.Add(ttl)
            };

            try
            {
                // If-Match the ETag we saw (null → create-only). A racing acquirer makes this fail.
                var etag = await _transport
                    .PutAsync(resource, JsonSerializer.Serialize(record), current?.ETag, token)
                    .ConfigureAwait(false);
                return new RemoteLease(key, resource, record, etag, ttl, _transport, _logger);
            }
            catch (SetupStateConflictException)
            {
                _logger?.LogWarning(
                    "Lost the race to acquire the setup lease on '{Key}'; another runner got it first.", key);
                return null;
            }
        }

        private sealed class LeaseRecord
        {
            public string RunId { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
        }

        private sealed class RemoteLease : ISetupStateLease
        {
            private readonly string _resource;
            private readonly TimeSpan _ttl;
            private readonly ISetupStateTransport _transport;
            private readonly ILogger _logger;
            private LeaseRecord _record;
            private string _etag;
            private bool _released;

            public RemoteLease(SetupStateKey key, string resource, LeaseRecord record, string etag,
                TimeSpan ttl, ISetupStateTransport transport, ILogger logger)
            {
                Key = key;
                _resource = resource;
                _record = record;
                _etag = etag;
                _ttl = ttl;
                _transport = transport;
                _logger = logger;
            }

            public SetupStateKey Key { get; }
            public string RunId => _record.RunId;
            public DateTimeOffset ExpiresAt => _record.ExpiresAt;

            public async Task<bool> IsStillHeldAsync(CancellationToken token)
            {
                if (_released) return false;
                var current = await _transport.GetAsync(_resource, token).ConfigureAwait(false);
                if (current == null) return false;
                try
                {
                    var r = JsonSerializer.Deserialize<LeaseRecord>(current.Body);
                    return r != null && r.RunId == _record.RunId && r.ExpiresAt > DateTimeOffset.UtcNow;
                }
                catch { return false; }
            }

            public async Task<bool> RenewAsync(CancellationToken token = default)
            {
                if (_released) return false;
                var renewed = new LeaseRecord { RunId = _record.RunId, ExpiresAt = DateTimeOffset.UtcNow.Add(_ttl) };
                try
                {
                    _etag = await _transport
                        .PutAsync(_resource, JsonSerializer.Serialize(renewed), _etag, token)
                        .ConfigureAwait(false);
                    _record = renewed;
                    return true;
                }
                catch (SetupStateConflictException)
                {
                    _logger?.LogWarning("Setup lease on '{Key}' was lost and could not be renewed.", Key);
                    return false;
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (_released) return;
                _released = true;
                try { await _transport.DeleteAsync(_resource, _etag).ConfigureAwait(false); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Could not release setup lease on '{Key}'.", Key); }
            }
        }
    }
}
