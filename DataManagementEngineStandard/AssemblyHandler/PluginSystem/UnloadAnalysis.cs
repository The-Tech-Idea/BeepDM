using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Analysis result for plugin unload operation
    /// Provides information about what will happen when unloading a plugin
    /// </summary>
    public class UnloadAnalysis
    {
        public string PluginId { get; set; }
        public string PluginName { get; set; }
        public string Version { get; set; }
        public bool CanUnloadImmediately { get; set; }
        public List<SharedAssemblyInfo> SharedAssemblies { get; set; } = new();
        public List<SharedAssemblyInfo> UniqueAssemblies { get; set; } = new();
        public List<string> DependentPlugins { get; set; } = new();
        public long EstimatedMemoryFreed { get; set; }
        public UnloadRecommendation Recommendation { get; set; }

        /// <summary>
        /// Get user-friendly message explaining unload status
        /// </summary>
        public string GetMessage()
        {
            if (CanUnloadImmediately)
            {
                return $"✓ Can unload immediately. Will free approximately {FormatBytes(EstimatedMemoryFreed)}";
            }
            else
            {
                var pluginList = string.Join(", ", DependentPlugins);
                return $"⚠ Cannot unload immediately. Still used by: {pluginList}";
            }
        }

        /// <summary>
        /// Get detailed explanation with recommendations
        /// </summary>
        public string GetDetailedExplanation()
        {
            var lines = new List<string>();

            lines.Add($"Plugin: {PluginName} v{Version}");
            lines.Add($"Status: {(CanUnloadImmediately ? "✓ Safe to unload" : "⚠ Has dependencies")}");
            lines.Add("");

            if (SharedAssemblies.Any())
            {
                lines.Add($"Shared Assemblies ({SharedAssemblies.Count}):");
                foreach (var asm in SharedAssemblies.Take(10))
                {
                    var users = string.Join(", ", asm.UsedByPlugins.Where(p => p != PluginId));
                    lines.Add($"  • {asm.AssemblyName} - used by: {users}");
                }
                if (SharedAssemblies.Count > 10)
                {
                    lines.Add($"  ... and {SharedAssemblies.Count - 10} more");
                }
                lines.Add("");
            }

            if (UniqueAssemblies.Any())
            {
                lines.Add($"Unique Assemblies ({UniqueAssemblies.Count}): Can be unloaded ✓");
            }

            lines.Add("");
            lines.Add($"Estimated Memory: {FormatBytes(EstimatedMemoryFreed)}");
            
            if (Recommendation != UnloadRecommendation.SafeToUnload)
            {
                lines.Add("");
                lines.Add("Recommendation: " + GetRecommendationText());
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string GetRecommendationText()
        {
            return Recommendation switch
            {
                UnloadRecommendation.SafeToUnload => "Safe to unload immediately",
                UnloadRecommendation.UnloadDependentsFirst => $"Unload dependent plugins first: {string.Join(", ", DependentPlugins)}",
                UnloadRecommendation.ForceUnload => "Use --force to unload anyway (may break other plugins)",
                UnloadRecommendation.ScheduleUnload => "Schedule unload for when dependencies are resolved",
                _ => "Unknown"
            };
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Information about a shared assembly
    /// </summary>
    public class SharedAssemblyInfo
    {
        public string AssemblyName { get; set; }
        public string Version { get; set; }
        public List<string> UsedByPlugins { get; set; } = new();
        public bool IsUnique { get; set; }
        public bool IsSystemAssembly { get; set; }
        public long Size { get; set; }

        public int ReferenceCount => UsedByPlugins.Count;
    }

    /// <summary>
    /// Recommendation for unload operation
    /// </summary>
    public enum UnloadRecommendation
    {
        SafeToUnload,
        UnloadDependentsFirst,
        ForceUnload,
        ScheduleUnload
    }

    /// <summary>
    /// Mode for unloading plugins
    /// </summary>
    public enum UnloadMode
    {
        /// <summary>
        /// Only unload if no other plugins depend on it
        /// </summary>
        Safe,

        /// <summary>
        /// Unload with all dependent plugins
        /// </summary>
        Cascade,

        /// <summary>
        /// Force unload even if dependencies exist (may break things)
        /// </summary>
        Force,

        /// <summary>
        /// Schedule for unload when safe (monitor for opportunity)
        /// </summary>
        Scheduled
    }

    /// <summary>
    /// Helper class to analyze plugin dependencies and create UnloadAnalysis
    /// </summary>
    public static class UnloadAnalyzer
    {
        /// <summary>
        /// Analyze what would happen if we unload a plugin
        /// </summary>
        public static UnloadAnalysis Analyze(
            string pluginId, 
            string pluginName, 
            string version,
            SharedAssemblyTracker tracker)
        {
            var analysis = new UnloadAnalysis
            {
                PluginId = pluginId,
                PluginName = pluginName,
                Version = version
            };

            // Get all assemblies used by this plugin
            var pluginAssemblies = tracker.GetAssemblies(pluginId);

            foreach (var assembly in pluginAssemblies)
            {
                var users = tracker.GetUsers(assembly);
                var isUnique = users.Count == 1 && users[0].Equals(pluginId, StringComparison.OrdinalIgnoreCase);
                var assemblyName = assembly.GetName();
                var isSystemAssembly = IsSystemAssembly(assemblyName.Name);

                var assemblyInfo = new SharedAssemblyInfo
                {
                    AssemblyName = assemblyName.Name,
                    Version = assemblyName.Version?.ToString() ?? "Unknown",
                    UsedByPlugins = users,
                    IsUnique = isUnique,
                    IsSystemAssembly = isSystemAssembly,
                    Size = EstimateAssemblySize(assembly)
                };

                if (isUnique)
                {
                    analysis.UniqueAssemblies.Add(assemblyInfo);
                    analysis.EstimatedMemoryFreed += assemblyInfo.Size;
                }
                else
                {
                    analysis.SharedAssemblies.Add(assemblyInfo);
                }
            }

            // Determine if can unload immediately
            analysis.CanUnloadImmediately = !analysis.SharedAssemblies.Any();

            // Get dependent plugins
            analysis.DependentPlugins = analysis.SharedAssemblies
                .SelectMany(a => a.UsedByPlugins)
                .Where(p => !p.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p)
                .ToList();

            // Determine recommendation
            analysis.Recommendation = DetermineRecommendation(analysis);

            return analysis;
        }

        private static UnloadRecommendation DetermineRecommendation(UnloadAnalysis analysis)
        {
            if (analysis.CanUnloadImmediately)
            {
                return UnloadRecommendation.SafeToUnload;
            }

            if (analysis.DependentPlugins.Count <= 3)
            {
                return UnloadRecommendation.UnloadDependentsFirst;
            }

            if (analysis.DependentPlugins.Count > 3 && analysis.DependentPlugins.Count <= 10)
            {
                return UnloadRecommendation.ScheduleUnload;
            }

            return UnloadRecommendation.ForceUnload;
        }

        private static bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System.") ||
                   assemblyName.StartsWith("Microsoft.") ||
                   assemblyName.StartsWith("netstandard") ||
                   assemblyName.StartsWith("mscorlib");
        }

        private static long EstimateAssemblySize(Assembly assembly)
        {
            try
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    var fileInfo = new System.IO.FileInfo(assembly.Location);
                    return fileInfo.Length;
                }
            }
            catch
            {
                // Ignore errors
            }

            // Rough estimate if location not available
            return 100 * 1024; // 100KB default estimate
        }
    }
}

