namespace TheTechIdea.Beep.Editor.UOWManager.Configuration
{
    /// <summary>
    /// Validation-related configuration settings
    /// </summary>
    public class ValidationConfiguration
    {
        /// <summary>Gets or sets whether to validate on insert</summary>
        public bool ValidateOnInsert { get; set; } = true;
        
        /// <summary>Gets or sets whether to validate on update</summary>
        public bool ValidateOnUpdate { get; set; } = true;
        
        /// <summary>Gets or sets whether to validate on navigation</summary>
        public bool ValidateOnNavigation { get; set; } = false;
        
        /// <summary>Gets or sets whether to show validation messages</summary>
        public bool ShowValidationMessages { get; set; } = true;
        
        /// <summary>Gets or sets the maximum number of validation errors to collect</summary>
        public int MaxValidationErrors { get; set; } = 10;
        
        /// <summary>Gets or sets whether to validate on delete</summary>
        public bool ValidateOnDelete { get; set; } = false;
        
        /// <summary>Gets or sets whether to use strict validation</summary>
        public bool StrictValidation { get; set; } = true;
    }
}