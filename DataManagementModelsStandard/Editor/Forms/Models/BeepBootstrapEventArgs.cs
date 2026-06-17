using System;

namespace TheTechIdea.Beep.Editor.Forms.Models;

public class BeepBootstrapEventArgs : EventArgs
{
    public BootstrapState State { get; }
    public string? ErrorMessage { get; }

    public BeepBootstrapEventArgs(BootstrapState state, string? errorMessage = null)
    {
        State = state;
        ErrorMessage = errorMessage;
    }
}
