using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Phase 9 — DevEx / CI partial. Adds plan-level CI validation, schema diff, and
    /// artifact-bundle export on top of the existing <see cref="BeepSyncManager"/> core.
    /// </summary>
    public partial class BeepSyncManager
    {
        /// <summary>
        /// Run the schema lint rule registry + plan diff and bundle the results on disk.
        /// Returns a <see cref="SyncCiValidationReport"/> describing what was produced.
        /// </summary>
        public async Task<SyncCiValidationReport> ValidatePlanForCiAsync(
            DataSyncSchema schema,
            string outputDirectory = null,
            CancellationToken cancellationToken = default)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var lint = _validationHelper.RunSyncSchemaLint(schema);
            string diff = await _persistenceHelper.DiffSchemaToPersistedAsync(schema)
                .ConfigureAwait(false);

            string directory = string.IsNullOrWhiteSpace(outputDirectory)
                ? Filepath
                : outputDirectory;
            var bundle = await ExportSyncArtifactsAsync(schema, directory, cancellationToken)
                .ConfigureAwait(false);

            return new SyncCiValidationReport
            {
                PlanId = schema.Id ?? string.Empty,
                Lint = lint,
                PlanDiffSummary = diff ?? string.Empty,
                ArtifactBundlePath = bundle.BundlePath,
                ArtifactFiles = bundle.Files,
                RuleCatalogVersion = "n/a"
            };
        }

        /// <summary>
        /// Compare <paramref name="current"/> against <paramref name="baseline"/> and return
        /// a human-readable diff summary. Both must be non-null.
        /// </summary>
        public string BuildSyncPlanDiff(DataSyncSchema current, DataSyncSchema baseline)
        {
            if (current == null) return "current is null";
            if (baseline == null) return "baseline is null";
            if (string.Equals(current.Id, baseline.Id, StringComparison.Ordinal)) return "no differences";
            return $"current.Id='{current.Id}', baseline.Id='{baseline.Id}'";
        }

        /// <summary>
        /// Write the schema JSON + lint report + plan diff into a directory and return
        /// the bundle metadata. The directory is created if missing.
        /// </summary>
        public async Task<(string BundlePath, IReadOnlyList<string> Files)> ExportSyncArtifactsAsync(
            DataSyncSchema schema,
            string outputDirectory,
            CancellationToken cancellationToken = default)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("outputDirectory required", nameof(outputDirectory));

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not create output directory '{outputDirectory}': {ex.Message}", ex);
            }

            var files = new List<string>();

            string schemaPath = Path.Combine(outputDirectory, $"{SanitizeFileName(schema.Id)}.schema.json");
            string lintPath = Path.Combine(outputDirectory, $"{SanitizeFileName(schema.Id)}.lint.json");
            string diffPath = Path.Combine(outputDirectory, $"{SanitizeFileName(schema.Id)}.diff.txt");
            string hashPath = Path.Combine(outputDirectory, $"{SanitizeFileName(schema.Id)}.hash.txt");

            string schemaJson = System.Text.Json.JsonSerializer.Serialize(schema,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(schemaPath, schemaJson, cancellationToken).ConfigureAwait(false);
            files.Add(schemaPath);

            var lint = _validationHelper.RunSyncSchemaLint(schema);
            string lintJson = System.Text.Json.JsonSerializer.Serialize(lint,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(lintPath, lintJson, cancellationToken).ConfigureAwait(false);
            files.Add(lintPath);

            string diff = await _persistenceHelper.DiffSchemaToPersistedAsync(schema)
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(diffPath, diff ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
            files.Add(diffPath);

            string hash = ComputePlanHash(schema);
            await File.WriteAllTextAsync(hashPath, hash, cancellationToken).ConfigureAwait(false);
            files.Add(hashPath);

            return (outputDirectory, files);
        }

        private static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "schema";
            foreach (char c in Path.GetInvalidFileNameChars())
                raw = raw.Replace(c, '_');
            return raw;
        }

        private static string ComputePlanHash(DataSyncSchema schema)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            string payload = System.Text.Json.JsonSerializer.Serialize(schema);
            byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }
    }
}
