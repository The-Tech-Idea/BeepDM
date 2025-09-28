using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Configuration
{
    /// <summary>
    /// Configuration settings for UnitofWorksManager
    /// </summary>
    public class UnitofWorksManagerConfiguration
    {
        /// <summary>Gets or sets performance-related configuration</summary>
        public PerformanceConfiguration Performance { get; set; } = new();
        
        /// <summary>Gets or sets validation-related configuration</summary>
        public ValidationConfiguration Validation { get; set; } = new();
        
        /// <summary>Gets or sets navigation-related configuration</summary>
        public NavigationConfiguration Navigation { get; set; } = new();
        
        /// <summary>Gets or sets form-related configuration</summary>
        public FormConfiguration Forms { get; set; } = new();
        
        /// <summary>Gets or sets block-specific configurations</summary>
        public Dictionary<string, BlockConfiguration> BlockConfigurations { get; set; } = new();
        
        /// <summary>Gets or sets form-specific configurations</summary>
        public Dictionary<string, FormConfiguration> FormConfigurations { get; set; } = new();
        
        // Global settings
        /// <summary>Gets or sets whether logging is enabled</summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>Gets or sets whether to validate before commit</summary>
        public bool ValidateBeforeCommit { get; set; } = true;
        
        /// <summary>Gets or sets whether to confirm before clearing</summary>
        public bool ConfirmBeforeClear { get; set; } = true;
        
        /// <summary>Gets or sets whether to stop validation on first error</summary>
        public bool StopValidationOnFirstError { get; set; } = false;
        
        /// <summary>Gets or sets whether to clear cache on form close</summary>
        public bool ClearCacheOnFormClose { get; set; } = false;
        
        // Default operation options
        /// <summary>Gets or sets default save options</summary>
        public SaveOptions DefaultSaveOptions { get; set; } = SaveOptions.Default;
        
        /// <summary>Gets or sets default rollback options</summary>
        public RollbackOptions DefaultRollbackOptions { get; set; } = RollbackOptions.Default;

        /// <summary>Gets the default configuration</summary>
        public static UnitofWorksManagerConfiguration Default => new();

        /// <summary>Gets the configuration for a specific block</summary>
        /// <param name="blockName">The name of the block</param>
        /// <returns>Block configuration or default if not found</returns>
        public BlockConfiguration GetBlockConfiguration(string blockName)
        {
            return BlockConfigurations.TryGetValue(blockName, out var config) ? config : new BlockConfiguration();
        }
        public int MaxRecordsPerBlock { get; set; } = 1000;
        /// <summary>Gets the configuration for a specific form</summary>
        /// <param name="formName">The name of the form</param>
        /// <returns>Form configuration or default if not found</returns>
        public FormConfiguration GetFormConfiguration(string formName)
        {
            return FormConfigurations.TryGetValue(formName, out var config) ? config : Forms;
        }
    }
}