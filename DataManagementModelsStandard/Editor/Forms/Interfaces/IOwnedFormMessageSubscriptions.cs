using System;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional bus capability for detaching exactly one owner's handler.</summary>
    public interface IOwnedFormMessageSubscriptions
    {
        IDisposable SubscribeOwned(string formName, string messageType, Action<FormMessage> handler);
    }
}
