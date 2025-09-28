using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for user context information with enhanced user data access
    /// </summary>
    public class UserContextResolver : BaseDefaultValueResolver
    {
        public UserContextResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "UserContext";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "USERNAME", "USERID", "USEREMAIL", "USERLOGIN", "CURRENTUSER",
            "USERDOMAIN", "USERPROFILE", "USERGROUP", "USERROLE", "USERPRINCIPAL"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return Environment.UserName;

            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    "USERNAME" or "CURRENTUSER" or "USERLOGIN" => Environment.UserName,
                    "USERID" => GetUserId(),
                    "USEREMAIL" => GetUserEmail(),
                    "USERDOMAIN" => Environment.UserDomainName,
                    "USERPROFILE" => GetUserProfile(),
                    "USERGROUP" => GetUserGroup(),
                    "USERROLE" => GetUserRole(parameters),
                    "USERPRINCIPAL" => GetUserPrincipal(),
                    _ when upperRule.StartsWith("USERPROFILE(") => GetUserProfileProperty(rule),
                    _ when upperRule.StartsWith("USERROLE(") => GetSpecificUserRole(rule, parameters),
                    _ => Environment.UserName
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving user context rule '{rule}'", ex);
                return Environment.UserName;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return SupportedRuleTypes.Any(type => upperRule.Contains(type)) ||
                   upperRule.StartsWith("USERPROFILE(") ||
                   upperRule.StartsWith("USERROLE(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "USERNAME - Current Windows username",
                "CURRENTUSER - Same as USERNAME",
                "USERID - Current user identifier/SID",
                "USEREMAIL - Current user email (if available)",
                "USERDOMAIN - User's domain name",
                "USERPROFILE - User profile directory path",
                "USERGROUP - Primary user group",
                "USERROLE - User's application role",
                "USERPRINCIPAL - User principal name",
                "USERPROFILE(Documents) - Get user's Documents folder",
                "USERPROFILE(Desktop) - Get user's Desktop folder",
                "USERROLE(Application) - Get role for specific application"
            };
        }

        #region Private Helper Methods

        private string GetUserId()
        {
            try
            {
                // Try to get Windows user SID
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                return identity?.User?.Value ?? Environment.UserName;
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get user ID: {ex.Message}");
                return Environment.UserName;
            }
        }

        private string GetUserEmail()
        {
            try
            {
                // Try to get email from Active Directory
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                if (identity != null)
                {
                    // This is a simplified implementation
                    // In a real scenario, you'd query Active Directory
                    var domain = Environment.UserDomainName;
                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        return $"{Environment.UserName}@{domain}.com";
                    }
                }

                // Fallback to constructed email
                return $"{Environment.UserName}@{Environment.MachineName}.local";
            }
            catch (Exception ex)
            {
                LogWarning($"Could not determine user email: {ex.Message}");
                return $"{Environment.UserName}@unknown.local";
            }
        }

        private string GetUserProfile()
        {
            try
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get user profile path: {ex.Message}");
                return Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty;
            }
        }

        private string GetUserGroup()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                if (identity != null)
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    
                    // Check common groups
                    if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                        return "Administrators";
                    if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.User))
                        return "Users";
                    if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.PowerUser))
                        return "Power Users";
                }

                return "Users"; // Default
            }
            catch (Exception ex)
            {
                LogWarning($"Could not determine user group: {ex.Message}");
                return "Unknown";
            }
        }

        private string GetUserRole(IPassedArgs parameters)
        {
            try
            {
                // Try to get role from parameters first
                var role = GetParameterValue<string>(parameters, "UserRole");
                if (!string.IsNullOrWhiteSpace(role))
                    return role;

                // Try to get from current user context (this would be application-specific)
                // This is a placeholder - in a real implementation, you'd check your user management system
                return GetUserGroup(); // Fallback to Windows group
            }
            catch (Exception ex)
            {
                LogWarning($"Could not determine user role: {ex.Message}");
                return "User";
            }
        }

        private string GetUserPrincipal()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                return identity?.Name ?? $"{Environment.UserDomainName}\\{Environment.UserName}";
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get user principal: {ex.Message}");
                return Environment.UserName;
            }
        }

        private string GetUserProfileProperty(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var folderName = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(folderName))
                    return GetUserProfile();

                // Map common folder names to special folders
                var specialFolder = folderName.ToUpperInvariant() switch
                {
                    "DOCUMENTS" or "MYDOCUMENTS" => Environment.SpecialFolder.MyDocuments,
                    "DESKTOP" => Environment.SpecialFolder.Desktop,
                    "PICTURES" or "MYPICTURES" => Environment.SpecialFolder.MyPictures,
                    "MUSIC" or "MYMUSIC" => Environment.SpecialFolder.MyMusic,
                    "VIDEOS" or "MYVIDEOS" => Environment.SpecialFolder.MyVideos,
                    "DOWNLOADS" => Environment.SpecialFolder.UserProfile, // Downloads not in enum
                    "APPDATA" => Environment.SpecialFolder.ApplicationData,
                    "LOCALAPPDATA" => Environment.SpecialFolder.LocalApplicationData,
                    "TEMP" => Environment.SpecialFolder.InternetCache, // Close enough
                    _ => Environment.SpecialFolder.UserProfile
                };

                var path = Environment.GetFolderPath(specialFolder);

                // Handle Downloads folder specifically
                if (folderName.Equals("DOWNLOADS", StringComparison.OrdinalIgnoreCase))
                {
                    path = System.IO.Path.Combine(GetUserProfile(), "Downloads");
                }

                return path;
            }
            catch (Exception ex)
            {
                LogError($"Error getting user profile property from rule '{rule}'", ex);
                return GetUserProfile();
            }
        }

        private string GetSpecificUserRole(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var application = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(application))
                    return GetUserRole(parameters);

                // This would typically query your application's user management system
                // For now, return a placeholder
                var role = GetParameterValue<string>(parameters, $"{application}Role");
                return role ?? GetUserRole(parameters);
            }
            catch (Exception ex)
            {
                LogError($"Error getting specific user role from rule '{rule}'", ex);
                return GetUserRole(parameters);
            }
        }

        #endregion
    }
}