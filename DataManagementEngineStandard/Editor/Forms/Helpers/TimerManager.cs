using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Manages named form-level timers that fire WHEN-TIMER-EXPIRED triggers.
    /// Corresponds to Oracle Forms CREATE_TIMER / DELETE_TIMER / GET_TIMER built-ins.
    /// Thread-safe — uses System.Threading.Timer internally.
    /// </summary>
    public class TimerManager : ITimerManager
    {
        private readonly ConcurrentDictionary<string, TimerEntry> _timers =
            new(StringComparer.OrdinalIgnoreCase);

        private bool _disposed;

        // ReSharper disable once InconsistentNaming
        /// <summary>Raised when a named timer fires.</summary>
        public event EventHandler<TimerFiredEventArgs> TimerFired;

        private sealed class TimerEntry
        {
            public readonly TimerDefinition Definition;
            public readonly Timer SystemTimer;

            public TimerEntry(TimerDefinition def, Timer timer)
            {
                Definition = def;
                SystemTimer = timer;
            }
        }

        /// <inheritdoc/>
        public TimerDefinition CreateTimer(string timerName, TimeSpan interval, bool repeating = false)
        {
            if (string.IsNullOrWhiteSpace(timerName)) throw new ArgumentNullException(nameof(timerName));
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive");

            // Replace any existing timer with the same name
            DeleteTimer(timerName);

            var def = new TimerDefinition
            {
                TimerName = timerName,
                Interval = interval,
                Repeating = repeating,
                State = TimerState.Running,
                CreatedAt = DateTime.Now
            };

            var period = repeating ? interval : Timeout.InfiniteTimeSpan;
            var sysTimer = new Timer(OnTimerCallback, timerName, interval, period);

            _timers[timerName] = new TimerEntry(def, sysTimer);
            return def;
        }

        /// <inheritdoc/>
        public bool DeleteTimer(string timerName)
        {
            if (string.IsNullOrWhiteSpace(timerName)) return false;
            if (!_timers.TryRemove(timerName, out var entry)) return false;

            entry.SystemTimer.Dispose();
            entry.Definition.State = TimerState.Deleted;
            return true;
        }

        /// <inheritdoc/>
        public TimerDefinition GetTimer(string timerName)
        {
            if (string.IsNullOrWhiteSpace(timerName)) return null;
            return _timers.TryGetValue(timerName, out var entry) ? entry.Definition : null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<TimerDefinition> GetAllTimers() =>
            _timers.Values.Select(e => e.Definition).ToList();

        /// <inheritdoc/>
        public bool TimerExists(string timerName) =>
            !string.IsNullOrWhiteSpace(timerName) && _timers.ContainsKey(timerName);

        private void OnTimerCallback(object state)
        {
            if (_disposed) return;
            var timerName = (string)state;
            if (!_timers.TryGetValue(timerName, out var entry)) return;

            var def = entry.Definition;
            def.FireCount++;
            def.LastFiredAt = DateTime.Now;

            if (!def.Repeating)
            {
                def.State = TimerState.Expired;
                _timers.TryRemove(timerName, out _);
                entry.SystemTimer.Dispose();
            }

            TimerFired?.Invoke(this, new TimerFiredEventArgs
            {
                TimerName = timerName,
                FireCount = def.FireCount,
                FiredAt = def.LastFiredAt.Value
            });
        }

        /// <summary>Disposes all timers managed by the instance.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var entry in _timers.Values)
            {
                entry.Definition.State = TimerState.Deleted;
                entry.SystemTimer.Dispose();
            }
            _timers.Clear();
        }
    }
}
