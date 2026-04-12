using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for sync validation operations
    /// Based on validation patterns from DataSyncManager.ValidateSchema
    /// </summary>
    public class SyncValidationHelper : ISyncValidationHelper
    {
        private readonly IDMEEditor _editor;

        /// <summary>
        /// Initializes a new instance of the SyncValidationHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        public SyncValidationHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Validate complete sync schema for all required fields and configurations
        /// Based on DataSyncManager.ValidateSchema method
        /// </summary>
        /// <param name="schema">The sync schema to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        public IErrorsInfo ValidateSchema(DataSyncSchema schema)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (schema == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Schema cannot be null";
                return result;
            }

            var errors = new List<ErrorsInfo>();

            // Validate required string fields
            ValidateRequiredField(schema.SourceDataSourceName, "Source Data Source Name", errors);
            ValidateRequiredField(schema.DestinationDataSourceName, "Destination Data Source Name", errors);
            ValidateRequiredField(schema.SourceEntityName, "Source Entity Name", errors);
            ValidateRequiredField(schema.DestinationEntityName, "Destination Entity Name", errors);
            ValidateRequiredField(schema.SourceSyncDataField, "Source Sync Data Field", errors);
            ValidateRequiredField(schema.DestinationSyncDataField, "Destination Sync Data Field", errors);
            ValidateRequiredField(schema.SyncType, "Sync Type", errors);
            ValidateRequiredField(schema.SyncDirection, "Sync Direction", errors);

            // Validate data sources exist and are accessible
            var sourceValidation = ValidateDataSource(schema.SourceDataSourceName);
            if (sourceValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Source data source validation failed: {sourceValidation.Message}" });

            var destValidation = ValidateDataSource(schema.DestinationDataSourceName);
            if (destValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Destination data source validation failed: {destValidation.Message}" });

            // Validate entities exist in their respective data sources
            var sourceEntityValidation = ValidateEntity(schema.SourceDataSourceName, schema.SourceEntityName);
            if (sourceEntityValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Source entity validation failed: {sourceEntityValidation.Message}" });

            var destEntityValidation = ValidateEntity(schema.DestinationDataSourceName, schema.DestinationEntityName);
            if (destEntityValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Destination entity validation failed: {destEntityValidation.Message}" });

            // Set result based on errors found
            if (errors.Count > 0)
            {
                result.Flag = Errors.Failed;
                result.Errors = errors.Cast<IErrorsInfo>().ToList();
                result.Message = $"Schema validation failed with {errors.Count} error(s)";
            }
            else
            {
                result.Message = "Schema validation successful";
            }

            return result;
        }

        /// <summary>
        /// Validate that a data source exists and is accessible
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateDataSource(string dataSourceName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (string.IsNullOrWhiteSpace(dataSourceName))
            {
                result.Flag = Errors.Failed;
                result.Message = "Data source name cannot be null or empty";
                return result;
            }

            try
            {
                var ds = _editor.GetDataSource(dataSourceName);
                if (ds == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Data source '{dataSourceName}' does not exist";
                    return result;
                }

                var state = DataSourceLifecycleHelper.OpenWithRetryAsync(ds, 3).GetAwaiter().GetResult();
                if (state != ConnectionState.Open)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Data source '{dataSourceName}' could not be opened (state: {state})";
                }
                else
                {
                    result.Message = $"Data source '{dataSourceName}' validation successful";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating data source '{dataSourceName}': {ex.Message}";
                _editor.AddLogMessage("BeepSync", result.Message, DateTime.Now, -1, "", Errors.Failed);
            }

            return result;
        }

        /// <summary>
        /// Validate that an entity exists in the specified data source
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateEntity(string dataSourceName, string entityName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (string.IsNullOrWhiteSpace(dataSourceName))
            {
                result.Flag = Errors.Failed;
                result.Message = "Data source name cannot be null or empty";
                return result;
            }

            if (string.IsNullOrWhiteSpace(entityName))
            {
                result.Flag = Errors.Failed;
                result.Message = "Entity name cannot be null or empty";
                return result;
            }

            try
            {
                var dataSource = _editor.GetDataSource(dataSourceName);
                if (dataSource == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Data source '{dataSourceName}' not found";
                    return result;
                }

                // Try to get entity structure to validate entity exists
                var entityStructure = dataSource.GetEntityStructure(entityName, false);
                if (entityStructure == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Entity '{entityName}' not found in data source '{dataSourceName}'";
                }
                else
                {
                    result.Message = $"Entity '{entityName}' validation successful in data source '{dataSourceName}'";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating entity '{entityName}' in data source '{dataSourceName}': {ex.Message}";
                _editor.AddLogMessage("BeepSync", result.Message, DateTime.Now, -1, "", Errors.Failed);
            }

            return result;
        }

        /// <summary>
        /// Validate sync operation before execution - comprehensive pre-sync validation
        /// </summary>
        /// <param name="schema">The sync schema to validate for operation</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateSyncOperation(DataSyncSchema schema)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            // First validate the schema itself
            var schemaValidation = ValidateSchema(schema);
            if (schemaValidation.Flag == Errors.Failed)
                return schemaValidation;

            var errors = new List<ErrorsInfo>();

            try
            {
                // Validate sync field exists in source entity
                var sourceDs = _editor.GetDataSource(schema.SourceDataSourceName);
                if (sourceDs != null)
                {
                    var sourceStructure = sourceDs.GetEntityStructure(schema.SourceEntityName, false);
                    if (sourceStructure?.Fields != null)
                    {
                        var sourceSyncField = sourceStructure.Fields.FirstOrDefault(f => 
                            string.Equals(f.FieldName, schema.SourceSyncDataField, StringComparison.OrdinalIgnoreCase));
                        
                        if (sourceSyncField == null)
                            errors.Add(new ErrorsInfo { Message = $"Source sync field '{schema.SourceSyncDataField}' not found in entity '{schema.SourceEntityName}'" });
                    }
                }

                // Validate sync field exists in destination entity
                var destDs = _editor.GetDataSource(schema.DestinationDataSourceName);
                if (destDs != null)
                {
                    var destStructure = destDs.GetEntityStructure(schema.DestinationEntityName, false);
                    if (destStructure?.Fields != null)
                    {
                        var destSyncField = destStructure.Fields.FirstOrDefault(f => 
                            string.Equals(f.FieldName, schema.DestinationSyncDataField, StringComparison.OrdinalIgnoreCase));
                        
                        if (destSyncField == null)
                            errors.Add(new ErrorsInfo { Message = $"Destination sync field '{schema.DestinationSyncDataField}' not found in entity '{schema.DestinationEntityName}'" });
                    }
                }

                // Validate field mappings if they exist
                if (schema.MappedFields != null && schema.MappedFields.Count > 0)
                {
                    var mappingHelper = new FieldMappingHelper(_editor);
                    var mappingValidation = mappingHelper.ValidateFieldMappings(schema.MappedFields);
                    if (mappingValidation.Flag == Errors.Failed)
                        errors.Add(new ErrorsInfo { Message = $"Field mapping validation failed: {mappingValidation.Message}" });
                }

                // Check if sync type is supported
                var supportedSyncTypes = new[] { "Full", "Incremental", "Upsert", "Delta", "Manual" };
                if (!supportedSyncTypes.Contains(schema.SyncType, StringComparer.OrdinalIgnoreCase))
                    errors.Add(new ErrorsInfo { Message = $"Unsupported sync type: {schema.SyncType}" });

                // Check if sync direction is supported
                var supportedDirections = new[] { "SourceToDestination", "DestinationToSource", "Bidirectional" };
                if (!supportedDirections.Contains(schema.SyncDirection, StringComparer.OrdinalIgnoreCase))
                    errors.Add(new ErrorsInfo { Message = $"Unsupported sync direction: {schema.SyncDirection}" });

            }
            catch (Exception ex)
            {
                errors.Add(new ErrorsInfo { Message = $"Error during sync operation validation: {ex.Message}" });
                _editor.AddLogMessage("BeepSync", $"Error during sync operation validation: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }

            // Set result based on errors found
            if (errors.Count > 0)
            {
                result.Flag = Errors.Failed;
                result.Errors = errors.Cast<IErrorsInfo>().ToList();
                result.Message = $"Sync operation validation failed with {errors.Count} error(s)";
            }
            else
            {
                result.Message = "Sync operation validation successful";
            }

            return result;
        }

        // ── Phase 3: Watermark / CDC validation ────────────────────────────────────────

        /// <inheritdoc cref="ISyncValidationHelper.ValidateWatermarkPolicy"/>
        public IErrorsInfo ValidateWatermarkPolicy(DataSyncSchema schema)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (schema?.WatermarkPolicy == null)
            {
                result.Message = "No watermark policy — full-load mode.";
                return result;
            }

            var policy = schema.WatermarkPolicy;
            var errors = new List<string>();

            // Mode must be a known value
            var knownModes = new[] { "Timestamp", "Sequence", "CompositeKey" };
            if (!string.IsNullOrWhiteSpace(policy.WatermarkMode) &&
                !knownModes.Contains(policy.WatermarkMode, StringComparer.OrdinalIgnoreCase))
                errors.Add($"Unknown WatermarkMode '{policy.WatermarkMode}'. Expected: {string.Join(", ", knownModes)}.");

            // Watermark field must be specified
            if (string.IsNullOrWhiteSpace(policy.WatermarkField))
            {
                errors.Add("WatermarkPolicy.WatermarkField is required.");
            }
            else
            {
                // Watermark field must exist on the source entity
                try
                {
                    var sourceDs = _editor.GetDataSource(schema.SourceDataSourceName);
                    if (sourceDs != null)
                    {
                        var structure = sourceDs.GetEntityStructure(schema.SourceEntityName, false);
                        if (structure?.Fields != null)
                        {
                            bool fieldExists = structure.Fields.Any(f =>
                                string.Equals(f.FieldName, policy.WatermarkField,
                                    StringComparison.OrdinalIgnoreCase));
                            if (!fieldExists)
                                errors.Add($"Watermark field '{policy.WatermarkField}' not found in " +
                                           $"source entity '{schema.SourceEntityName}'.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error checking watermark field: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                result.Flag    = Errors.Failed;
                result.Message = string.Join("; ", errors);
                result.Errors  = errors.Select(e => (IErrorsInfo)new ErrorsInfo { Message = e }).ToList();
            }
            else
            {
                result.Message = $"Watermark policy valid (field='{policy.WatermarkField}', mode='{policy.WatermarkMode}').";
            }

            return result;
        }

        /// <summary>
        /// Helper method to validate required fields
        /// </summary>
        private void ValidateRequiredField(string fieldValue, string FieldName, List<ErrorsInfo> errors)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                errors.Add(new ErrorsInfo { Message = $"{FieldName} is empty or null" });
        }

        // ── Phase 6: DQ gate helpers ──────────────────────────────────────────────

        /// <inheritdoc cref="ISyncValidationHelper.EvaluateDqGateRules"/>
        public List<DqGateResult> EvaluateDqGateRules(
            DataSyncSchema schema,
            Dictionary<string, object> record,
            TheTechIdea.Beep.Rules.IRuleEngine ruleEngine = null)
        {
            var failures = new List<DqGateResult>();

            var dqPolicy = schema?.DqPolicy;
            if (dqPolicy == null || !dqPolicy.Enabled || dqPolicy.RuleKeys?.Count == 0)
                return failures;

            var rulePolicy = new TheTechIdea.Beep.Rules.RuleExecutionPolicy
            {
                MaxDepth       = schema.RulePolicy?.MaxDepth > 0 ? schema.RulePolicy.MaxDepth : 10,
                MaxExecutionMs = schema.RulePolicy?.MaxExecutionMs > 0 ? schema.RulePolicy.MaxExecutionMs : 5000
            };

            foreach (var ruleKey in dqPolicy.RuleKeys)
            {
                if (string.IsNullOrWhiteSpace(ruleKey)) continue;

                if (ruleEngine == null || !ruleEngine.HasRule(ruleKey))
                {
                    // Rule not registered — skip gracefully (log only)
                    _editor.AddLogMessage("BeepSync",
                        $"DQ rule '{ruleKey}' not registered in Rule Engine — skipped.",
                        DateTime.Now, -1, "", Errors.Ok);
                    continue;
                }

                try
                {
                    var context = new Dictionary<string, object>
                    {
                        ["record"]     = record,
                        ["entityName"] = schema.DestinationEntityName,
                        ["schemaId"]   = schema.Id
                    };

                    var (outputs, dqResult) = ruleEngine.SolveRule(ruleKey, context, rulePolicy);

                    bool passed = dqResult is bool b ? b : dqResult?.ToString() != "false";
                    if (!passed)
                    {
                        failures.Add(new DqGateResult
                        {
                            RuleKey    = ruleKey,
                            Passed     = false,
                            ReasonCode = outputs?.TryGetValue("reasonCode", out var rc) == true
                                         ? rc?.ToString() ?? "DQ-FAIL" : "DQ-FAIL",
                            FieldName  = outputs?.TryGetValue("field", out var fn) == true
                                         ? fn?.ToString() : null,
                            Message    = outputs?.TryGetValue("message", out var msg) == true
                                         ? msg?.ToString() : null,
                            EntityName = schema.DestinationEntityName,
                            EvaluatedAt = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"DQ rule '{ruleKey}' threw: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    failures.Add(new DqGateResult
                    {
                        RuleKey    = ruleKey,
                        Passed     = false,
                        ReasonCode = "RULE-EXCEPTION",
                        Message    = ex.Message,
                        EntityName = schema.DestinationEntityName
                    });
                }
            }

            return failures;
        }

        /// <inheritdoc cref="ISyncValidationHelper.FillMissingFieldsWithDefaults"/>
        public int FillMissingFieldsWithDefaults(
            DataSyncSchema schema,
            Dictionary<string, object> record,
            TheTechIdea.Beep.Editor.Defaults.IDefaultsManager defaultsManager)
        {
            if (schema == null || record == null || defaultsManager == null)
                return 0;

            int countBefore = record.Count(kvp => kvp.Value != null);

            try
            {
                defaultsManager.Apply(
                    _editor,
                    schema.DestinationDataSourceName,
                    schema.DestinationEntityName,
                    record);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync",
                    $"DefaultsManager.Apply threw for schema '{schema.Id}': {ex.Message}",
                    DateTime.Now, -1, "", Errors.Failed);
            }

            int countAfter = record.Count(kvp => kvp.Value != null);
            return Math.Max(0, countAfter - countBefore);
        }

        /// <inheritdoc cref="ISyncValidationHelper.CheckMappingQualityGate"/>
        public IErrorsInfo CheckMappingQualityGate(
            DataSyncSchema schema,
            out int qualityScore,
            out string qualityBand)
        {
            qualityScore = -1;
            qualityBand  = null;

            var policy = schema?.MappingPolicy;
            if (policy == null || !policy.Enabled || policy.MinQualityScore <= 0)
                return new ErrorsInfo { Flag = Errors.Ok, Message = "Mapping quality gate not configured — skipped." };

            try
            {
                // Build a simple score from mapped-field coverage
                var fields      = schema.MappedFields;
                var totalFields = fields?.Count ?? 0;

                if (totalFields == 0)
                {
                    qualityScore = 0;
                    qualityBand  = "Poor";
                }
                else
                {
                    int mapped   = fields.Count(f =>
                        !string.IsNullOrWhiteSpace(f.SourceField) &&
                        !string.IsNullOrWhiteSpace(f.DestinationField));
                    qualityScore = (int)Math.Round(100.0 * mapped / totalFields);
                    qualityBand  = qualityScore >= 90 ? "Good" : qualityScore >= 70 ? "Fair" : "Poor";
                }

                if (qualityScore < policy.MinQualityScore)
                    return new ErrorsInfo
                    {
                        Flag    = Errors.Failed,
                        Message = $"Mapping quality score {qualityScore} is below threshold " +
                                  $"{policy.MinQualityScore} ({qualityBand}). DQ checks cannot run."
                    };

                return new ErrorsInfo
                {
                    Flag    = Errors.Ok,
                    Message = $"Mapping quality OK: {qualityScore} ({qualityBand})."
                };
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = $"Error checking mapping quality: {ex.Message}" };
            }
        }
    }
}
