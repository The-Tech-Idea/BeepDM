using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp.Definition;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// Structural validation of a <see cref="SetupDefinition"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately needs no <c>IDMEEditor</c> and no live datasource: this is what a CI job runs on
    /// a pull request to catch a broken definition before it ever reaches a database.
    /// Returns <see cref="IErrorsInfo"/> per repo convention — a bad document is data, not an
    /// exceptional condition.
    /// </remarks>
    public sealed class SetupDefinitionValidator : ISetupDefinitionValidator
    {
        private readonly ISetupStepFactory _factory;

        public SetupDefinitionValidator(ISetupStepFactory factory)
            => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public IErrorsInfo Validate(SetupDefinition definition)
        {
            if (definition == null) return Fail("Definition is null.");

            if (definition.SchemaVersion <= 0)
                return Fail($"SchemaVersion must be positive; found {definition.SchemaVersion}.");

            if (definition.SchemaVersion > SetupDefinition.CurrentSchemaVersion)
                return Fail(
                    $"Definition schemaVersion {definition.SchemaVersion} is newer than this build " +
                    $"supports ({SetupDefinition.CurrentSchemaVersion}). Upgrade BeepDM.");

            if (string.IsNullOrWhiteSpace(definition.Id))
                return Fail("Definition Id must not be empty — it keys persisted setup state.");

            var steps = (definition.Steps ?? new List<SetupStepDefinition>())
                .Where(s => s != null && s.Enabled)
                .ToList();

            if (steps.Count == 0)
                return Ok("Definition has no enabled steps.");

            foreach (var s in steps)
            {
                if (string.IsNullOrWhiteSpace(s.StepId))
                    return Fail($"A step of type '{s.Type}' has an empty StepId.");

                if (!_factory.CanCreate(s.Type))
                    return Fail(
                        $"Step '{s.StepId}' names unknown type '{s.Type}'. " +
                        $"Known types: {string.Join(", ", _factory.RegisteredTypes.OrderBy(x => x, StringComparer.Ordinal))}.");
            }

            var duplicate = steps.GroupBy(s => s.StepId, StringComparer.Ordinal)
                                 .FirstOrDefault(g => g.Count() > 1)?.Key;
            if (duplicate != null)
                return Fail($"Duplicate step id '{duplicate}'. Step ids must be unique within a definition.");

            var indexById = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < steps.Count; i++) indexById[steps[i].StepId] = i;

            foreach (var s in steps)
            {
                foreach (var dep in s.DependsOn ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(dep)) continue;

                    if (!indexById.ContainsKey(dep))
                        return Fail(
                            $"Step '{s.StepId}' depends on '{dep}', which is not an enabled step in " +
                            "this definition.");

                    if (string.Equals(dep, s.StepId, StringComparison.Ordinal))
                        return Fail($"Step '{s.StepId}' cannot depend on itself.");

                    // Mirrors SetupWizardBuilder: dependencies must be declared first (P1-06).
                    if (indexById[dep] > indexById[s.StepId])
                        return Fail(
                            $"Step '{s.StepId}' depends on '{dep}', but '{dep}' is declared after it. " +
                            "Reorder steps so dependencies appear first.");
                }
            }

            var cycle = FindCycle(steps);
            if (cycle != null)
                return Fail($"Cycle detected in step dependencies: {cycle}.");

            // Options must bind to their declared shape. This is the check that catches a typo'd
            // option in CI rather than at migration time.
            foreach (var s in steps)
            {
                try { _factory.Create(s); }
                catch (Exception ex) { return Fail($"Step '{s.StepId}' is not constructible: {ex.Message}"); }
            }

            return Ok($"Definition '{definition.Id}' is valid ({steps.Count} step(s)).");
        }

        /// <summary>Kahn's algorithm over dependency → dependent edges. Returns null when acyclic.</summary>
        private static string FindCycle(List<SetupStepDefinition> steps)
        {
            var inDegree = steps.ToDictionary(s => s.StepId, _ => 0, StringComparer.Ordinal);
            var dependents = steps.ToDictionary(s => s.StepId, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var s in steps)
            {
                foreach (var dep in (s.DependsOn ?? new List<string>()).Where(d => !string.IsNullOrWhiteSpace(d)))
                {
                    if (!dependents.ContainsKey(dep)) continue;
                    dependents[dep].Add(s.StepId);
                    inDegree[s.StepId]++;
                }
            }

            var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var sorted = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                sorted++;
                foreach (var d in dependents[current])
                    if (--inDegree[d] == 0) queue.Enqueue(d);
            }

            if (sorted == steps.Count) return null;

            return string.Join(" → ", inDegree.Where(kv => kv.Value > 0).Select(kv => $"'{kv.Key}'"));
        }
    }
}
