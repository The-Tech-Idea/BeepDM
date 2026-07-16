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
    ///   <item>MIG-MANIFEST-005 — null or empty manifest file path</item>
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
                    Code = "MIG-MANIFEST-005",
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
        //   1. Try AssemblyHint when provided
        //   2. Try manually registered assemblies
        //   3. Try assembly-handler plugin assemblies
        //   4. Try already-loaded AppDomain assemblies as a broad fallback
        //   First-wins policy.
        // ────────────────────────────────────────────────────────────────────

        internal (Type ResolvedType, string Error) ResolveManifestEntryType(MigrationManifestEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TypeFullName))
                return (null, "MIG-MANIFEST-002: type name is empty");

            // Pass 1 — AssemblyHint load (policy-controlled: only when hint is provided)
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
                catch (Exception ex)
                {
                    // GetType(throwOnError:false) returns null when the type is simply absent, so this
                    // catch only fires on a real load fault — surface it instead of swallowing.
                    _editor?.AddLogMessage("MigrationManager",
                        $"ResolveType: assembly hint '{entry.AssemblyHint}' failed to load for '{entry.TypeFullName}': {ex.Message}",
                        DateTime.Now, 0, null, Errors.Warning);
                }
            }

            // Pass 2 — registered assemblies (RegisterAssembly / RegisterAssemblies)
            // before broad AppDomain scanning. This mirrors discovery priority and
            // avoids resolving an older type version that happens to be loaded.
            foreach (var asm in GetRegisteredAssemblies())
            {
                if (asm == null || asm.IsDynamic) continue;
                try
                {
                    var t = asm.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
                    if (t != null) return (t, null);
                }
                catch (Exception ex)
                {
                    _editor?.AddLogMessage("MigrationManager",
                        $"ResolveType: probing registered assembly '{asm.GetName()?.Name}' for '{entry.TypeFullName}' faulted: {ex.Message}",
                        DateTime.Now, 0, null, Errors.Warning);
                }
            }

            // Pass 3 — editor's assembly handler (plugin assemblies). These are
            // runtime plugin sources used by DiscoverEntityTypes.
            if (_editor?.assemblyHandler?.Assemblies != null)
            {
                foreach (var asmRef in _editor.assemblyHandler.Assemblies)
                {
                    var asm = asmRef?.DllLib;
                    if (asm == null || asm.IsDynamic) continue;
                    try
                    {
                        var t = asm.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
                        if (t != null) return (t, null);
                    }
                    catch (Exception ex)
                    {
                        _editor?.AddLogMessage("MigrationManager",
                            $"ResolveType: probing plugin assembly '{asm.GetName()?.Name}' for '{entry.TypeFullName}' faulted: {ex.Message}",
                            DateTime.Now, 0, null, Errors.Warning);
                    }
                }
            }

            if (_editor?.assemblyHandler?.LoadedAssemblies != null)
            {
                foreach (var asm in _editor.assemblyHandler.LoadedAssemblies)
                {
                    if (asm == null || asm.IsDynamic) continue;
                    try
                    {
                        var t = asm.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
                        if (t != null) return (t, null);
                    }
                    catch (Exception ex)
                    {
                        _editor?.AddLogMessage("MigrationManager",
                            $"ResolveType: probing loaded plugin assembly '{asm.GetName()?.Name}' for '{entry.TypeFullName}' faulted: {ex.Message}",
                            DateTime.Now, 0, null, Errors.Warning);
                    }
                }
            }

            // Pass 4 — loaded assemblies (broad fallback, no disk I/O).
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == null || asm.IsDynamic) continue;
                try
                {
                    var t = asm.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
                    if (t != null) return (t, null);
                }
                catch (Exception ex)
                {
                    _editor?.AddLogMessage("MigrationManager",
                        $"ResolveType: probing loaded assembly '{asm.GetName()?.Name}' for '{entry.TypeFullName}' faulted: {ex.Message}",
                        DateTime.Now, 0, null, Errors.Warning);
                }
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
        /// Shared preamble for manifest-based migration methods. Parses the
        /// manifest file, resolves all entries to runtime Types, logs
        /// warnings, and validates that at least one type was found. Returns
        /// the resolved type list on success, or an ErrorsInfo on failure
        /// (null resolved list).
        /// </summary>
        private (IErrorsInfo Error, List<Type> ResolvedTypes) ParseAndResolveManifestEntities(
            string manifestFilePath,
            string callerName,
            bool addMissingColumns,
            IProgress<PassedArgs> progress,
            bool applyForeignKeys,
            bool applyIndexes)
        {
            if (MigrateDataSource == null)
                return (CreateErrorsInfo(Errors.Failed, "Migration data source is not set"), null);

            var parseResult = ParseManifestFile(manifestFilePath);

            if (parseResult.Errors.Any(e => e.Code == "MIG-MANIFEST-001" || e.Code == "MIG-MANIFEST-002" || e.Code == "MIG-MANIFEST-005"))
            {
                var fatalErrors = parseResult.Errors
                    .Where(e => e.Code == "MIG-MANIFEST-001" || e.Code == "MIG-MANIFEST-002" || e.Code == "MIG-MANIFEST-005")
                    .Select(e => e.Message);
                return (CreateErrorsInfo(Errors.Failed, $"Manifest parse failed: {string.Join("; ", fatalErrors)}"), null);
            }

            var resolved = ResolveManifestTypes(parseResult);
            if (parseResult.Errors.Count > 0)
            {
                var errMsgs = parseResult.Errors.Select(e => $"Line {e.LineNumber}: [{e.Code}] {e.Message}");
                _editor?.AddLogMessage("Beep",
                    $"MigrationManager.{callerName}: {parseResult.Errors.Count} manifest warning(s): {string.Join(" | ", errMsgs)}",
                    DateTime.Now, 0, null, Errors.Warning);
            }

            if (resolved.Count == 0)
                return (CreateErrorsInfo(Errors.Warning, $"No entity types could be resolved from manifest '{manifestFilePath}'"), null);

            var typeList = resolved.Select(r => r.ResolvedType).ToList();
            return (null, typeList);
        }

        /// <summary>
        /// Applies migrations sourced from a manifest file.
        /// Parses the manifest, resolves types, and runs them through the shared entity pipeline.
        /// </summary>
        /// <param name="applyForeignKeys">When true, FKs declared on the entity structures (and any
        /// model-interop-sourced ORM FKs already cached on the manager) are created after their
        /// dependent tables. Defaults to false to preserve prior behavior.</param>
        /// <param name="applyIndexes">When true, indexes declared on the entity structures are
        /// created after their tables. Defaults to false to preserve prior behavior.</param>
        public IErrorsInfo ApplyMigrationsFromManifest(
            string manifestFilePath,
            bool addMissingColumns = true,
            IProgress<PassedArgs> progress = null,
            bool applyForeignKeys = false,
            bool applyIndexes = false)
        {
            var (parseError, typeList) = ParseAndResolveManifestEntities(
                manifestFilePath, nameof(ApplyMigrationsFromManifest),
                addMissingColumns, progress, applyForeignKeys, applyIndexes);
            if (parseError != null)
                return parseError;
            var pipelineResult = ExecuteEntityMigrationPipeline(
                typeList,
                usesDiscovery: true,
                source: EntityMigrationSource.DiscoveryFileSystem,
                addMissingColumns: addMissingColumns,
                progress: progress,
                applyForeignKeys: applyForeignKeys,
                applyIndexes: applyIndexes);

            return pipelineResult.HasBlockingReasons
                ? CreateErrorsInfo(Errors.Failed, $"{pipelineResult.ToSummaryString()}. Blocking: {string.Join("; ", pipelineResult.BlockingReasons)}")
                : CreateErrorsInfo(pipelineResult.ErrorCount > 0 ? Errors.Failed : Errors.Ok, pipelineResult.ToSummaryString());
        }

        /// <summary>
        /// Ensures that the migration datasource contains every entity declared in the
        /// manifest file. Mirrors <see cref="ApplyMigrationsFromManifest"/> but never
        /// adds missing columns and never fails on a column-shape mismatch — it only
        /// creates entities that are missing and, when the corresponding opt-in flags
        /// are set, applies the indexes and foreign keys declared on each entity.
        /// </summary>
        /// <param name="applyForeignKeys">When true, FKs declared on the entity structures are
        /// created after their dependent tables. Defaults to false.</param>
        /// <param name="applyIndexes">When true, indexes declared on the entity structures are
        /// created after their tables. Defaults to false.</param>
        public IErrorsInfo EnsureDatabaseCreatedFromManifest(
            string manifestFilePath,
            IProgress<PassedArgs> progress = null,
            bool applyForeignKeys = false,
            bool applyIndexes = false)
        {
            var (parseError, typeList) = ParseAndResolveManifestEntities(
                manifestFilePath, nameof(EnsureDatabaseCreatedFromManifest),
                addMissingColumns: false, progress, applyForeignKeys, applyIndexes);
            if (parseError != null)
                return parseError;

            // Run the same shared pipeline as the explicit-type path. addMissingColumns=false
            // mirrors the contract of EnsureDatabaseCreatedForTypes: never add columns that
            // aren't already on the target schema, just create missing entities and (when
            // opted in) their relational artifacts.
            var pipelineResult = ExecuteEntityMigrationPipeline(
                typeList,
                usesDiscovery: true,
                source: EntityMigrationSource.DiscoveryFileSystem,
                addMissingColumns: false,
                progress: progress,
                applyForeignKeys: applyForeignKeys,
                applyIndexes: applyIndexes);

            return pipelineResult.HasBlockingReasons
                ? CreateErrorsInfo(Errors.Failed, $"{pipelineResult.ToSummaryString()}. Blocking: {string.Join("; ", pipelineResult.BlockingReasons)}")
                : CreateErrorsInfo(pipelineResult.ErrorCount > 0 ? Errors.Failed : Errors.Ok, pipelineResult.ToSummaryString());
        }
    }
}
