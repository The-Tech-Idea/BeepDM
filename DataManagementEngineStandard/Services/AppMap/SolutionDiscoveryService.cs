using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Scans file-system paths for .sln files, parses .csproj metadata,
    /// builds project dependency graphs, and auto-detects Data folders.
    /// </summary>
    public sealed class SolutionDiscoveryService : ISolutionDiscoveryService
    {
        private readonly IDMEEditor _editor;
        private static readonly Regex SlnProjectRegex = new(
            @"Project\(""\{(?<typeGuid>[A-Fa-f0-9\-]+)\}""\)\s*=\s*""(?<name>[^""]+)""\s*,\s*""(?<path>[^""]+)""\s*,\s*""\{(?<projectGuid>[A-Fa-f0-9\-]+)\}""",
            RegexOptions.Compiled);

        public SolutionDiscoveryService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        // ── Public API ──────────────────────────────────────────────

        public Task<SolutionInfo?> DiscoverAsync(string path, DiscoveryOptions? options = null, CancellationToken token = default)
        {
            options ??= new DiscoveryOptions();
            Log("Starting solution discovery", path);

            // 1. Find .sln file
            var slnPath = ResolveSlnPath(path, options.MaxDepth);
            if (slnPath == null)
            {
                Log("No .sln file found", path);
                return Task.FromResult<SolutionInfo?>(null);
            }

            // 2. Parse .sln
            var rawPaths = new List<string>();
            var solution = ParseSln(slnPath, rawPaths);
            if (solution == null) return Task.FromResult<SolutionInfo?>(null);

            // 3. Parse each .csproj
            var slnDir = Path.GetDirectoryName(slnPath)!;
            var projects = new List<ProjectInfo>();
            foreach (var projPath in rawPaths)
            {
                token.ThrowIfCancellationRequested();
                var fullPath = Path.GetFullPath(Path.Combine(slnDir, projPath));
                if (!File.Exists(fullPath))
                {
                    Log("Project file not found", fullPath);
                    continue;
                }

                var info = ParseCsproj(fullPath, options);
                if (info != null) projects.Add(info);
            }

            solution.Projects = projects;
            solution.DiscoveredAt = DateTime.UtcNow;

            Log("Solution discovery complete", $"{projects.Count} projects found in {solution.Name}");
            return Task.FromResult<SolutionInfo?>(solution);
        }

        public Task<ProjectInfo?> DiscoverProjectAsync(string csprojPath, CancellationToken token = default)
        {
            if (!File.Exists(csprojPath)) return Task.FromResult<ProjectInfo?>(null);
            var info = ParseCsproj(csprojPath, new DiscoveryOptions());
            return Task.FromResult(info);
        }

        public ProjectDependencyGraph BuildDependencyGraph(IReadOnlyList<ProjectInfo> projects)
        {
            var graph = new ProjectDependencyGraph();
            foreach (var p in projects)
            {
                graph.Adjacency[p.Name] = p.ProjectReferences
                    .Where(r => projects.Any(op => op.Name.Equals(r, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            return graph;
        }

        public List<string> FindSolutionFiles(string directory, int maxDepth = 4)
        {
            var results = new List<string>();
            FindSlnRecursive(directory, 0, maxDepth, results);
            return results;
        }

        // ── .sln parser ─────────────────────────────────────────────

        private string? ResolveSlnPath(string path, int maxDepth)
        {
            // If it's a direct .sln file
            if (File.Exists(path) && path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                return path;

            // If it's a directory, search for .sln
            var dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path) ?? path;
            var slnFiles = FindSolutionFiles(dir, maxDepth);

            if (slnFiles.Count == 1) return slnFiles[0];
            if (slnFiles.Count > 1)
            {
                // Prefer the one matching the directory name
                var dirName = new DirectoryInfo(dir).Name;
                var best = slnFiles.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals(dirName, StringComparison.OrdinalIgnoreCase));
                return best ?? slnFiles[0];
            }
            return null;
        }

        private void FindSlnRecursive(string dir, int depth, int maxDepth, List<string> results)
        {
            if (depth > maxDepth || !Directory.Exists(dir)) return;

            try
            {
                results.AddRange(Directory.GetFiles(dir, "*.sln"));
                if (results.Count == 0)
                {
                    foreach (var sub in Directory.GetDirectories(dir))
                        FindSlnRecursive(sub, depth + 1, maxDepth, results);
                }
            }
            catch { /* skip inaccessible directories */ }
        }

        private SolutionInfo? ParseSln(string slnPath, List<string> rawPaths)
        {
            try
            {
                var content = File.ReadAllText(slnPath);
                var solution = new SolutionInfo
                {
                    Name = Path.GetFileNameWithoutExtension(slnPath),
                    SlnPath = slnPath,
                    FormatVersion = "12.00" // default
                };

                // Detect format version
                var verMatch = Regex.Match(content, @"Microsoft Visual Studio Solution File, Format Version\s+(\d+\.\d+)");
                if (verMatch.Success) solution.FormatVersion = verMatch.Groups[1].Value;

                // Parse configurations
                var cfgMatch = Regex.Match(content, @"GlobalSection\(SolutionConfigurationPlatforms\)[^}]*\}(.*?)EndGlobalSection", RegexOptions.Singleline);
                if (cfgMatch.Success)
                {
                    var cfgLines = cfgMatch.Groups[1].Value.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    solution.Configurations = cfgLines
                        .Select(l => l.Split('=')[0].Trim())
                        .Distinct()
                        .ToList();
                }

                // Parse projects
                var matches = SlnProjectRegex.Matches(content);
                foreach (Match m in matches)
                {
                    var projPath = m.Groups["path"].Value.Trim();
                    if (projPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                        rawPaths.Add(projPath);
                }

                return solution;
            }
            catch (Exception ex)
            {
                Log("Error parsing .sln", ex.Message);
                return null;
            }
        }

        // ── .csproj parser ──────────────────────────────────────────

        private ProjectInfo? ParseCsproj(string csprojPath, DiscoveryOptions options)
        {
            try
            {
                var xml = XDocument.Load(csprojPath);
                var root = xml.Root;
                if (root == null) return null;

                var info = new ProjectInfo
                {
                    Name = Path.GetFileNameWithoutExtension(csprojPath),
                    CsprojPath = csprojPath,
                    ProjectDirectory = Path.GetDirectoryName(csprojPath)!,
                    Sdk = root.Attribute("Sdk")?.Value
                };

                // Target framework
                var tfm = root.Descendants("TargetFramework").FirstOrDefault()?.Value
                       ?? root.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(tfm)) info.TargetFramework = tfm;

                // Output type
                var outputType = root.Descendants("OutputType").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(outputType)) info.OutputType = outputType;

                // Root namespace
                var rootNs = root.Descendants("RootNamespace").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(rootNs)) info.RootNamespace = rootNs;

                // Package references
                if (options.IncludeNuGetPackages)
                {
                    foreach (var pr in root.Descendants("PackageReference"))
                    {
                        var pkgName = pr.Attribute("Include")?.Value ?? pr.Attribute("Update")?.Value;
                        var pkgVer = pr.Attribute("Version")?.Value ?? pr.Descendants("Version").FirstOrDefault()?.Value;
                        if (!string.IsNullOrEmpty(pkgName))
                            info.PackageReferences[pkgName] = pkgVer ?? "unknown";
                    }
                }

                // Project references
                foreach (var pr in root.Descendants("ProjectReference"))
                {
                    var projPath = pr.Attribute("Include")?.Value;
                    if (!string.IsNullOrEmpty(projPath))
                    {
                        var refName = Path.GetFileNameWithoutExtension(projPath);
                        info.ProjectReferences.Add(refName);
                    }
                }

                // Test project detection
                info.IsTestProject = info.PackageReferences.Keys.Any(k =>
                    k.StartsWith("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                    k.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                    k.StartsWith("NUnit", StringComparison.OrdinalIgnoreCase) ||
                    k.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase));

                // Data folder detection
                if (options.DataFolderPatterns.Count > 0)
                {
                    info.DataFolders = DetectDataFolders(info.ProjectDirectory, options);
                }

                return info;
            }
            catch (Exception ex)
            {
                Log("Error parsing .csproj", $"{csprojPath}: {ex.Message}");
                return null;
            }
        }

        // ── Data folder detection ────────────────────────────────────

        private List<DataFolderInfo> DetectDataFolders(string projectDir, DiscoveryOptions options)
        {
            var results = new List<DataFolderInfo>();

            foreach (var pattern in options.DataFolderPatterns)
            {
                var candidate = Path.Combine(projectDir, pattern);
                if (!Directory.Exists(candidate)) continue;

                var csFiles = Directory.GetFiles(candidate, "*.cs", SearchOption.AllDirectories);
                if (csFiles.Length == 0) continue;

                var hasDbContext = csFiles.Any(f =>
                {
                    try
                    {
                        var text = File.ReadAllText(f);
                        return text.Contains("DbContext") || text.Contains("IdentityDbContext");
                    }
                    catch { return false; }
                });

                results.Add(new DataFolderInfo
                {
                    Path = candidate,
                    HasDbContext = hasDbContext,
                    FileCount = csFiles.Length,
                    Confidence = hasDbContext ? 0.95f : 0.4f + Math.Min(csFiles.Length / 20f, 0.4f)
                });
            }

            return results;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void Log(string message, string? detail = null)
        {
            _editor.AddLogMessage("SolutionDiscovery",
                detail != null ? $"{message}: {detail}" : message,
                DateTime.Now, 0, null, Errors.Ok);
        }
    }
}
