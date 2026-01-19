using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.ETL
{
    /// <summary>
    /// Provides validation utilities for ETL scripts, mappings, and entities.
    /// </summary>
    public class ETLValidator
    {
        private readonly IDMEEditor _dmeEditor;

        public ETLValidator(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        /// <summary>
        /// Validates the EntityDataMap to ensure all required fields and configurations are correct.
        /// </summary>
        public IErrorsInfo ValidateEntityMapping(EntityDataMap mapping)
        {
            var errorObject = new ErrorsInfo { Flag = Errors.Ok };

            if (mapping == null)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = "Mapping is null.";
                return errorObject;
            }

            if (string.IsNullOrWhiteSpace(mapping.EntityName))
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "EntityName is missing in the mapping." });
            }

            if (string.IsNullOrWhiteSpace(mapping.EntityDataSource))
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "EntityDataSource is missing in the mapping." });
            }

            if (mapping.EntityFields == null || !mapping.EntityFields.Any())
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "EntityFields are missing in the mapping." });
            }

            if (mapping.MappedEntities == null || !mapping.MappedEntities.Any())
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "MappedEntities are missing in the mapping." });
            }
            else
            {
                foreach (var detail in mapping.MappedEntities)
                {
                    var detailValidation = ValidateMappedEntity(detail);
                    if (detailValidation.Flag == Errors.Failed)
                    {
                        errorObject.Flag = Errors.Failed;
                        errorObject.Errors.AddRange(detailValidation.Errors);
                    }
                }
            }

            return errorObject;
        }

        /// <summary>
        /// Validates an individual EntityDataMap_DTL to ensure all required mappings are correct.
        /// </summary>
        public IErrorsInfo ValidateMappedEntity(EntityDataMap_DTL mappedEntity)
        {
            var errorObject = new ErrorsInfo { Flag = Errors.Ok };

            if (mappedEntity == null)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = "MappedEntity is null.";
                return errorObject;
            }

            if (string.IsNullOrWhiteSpace(mappedEntity.EntityName))
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "EntityName is missing in the mapped entity." });
            }

            if (string.IsNullOrWhiteSpace(mappedEntity.EntityDataSource))
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "EntityDataSource is missing in the mapped entity." });
            }

            if (mappedEntity.EntityFields == null || !mappedEntity.EntityFields.Any())
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "EntityFields are missing in the mapped entity." });
            }

            if (mappedEntity.SelectedDestFields == null || !mappedEntity.SelectedDestFields.Any())
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "SelectedDestFields are missing in the mapped entity." });
            }

            if (mappedEntity.FieldMapping == null || !mappedEntity.FieldMapping.Any())
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Errors.Add(new ErrorsInfo { Message = "FieldMapping is missing in the mapped entity." });
            }
            else
            {
                foreach (var fieldMap in mappedEntity.FieldMapping)
                {
                    if (string.IsNullOrWhiteSpace(fieldMap.FromFieldName) || string.IsNullOrWhiteSpace(fieldMap.ToFieldName))
                    {
                        errorObject.Flag = Errors.Failed;
                        errorObject.Errors.Add(new ErrorsInfo
                        {
                            Message = $"FieldMapping is invalid. FromField: {fieldMap.FromFieldName}, ToField: {fieldMap.ToFieldName}"
                        });
                    }
                }
            }

            return errorObject;
        }

        /// <summary>
        /// Validates entity consistency between source and destination fields.
        /// </summary>
        public IErrorsInfo ValidateEntityConsistency(
            IDataSource sourceDs,
            IDataSource destDs,
            string srcEntity,
            string destEntity)
        {
            var errorObject = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var srcStructure = sourceDs.GetEntityStructure(srcEntity, true);
                var destStructure = destDs.GetEntityStructure(destEntity, true);

                if (srcStructure == null)
                {
                    errorObject.Flag = Errors.Failed;
                    errorObject.Errors.Add(new ErrorsInfo { Message = $"Source entity {srcEntity} does not exist." });
                }

                if (destStructure == null)
                {
                    errorObject.Flag = Errors.Failed;
                    errorObject.Errors.Add(new ErrorsInfo { Message = $"Destination entity {destEntity} does not exist." });
                }

                if (srcStructure != null && destStructure != null)
                {
                    foreach (var field in srcStructure.Fields)
                    {
                        if (!destStructure.Fields.Any(f => f.FieldName.Equals(field.FieldName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            errorObject.Flag = Errors.Failed;
                            errorObject.Errors.Add(new ErrorsInfo
                            {
                                Message = $"Field {field.FieldName} in source entity {srcEntity} does not exist in destination entity {destEntity}."
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = $"Error during entity consistency validation: {ex.Message}";
            }

            return errorObject;
        }

        /// <summary>
        /// Validates if a specific mapping entity exists.
        /// </summary>
        public IErrorsInfo CheckIfMappingEntityExists(EntityDataMap mapping, string entityName)
        {
            var errorObject = new ErrorsInfo { Flag = Errors.Ok };

            if (mapping.MappedEntities.Any(p => p.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase)))
            {
                errorObject.Message = $"Mapping entity {entityName} exists.";
            }
            else
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = $"Mapping entity {entityName} does not exist.";
            }

            return errorObject;
        }
    }
}
