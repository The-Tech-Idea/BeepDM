using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for field mapping operations
    /// Based on mapping patterns from DataSyncManager
    /// </summary>
    public class FieldMappingHelper : IFieldMappingHelper
    {
        private readonly IDMEEditor _editor;

        /// <summary>
        /// Initializes a new instance of the FieldMappingHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        public FieldMappingHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Auto-map fields based on name matching between source and destination
        /// </summary>
        /// <param name="sourceDataSource">Source data source name</param>
        /// <param name="sourceEntity">Source entity name</param>
        /// <param name="destDataSource">Destination data source name</param>
        /// <param name="destEntity">Destination entity name</param>
        /// <returns>List of auto-generated field mappings</returns>
        public List<FieldSyncData> AutoMapFields(string sourceDataSource, string sourceEntity, string destDataSource, string destEntity)
        {
            var mappings = new List<FieldSyncData>();

            try
            {
                var sourceDs = _editor.GetDataSource(sourceDataSource);
                var destDs = _editor.GetDataSource(destDataSource);

                if (sourceDs == null || destDs == null)
                    return mappings;

                // Get entity structures
                var sourceStructure = sourceDs.GetEntityStructure(sourceEntity, false);
                var destStructure = destDs.GetEntityStructure(destEntity, false);

                if (sourceStructure?.Fields == null || destStructure?.Fields == null)
                    return mappings;

                // Match fields by name (case-insensitive)
                foreach (var sourceField in sourceStructure.Fields)
                {
                    var matchingDestField = destStructure.Fields.FirstOrDefault(df => 
                        string.Equals(df.FieldName, sourceField.FieldName, StringComparison.OrdinalIgnoreCase));

                    if (matchingDestField != null)
                    {
                        mappings.Add(new FieldSyncData
                        {
                            Id = Guid.NewGuid().ToString(),
                            SourceField = sourceField.FieldName,
                            DestinationField = matchingDestField.FieldName,
                            SourceFieldType = sourceField.Fieldtype,
                            DestinationFieldType = matchingDestField.Fieldtype
                        });
                    }
                }

                _editor.AddLogMessage("BeepSync", $"Auto-mapped {mappings.Count} fields between {sourceEntity} and {destEntity}", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error auto-mapping fields: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }

            return mappings;
        }

        /// <summary>
        /// Validate field mappings for correctness
        /// </summary>
        /// <param name="mappedFields">Field mappings to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateFieldMappings(IEnumerable<FieldSyncData> mappedFields)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (mappedFields == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Field mappings cannot be null";
                return result;
            }

            var errors = new List<string>();

            foreach (var mapping in mappedFields)
            {
                if (string.IsNullOrWhiteSpace(mapping.SourceField))
                    errors.Add($"Source field is empty for mapping Id: {mapping.Id}");

                if (string.IsNullOrWhiteSpace(mapping.DestinationField))
                    errors.Add($"Destination field is empty for mapping Id: {mapping.Id}");

                // Check for duplicate destination fields
                var duplicateDestFields = mappedFields
                    .Where(m => !string.IsNullOrWhiteSpace(m.DestinationField))
                    .GroupBy(m => m.DestinationField)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicateField in duplicateDestFields)
                {
                    errors.Add($"Duplicate destination field: {duplicateField}");
                }
            }

            if (errors.Any())
            {
                result.Flag = Errors.Failed;
                result.Message = string.Join("; ", errors);
                result.Errors = errors.Select(e => (IErrorsInfo)new ErrorsInfo { Message = e }).ToList();
            }

            return result;
        }

        // ── Phase 2: Schema Governance ────────────────────────────────────────────

        /// <inheritdoc cref="IFieldMappingHelper.LoadGovernedMapping"/>
        public IEnumerable<FieldSyncData> LoadGovernedMapping(DataSyncSchema schema)
        {
            try
            {
                if (schema?.MappedFields == null || schema.MappedFields.Count == 0)
                {
                    _editor.AddLogMessage("BeepSync", $"Schema '{schema?.Id}' has no governed field mappings.", DateTime.Now, -1, "", Errors.Ok);
                    return Enumerable.Empty<FieldSyncData>();
                }

                _editor.AddLogMessage("BeepSync", $"Loaded {schema.MappedFields.Count} governed mappings for schema '{schema.Id}'.", DateTime.Now, -1, "", Errors.Ok);
                return schema.MappedFields.ToList();
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error loading governed mapping: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return Enumerable.Empty<FieldSyncData>();
            }
        }

        /// <inheritdoc cref="IFieldMappingHelper.CheckMappingDrift"/>
        public bool CheckMappingDrift(DataSyncSchema schema, IEnumerable<FieldSyncData> baseline)
        {
            try
            {
                if (schema?.MappedFields == null && baseline == null) return false;
                if (schema?.MappedFields == null || baseline == null) return true;

                var livePairs = schema.MappedFields
                    .Select(f => $"{f.SourceField}>{f.DestinationField}")
                    .OrderBy(x => x).ToList();
                var basePairs = baseline
                    .Select(f => $"{f.SourceField}>{f.DestinationField}")
                    .OrderBy(x => x).ToList();

                bool drifted = !livePairs.SequenceEqual(basePairs);
                if (drifted)
                    _editor.AddLogMessage("BeepSync", $"Mapping drift detected for schema '{schema?.Id}'.", DateTime.Now, -1, "", Errors.Ok);
                return drifted;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error checking mapping drift: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        /// <inheritdoc cref="IFieldMappingHelper.PromoteMappingState"/>
        public IErrorsInfo PromoteMappingState(DataSyncSchema schema, string targetState)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (schema == null)
                {
                    result.Flag    = Errors.Failed;
                    result.Message = "Schema cannot be null.";
                    return result;
                }

                if (schema.CurrentSchemaVersion == null)
                {
                    schema.CurrentSchemaVersion = new SyncSchemaVersion
                    {
                        SchemaId = schema.Id,
                        Version  = 1
                    };
                }

                schema.CurrentSchemaVersion.ApprovalState = targetState;
                result.Message = $"Mapping state promoted to '{targetState}' for schema '{schema.Id}'.";
                _editor.AddLogMessage("BeepSync", result.Message, DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                result.Flag    = Errors.Failed;
                result.Message = $"Error promoting mapping state: {ex.Message}";
                _editor.AddLogMessage("BeepSync", result.Message, DateTime.Now, -1, "", Errors.Failed);
            }
            return result;
        }
    }
}
