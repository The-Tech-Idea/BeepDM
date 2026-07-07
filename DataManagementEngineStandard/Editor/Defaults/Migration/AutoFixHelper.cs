using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;

namespace TheTechIdea.Beep.Editor.Defaults.Migration
{
    /// <summary>
    /// For each <see cref="RuleDiagnostic"/> produced by <see cref="RuleScanner"/>,
    /// suggest a modernised replacement rule. Read-only — does not mutate the profile.
    /// </summary>
    public sealed class AutoFixSuggestion
    {
        public string FieldName { get; }
        public string OriginalRule { get; }
        public string SuggestedRule { get; }
        public string Reason { get; }

        public AutoFixSuggestion(
            string fieldName, string originalRule, string suggestedRule, string reason)
        {
            FieldName = fieldName;
            OriginalRule = originalRule;
            SuggestedRule = suggestedRule;
            Reason = reason;
        }
    }

    public static class AutoFixHelper
    {
        private static readonly Dictionary<string, string> _tokenReplacements =
            new(System.StringComparer.OrdinalIgnoreCase)
            {
                // ── Date / time ─────────────────────────────────────────────────
                { "TODAY",         ":NOW.date" },
                { "YESTERDAY",     ":NOW.date.adddays(-1)" },
                { "TOMORROW",      ":NOW.date.adddays(1)" },
                { "CURRENTDATE",   ":NOW.date" },
                { "CURRENTTIME",   ":NOW.time" },
                { "CURRENTDATETIME", ":NOW" },

                // ── User identity ───────────────────────────────────────────────
                { "USERNAME",      ":USER.name" },
                { "CURRENTUSER",   ":USER.name" },
                { "USERID",        ":USER.id" },
                { "USEREMAIL",     ":USER.email" },
                { "USERLOGIN",     ":USER.login" },
                { "USERDOMAIN",    ":USER.domain" },

                // ── Ids / generators ─────────────────────────────────────────────
                { "NEWGUID",       ":GUID.new" },
                { "GUID",          ":GUID.new" },
                { "MACHINENAME",   ":MACHINE.name" },
                { "SEQUENCE",      ":SEQUENCE.next" },
                { "INCREMENT",     ":SEQUENCE.next" },
                { "RANDOM",        ":RANDOM.int(0,100)" },

                // ── Environment / config ────────────────────────────────────────
                { "ENV",           ":ENV.get" },
                { "CONFIG",        ":CONFIG.get" },
                { "PROPERTY",      ":RECORD.get" },
                { "RECORD",        ":RECORD.get" },
                { "FIELD",         ":RECORD.get" }
            };

        /// <summary>
        /// Walk <paramref name="profile"/> and emit a fix suggestion per field that uses
        /// a deprecated token. Fields with no rule, or with a rule that's already canonical,
        /// produce no suggestion.
        /// </summary>
        public static IReadOnlyList<AutoFixSuggestion> SuggestFixes(EntityDefaultsProfile profile)
        {
            var result = new List<AutoFixSuggestion>();
            if (profile?.Rules == null) return result;

            foreach (var field in profile.Rules)
            {
                if (field == null) continue;

                ParsedRule parsed = RuleNormalizer.Normalize(field.RuleString);

                if (parsed.Operator is { Length: > 0 } op &&
                    _tokenReplacements.TryGetValue(op, out var modern))
                {
                    result.Add(new AutoFixSuggestion(
                        fieldName: field.FieldName,
                        originalRule: field.RuleString,
                        suggestedRule: modern,
                        reason: $"Replace legacy token '{op}' with canonical '{modern}'."));
                }
            }
            return result;
        }
    }
}
