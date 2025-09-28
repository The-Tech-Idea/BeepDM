using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for system information with enhanced system data access
    /// </summary>
    public class SystemInfoResolver : BaseDefaultValueResolver
    {
        public SystemInfoResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "SystemInfo";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "MACHINENAME", "HOSTNAME", "VERSION", "APPVERSION", "OSVERSION",
            "PLATFORM", "PROCESSORCOUNT", "WORKINGSET", "TIMESTAMP", "TICKS"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    "MACHINENAME" or "HOSTNAME" => Environment.MachineName,
                    "VERSION" => Environment.Version.ToString(),
                    "APPVERSION" => GetApplicationVersion(),
                    "OSVERSION" => Environment.OSVersion.ToString(),
                    "PLATFORM" => Environment.OSVersion.Platform.ToString(),
                    "PROCESSORCOUNT" => Environment.ProcessorCount,
                    "WORKINGSET" => Environment.WorkingSet,
                    "TIMESTAMP" => DateTimeOffset.Now.ToUnixTimeSeconds(),
                    "TICKS" => DateTime.Now.Ticks,
                    _ => Environment.MachineName
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving system info rule '{rule}'", ex);
                return Environment.MachineName;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return SupportedRuleTypes.Any(type => upperRule.Contains(type));
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "MACHINENAME - Current machine name",
                "HOSTNAME - Same as machine name", 
                "VERSION - .NET Framework version",
                "APPVERSION - Application version",
                "OSVERSION - Operating system version",
                "PLATFORM - Operating system platform",
                "PROCESSORCOUNT - Number of processors",
                "WORKINGSET - Application working set size",
                "TIMESTAMP - Current Unix timestamp",
                "TICKS - Current DateTime ticks"
            };
        }

        private string GetApplicationVersion()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                return assembly?.GetName().Version?.ToString() ?? "1.0.0.0";
            }
            catch (Exception ex)
            {
                LogWarning($"Could not get application version: {ex.Message}");
                return "1.0.0.0";
            }
        }
    }
}