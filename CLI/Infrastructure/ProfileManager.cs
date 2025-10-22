using System;
using System.IO;
using System.Linq;

namespace TheTechIdea.Beep.CLI.Infrastructure
{
    /// <summary>
    /// Manages CLI profiles for different environments
    /// </summary>
    public static class ProfileManager
    {
        private const string CLI_CONFIG_DIR = "BeepCLI";
        private const string PROFILES_DIR = "Profiles";
        public const string DEFAULT_PROFILE = "default";

        public static string GetProfilePath(string profileName)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea",
                CLI_CONFIG_DIR,
                PROFILES_DIR,
                profileName
            );
        }

        public static string[] ListProfiles()
        {
            var profilesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea",
                CLI_CONFIG_DIR,
                PROFILES_DIR
            );

            if (!Directory.Exists(profilesPath))
                return new[] { DEFAULT_PROFILE };

            return Directory.GetDirectories(profilesPath)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray();
        }

        public static bool ProfileExists(string profileName)
        {
            var profilePath = GetProfilePath(profileName);
            return Directory.Exists(profilePath);
        }

        public static bool CreateProfile(string newProfileName, string sourceProfile = null)
        {
            try
            {
                var newPath = GetProfilePath(newProfileName);
                
                if (Directory.Exists(newPath))
                    return false; // Profile already exists

                if (!string.IsNullOrEmpty(sourceProfile))
                {
                    var sourcePath = GetProfilePath(sourceProfile);
                    if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, newPath);
                        return true;
                    }
                }

                // Create empty profile
                Directory.CreateDirectory(newPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteProfile(string profileName)
        {
            if (profileName == DEFAULT_PROFILE)
                return false; // Can't delete default

            try
            {
                var profilePath = GetProfilePath(profileName);
                if (Directory.Exists(profilePath))
                {
                    Directory.Delete(profilePath, true);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}
