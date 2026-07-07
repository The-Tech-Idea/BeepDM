// Examples for the DefaultsManager migration toolkit + the QUERY data-source resolver.
//
// Each example is a static class with a `Run(...)` entry point that the host can invoke.
// The examples are pure (no IDataSource stub) so they compile against any IDataSource
// implementation the caller has already configured.

using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Migration;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;

namespace TheTechIdea.Beep.Editor.Defaults.Examples
{
    // ───────────────────────────────────────────────────────────────────────────
    //  Example 1 — Register a profile, scan it for legacy tokens, build a report
    // ───────────────────────────────────────────────────────────────────────────

    public static class Example01_ProfileScan
    {
        public static (CompatibilityReport Report, IReadOnlyList<AutoFixSuggestion> Fixes)
            Run(EntityDefaultsProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            // ── 1. Scan the profile for legacy rule tokens ────────────────────────
            IReadOnlyList<RuleDiagnostic> issues = DefaultsManager.ScanProfile(profile);
            Console.WriteLine($"Scan found {issues.Count} diagnostic(s):");
            foreach (var issue in issues)
                Console.WriteLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");

            // ── 2. Build the per-version / per-operator compatibility report ─────
            CompatibilityReport report = DefaultsManager.BuildCompatibilityReport(profile);
            Console.WriteLine(
                $"Compatibility report for '{report.EntityName}': " +
                $"{report.SyntaxVersionCounts.Count} syntax versions, " +
                $"{report.OperatorCounts.Count} operators, " +
                $"{report.LegacyTokenCounts.Count} legacy-token codes.");

            // ── 3. Suggest autofixes ─────────────────────────────────────────────
            IReadOnlyList<AutoFixSuggestion> fixes = DefaultsManager.SuggestFixes(profile);
            Console.WriteLine($"Autofix suggestions: {fixes.Count}");
            foreach (var fix in fixes)
            {
                Console.WriteLine(
                    $"  Field '{fix.FieldName}': '{fix.OriginalRule}' → '{fix.SuggestedRule}' " +
                    $"({fix.Reason})");
            }
            return (report, fixes);
        }
    }

    // ───────────────────────────────────────────────────────────────────────────
    //  Example 2 — QUERY.* mode-branching on the DataSource resolver
    //
    //  Validates rule syntax via the rule normalizer — does not actually call
    //  the data source, so the example compiles without any IDataSource stub.
    // ───────────────────────────────────────────────────────────────────────────

    public static class Example02_QueryModes
    {
        public static void Run()
        {
            // The DataSource resolver routes through the canonical QUERY.* modes:
            //   QUERY(scalar,  Entity, Field, Filter=Value)        → single field
            //   QUERY(first,    Entity, Filter=Value)              → first record
            //   QUERY(exists,   Entity, Filter=Value)              → bool
            //   QUERY(count,    Entity, Filter=Value)              → int
            //   QUERY(aggregate, Op, Entity, Field, Filter=Value) → typed aggregate
            string[] samples =
            {
                "QUERY(scalar, Customers, Email, IsActive=true)",
                "QUERY(first,   Orders, CustomerID=@CustomerID)",
                "QUERY(exists,  Users, Email=admin@example.com)",
                "QUERY(count,    Orders, Status=Pending)",
                "QUERY(aggregate, MAX, Orders, Total, OrderDate > @Since)"
            };
            foreach (var sample in samples)
            {
                ParsedRule parsed = RuleNormalizer.Normalize(sample);
                Console.WriteLine($"{sample} → op={parsed.Operator}, args={parsed.Args.Count}");
            }
        }
    }

    // ───────────────────────────────────────────────────────────────────────────
    //  Example 3 — Wave-rollout policy + re-registration workflow
    // ───────────────────────────────────────────────────────────────────────────

    public static class Example03_WaveRollout
    {
        public static IErrorsInfo Run()
        {
            // Build a per-resolver wave policy. Missing resolvers default to enabled.
            var settings = new WaveRolloutSettings
            {
                Environment = "Production",
                Resolvers = new Dictionary<string, ResolverWaveSettings>
                {
                    ["DateTime"]    = new() { Enabled = true,  Wave = "Wave1" },
                    ["Guid"]        = new() { Enabled = true,  Wave = "Wave1" },
                    ["UserContext"] = new() { Enabled = false, Wave = "Wave2" },
                    ["DataSource"]  = new() { Enabled = true,  Wave = "Wave3" }
                }
            };

            IErrorsInfo result = DefaultsManager.ApplyWaveSettings(settings);
            Console.WriteLine($"Wave rollout: {result.Message}");
            return result;
        }
    }
}
