using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional ownership of one event translation subscription, independent of block-name lookup.</summary>
    public interface IOwnedUnitOfWorkEventSubscriptions
    {
        /// <summary>
        /// Attaches a complete subscription or unwinds attempted attachments and throws.
        /// The returned lease retires only its own delegates and captured source.
        /// </summary>
        IDisposable SubscribeOwned(IUnitofWork unitOfWork, string blockName);
    }
}
