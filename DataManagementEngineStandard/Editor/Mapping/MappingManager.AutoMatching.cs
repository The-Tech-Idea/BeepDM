using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public enum MatchConfidenceDecision
    {
        AutoAccept,
        ReviewRequired,
        Reject
    }

    public sealed class AutoMatchOptions
    {
        public double AutoAcceptThreshold { get; set; } = 0.90;
        public double ReviewThreshold { get; set; } = 0.65;
        public double AmbiguityDelta { get; set; } = 0.08;
        public int TopSuggestionsPerField { get; set; } = 3;
        public bool EnableTokenMatching { get; set; } = true;
        public bool EnableSynonymMatching { get; set; } = true;
        public bool EnableFuzzyDistanceMatching { get; set; } = true;
        public bool AssignReviewMatches { get; set; } = false;
        public Dictionary<string, List<string>> Synonyms { get; set; } = CreateDefaultSynonyms();

        private static Dictionary<string, List<string>> CreateDefaultSynonyms()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = new List<string> { "identifier", "key", "code" },
                ["first"] = new List<string> { "given", "forename" },
                ["last"] = new List<string> { "surname", "family" },
                ["name"] = new List<string> { "title", "label" },
                ["amount"] = new List<string> { "total", "sum", "value" },
                ["created"] = new List<string> { "creation", "inserted" },
                ["updated"] = new List<string> { "modified", "changed" }
            };
        }
    }

    public sealed class FieldMatchCandidate
    {
        public string SourceFieldName { get; set; } = string.Empty;
        public string SourceFieldType { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public MatchConfidenceDecision Decision { get; set; } = MatchConfidenceDecision.Reject;
        public List<string> Reasons { get; set; } = new List<string>();
    }

    public sealed class FieldMatchSuggestion
    {
        public string DestinationFieldName { get; set; } = string.Empty;
        public string DestinationFieldType { get; set; } = string.Empty;
        public List<FieldMatchCandidate> Candidates { get; set; } = new List<FieldMatchCandidate>();
        public bool IsAmbiguous { get; set; }
    }

    public sealed class MappingAutoMatchReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string SourceEntityName { get; set; } = string.Empty;
        public string SourceDataSource { get; set; } = string.Empty;
        public string DestinationEntityName { get; set; } = string.Empty;
        public string DestinationDataSource { get; set; } = string.Empty;
        public NameMatchMode Mode { get; set; } = NameMatchMode.CaseInsensitive;
        public int TotalDestinationFields { get; set; }
        public int AutoAcceptedCount { get; set; }
        public int ReviewRequiredCount { get; set; }
        public int RejectedCount { get; set; }
        public List<FieldMatchSuggestion> Suggestions { get; set; } = new List<FieldMatchSuggestion>();
    }

    public static partial class MappingManager
    {
        /// <summary>
        /// Builds mapping suggestions with confidence scoring and explainability.
        /// By default only auto-accepted matches are assigned to field mappings.
        /// </summary>
        public static Tuple<EntityDataMap, MappingAutoMatchReport> AutoMapByConventionWithScoring(
            IDMEEditor editor,
            string srcEntityName,
            string srcDataSourceName,
            string destEntityName,
            string destDataSourceName,
            NameMatchMode mode = NameMatchMode.CaseInsensitive,
            AutoMatchOptions options = null)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            options ??= new AutoMatchOptions();

            var mapping = LoadOrInitializeMapping(editor, destEntityName, destDataSourceName);
            mapping.MappingName = $"{destEntityName}_{destDataSourceName}_auto_scored";

            var report = new MappingAutoMatchReport
            {
                SourceEntityName = srcEntityName ?? string.Empty,
                SourceDataSource = srcDataSourceName ?? string.Empty,
                DestinationEntityName = destEntityName ?? string.Empty,
                DestinationDataSource = destDataSourceName ?? string.Empty,
                Mode = mode
            };

            try
            {
                var destStructure = GetEntityStructure(editor, destDataSourceName, destEntityName);
                var srcStructure = GetEntityStructure(editor, srcDataSourceName, srcEntityName);

                var destFields = destStructure?.Fields ?? new List<EntityField>();
                var srcFields = srcStructure?.Fields ?? new List<EntityField>();
                mapping.EntityFields = destFields;
                report.TotalDestinationFields = destFields.Count;

                var suggestions = BuildSuggestions(destFields, srcFields, mode, options);
                report.Suggestions = suggestions;
                report.AutoAcceptedCount = suggestions.Count(item =>
                    item.Candidates.FirstOrDefault()?.Decision == MatchConfidenceDecision.AutoAccept);
                report.ReviewRequiredCount = suggestions.Count(item =>
                    item.Candidates.FirstOrDefault()?.Decision == MatchConfidenceDecision.ReviewRequired);
                report.RejectedCount = suggestions.Count(item =>
                    item.Candidates.Count == 0 || item.Candidates.First().Decision == MatchConfidenceDecision.Reject);

                var fieldMappings = BuildFieldMappingsFromSuggestions(suggestions, options);
                var detail = new EntityDataMap_DTL
                {
                    EntityName = srcEntityName,
                    EntityDataSource = srcDataSourceName,
                    SelectedDestFields = srcFields,
                    FieldMapping = fieldMappings
                };

                mapping.MappedEntities.Clear();
                mapping.MappedEntities.Add(detail);
            }
            catch (Exception ex)
            {
                LogError(editor, $"AutoMapByConventionWithScoring failed for {destEntityName}", ex);
            }

            return new Tuple<EntityDataMap, MappingAutoMatchReport>(mapping, report);
        }

        private static List<FieldMatchSuggestion> BuildSuggestions(
            List<EntityField> destinationFields,
            List<EntityField> sourceFields,
            NameMatchMode mode,
            AutoMatchOptions options)
        {
            var suggestions = new List<FieldMatchSuggestion>();
            foreach (var destination in destinationFields)
            {
                var suggestion = new FieldMatchSuggestion
                {
                    DestinationFieldName = destination.FieldName,
                    DestinationFieldType = destination.Fieldtype
                };

                var candidates = sourceFields
                    .Select(source => BuildCandidate(source, destination, mode, options))
                    .Where(candidate => candidate.ConfidenceScore > 0)
                    .OrderByDescending(candidate => candidate.ConfidenceScore)
                    .ThenBy(candidate => candidate.SourceFieldName, StringComparer.OrdinalIgnoreCase)
                    .Take(Math.Max(1, options.TopSuggestionsPerField))
                    .ToList();

                MarkCandidateDecisions(candidates, options);
                suggestion.IsAmbiguous = IsAmbiguous(candidates, options);
                if (suggestion.IsAmbiguous && candidates.Count > 0 && candidates[0].Decision == MatchConfidenceDecision.AutoAccept)
                {
                    candidates[0].Decision = MatchConfidenceDecision.ReviewRequired;
                    candidates[0].Reasons.Add("Top match downgraded to review-required due to ambiguity.");
                }

                suggestion.Candidates = candidates;
                suggestions.Add(suggestion);
            }
            return suggestions;
        }

        private static List<Mapping_rep_fields> BuildFieldMappingsFromSuggestions(
            IEnumerable<FieldMatchSuggestion> suggestions,
            AutoMatchOptions options)
        {
            var mappings = new List<Mapping_rep_fields>();
            foreach (var suggestion in suggestions)
            {
                var best = suggestion.Candidates.FirstOrDefault();
                var shouldAssign =
                    best != null &&
                    (best.Decision == MatchConfidenceDecision.AutoAccept ||
                     (options.AssignReviewMatches && best.Decision == MatchConfidenceDecision.ReviewRequired));

                mappings.Add(new Mapping_rep_fields
                {
                    ToFieldName = suggestion.DestinationFieldName,
                    ToFieldType = suggestion.DestinationFieldType,
                    FromFieldName = shouldAssign ? best.SourceFieldName : null,
                    FromFieldType = shouldAssign ? best.SourceFieldType : null
                });
            }
            return mappings;
        }

        private static FieldMatchCandidate BuildCandidate(
            EntityField source,
            EntityField destination,
            NameMatchMode mode,
            AutoMatchOptions options)
        {
            var reasons = new List<string>();
            var score = ScoreNames(source.FieldName, destination.FieldName, mode, options, reasons);

            return new FieldMatchCandidate
            {
                SourceFieldName = source.FieldName ?? string.Empty,
                SourceFieldType = source.Fieldtype ?? string.Empty,
                ConfidenceScore = Math.Round(score, 4),
                Reasons = reasons
            };
        }

        private static void MarkCandidateDecisions(List<FieldMatchCandidate> candidates, AutoMatchOptions options)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.ConfidenceScore >= options.AutoAcceptThreshold)
                    candidate.Decision = MatchConfidenceDecision.AutoAccept;
                else if (candidate.ConfidenceScore >= options.ReviewThreshold)
                    candidate.Decision = MatchConfidenceDecision.ReviewRequired;
                else
                    candidate.Decision = MatchConfidenceDecision.Reject;
            }
        }

        private static bool IsAmbiguous(List<FieldMatchCandidate> candidates, AutoMatchOptions options)
        {
            if (candidates.Count < 2)
                return false;

            var top = candidates[0];
            var second = candidates[1];
            return top.ConfidenceScore >= options.ReviewThreshold &&
                   second.ConfidenceScore >= options.ReviewThreshold &&
                   Math.Abs(top.ConfidenceScore - second.ConfidenceScore) <= options.AmbiguityDelta;
        }

        private static double ScoreNames(
            string sourceName,
            string destinationName,
            NameMatchMode mode,
            AutoMatchOptions options,
            List<string> reasons)
        {
            if (string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(destinationName))
                return 0;

            var score = 0.0;

            if (string.Equals(sourceName, destinationName, StringComparison.Ordinal))
            {
                score = 1.0;
                reasons.Add("Exact ordinal match.");
                return score;
            }

            if (string.Equals(sourceName, destinationName, StringComparison.OrdinalIgnoreCase))
            {
                score = Math.Max(score, 0.95);
                reasons.Add("Case-insensitive exact match.");
            }

            if (mode == NameMatchMode.FuzzyPrefix &&
                (sourceName.StartsWith(destinationName, StringComparison.OrdinalIgnoreCase) ||
                 destinationName.StartsWith(sourceName, StringComparison.OrdinalIgnoreCase)))
            {
                score = Math.Max(score, 0.78);
                reasons.Add("Prefix-based match.");
            }

            var sourceTokens = Tokenize(sourceName);
            var destinationTokens = Tokenize(destinationName);

            if (options.EnableTokenMatching)
            {
                var tokenSimilarity = JaccardSimilarity(sourceTokens, destinationTokens);
                if (tokenSimilarity > 0)
                {
                    var tokenScore = 0.50 + (0.35 * tokenSimilarity);
                    score = Math.Max(score, tokenScore);
                    reasons.Add($"Token similarity score {tokenSimilarity:0.00}.");
                }
            }

            if (options.EnableSynonymMatching && HasSynonymIntersection(sourceTokens, destinationTokens, options.Synonyms))
            {
                score = Math.Max(score, 0.74);
                reasons.Add("Synonym-token match.");
            }

            if (options.EnableFuzzyDistanceMatching)
            {
                var similarity = LevenshteinSimilarity(sourceName, destinationName);
                if (similarity >= 0.70)
                {
                    var fuzzyScore = 0.45 + (0.35 * similarity);
                    score = Math.Max(score, fuzzyScore);
                    reasons.Add($"Levenshtein similarity {similarity:0.00}.");
                }
            }

            return Math.Min(1.0, Math.Max(0.0, score));
        }

        private static HashSet<string> Tokenize(string value)
        {
            var normalized = NormalizeFieldName(value);
            return new HashSet<string>(
                normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeFieldName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder();
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (char.IsLetterOrDigit(current))
                {
                    if (index > 0 &&
                        char.IsUpper(current) &&
                        char.IsLetter(value[index - 1]) &&
                        char.IsLower(value[index - 1]))
                    {
                        builder.Append(' ');
                    }
                    builder.Append(char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append(' ');
                }
            }
            return builder.ToString().Trim();
        }

        private static double JaccardSimilarity(HashSet<string> left, HashSet<string> right)
        {
            if (left.Count == 0 || right.Count == 0)
                return 0;

            var intersectionCount = left.Intersect(right, StringComparer.OrdinalIgnoreCase).Count();
            var unionCount = left.Union(right, StringComparer.OrdinalIgnoreCase).Count();
            return unionCount == 0 ? 0 : (double)intersectionCount / unionCount;
        }

        private static bool HasSynonymIntersection(
            HashSet<string> sourceTokens,
            HashSet<string> destinationTokens,
            Dictionary<string, List<string>> synonyms)
        {
            if (sourceTokens.Count == 0 || destinationTokens.Count == 0 || synonyms == null || synonyms.Count == 0)
                return false;

            foreach (var sourceToken in sourceTokens)
            {
                foreach (var destinationToken in destinationTokens)
                {
                    if (sourceToken.Equals(destinationToken, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (AreSynonyms(sourceToken, destinationToken, synonyms))
                        return true;
                }
            }

            return false;
        }

        private static bool AreSynonyms(string left, string right, Dictionary<string, List<string>> synonyms)
        {
            foreach (var pair in synonyms)
            {
                var cluster = new HashSet<string>(pair.Value ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
                {
                    pair.Key
                };
                if (cluster.Contains(left) && cluster.Contains(right))
                    return true;
            }
            return false;
        }

        private static double LevenshteinSimilarity(string left, string right)
        {
            var source = (left ?? string.Empty).ToLowerInvariant();
            var target = (right ?? string.Empty).ToLowerInvariant();
            if (source.Length == 0 && target.Length == 0)
                return 1;
            if (source.Length == 0 || target.Length == 0)
                return 0;

            var distance = LevenshteinDistance(source, target);
            var maxLength = Math.Max(source.Length, target.Length);
            return maxLength == 0 ? 1 : 1.0 - ((double)distance / maxLength);
        }

        private static int LevenshteinDistance(string source, string target)
        {
            var matrix = new int[source.Length + 1, target.Length + 1];

            for (var i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;
            for (var j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (var i = 1; i <= source.Length; i++)
            {
                for (var j = 1; j <= target.Length; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }
    }
}
