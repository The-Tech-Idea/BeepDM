using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public enum MappingIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum MappingQualityBand
    {
        Excellent,
        Good,
        NeedsReview,
        HighRisk
    }

    public sealed class MappingQualityIssue
    {
        public string Code { get; set; } = string.Empty;
        public MappingIssueSeverity Severity { get; set; } = MappingIssueSeverity.Info;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
    }

    public sealed class MappingDriftEntry
    {
        public string ChangeType { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string SourceValue { get; set; } = string.Empty;
        public string DestinationValue { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public sealed class MappingDriftReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string MappingName { get; set; } = string.Empty;
        public bool DriftDetected => Entries.Count > 0;
        public List<MappingDriftEntry> Entries { get; set; } = new List<MappingDriftEntry>();
    }

    public sealed class MappingQualityReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string MappingName { get; set; } = string.Empty;
        public int Score { get; set; }
        public MappingQualityBand Band { get; set; } = MappingQualityBand.HighRisk;
        public bool MeetsProductionThreshold { get; set; }
        public int ProductionThreshold { get; set; }
        public List<MappingQualityIssue> Issues { get; set; } = new List<MappingQualityIssue>();
        public MappingDriftReport DriftReport { get; set; } = new MappingDriftReport();
    }

    public static partial class MappingManager
    {
        public static MappingQualityReport ValidateMappingWithScore(
            IDMEEditor editor,
            EntityDataMap mapping,
            int productionThreshold = 70)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            var report = new MappingQualityReport
            {
                GeneratedOnUtc = DateTime.UtcNow,
                MappingName = mapping.MappingName ?? string.Empty,
                ProductionThreshold = productionThreshold
            };

            var validation = ValidateMappingCore(editor, mapping);
            foreach (var error in validation.Errors)
            {
                report.Issues.Add(new MappingQualityIssue
                {
                    Code = "validation-error",
                    Severity = MappingIssueSeverity.Error,
                    Category = "baseline-validation",
                    Message = error,
                    Recommendation = "Fix mapping assignments before production use."
                });
            }

            foreach (var warning in validation.Warnings)
            {
                report.Issues.Add(new MappingQualityIssue
                {
                    Code = "validation-warning",
                    Severity = MappingIssueSeverity.Warning,
                    Category = "baseline-validation",
                    Message = warning,
                    Recommendation = "Review and adjust mapping where needed."
                });
            }

            var ruleValidation = ValidateRuleSet(mapping);
            foreach (var warning in ruleValidation.Warnings)
            {
                report.Issues.Add(new MappingQualityIssue
                {
                    Code = "rule-warning",
                    Severity = MappingIssueSeverity.Warning,
                    Category = "rule-engine",
                    Message = warning,
                    Recommendation = "Review rule syntax/semantics and revalidate."
                });
            }

            if (!ruleValidation.IsValid)
            {
                foreach (var error in ruleValidation.Errors)
                {
                    report.Issues.Add(new MappingQualityIssue
                    {
                        Code = "rule-error",
                        Severity = MappingIssueSeverity.Error,
                        Category = "rule-engine",
                        Message = error,
                        Recommendation = "Correct invalid rules before execution."
                    });
                }
            }

            AnalyzeTypeCompatibility(mapping, report.Issues);
            AnalyzeNullabilityRisk(mapping, report.Issues);

            report.DriftReport = DetectMappingDrift(editor, mapping);
            foreach (var drift in report.DriftReport.Entries)
            {
                report.Issues.Add(new MappingQualityIssue
                {
                    Code = $"drift-{drift.ChangeType}".ToLowerInvariant(),
                    Severity = MappingIssueSeverity.Warning,
                    Category = "schema-drift",
                    FieldName = drift.FieldName,
                    Message = $"{drift.ChangeType}: {drift.FieldName}",
                    Recommendation = drift.Recommendation
                });
            }

            report.Score = ComputeQualityScore(report.Issues);
            report.Band = ResolveBand(report.Score);
            report.MeetsProductionThreshold = report.Score >= Math.Max(0, productionThreshold);
            return report;
        }

        public static MappingDriftReport DetectMappingDrift(IDMEEditor editor, EntityDataMap mapping)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            var report = new MappingDriftReport
            {
                GeneratedOnUtc = DateTime.UtcNow,
                MappingName = mapping.MappingName ?? string.Empty
            };

            foreach (var detail in mapping.MappedEntities ?? new List<EntityDataMap_DTL>())
            {
                if (detail == null)
                    continue;

                EntityStructure sourceStructure = null;
                EntityStructure destinationStructure = null;
                try
                {
                    sourceStructure = GetEntityStructure(editor, detail.EntityDataSource, detail.EntityName);
                    destinationStructure = GetEntityStructure(editor, mapping.EntityDataSource, mapping.EntityName);
                }
                catch
                {
                    continue;
                }

                var sourceFields = new HashSet<string>((sourceStructure?.Fields ?? new List<EntityField>()).Select(field => field.FieldName), StringComparer.OrdinalIgnoreCase);
                var destinationFields = new HashSet<string>((destinationStructure?.Fields ?? new List<EntityField>()).Select(field => field.FieldName), StringComparer.OrdinalIgnoreCase);

                foreach (var field in detail.FieldMapping ?? new List<Mapping_rep_fields>())
                {
                    if (field == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(field.FromFieldName) && !sourceFields.Contains(field.FromFieldName))
                    {
                        report.Entries.Add(new MappingDriftEntry
                        {
                            ChangeType = "source-field-removed-or-renamed",
                            FieldName = field.FromFieldName,
                            SourceValue = field.FromFieldType ?? string.Empty,
                            DestinationValue = field.ToFieldName ?? string.Empty,
                            Recommendation = "Re-map source field using latest schema or auto-match suggestions."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(field.ToFieldName) && !destinationFields.Contains(field.ToFieldName))
                    {
                        report.Entries.Add(new MappingDriftEntry
                        {
                            ChangeType = "destination-field-removed-or-renamed",
                            FieldName = field.ToFieldName,
                            SourceValue = field.FromFieldName ?? string.Empty,
                            DestinationValue = field.ToFieldType ?? string.Empty,
                            Recommendation = "Update target mapping to existing destination field."
                        });
                    }
                }

                foreach (var sourceField in sourceFields)
                {
                    var mapped = (detail.FieldMapping ?? new List<Mapping_rep_fields>())
                        .Any(field => string.Equals(field?.FromFieldName, sourceField, StringComparison.OrdinalIgnoreCase));
                    if (!mapped)
                    {
                        report.Entries.Add(new MappingDriftEntry
                        {
                            ChangeType = "source-field-added-unmapped",
                            FieldName = sourceField,
                            Recommendation = "Evaluate new source field for mapping inclusion."
                        });
                    }
                }
            }

            return report;
        }

        public static bool EnforceProductionQualityThreshold(
            IDMEEditor editor,
            EntityDataMap mapping,
            int minimumScore,
            out MappingQualityReport report)
        {
            report = ValidateMappingWithScore(editor, mapping, minimumScore);
            return report.MeetsProductionThreshold;
        }

        private static void AnalyzeTypeCompatibility(EntityDataMap mapping, List<MappingQualityIssue> issues)
        {
            foreach (var field in mapping.MappedEntities?.SelectMany(detail => detail.FieldMapping ?? new List<Mapping_rep_fields>()) ?? Enumerable.Empty<Mapping_rep_fields>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.FromFieldType) || string.IsNullOrWhiteSpace(field.ToFieldType))
                    continue;

                if (string.Equals(field.FromFieldType, field.ToFieldType, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (IsRiskyTypePair(field.FromFieldType, field.ToFieldType))
                {
                    issues.Add(new MappingQualityIssue
                    {
                        Code = "type-compatibility-risk",
                        Severity = MappingIssueSeverity.Warning,
                        Category = "type-compatibility",
                        FieldName = field.ToFieldName ?? string.Empty,
                        Message = $"Potential type risk mapping '{field.FromFieldType}' -> '{field.ToFieldType}'.",
                        Recommendation = "Add explicit conversion policy or transform rule."
                    });
                }
            }
        }

        private static void AnalyzeNullabilityRisk(EntityDataMap mapping, List<MappingQualityIssue> issues)
        {
            var destinationFieldMeta = (mapping.EntityFields ?? new List<EntityField>())
                .ToDictionary(field => field.FieldName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            foreach (var field in mapping.MappedEntities?.SelectMany(detail => detail.FieldMapping ?? new List<Mapping_rep_fields>()) ?? Enumerable.Empty<Mapping_rep_fields>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.ToFieldName))
                    continue;

                if (!destinationFieldMeta.TryGetValue(field.ToFieldName, out var destinationField))
                    continue;

                if (!destinationField.AllowDBNull && string.IsNullOrWhiteSpace(field.FromFieldName))
                {
                    issues.Add(new MappingQualityIssue
                    {
                        Code = "nullability-mismatch",
                        Severity = MappingIssueSeverity.Error,
                        Category = "nullability",
                        FieldName = field.ToFieldName,
                        Message = $"Destination field '{field.ToFieldName}' is non-nullable but has no source mapping.",
                        Recommendation = "Provide source mapping or explicit default policy."
                    });
                }
            }
        }

        private static bool IsRiskyTypePair(string fromType, string toType)
        {
            var from = (fromType ?? string.Empty).Trim().ToLowerInvariant();
            var to = (toType ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || from == to)
                return false;

            var numericTargets = new[] { "int", "int32", "int64", "long", "short", "byte", "decimal", "double", "float", "numeric" };
            var dateTargets = new[] { "date", "datetime", "datetime2", "datetimeoffset", "timestamp" };
            var guidTargets = new[] { "guid", "uniqueidentifier" };

            if (numericTargets.Contains(to) && !numericTargets.Contains(from))
                return true;
            if (dateTargets.Contains(to) && !dateTargets.Contains(from))
                return true;
            if (guidTargets.Contains(to) && !guidTargets.Contains(from))
                return true;
            return false;
        }

        private static int ComputeQualityScore(IEnumerable<MappingQualityIssue> issues)
        {
            var score = 100;
            foreach (var issue in issues ?? Enumerable.Empty<MappingQualityIssue>())
            {
                switch (issue.Severity)
                {
                    case MappingIssueSeverity.Error:
                        score -= 20;
                        break;
                    case MappingIssueSeverity.Warning:
                        score -= 8;
                        break;
                    case MappingIssueSeverity.Info:
                        score -= 2;
                        break;
                }
            }
            return Math.Max(0, Math.Min(100, score));
        }

        private static MappingQualityBand ResolveBand(int score)
        {
            if (score >= 90) return MappingQualityBand.Excellent;
            if (score >= 75) return MappingQualityBand.Good;
            if (score >= 60) return MappingQualityBand.NeedsReview;
            return MappingQualityBand.HighRisk;
        }
    }
}
