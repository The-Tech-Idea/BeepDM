using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Lifecycle state of a form-level timer.
    /// </summary>
    public enum TimerState
    {
        /// <summary>Timer is created but not yet started</summary>
        Created,

        /// <summary>Timer is actively firing</summary>
        Running,

        /// <summary>Timer has been paused</summary>
        Paused,

        /// <summary>Timer fired once and has stopped (non-repeating)</summary>
        Expired,

        /// <summary>Timer was explicitly deleted</summary>
        Deleted
    }

    /// <summary>
    /// Defines a named form-level timer.
    /// Corresponds to Oracle Forms CREATE_TIMER / DELETE_TIMER built-ins.
    /// </summary>
    public class TimerDefinition
    {
        /// <summary>Unique name of the timer (case-insensitive)</summary>
        public string TimerName { get; set; }

        /// <summary>How long between firings</summary>
        public TimeSpan Interval { get; set; }

        /// <summary>Whether the timer fires repeatedly (true) or only once (false)</summary>
        public bool Repeating { get; set; }

        /// <summary>Current state of this timer</summary>
        public TimerState State { get; set; } = TimerState.Created;

        /// <summary>When the timer was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>When the timer last fired (null if never)</summary>
        public DateTime? LastFiredAt { get; set; }

        /// <summary>How many times the timer has fired</summary>
        public int FireCount { get; set; }
    }
}
