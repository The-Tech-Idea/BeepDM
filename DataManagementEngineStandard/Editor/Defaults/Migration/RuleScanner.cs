using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;

namespace TheTechIdea.Beep.Editor.Defaults.Migration
{
    /// <summary>
    /// Scans an <see cref="EntityDefaultsProfile"/> for legacy / deprecated rule forms
    /// and emits a list of <see cref="RuleDiagnostic"/> codes matching the normalizer's
    /// <c>legacyExpressionTokens</c> set.
    /// Pure function — does not mutate the profile.
    /// </summary>
    public static class RuleScanner
    {
        /// <summary>
        /// Scan every rule string in <paramref name="profile"/>'s fields and return
        /// the diagnostics emitted by the normalizer, scoped to legacy-token usage.
        /// </summary>
        public static IReadOnlyList<RuleDiagnostic> ScanProfile(EntityDefaultsProfile profile)
        {
            if (profile?.Rules == null) return Array.Empty<RuleDiagnostic>();

            var result = new List<RuleDiagnostic>();
            foreach (var field in profile.Rules)
            {
                if (field == null) continue;

                ParsedRule parsed = RuleNormalizer.Normalize(field.RuleString);
                if (parsed.Diagnostics == null) continue;

                foreach (var diag in parsed.Diagnostics)
                {
                    result.Add(new RuleDiagnostic(
                        diag.Severity,
                        diag.Code,
                        $"Field '{field.FieldName}': {diag.Message}"));
                }
            }
            return result;
        }
    }
}
