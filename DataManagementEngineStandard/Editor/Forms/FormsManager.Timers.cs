using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Timer management built-ins partial class.
    /// Exposes Oracle Forms CREATE_TIMER / DELETE_TIMER / GET_TIMER equivalents.
    /// Timer expiry fires the TriggerType.WhenTimerExpired trigger on the current form
    /// (see OnTimerManagerFired in FormsManager.cs core).
    /// </summary>
    public partial class FormsManager
    {
        #region Timer Built-ins

        /// <summary>
        /// Create (and immediately start) a named timer.
        /// Corresponds to Oracle Forms CREATE_TIMER built-in.
        /// </summary>
        /// <param name="timerName">Unique name for the timer (case-insensitive)</param>
        /// <param name="interval">How long until the timer fires</param>
        /// <param name="repeating">True = fires repeatedly; false = fires once then expires</param>
        /// <returns>The timer definition describing the created timer</returns>
        public TimerDefinition CreateTimer(string timerName, TimeSpan interval, bool repeating = false)
        {
            var def = _timerManager.CreateTimer(timerName, interval, repeating);
            LogOperation($"Timer '{timerName}' created (interval={interval}, repeating={repeating})");
            return def;
        }

        /// <summary>
        /// Stop and remove a named timer.
        /// Corresponds to Oracle Forms DELETE_TIMER built-in.
        /// Returns false if the timer was not found.
        /// </summary>
        public bool DeleteTimer(string timerName)
        {
            var ok = _timerManager.DeleteTimer(timerName);
            LogOperation($"Timer '{timerName}' deleted (found={ok})");
            return ok;
        }

        /// <summary>
        /// Get the definition and current state of a named timer.
        /// Corresponds to Oracle Forms GET_TIMER built-in.
        /// Returns null if no timer with that name exists.
        /// </summary>
        public TimerDefinition GetTimer(string timerName)
            => _timerManager.GetTimer(timerName);

        /// <summary>
        /// Returns all currently registered timers.
        /// </summary>
        public IReadOnlyList<TimerDefinition> GetAllTimers()
            => _timerManager.GetAllTimers();

        /// <summary>
        /// Whether a timer with the given name currently exists and is running (or paused).
        /// </summary>
        public bool TimerExists(string timerName)
            => _timerManager.TimerExists(timerName);

        #endregion
    }
}
