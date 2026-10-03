using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>Owns named timer entries; callbacks are admitted by entry identity, not name alone.</summary>
    public class TimerManager : ITimerManager
    {
        private readonly Dictionary<string, TimerEntry> _timers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<TimerCallbackFailure> _failures = new();
        private readonly object _gate = new();
        private readonly TimeProvider _timeProvider;
        private volatile bool _disposed;

        /// <inheritdoc/>
        public event EventHandler<TimerFiredEventArgs> TimerFired;

        /// <summary>Recent isolated scheduler, cleanup and observer failures (at most 64).</summary>
        public IReadOnlyList<TimerCallbackFailure> CallbackFailures
        {
            get { lock (_gate) return _failures.ToArray(); }
        }

        private sealed class TimerEntry
        {
            internal readonly TimerDefinition Definition;
            internal ITimer Timer;
            internal bool Ready;
            internal bool Pending;
            internal bool Dispatching;
            internal bool Retired;
            internal int TimerDisposed;
            internal TimerEntry(TimerDefinition definition) => Definition = definition;
        }

        /// <summary>Uses the system clock/scheduler unless a host or test supplies a TimeProvider.</summary>
        public TimerManager() : this(TimeProvider.System) { }

        public TimerManager(TimeProvider timeProvider) => _timeProvider = timeProvider ?? TimeProvider.System;

        /// <inheritdoc/>
        public TimerDefinition CreateTimer(string timerName, TimeSpan interval, bool repeating = false)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(timerName)) throw new ArgumentNullException(nameof(timerName));
            if (interval <= TimeSpan.Zero || interval.TotalMilliseconds > 4294967294d)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be within the system timer range.");
            var entry = new TimerEntry(new TimerDefinition
            {
                TimerName = timerName, Interval = interval, Repeating = repeating,
                State = TimerState.Running, CreatedAt = _timeProvider.GetLocalNow().LocalDateTime
            });
            entry.Timer = _timeProvider.CreateTimer(OnTimerCallback, entry, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            TimerEntry previous = null;
            bool published = false;
            try
            {
                lock (_gate)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    _timers.TryGetValue(timerName, out previous);
                    _timers[timerName] = entry;
                    published = true;
                }
                // No scheduler or observer is called under the ownership monitor.
                if (!entry.Timer.Change(interval, repeating ? interval : Timeout.InfiniteTimeSpan))
                    throw new InvalidOperationException("Timer scheduler did not acknowledge activation.");
                bool pending;
                lock (_gate)
                {
                    entry.Ready = true;
                    pending = entry.Pending && IsCurrent(entry);
                }
                Retire(previous);
                if (pending) OnTimerCallback(entry);
                lock (_gate) return Snapshot(entry.Definition);
            }
            catch
            {
                bool restored = false;
                lock (_gate)
                {
                    entry.Retired = true;
                    if (published && _timers.TryGetValue(timerName, out var current) && ReferenceEquals(entry, current))
                    {
                        if (!_disposed && previous != null && !previous.Retired) { _timers[timerName] = previous; restored = true; }
                        else _timers.Remove(timerName);
                    }
                }
                Retire(entry);
                if (!restored) Retire(previous);
                throw;
            }
        }

        /// <inheritdoc/>
        public bool DeleteTimer(string timerName)
        {
            if (string.IsNullOrWhiteSpace(timerName)) return false;
            TimerEntry entry;
            lock (_gate)
            {
                if (!_timers.Remove(timerName, out entry)) return false;
                entry.Retired = true;
            }
            Retire(entry);
            return true;
        }

        /// <inheritdoc/>
        public TimerDefinition GetTimer(string timerName)
        {
            if (string.IsNullOrWhiteSpace(timerName)) return null;
            lock (_gate) return _timers.TryGetValue(timerName, out var entry) ? Snapshot(entry.Definition) : null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<TimerDefinition> GetAllTimers()
        {
            lock (_gate) return _timers.Values.Select(e => Snapshot(e.Definition)).ToArray();
        }

        /// <inheritdoc/>
        public bool TimerExists(string timerName)
        {
            if (string.IsNullOrWhiteSpace(timerName)) return false;
            lock (_gate) return _timers.ContainsKey(timerName);
        }

        private bool IsCurrent(TimerEntry entry) => !_disposed && !entry.Retired &&
            _timers.TryGetValue(entry.Definition.TimerName, out var current) && ReferenceEquals(entry, current);

        private void OnTimerCallback(object state)
        {
            var entry = (TimerEntry)state;
            lock (_gate) if (!IsCurrent(entry)) return;
            DateTime now;
            try { now = _timeProvider.GetLocalNow().LocalDateTime; }
            catch (Exception ex) { RecordFailure(entry, ex); return; }
            TimerFiredEventArgs args;
            bool retire;
            lock (_gate)
            {
                if (!IsCurrent(entry)) return;
                if (!entry.Ready) { entry.Pending = true; return; }
                // Slow observers coalesce overlapping ticks instead of concurrently mutating one entry.
                if (entry.Dispatching) return;
                entry.Dispatching = true;
                var definition = entry.Definition;
                if (definition.FireCount == int.MaxValue)
                {
                    entry.Dispatching = false;
                    _failures.Enqueue(new TimerCallbackFailure(definition.TimerName, new OverflowException("Timer fire count exhausted.")));
                    if (_failures.Count > 64) _failures.Dequeue();
                    return;
                }
                definition.FireCount++;
                definition.LastFiredAt = now;
                retire = !definition.Repeating;
                if (retire)
                {
                    definition.State = TimerState.Expired;
                    _timers.Remove(definition.TimerName);
                }
                args = new TimerFiredEventArgs { TimerName = definition.TimerName, FireCount = definition.FireCount, FiredAt = now };
            }
            try
            {
                if (retire) DisposeTimer(entry);
                var handlers = TimerFired;
                foreach (EventHandler<TimerFiredEventArgs> handler in handlers?.GetInvocationList() ?? Array.Empty<Delegate>())
                {
                    if (_disposed) break;
                    try { handler(this, args); }
                    catch (Exception ex) { RecordFailure(entry, ex); }
                }
            }
            finally { lock (_gate) entry.Dispatching = false; }
        }

        private void RecordFailure(TimerEntry entry, Exception exception)
        {
            lock (_gate)
            {
                if (_failures.Count == 64) _failures.Dequeue();
                _failures.Enqueue(new TimerCallbackFailure(entry?.Definition.TimerName, exception));
            }
        }

        private void Retire(TimerEntry entry)
        {
            if (entry == null) return;
            lock (_gate) { entry.Retired = true; entry.Definition.State = TimerState.Deleted; }
            DisposeTimer(entry);
        }

        private void DisposeTimer(TimerEntry entry)
        {
            if (Interlocked.Exchange(ref entry.TimerDisposed, 1) != 0) return;
            try { entry.Timer?.Dispose(); }
            catch (Exception ex) { RecordFailure(entry, ex); }
        }

        private static TimerDefinition Snapshot(TimerDefinition source) => new TimerDefinition
        {
            TimerName = source.TimerName, Interval = source.Interval, Repeating = source.Repeating,
            State = source.State, CreatedAt = source.CreatedAt, LastFiredAt = source.LastFiredAt, FireCount = source.FireCount
        };

        /// <summary>Closes admission and retires each timer; already-admitted observers may finish.</summary>
        public void Dispose()
        {
            TimerEntry[] entries;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                entries = _timers.Values.ToArray();
                _timers.Clear();
            }
            foreach (var entry in entries) Retire(entry);
        }
    }
}
