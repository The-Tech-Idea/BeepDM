using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for configuration values from app settings, config files, etc.
    /// </summary>
    public class ConfigurationResolver : BaseDefaultValueResolver
    {
        public ConfigurationResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "Configuration";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "CONFIGURATIONVALUE", "CONFIG", "APPSETTING", "SETTING",
            "CONNECTIONSTRING", "APPCONFIG", "WEBCONFIG"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    _ when upperRule.StartsWith("CONFIGURATIONVALUE(") || upperRule.StartsWith("CONFIG(") => HandleConfigurationValue(rule),
                    _ when upperRule.StartsWith("APPSETTING(") || upperRule.StartsWith("SETTING(") => HandleAppSetting(rule),
                    _ when upperRule.StartsWith("CONNECTIONSTRING(") => HandleConnectionString(rule),
                    _ when upperRule.Contains(":") => HandleKeyValueFormat(rule),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving configuration rule '{rule}'", ex);
                return null;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return upperRule.StartsWith("CONFIGURATIONVALUE(") ||
                   upperRule.StartsWith("CONFIG(") ||
                   upperRule.StartsWith("APPSETTING(") ||
                   upperRule.StartsWith("SETTING(") ||
                   upperRule.StartsWith("CONNECTIONSTRING(") ||
                   (upperRule.StartsWith("APPCONFIG:") ||
                    upperRule.StartsWith("WEBCONFIG:") ||
                    upperRule.StartsWith("CONFIG:"));
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "CONFIGURATIONVALUE(DatabaseTimeout) - Get config value by key",
                "CONFIG(LogLevel) - Get configuration value",
                "APPSETTING(DefaultTheme) - Get app setting value",
                "SETTING(MaxRecords) - Get setting value",
                "CONNECTIONSTRING(DefaultConnection) - Get connection string",
                "CONFIG:DatabaseTimeout - Key-value format",
                "APPSETTING:DefaultPageSize - App setting in key-value format"
            };
        }

        #region Handler Methods

        private object HandleConfigurationValue(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var key = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(key))
                {
                    LogError("CONFIGURATIONVALUE requires a key parameter");
                    return null;
                }

                return GetConfigurationValue(key);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleConfigurationValue", ex);
                return null;
            }
        }

        private object HandleAppSetting(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var key = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(key))
                {
                    LogError("APPSETTING requires a key parameter");
                    return null;
                }

                return GetAppSetting(key);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleAppSetting", ex);
                return null;
            }
        }

        private object HandleConnectionString(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var name = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(name))
                {
                    LogError("CONNECTIONSTRING requires a connection name parameter");
                    return null;
                }

                return GetConnectionString(name);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleConnectionString", ex);
                return null;
            }
        }

        private object HandleKeyValueFormat(string rule)
        {
            try
            {
                var colonIndex = rule.IndexOf(':');
                if (colonIndex <= 0)
                    return null;

                var prefix = rule.Substring(0, colonIndex).Trim().ToUpperInvariant();
                var key = rule.Substring(colonIndex + 1).Trim();
                key = RemoveQuotes(key);

                return prefix switch
                {
                    "CONFIG" or "CONFIGURATIONVALUE" => GetConfigurationValue(key),
                    "APPSETTING" or "SETTING" => GetAppSetting(key),
                    "CONNECTIONSTRING" => GetConnectionString(key),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleKeyValueFormat", ex);
                return null;
            }
        }

        #endregion

        #region Helper Methods

        private string GetConfigurationValue(string key)
        {
            try
            {
                // Try app settings first
                var appSetting = GetAppSetting(key);
                if (appSetting != null)
                    return appSetting;

                // Try to get from editor configuration if available
                if (Editor?.ConfigEditor != null)
                {
                    // This would depend on how your ConfigEditor exposes settings
                    // For now, return null as we don't have access to the internal structure
                    LogInfo($"Would query ConfigEditor for key '{key}'");
                }

                return null;
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get configuration value for key '{key}': {ex.Message}");
                return null;
            }
        }

        private string GetAppSetting(string key)
        {
            try
            {
                // Try environment variable as fallback (common in .NET Core/5+)
                var envValue = Environment.GetEnvironmentVariable($"APPSETTING_{key}");
                if (!string.IsNullOrEmpty(envValue))
                    return envValue;

                // Also try without prefix
                envValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(envValue))
                    return envValue;

                LogInfo($"App setting '{key}' not found");
                return null;
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get app setting for key '{key}': {ex.Message}");
                return null;
            }
        }

        private string GetConnectionString(string name)
        {
            try
            {
                // Try to get from environment variables (common pattern in .NET Core/5+)
                var envConnString = Environment.GetEnvironmentVariable($"ConnectionStrings__{name}");
                if (!string.IsNullOrEmpty(envConnString))
                    return envConnString;

                // Try alternative format
                envConnString = Environment.GetEnvironmentVariable($"CONNECTIONSTRING_{name}");
                if (!string.IsNullOrEmpty(envConnString))
                    return envConnString;

                // Try to get from editor's configuration if available
                if (Editor?.ConfigEditor != null)
                {
                    // This would depend on your DataConnections structure
                    LogInfo($"Would query Editor DataConnections for '{name}'");
                }

                LogInfo($"Connection string '{name}' not found");
                return null;
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get connection string for name '{name}': {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}