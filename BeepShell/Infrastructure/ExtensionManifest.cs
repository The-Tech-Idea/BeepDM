using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Extension manifest for packaging and distribution
    /// </summary>
    public class ExtensionManifest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("homepage")]
        public string Homepage { get; set; }

        [JsonPropertyName("repository")]
        public string Repository { get; set; }

        [JsonPropertyName("license")]
        public string License { get; set; }

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("minShellVersion")]
        public string MinShellVersion { get; set; }

        [JsonPropertyName("maxShellVersion")]
        public string MaxShellVersion { get; set; }

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string> Dependencies { get; set; } = new();

        [JsonPropertyName("assembly")]
        public AssemblyInfo Assembly { get; set; }

        [JsonPropertyName("commands")]
        public List<CommandInfo> Commands { get; set; } = new();

        [JsonPropertyName("workflows")]
        public List<WorkflowInfo> Workflows { get; set; } = new();

        [JsonPropertyName("configuration")]
        public ConfigurationInfo Configuration { get; set; }

        [JsonPropertyName("changelog")]
        public List<ChangelogEntry> Changelog { get; set; } = new();

        public static ExtensionManifest Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ExtensionManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });
        }

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        public bool IsCompatible(string shellVersion)
        {
            if (string.IsNullOrEmpty(MinShellVersion) && string.IsNullOrEmpty(MaxShellVersion))
                return true;

            var currentVersion = Version.Parse(shellVersion);

            if (!string.IsNullOrEmpty(MinShellVersion))
            {
                if (currentVersion < Version.Parse(MinShellVersion))
                    return false;
            }

            if (!string.IsNullOrEmpty(MaxShellVersion))
            {
                if (currentVersion > Version.Parse(MaxShellVersion))
                    return false;
            }

            return true;
        }
    }

    public class AssemblyInfo
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("targetFramework")]
        public string TargetFramework { get; set; }

        [JsonPropertyName("runtimeDependencies")]
        public List<string> RuntimeDependencies { get; set; } = new();
    }

    public class CommandInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; } = new();

        [JsonPropertyName("examples")]
        public List<string> Examples { get; set; } = new();
    }

    public class WorkflowInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("parameters")]
        public List<ParameterInfo> Parameters { get; set; } = new();
    }

    public class ParameterInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("defaultValue")]
        public object DefaultValue { get; set; }
    }

    public class ConfigurationInfo
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("schema")]
        public Dictionary<string, object> Schema { get; set; } = new();
    }

    public class ChangelogEntry
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("changes")]
        public List<string> Changes { get; set; } = new();
    }

    /// <summary>
    /// Extension package validator
    /// </summary>
    public class ExtensionValidator
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public List<string> Errors => _errors;
        public List<string> Warnings => _warnings;

        public bool Validate(ExtensionManifest manifest)
        {
            _errors.Clear();
            _warnings.Clear();

            // Required fields
            if (string.IsNullOrWhiteSpace(manifest.Id))
                _errors.Add("Manifest must have an 'id' field");

            if (string.IsNullOrWhiteSpace(manifest.Name))
                _errors.Add("Manifest must have a 'name' field");

            if (string.IsNullOrWhiteSpace(manifest.Version))
                _errors.Add("Manifest must have a 'version' field");

            if (string.IsNullOrWhiteSpace(manifest.Author))
                _errors.Add("Manifest must have an 'author' field");

            // Version validation
            if (!string.IsNullOrWhiteSpace(manifest.Version))
            {
                if (!Version.TryParse(manifest.Version, out _))
                    _errors.Add($"Invalid version format: {manifest.Version}");
            }

            // Assembly validation
            if (manifest.Assembly == null)
                _errors.Add("Manifest must specify an 'assembly' section");
            else if (string.IsNullOrWhiteSpace(manifest.Assembly.FileName))
                _errors.Add("Assembly fileName must be specified");

            // Warnings
            if (string.IsNullOrWhiteSpace(manifest.Description))
                _warnings.Add("Description is recommended");

            if (string.IsNullOrWhiteSpace(manifest.License))
                _warnings.Add("License information is recommended");

            if (!manifest.Commands.Any() && !manifest.Workflows.Any())
                _warnings.Add("Extension should provide at least one command or workflow");

            return !_errors.Any();
        }
    }
}
