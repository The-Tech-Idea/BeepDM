using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for GUID generation with various format options
    /// </summary>
    public class GuidResolver : BaseDefaultValueResolver
    {
        public GuidResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "Guid";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "NEWGUID", "GUID", "UUID", "GENERATEUNIQUEID"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    "NEWGUID" or "GUID" or "UUID" or "GENERATEUNIQUEID" => Guid.NewGuid().ToString(),
                    _ when upperRule.StartsWith("GUID(") => ParseGuidFormat(rule),
                    _ when upperRule.StartsWith("UUID(") => ParseGuidFormat(rule),
                    _ => Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving GUID rule '{rule}'", ex);
                return Guid.NewGuid().ToString();
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return SupportedRuleTypes.Any(type => upperRule.Contains(type)) ||
                   upperRule.StartsWith("GUID(") ||
                   upperRule.StartsWith("UUID(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "NEWGUID - Generate new GUID",
                "GUID - Same as NEWGUID",
                "UUID - Same as NEWGUID", 
                "GENERATEUNIQUEID - Same as NEWGUID",
                "GUID(N) - GUID without hyphens",
                "GUID(D) - GUID with hyphens (default)",
                "GUID(B) - GUID in braces {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}",
                "GUID(P) - GUID in parentheses (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)",
                "UUID(N) - UUID without hyphens"
            };
        }

        private string ParseGuidFormat(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var format = RemoveQuotes(content.Trim());
                
                if (string.IsNullOrWhiteSpace(format))
                    return Guid.NewGuid().ToString();

                // Validate format specifier
                if (format.Length == 1 && "NDBPX".Contains(format.ToUpperInvariant()))
                {
                    return Guid.NewGuid().ToString(format.ToUpperInvariant());
                }

                LogWarning($"Invalid GUID format specifier '{format}', using default");
                return Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                LogError($"Error parsing GUID format from rule '{rule}'", ex);
                return Guid.NewGuid().ToString();
            }
        }
    }
}