using System;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep
{
    /// <summary>
    /// DMEEditor partial class extension for enhanced package management cleanup
    /// </summary>
    public partial class DMEEditor
    {
        /// <summary>
        /// Enhanced dispose implementation that includes nugget extension cleanup
        /// </summary>
        partial void OnDisposing()
        {
            try
            {
                // Cleanup nugget extensions when DMEEditor is being disposed
                this.CleanupNuggetExtensions();
                
                // Log cleanup completion
                Logger?.WriteLog("DMEEditor nugget extensions cleaned up successfully");
            }
            catch (Exception ex)
            {
                // Fallback logging if Logger is already disposed
                Console.WriteLine($"Warning: Error during nugget extension cleanup: {ex.Message}");
            }
        }
    }
}