using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>A failed teardown action; later actions are still attempted.</summary>
    public sealed class FormsCleanupFailure
    {
        public string Resource { get; }
        public Exception Exception { get; }
        public FormsCleanupFailure(string resource, Exception exception)
        { Resource = resource; Exception = exception; }
    }
}
