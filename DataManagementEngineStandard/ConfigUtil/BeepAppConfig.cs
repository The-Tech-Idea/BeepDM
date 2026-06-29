using System;
using System.IO;

namespace TheTechIdea.Beep.ConfigUtil;

/// <summary>
/// Runtime bridge: helps a .NET app (web or desktop) detect which environment
/// it is running in and resolve the correct <c>appsettings.{env}.json</c> path.
/// The Studio generates these config files; this utility tells the host which
/// one to load at startup.
///
/// <para><b>Environment detection fallback chain</b> (first to match wins):</para>
/// <list type="number">
///   <item><c>BEEP_ENV</c> environment variable (set by deploy script / CI)</item>
///   <item><c>ASPNETCORE_ENVIRONMENT</c> (standard ASP.NET)</item>
///   <item><c>DOTNET_ENVIRONMENT</c> (standard generic host)</item>
///   <item><c>.beep-env</c> marker file next to the executable (desktop apps, CI-stamped)</item>
///   <item>Machine-name lookup from <c>Environment.MachineName</c> (optional registered map)</item>
///   <item>Fallback: <c>"Development"</c></item>
/// </list>
///
/// <para><b>Usage — web app (Program.cs):</b></para>
/// <code>
/// var env = BeepAppConfig.DetectEnvironment();                                   // e.g. "Staging"
/// var configPath = BeepAppConfig.GetConfigFilePath(env);                         // "appsettings.Staging.json"
/// builder.Configuration.AddJsonFile(configPath, optional: true, reloadOnChange: false);
/// </code>
///
/// <para><b>Usage — desktop app (Program.cs / App.xaml.cs):</b></para>
/// <code>
/// BeepAppConfig.StampEnvironment("staging");                                     // deploy script stamps the marker
/// var env = BeepAppConfig.DetectEnvironment();                                   // reads ".beep-env" → "Staging"
/// var connStr = config.GetConnectionString("MyProject_main");
/// </code>
/// </summary>
public static class BeepAppConfig
{
    // ── Environment variables to probe (in order) ───────────────────────────

    private static readonly string[] EnvVarNames = { "BEEP_ENV", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT" };

    /// <summary>Name of the marker file next to the executable (e.g. contains <c>dev</c>).</summary>
    public const string EnvMarkerFileName = ".beep-env";

    // ── Environment detection ───────────────────────────────────────────────

    /// <summary>
    /// Detect the current environment using the fallback chain.
    /// Never returns null — last resort is <c>"Development"</c>.
    /// </summary>
    public static string DetectEnvironment()
    {
        // 1. Environment variables
        foreach (var name in EnvVarNames)
        {
            var val = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(val))
                return Normalise(val!);
        }

        // 2. .beep-env marker file
        var marker = FindEnvMarker();
        if (!string.IsNullOrWhiteSpace(marker))
            return Normalise(marker);

        // 3. Machine-name mapping (register via MapMachine)
        var machine = Environment.MachineName;
        if (!string.IsNullOrWhiteSpace(machine) && _machineMap.TryGetValue(machine.ToLowerInvariant(), out var mapped))
            return mapped;

        // 4. Fallback
        return "Development";
    }

    /// <summary>
    /// Detect the current environment's tier level (Dev, Test, Staging, Production, or raw).
    /// </summary>
    public static string DetectTier()
    {
        var env = DetectEnvironment().ToLowerInvariant();
        return env switch
        {
            "development" or "dev" or "debug" => "Dev",
            "test" or "testing" or "qa" => "Test",
            "staging" or "uat" => "Staging",
            "production" or "prod" or "live" or "release" => "Production",
            _ => env
        };
    }

    // ── Config-file path resolution ─────────────────────────────────────────

    /// <summary>
    /// Resolve the full path to the appsettings file for the given (or detected)
    /// environment id. The file is expected in the app's root directory or
    /// <paramref name="basePath"/> when supplied.
    /// </summary>
    /// <returns>The full path or <c>null</c> when the file does not exist.</returns>
    public static string? GetConfigFilePath(string? envId = null, string? basePath = null)
    {
        var env = envId ?? DetectEnvironment();
        var tier = env.ToLowerInvariant() switch
        {
            "development" or "dev" => "Development",
            "test" or "testing" => "Test",
            "staging" => "Staging",
            "production" or "prod" or "live" => "Production",
            _ => "Development"
        };
        var root = basePath ?? AppContext.BaseDirectory;
        var fileName = $"appsettings.{tier}.json";
        var fullPath = Path.Combine(root, fileName);
        return File.Exists(fullPath) ? fullPath : null;
    }

    // ── Marker-file helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Write a <c>.beep-env</c> marker file containing the environment id.
    /// Deploy / CI scripts call this once after publishing to stamp the target environment
    /// so the running app can discover it without environment variables.
    /// </summary>
    /// <param name="envId">The environment id to stamp (e.g. <c>dev</c>, <c>staging</c>, <c>prod</c>).</param>
    /// <param name="rootPath">App root. Defaults to the current directory.</param>
    public static void StampEnvironment(string envId, string? rootPath = null)
    {
        var root = rootPath ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, EnvMarkerFileName), envId.Trim().ToLowerInvariant());
    }

    /// <summary>Read the marker file, or null when absent.</summary>
    public static string? ReadEnvMarker(string? rootPath = null)
    {
        var root = rootPath ?? AppContext.BaseDirectory;
        var path = Path.Combine(root, EnvMarkerFileName);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    // ── Machine-name → environment mapping (optional, registry) ─────────────

    private static readonly System.Collections.Generic.Dictionary<string, string> _machineMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Map a machine name (hostname) to an environment id for auto-detection.</summary>
    public static void MapMachine(string machineName, string envId)
    {
        _machineMap[machineName.Trim().ToLowerInvariant()] = Normalise(envId);
    }

    /// <summary>Clear all machine-name mappings.</summary>
    public static void ClearMachineMap() => _machineMap.Clear();

    // ── Internal ────────────────────────────────────────────────────────────

    private static string? FindEnvMarker()
    {
        var dir = AppContext.BaseDirectory;
        for (int level = 0; level < 6; level++)
        {
            var path = Path.Combine(dir, EnvMarkerFileName);
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    private static string Normalise(string env)
    {
        var t = env.Trim().ToLowerInvariant();
        return t switch
        {
            "development" or "dev" or "debug" => "Development",
            "test" or "testing" or "qa" => "Test",
            "staging" or "uat" => "Staging",
            "production" or "prod" or "live" or "release" => "Production",
            _ => env.Trim()
        };
    }
}
