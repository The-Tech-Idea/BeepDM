using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;

namespace TheTechIdea.Beep.Editor.Defaults.Migration
{
    /// <summary>
    /// Grouped compatibility report for an <see cref="EntityDefaultsProfile"/>:
    ///  - rule-syntax version counts (per <see cref="RuleSyntaxVersion"/>)
    ///  - dialect counts (per operator / legacy token)
    ///  - ordered list of <see cref="RuleDiagnostic"/> issues
    /// </summary>
    public sealed class CompatibilityReport
    {
        public string EntityName { get; }
        public IReadOnlyDictionary<RuleSyntaxVersion, int> SyntaxVersionCounts { get; }
        public IReadOnlyDictionary<string, int> OperatorCounts { get; }
        public IReadOnlyDictionary<string, int> LegacyTokenCounts { get; }
        public IReadOnlyList<RuleDiagnostic> Issues { get; }

        public CompatibilityReport(
            string entityName,
            IReadOnlyDictionary<RuleSyntaxVersion, int> syntaxVersionCounts,
            IReadOnlyDictionary<string, int> operatorCounts,
            IReadOnlyDictionary<string, int> legacyTokenCounts,
            IReadOnlyList<RuleDiagnostic> issues)
        {
            EntityName = entityName;
            SyntaxVersionCounts = syntaxVersionCounts;
            OperatorCounts = operatorCounts;
            LegacyTokenCounts = legacyTokenCounts;
            Issues = issues;
        }

        public static CompatibilityReport FromProfile(EntityDefaultsProfile profile)
        {
            if (profile?.Rules == null)
            {
                return new CompatibilityReport(
                    profile?.EntityName ?? string.Empty,
                    new Dictionary<RuleSyntaxVersion, int>(),
                    new Dictionary<string, int>(),
                    new Dictionary<string, int>(),
                    new List<RuleDiagnostic>());
            }

            var versions = new Dictionary<RuleSyntaxVersion, int>();
            var operators = new Dictionary<string, int>();
            var legacy = new Dictionary<string, int>();
            var issues = new List<RuleDiagnostic>();

            foreach (var field in profile.Rules)
            {
                if (field == null) continue;

                ParsedRule parsed = RuleNormalizer.Normalize(field.RuleString);
                if (parsed.SyntaxVersion != RuleSyntaxVersion.Unknown)
                {
                    versions[parsed.SyntaxVersion] =
                        versions.TryGetValue(parsed.SyntaxVersion, out var n) ? n + 1 : 1;
                }
                if (!string.IsNullOrWhiteSpace(parsed.Operator))
                {
                    operators[parsed.Operator] =
                        operators.TryGetValue(parsed.Operator, out var n) ? n + 1 : 1;
                }

                // Group deprecated token diagnostics by their code (NRM001, etc.)
                if (parsed.Diagnostics != null)
                {
                    foreach (var diag in parsed.Diagnostics)
                    {
                        if (diag.Severity == RuleDiagnosticSeverity.Warning ||
                            diag.Severity == RuleDiagnosticSeverity.Error)
                        {
                            issues.Add(new RuleDiagnostic(
                                diag.Severity,
                                diag.Code,
                                $"Field '{field.FieldName}': {diag.Message}"));
                            legacy[diag.Code] = legacy.TryGetValue(diag.Code, out var n) ? n + 1 : 1;
                        }
                    }
                }
            }

            return new CompatibilityReport(
                profile.EntityName ?? string.Empty,
                versions,
                operators,
                legacy,
                issues);
        }
    }
}
