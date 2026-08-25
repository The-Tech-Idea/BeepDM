using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Manages named, reusable Alert objects (Oracle Forms ALERT). An alert is
    /// authored once, by name, and shown by name from any trigger — the
    /// SHOW_ALERT('alert_name') built-in — as opposed to the ad-hoc
    /// <c>ShowAlertAsync(title, message, ...)</c> overload on
    /// <see cref="IUnitofWorksManager"/>, which takes its content literally on
    /// every call and has no persisted definition.
    /// </summary>
    public interface IAlertRegistry
    {
        AlertDefinition CreateAlert(
            string name, string title, string message,
            AlertStyle style = AlertStyle.None,
            string button1Text = "OK", string button2Text = null, string button3Text = null);

        AlertDefinition GetAlert(string name);

        IReadOnlyList<AlertDefinition> GetAllAlerts();

        bool RemoveAlert(string name);

        void ClearAllAlerts();

        bool AlertExists(string name);

        /// <summary>
        /// Shows a previously-created alert by name (Oracle Forms:
        /// SHOW_ALERT('alert_name')). Returns <see cref="AlertResult.None"/>
        /// when no alert with that name has been created.
        /// </summary>
        Task<AlertResult> ShowAlertByNameAsync(string name, CancellationToken ct = default);
    }
}
