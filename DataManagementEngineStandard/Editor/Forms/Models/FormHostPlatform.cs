namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Identifies the UI host platform for form rendering.
    /// Set by the host (WinForms/WPF) and read by the IDE to determine
    /// which scanner, code generator, and templates to use.
    /// </summary>
    public enum FormHostPlatform
    {
        Unknown = 0,
        WinForms = 1,
        WPF = 2,
        Blazor = 3,
        Console = 4
    }
}
