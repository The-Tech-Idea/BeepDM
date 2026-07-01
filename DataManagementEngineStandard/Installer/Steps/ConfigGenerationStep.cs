using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Generates configuration files (appsettings.json, connection strings) from templates.</summary>
    public class ConfigGenerationStep : ISetupStep
    {
        public string StepId => "installer.config.generate";
        public string StepName => "Generate configuration";
        public string Description => "Creates application configuration files from templates.";
        public IReadOnlyList<string> DependsOn { get; }

        public ConfigGenerationStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<ConfigTemplate>>("ConfigTemplates")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var templates = context.TryGetProperty<List<ConfigTemplate>>("ConfigTemplates");
            if (templates == null || templates.Count == 0)
                return StepErrorHelpers.Ok("No config templates.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            int generated = 0;

            foreach (var tpl in templates)
            {
                progress?.Report(new PassedArgs { Messege = $"Generating: {tpl.FileName}" });
                try
                {
                    var destPath = Path.Combine(installPath, tpl.FileName);
                    var dir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    // If content template provided, fill variables
                    var content = tpl.Content ?? "{}";
                    content = content
                        .Replace("{InstallPath}", installPath)
                        .Replace("{ProductName}", context.TryGetProperty<InstallConfig>("InstallConfig")?.ProductName ?? "")
                        .Replace("{ProductVersion}", context.TryGetProperty<InstallConfig>("InstallConfig")?.ProductVersion ?? "")
                        .Replace("{MachineName}", Environment.MachineName)
                        .Replace("{UserName}", Environment.UserName)
                        .Replace("{InstalledAt}", DateTime.Now.ToString("O"));

                    if (tpl.IsJson && tpl.DefaultValues?.Count > 0)
                    {
                        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content) ?? new();
                        var updated = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in json) updated[kv.Key] = kv.Value;

                        foreach (var def in tpl.DefaultValues)
                            if (!updated.ContainsKey(def.Key) || updated[def.Key] == null)
                                updated[def.Key] = def.Value;

                        content = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });
                    }

                    if (!File.Exists(destPath) || tpl.Overwrite)
                    {
                        File.WriteAllText(destPath, content);
                        generated++;
                    }
                }
                catch (Exception ex)
                {
                    return StepErrorHelpers.Fail($"Failed to generate {tpl.FileName}: {ex.Message}");
                }
            }

            return StepErrorHelpers.Ok($"{generated} config files generated.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    public class ConfigTemplate
    {
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsJson { get; set; } = true;
        public bool Overwrite { get; set; }
        public Dictionary<string, object?> DefaultValues { get; set; } = new();
    }
}
