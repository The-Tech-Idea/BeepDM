using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages configuration paths with cross-platform compatibility
    /// </summary>
    public class ConfigPathManager
    {
        private readonly IDMLogger _logger;

        public string ExePath { get; private set; }
        public string ConfigPath { get; private set; }
        public string ContainerName { get; private set; }

        public ConfigPathManager(IDMLogger logger, string folderPath = null, string containerFolder = null)
        {
            _logger = logger;
            ExePath = ResolveCrossPlatformPath(folderPath, containerFolder);
            ContainerName = ExePath;
            ConfigPath = Path.Combine(ExePath, "Config");
        }

        /// <summary>
        /// Resolves the configuration path in a cross-platform compatible manner.
        /// </summary>
        private string ResolveCrossPlatformPath(string folderpath, string containerfolder)
        {
            string basePath;

            // If a specific folder path is provided, use it (after validation)
            if (!string.IsNullOrEmpty(folderpath))
            {
                try
                {
                    // Validate the provided path
                    basePath = Path.GetFullPath(folderpath);
                    
                    // Ensure the directory exists or can be created
                    if (!Directory.Exists(basePath))
                    {
                        Directory.CreateDirectory(basePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.WriteLog($"Warning: Invalid folderpath '{folderpath}': {ex.Message}. Using default path.");
                    basePath = GetDefaultApplicationDataPath();
                }
            }
            else
            {
                // Use platform-appropriate default path
                basePath = GetDefaultApplicationDataPath();
            }

            // Add container folder if specified
            if (!string.IsNullOrEmpty(containerfolder))
            {
                // Sanitize the container folder name for cross-platform compatibility
                string sanitizedContainer = SanitizeFolderName(containerfolder);
                basePath = Path.Combine(basePath, sanitizedContainer);
            }

            // Ensure the final directory exists
            try
            {
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error creating directory '{basePath}': {ex.Message}");
                // Fall back to a temp directory if all else fails
                basePath = Path.Combine(Path.GetTempPath(), "BeepConfig");
                Directory.CreateDirectory(basePath);
            }

            return basePath;
        }

        /// <summary>
        /// Gets the platform-appropriate application data directory.
        /// </summary>
        private string GetDefaultApplicationDataPath()
        {
            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    // Windows: Use CommonApplicationData (C:\ProgramData) or fallback to AppData
                    string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    if (!string.IsNullOrEmpty(commonAppData) && HasWriteAccess(commonAppData))
                    {
                        return Path.Combine(commonAppData, "TheTechIdea", "Beep");
                    }
                    
                    // Fallback to user's AppData
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        "TheTechIdea", "Beep");
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    // macOS: Use ~/Library/Application Support
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Library", "Application Support", "TheTechIdea", "Beep");
                }
                else
                {
                    // Linux and other Unix-like systems: Use ~/.config
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".config", "TheTechIdea", "Beep");
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error determining platform-specific path: {ex.Message}");
                
                // Ultimate fallback: try to get assembly location or use temp
                return GetAssemblyLocationFallback();
            }
        }

        /// <summary>
        /// Attempts to get a path based on assembly location as a last resort.
        /// </summary>
        private string GetAssemblyLocationFallback()
        {
            try
            {
                // Try various methods to get assembly location
                var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
                if (entryAssembly?.Location != null)
                {
                    return Path.GetDirectoryName(entryAssembly.Location);
                }

                var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (executingAssembly?.Location != null)
                {
                    return Path.GetDirectoryName(executingAssembly.Location);
                }

                var callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if (callingAssembly?.Location != null)
                {
                    return Path.GetDirectoryName(callingAssembly.Location);
                }

                // If all assembly methods fail, use AppDomain
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    return baseDirectory;
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Assembly location fallback failed: {ex.Message}");
            }

            // Final fallback to temp directory
            _logger?.WriteLog("Using temp directory as final fallback for configuration path.");
            return Path.Combine(Path.GetTempPath(), "BeepConfig");
        }

        /// <summary>
        /// Checks if the current process has write access to the specified directory.
        /// </summary>
        private bool HasWriteAccess(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Try to create a temporary file to test write access
                string testFile = Path.Combine(path, $"writetest_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sanitizes folder names for cross-platform compatibility.
        /// </summary>
        private string SanitizeFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return folderName;

            // Replace invalid characters with underscores
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                folderName = folderName.Replace(c, '_');
            }

            // Also replace path separators
            folderName = folderName.Replace(Path.DirectorySeparatorChar, '_');
            folderName = folderName.Replace(Path.AltDirectorySeparatorChar, '_');

            // Trim whitespace and dots (problematic on Windows)
            folderName = folderName.Trim(' ', '.');

            // Ensure it's not empty after sanitization
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = "BeepConfig";
            }

            // Limit length for file system compatibility
            if (folderName.Length > 100)
            {
                folderName = folderName.Substring(0, 100);
            }

            return folderName;
        }

        /// <summary>
        /// Creates a directory at the specified path if it doesn't already exist.
        /// </summary>
        public void CreateDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Creates a directory configuration.
        /// </summary>
        public void CreateDirConfig(string path, FolderFileTypes foldertype, ConfigandSettings config)
        {
            CreateDir(path);

            if (!config.Folders.Any(item => item.FolderPath.Equals(@path, StringComparison.InvariantCultureIgnoreCase)))
            {
                config.Folders.Add(new StorageFolders(path, foldertype));
            }
        }
    }
}