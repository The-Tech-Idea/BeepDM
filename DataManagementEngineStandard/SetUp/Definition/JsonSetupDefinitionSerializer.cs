using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TheTechIdea.Beep.SetUp.Definition;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// <see cref="ISetupDefinitionSerializer"/> over <c>System.Text.Json</c> — matching the
    /// neighbouring setup code (the local state store), not the engine's Newtonsoft dependency.
    /// </summary>
    public sealed class JsonSetupDefinitionSerializer : ISetupDefinitionSerializer
    {
        // Shared settings: enums as names, so a hand-authored definition binds and stays readable
        // across enum edits. See SetupJson.
        private static readonly JsonSerializerOptions WriteOptions = SetupJson.IndentedOptions;
        private static readonly JsonSerializerOptions ReadOptions = SetupJson.Options;

        public string Serialize(SetupDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var canonical = Canonicalize(definition);
            canonical.ContentHash = ComputeContentHash(definition);
            return JsonSerializer.Serialize(canonical, WriteOptions);
        }

        public SetupDefinition Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Definition JSON is empty.", nameof(json));

            var def = JsonSerializer.Deserialize<SetupDefinition>(json, ReadOptions);
            if (def == null)
                throw new JsonException("Definition JSON deserialized to null.");

            def.Steps ??= new List<SetupStepDefinition>();
            foreach (var s in def.Steps)
                s.DependsOn ??= new List<string>();

            return def;
        }

        public string ComputeContentHash(SetupDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var canonical = Canonicalize(definition);

            // Excluded from its own hash — otherwise the value would depend on itself.
            canonical.ContentHash = null;

            var json = JsonSerializer.Serialize(canonical, SetupJson.Options);

            return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        }

        /// <summary>
        /// Returns a copy with deterministic ordering so the same logical definition always
        /// serializes and hashes identically.
        /// <para>
        /// <c>DependsOn</c> is sorted because it is a set, not a sequence — reordering it changes
        /// nothing semantically and must not churn the hash. <c>Steps</c> order is preserved: it is
        /// load-bearing today (the builder enforces dependencies-declared-first, see P1-06).
        /// </para>
        /// </summary>
        private static SetupDefinition Canonicalize(SetupDefinition source) => new()
        {
            SchemaVersion = source.SchemaVersion,
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Environment = source.Environment,
            ContentHash = source.ContentHash,
            Steps = (source.Steps ?? new List<SetupStepDefinition>())
                .Select(s => new SetupStepDefinition
                {
                    StepId = s.StepId,
                    Type = s.Type,
                    DependsOn = (s.DependsOn ?? new List<string>())
                        .Where(d => !string.IsNullOrWhiteSpace(d))
                        .OrderBy(d => d, StringComparer.Ordinal)
                        .ToList(),
                    Enabled = s.Enabled,
                    Options = s.Options
                })
                .ToList()
        };
    }
}
