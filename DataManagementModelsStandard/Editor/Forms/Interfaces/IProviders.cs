using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;


namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{

    #region Alert Provider Interface

    /// <summary>
    /// Pluggable UI provider for modal alert dialogs.
    /// Inject an implementation from the UI layer; the default no-op implementation
    /// logs to Status and returns AlertResult.Button1.
    /// </summary>
    public interface IAlertProvider
    {
        /// <summary>
        /// Display an alert dialog and return the button the user pressed.
        /// Corresponds to Oracle Forms SHOW_ALERT built-in.
        /// </summary>
        Task<Forms.Models.AlertResult> ShowAlertAsync(
            string title,
            string message,
            Forms.Models.AlertStyle style = Forms.Models.AlertStyle.None,
            string button1Text = "OK",
            string button2Text = null,
            string button3Text = null,
            CancellationToken ct = default);
    }

    #endregion

    #region Sequence Provider Interface

    /// <summary>
    /// Provides named auto-increment sequences.
    /// Corresponds to Oracle Forms :SEQUENCE.NEXTVAL usage.
    /// </summary>
    public interface ISequenceProvider
    {
        /// <summary>Increment and return the next value for the named sequence</summary>
        long GetNextSequence(string sequenceName);

        /// <summary>Peek at the next value without incrementing</summary>
        long PeekNextSequence(string sequenceName);

        /// <summary>Reset a sequence to a starting value (default 1)</summary>
        void ResetSequence(string sequenceName, long startValue = 1);

        /// <summary>Whether the named sequence has been created</summary>
        bool SequenceExists(string sequenceName);

        /// <summary>Create a new named sequence with the given starting value</summary>
        void CreateSequence(string sequenceName, long startValue = 1, long incrementBy = 1);

        /// <summary>Remove a named sequence. Returns false when it does not exist.</summary>
        bool DropSequence(string sequenceName);
    }

    #endregion

    #region Timer Manager Interface

    /// <summary>
    /// Manages named form-level timers that fire WHEN-TIMER-EXPIRED triggers.
    /// Corresponds to Oracle Forms CREATE_TIMER / DELETE_TIMER / GET_TIMER built-ins.
    /// </summary>
    public interface ITimerManager : IDisposable
    {
        /// <summary>
        /// Create and start a named timer.
        /// Corresponds to Oracle Forms CREATE_TIMER.
        /// </summary>
        Forms.Models.TimerDefinition CreateTimer(string timerName, TimeSpan interval, bool repeating = false);

        /// <summary>
        /// Stop and remove a named timer.
        /// Corresponds to Oracle Forms DELETE_TIMER.
        /// Returns false if the timer was not found.
        /// </summary>
        bool DeleteTimer(string timerName);

        /// <summary>
        /// Get the definition/state of a named timer.
        /// Corresponds to Oracle Forms GET_TIMER.
        /// </summary>
        Forms.Models.TimerDefinition GetTimer(string timerName);

        /// <summary>Returns all currently registered timers (running and paused)</summary>
        IReadOnlyList<Forms.Models.TimerDefinition> GetAllTimers();

        /// <summary>Whether a timer with the given name exists</summary>
        bool TimerExists(string timerName);

        /// <summary>
        /// Event raised when a timer fires.
        /// Handlers should fire TriggerType.WhenTimerExpired for their block/form.
        /// </summary>
        event EventHandler<TimerFiredEventArgs> TimerFired;
    }

    /// <summary>
    /// Event arguments for the ITimerManager.TimerFired event.
    /// </summary>
    public class TimerFiredEventArgs : EventArgs
    {
        /// <summary>Gets the logical timer name that fired.</summary>
        public string TimerName { get; init; }

        /// <summary>Gets the number of times the timer has fired.</summary>
        public int FireCount { get; init; }

        /// <summary>Gets the timestamp when the timer fired.</summary>
        public DateTime FiredAt { get; init; } = DateTime.Now;
    }

    #endregion

}
