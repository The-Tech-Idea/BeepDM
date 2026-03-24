using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Parses migration manifest .txt files that declare entity types for filesystem-based
    /// discovery (source = <see cref="EntityMigrationSource.DiscoveryFileSystem"/>).
    /// <para>
    /// Line format (pipe-delimited, 1–3 segments):
    ///   Full.Type.Name[|AssemblyHint[|NamespacePrefix]]
    /// </para>
    /// <para>
    /// Comments (#) and empty lines are silently ignored.
    /// </para>
    /// Error codes:
    /// <list type="bullet">
    ///   <item>MIG-MANIFEST-001 — invalid segment count (not 1..3)</item>
    ///   <item>MIG-MANIFEST-002 — empty type name segment</item>
    ///   <item>MIG-MANIFEST-003 — unresolved type after resolution pipeline</item>
    ///   <item>MIG-MANIFEST-004 — duplicate entity declaration</item>
    /// </list>
    /// </summary>
    public partial class MigrationManager
    {
        // ── Thread-safe backing store for manifest-resolved types ──
        private readonly object _manifestLock = new object();
        private List<(MigrationManifestEntry Entry, Type ResolvedType)> _lastManifestResolutions
            = new List<(MigrationManifestEntry, Type)>();

        // ────────────────────────────────────────────────────────────────────
        // IMigrationManager implementation
        // ────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public MigrationManifestParseResult ParseManifestFile(string filePath)
        {
            var result = new MigrationManifestParseResult { FilePath = filePath ?? string.Empty };

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.Errors.Add(new MigrationManifestParseError
                {
                    Code = "MIG-MANIFEST-002",
                    LineNumber = 0,
                    LineContent = string.Empty,
                    Message = "Manifest file path cannot be null or empty."
                });
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.Errors.Add(new MigrationManifestParseError
                {
                    Code = "MIG-MANIFEST-001",
                    LineNumber = 0,
                    LineContent = filePath,
                    Message = $"Manifest file not found: '{filePath}'"
                });
                return result;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new MigrationManifestParseError
                {
                    Code = "MIG-MANIFEST-001",
                    LineNumber = 0,
                    LineContent = filePath,
                    Message = $"Could not read manifest file: {ex.Message}"
                });
                return result;
            }

            ParseManifestLines(lines, result);
            return result;
        }

        // ────────────────────────────────────────────────────────────────────
        // Core parsing — separated so unit tests can drive it without I/O
        // ────────────────────────────────────────────────────────────────────

        internal void ParseManifestLines(IReadOnlyList<string> lines, MigrationManifestParseResult result)
        {
            var seenTypeNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Count; i++)
            {
                int lineNumber = i + 1;
                var raw = lines[i];

                // Ignore blank lines and comments
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var trimmed = raw.Trim();
                if (trimmed.StartsWith("#")) continue;

                var segments = trimmed.Split('|');

                if (segments.Length < 1 || segments.Length > 3)
                {
                    result.Errors.Add(new MigrationManifestParseError
                    {
                        Code = "MIG-MANIFEST-001",
                        LineNumber = lineNumber,
                        LineContent = trimmed,
                        Message = $"Line {lineNumber}: invalid segment count {segments.Length}. Expected 1–3 pipe-delimited segments."
                    });
                    continue;
                }

                var typeName = segments[0].Trim();
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    result.Errors.Add(new MigrationManifestParseError
                    {
                        Code = "MIG-MANIFEST-002",
                        LineNumber = lineNumber,
                        LineContent = trimmed,
                        Message = $"Line {lineNumber}: type name segment is empty."
                    });
                    continue;
                }

                // Duplicate check — MIG-MANIFEST-004
                if (seenTypeNames.TryGetValue(typeName, out var firstLine))
                {
                    result.Errors.Add(new MigrationManifestParseError
                    {
                        Code = "MIG-MANIFEST-004",
                        LineNumber = lineNumber,
                        LineContent = trimmed,
                        Message = $"Line {lineNumber}: duplicate declaration of '{typeName}' (first seen on line {firstLine})."
                    });
                    continue;
                }
                seenTypeNames[typeName] = lineNumber;

                var entry = new MigrationManifestEntry
                {
                    LineNumber = lineNumber,
                    TypeFullName = typeName,
                    AssemblyHint = segments.Length >= 2 ? segments[1].Trim() : string.Empty,
                    NamespacePrefix = segments.Length >= 3 ? segments[2].Trim() : string.Empty
                };

                result.Entries.Add(entry);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Type resolution — used by filesystem discovery path
        // Resolution order:
        //   1. Try already-loaded assemblies (no I/O)
        //   2. Try AssemblyHint (load by name if not loaded)
        //   First-wins policy.
        // ────────────────────────────────────────────────────────────────────

        internal (Type ResolvedType, string Error) ResolveManifestEntryType(MigrationManifestEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TypeFullName))
                return (null, "MIG-MANIFEST-002: type name is empty");

            // Pass 1 — loaded assemblies (deterministic, no disk I/O)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                try
                {
                    var t = asm.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
                    if (t != null) return (t, null);
                }
                catch { }
            }

            // Pass 2 — AssemblyHint load (policy-controlled: only when hint is provided)
            if (!string.IsNullOrWhiteSpace(entry.AssemblyHint))
            {
                try
                {
                    var hinted = System.Reflection.Assembly.Load(entry.AssemblyHint);
                    if (hinted != null)
                    {
                        var t = hinted.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
                        if (t != null) return (t, null);
                    }
                }
                catch { }
            }

            return (null, $"MIG-MANIFEST-003: type '{entry.TypeFullName}' could not be resolved");
        }

        /// <summary>
        /// Resolves all entries in a <see cref="MigrationManifestParseResult"/> to <see cref="Type"/> instances.
        /// Adds MIG-MANIFEST-003 errors for unresolved types.
        /// Returns only the successfully resolved types.
        /// </summary>
        public List<(Type ResolvedType, EntityMigrationMetadata Metadata)> ResolveManifestTypes(
            MigrationManifestParseResult parseResult)
        {
            if (parseResult == null) return new List<(Type, EntityMigrationMetadata)>();

            var resolved = new List<(Type, EntityMigrationMetadata)>();

            foreach (var entry in parseResult.Entries)
            {
                var (type, error) = ResolveManifestEntryType(entry);
                if (type == null)
                {
                    parseResult.Errors.Add(new MigrationManifestParseError
                    {
                        Code = "MIG-MANIFEST-003",
                        LineNumber = entry.LineNumber,
                        LineContent = entry.TypeFullName,
                        Message = error ?? $"MIG-MANIFEST-003: type '{entry.TypeFullName}' could not be resolved"
                    });
                    continue;
                }

                var meta = new EntityMigrationMetadata
                {
                    TypeFullName = entry.TypeFullName,
                    AssemblyName = type.Assembly?.GetName().Name ?? string.Empty,
                    Source = EntityMigrationSource.DiscoveryFileSystem,
                    NamespacePrefix = entry.NamespacePrefix
                };

                resolved.Add((type, meta));
            }

            return resolved;
        }

        /// <summary>
        /// Applies migrations sourced from a manifest file.
        /// Parses the manifest, resolves types, and runs them through the shared entity pipeline.
        /// </summary>
        public IErrorsInfo ApplyMigrationsFromManifest(
            string manifestFilePath,
            bool addMissingColumns = true,
            IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (string.IsNullOrWhiteSpace(manifestFilePath))
                return CreateErrorsInfo(Errors.Failed, "Manifest file path cannot be null or empty");

            var parseResult = ParseManifestFile(manifestFilePath);

            if (parseResult.Errors.Any(e => e.Code == "MIG-MANIFEST-001" || e.Code == "MIG-MANIFEST-002"))
            {
                var fatalErrors = parseResult.Errors
                    .Where(e => e.Code == "MIG-MANIFEST-001" || e.Code == "MIG-MANIFEST-002")
                    .Select(e => e.Message);
                return CreateErrorsInfo(Errors.Failed, $"Manifest parse failed: {string.Join("; ", fatalErrors)}");
            }

            var resolved = ResolveManifestTypes(parseResult);
            if (parseResult.Errors.Count > 0)
            {
                var errMsgs = parseResult.Errors.Select(e => $"Line {e.LineNumber}: [{e.Code}] {e.Message}");
                _editor?.AddLogMessage("Beep",
                    $"MigrationManager.ApplyMigrationsFromManifest: {parseResult.Errors.Count} manifest warning(s): {string.Join(" | ", errMsgs)}",
                    DateTime.Now, 0, null, Errors.Warning);
            }

            if (resolved.Count == 0)
                return CreateErrorsInfo(Errors.Warning, $"No entity types could be resolved from manifest '{manifestFilePath}'");

            var typeList = resolved.Select(r => r.ResolvedType).ToList();
            var pipelineResult = ExecuteEntityMigrationPipeline(
                typeList,
                usesDiscovery: true,
                source: EntityMigrationSource.DiscoveryFileSystem,
                addMissingColumns: addMissingColumns,
                progress: progress);

            return pipelineResult.HasBlockingReasons
                ? CreateErrorsInfo(Errors.Failed, $"{pipelineResult.ToSummaryString()}. Blocking: {string.Join("; ", pipelineResult.BlockingReasons)}")
                : CreateErrorsInfo(pipelineResult.ErrorCount > 0 ? Errors.Failed : Errors.Ok, pipelineResult.ToSummaryString());
        }
    }
}
