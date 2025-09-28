namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Actions that can be taken when unsaved changes are detected
    /// </summary>
    public enum UnsavedChangesAction
    {
        /// <summary>Save all changes and continue with the operation</summary>
        Save,
        
        /// <summary>Discard all changes and continue with the operation</summary>
        Discard,
        
        /// <summary>Cancel the operation without saving or discarding</summary>
        Cancel,
        
        /// <summary>Prompt the user to decide what to do</summary>
        Prompt,
        
        /// <summary>Apply changes to a backup copy first</summary>
        Backup
    }
}