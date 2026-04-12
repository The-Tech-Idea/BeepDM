using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Default no-op implementation of IAlertProvider used when no UI layer is present.
    /// Always returns AlertResult.Button1 and logs the alert text to the console.
    /// Replace by injecting a real implementation from the UI project.
    /// </summary>
    public class DefaultAlertProvider : IAlertProvider
    {
        /// <summary>Shows a fallback alert when no UI-specific provider is available.</summary>
        public Task<AlertResult> ShowAlertAsync(
            string title,
            string message,
            AlertStyle style = AlertStyle.None,
            string button1Text = "OK",
            string button2Text = null,
            string button3Text = null,
            CancellationToken ct = default)
        {
            // No UI available — auto-accept
            Console.WriteLine($"[ALERT {style}] {title}: {message}");
            return Task.FromResult(AlertResult.Button1);
        }
    }
}
