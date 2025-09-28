using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for environment variables and system environment settings
    /// </summary>
    public class EnvironmentResolver : BaseDefaultValueResolver
    {
        public EnvironmentResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "Environment";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "ENVIRONMENTVARIABLE", "ENV", "ENVIRONMENT", "ENVVAR",
            "SYSTEMPATH", "USERPATH", "TEMP", "TEMPPATH"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    "TEMP" or "TEMPPATH" => Environment.GetEnvironmentVariable("TEMP") ?? Environment.GetEnvironmentVariable("TMP"),
                    "SYSTEMPATH" => Environment.GetEnvironmentVariable("PATH"),
                    "USERPATH" => Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
                    _ when upperRule.StartsWith("ENVIRONMENTVARIABLE(") || upperRule.StartsWith("ENV(") => HandleEnvironmentVariable(rule),
                    _ when upperRule.StartsWith("ENVIRONMENT(") || upperRule.StartsWith("ENVVAR(") => HandleEnvironmentVariable(rule),
                    _ when upperRule.Contains(":") => HandleKeyValueFormat(rule),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving environment rule '{rule}'", ex);
                return null;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return upperRule.StartsWith("ENVIRONMENTVARIABLE(") ||
                   upperRule.StartsWith("ENV(") ||
                   upperRule.StartsWith("ENVIRONMENT(") ||
                   upperRule.StartsWith("ENVVAR(") ||
                   upperRule.Equals("TEMP") ||
                   upperRule.Equals("TEMPPATH") ||
                   upperRule.Equals("SYSTEMPATH") ||
                   upperRule.Equals("USERPATH") ||
                   (upperRule.StartsWith("ENV:") ||
                    upperRule.StartsWith("ENVIRONMENT:"));
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "ENVIRONMENTVARIABLE(PATH) - Get PATH environment variable",
                "ENV(USERNAME) - Get USERNAME environment variable",
                "ENVIRONMENT(COMPUTERNAME) - Get computer name",
                "ENVVAR(APPDATA) - Get application data folder",
                "ENV:PATH - Key-value format for PATH",
                "ENVIRONMENT:TEMP - Key-value format for temp folder",
                "TEMP - Get temporary folder path",
                "TEMPPATH - Same as TEMP",
                "SYSTEMPATH - Get system PATH variable",
                "USERPATH - Get user-specific PATH variable"
            };
        }

        #region Handler Methods

        private object HandleEnvironmentVariable(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length == 0)
                {
                    LogError("Environment variable rule requires a variable name");
                    return null;
                }

                var variableName = RemoveQuotes(parts[0].Trim());
                if (string.IsNullOrWhiteSpace(variableName))
                {
                    LogError("Environment variable name cannot be empty");
                    return null;
                }

                // Optional target parameter
                EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;
                if (parts.Length > 1)
                {
                    var targetStr = RemoveQuotes(parts[1].Trim()).ToUpperInvariant();
                    target = targetStr switch
                    {
                        "USER" => EnvironmentVariableTarget.User,
                        "MACHINE" or "SYSTEM" => EnvironmentVariableTarget.Machine,
                        _ => EnvironmentVariableTarget.Process
                    };
                }

                return GetEnvironmentVariable(variableName, target);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleEnvironmentVariable", ex);
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
                var variableName = rule.Substring(colonIndex + 1).Trim();
                variableName = RemoveQuotes(variableName);

                if (prefix == "ENV" || prefix == "ENVIRONMENT")
                {
                    return GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleKeyValueFormat", ex);
                return null;
            }
        }

        #endregion

        #region Helper Methods

        private string GetEnvironmentVariable(string variableName, EnvironmentVariableTarget target)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(variableName, target);
                
                // If not found in specified target, try other targets
                if (string.IsNullOrEmpty(value) && target != EnvironmentVariableTarget.Process)
                {
                    value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);
                }
                
                if (string.IsNullOrEmpty(value) && target != EnvironmentVariableTarget.User)
                {
                    value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
                }
                
                if (string.IsNullOrEmpty(value) && target != EnvironmentVariableTarget.Machine)
                {
                    value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
                }

                if (string.IsNullOrEmpty(value))
                {
                    LogInfo($"Environment variable '{variableName}' not found in any scope");
                }

                return value;
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get environment variable '{variableName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all environment variables for debugging/inspection purposes
        /// </summary>
        /// <param name="target">Target scope</param>
        /// <returns>Dictionary of all environment variables</returns>
        public Dictionary<string, string> GetAllEnvironmentVariables(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            try
            {
                var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var envVars = Environment.GetEnvironmentVariables(target);
                
                foreach (System.Collections.DictionaryEntry entry in envVars)
                {
                    if (entry.Key != null && entry.Value != null)
                    {
                        variables[entry.Key.ToString()] = entry.Value.ToString();
                    }
                }

                return variables;
            }
            catch (Exception ex)
            {
                LogError($"Error getting all environment variables", ex);
                return new Dictionary<string, string>();
            }
        }

        #endregion
    }
}