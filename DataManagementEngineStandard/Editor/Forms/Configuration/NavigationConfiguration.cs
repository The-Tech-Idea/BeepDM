namespace TheTechIdea.Beep.Editor.UOWManager.Configuration
{
    /// <summary>
    /// Navigation-related configuration settings
    /// </summary>
    public class NavigationConfiguration
    {
        /// <summary>Gets or sets whether to check for unsaved changes during navigation</summary>
        public bool CheckUnsavedChanges { get; set; } = true;
        
        /// <summary>Gets or sets whether to synchronize detail blocks during navigation</summary>
        public bool SynchronizeDetailBlocks { get; set; } = true;
        
        /// <summary>Gets or sets whether keyboard navigation is enabled</summary>
        public bool EnableKeyboardNavigation { get; set; } = true;
        
        /// <summary>Gets or sets whether navigation wraps around at boundaries</summary>
        public bool WrapAroundNavigation { get; set; } = false;
        
        /// <summary>Gets or sets whether to confirm navigation when data is modified</summary>
        public bool ConfirmNavigationWithChanges { get; set; } = true;
        
        /// <summary>Gets or sets whether to enable fast navigation mode</summary>
        public bool EnableFastNavigation { get; set; } = false;
    }
}