using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Configuration
{
    /// <summary>
    /// Form-related configuration settings
    /// </summary>
    public class FormConfiguration
    {
        /// <summary>Gets or sets whether to auto-save on form close</summary>
        public bool AutoSaveOnClose { get; set; } = false;
        
        /// <summary>Gets or sets whether to confirm unsaved changes</summary>
        public bool ConfirmUnsavedChanges { get; set; } = true;
        
        /// <summary>Gets or sets whether form validation is enabled</summary>
        public bool EnableFormValidation { get; set; } = true;
        
        /// <summary>Gets or sets the query timeout in seconds</summary>
        public int QueryTimeout { get; set; } = 30;
        
        /// <summary>Gets or sets custom form settings</summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new();
        
        /// <summary>Gets or sets whether to enable form auditing</summary>
        public bool EnableAuditing { get; set; } = false;
        
        /// <summary>Gets or sets the maximum number of records to load</summary>
        public int MaxRecords { get; set; } = 1000;
        
        /// <summary>Gets or sets whether to enable form caching</summary>
        public bool EnableCaching { get; set; } = true;
    }
}