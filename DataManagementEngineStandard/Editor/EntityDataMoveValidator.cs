using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep
{
    /// <summary>Validates if a record can be inserted into a data source for a given entity.</summary>
    /// <param name="DMEEditor">The DMEEditor instance.</param>
    /// <param name="DataSource">The data source.</param>
    /// <param name="Entity">The entity structure.</param>
    /// <param name="record">The record to be inserted.</param>
    /// <param name="entityname">The name of the entity.</param>
    /// <param name="datasource">The name of the data source.</param>
    /// <returns>An EntityValidatorMesseges indicating the validation result.</returns>
    /// <remarks>
    /// This method checks if the DMEEditor instance is not null.
    /// If
    public static partial class EntityDataMoveValidator
    {
        /// <summary>
        /// Checks if a record can be inserted into a data source for a specific entity.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The IDataSource instance.</param>
        /// <param name="Entity">The EntityStructure instance.</param>
        /// <param name="record">The record to be inserted.</param>
        /// <param name="entityname">The name of the entity.</param>
        /// <param name="datasource">The name of the data source.</param>
        /// <returns>A list of validation messages indicating whether the record can be inserted or not.</returns>
        public static EntityValidatorMesseges CanInsertRecord(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity, object record, string entityname, string datasource)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource != null && record != null)
                    {
                        if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            Entity = DataSource.GetEntityStructure(entityname, false);
                            if (Entity != null)
                            {
                                retval = CanInsertRecord(DMEEditor, DataSource, record, Entity);
                            }
                        }
                    }
                }
            }
            return retval;
        }
        /// <summary>Checks if a record can be inserted into a data source.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The data source.</param>
        /// <param name="record">The record to be inserted.</param>
        /// <param name="Entity">The entity structure.</param>
        /// <returns>A list of validation messages indicating if the record can be inserted.</returns>
        public static EntityValidatorMesseges CanInsertRecord(IDMEEditor DMEEditor, IDataSource DataSource, object record, EntityStructure Entity)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;

            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource != null && record != null)
                    {
                        if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            if (Entity != null)
                            {
                                foreach (EntityField fld in Entity.Fields)
                                {
                                    var fldval = DMEEditor.Utilfunction.GetFieldValueFromObject(fld.fieldname, record);
                                    if (fld.AllowDBNull == false)
                                    {

                                        retval = TrueifNotNull(fldval);

                                    }
                                    if (fld.IsUnique)
                                    {
                                        retval = TrueifNotUnique(DMEEditor, DataSource, Entity, record, fld.fieldname, fldval);


                                    }
                                    if (fld.ValueRetrievedFromParent)
                                    {
                                        retval = TrueifNotUnique(DMEEditor, DataSource, Entity, record, fld.fieldname, fldval);
                                    }
                                }

                            }
                        }
                    }
                }


            }
            return retval;
        }
        /// <summary>
        /// Checks if the given object is not null.
        /// </summary>
        /// <param name="fldval">The object to be checked.</param>
        /// <returns>True if the object is not null, otherwise false.</returns>
        public static EntityValidatorMesseges TrueifNotNull(object fldval)
        {

            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (fldval != null)
            {
                retval = EntityValidatorMesseges.OK;
            }
            return retval;
        }
        /// <summary>
        /// Checks if a field value is not unique within a given entity and data source.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The IDataSource instance.</param>
        /// <param name="Entity">The EntityStructure instance.</param>
        /// <param name="record">The record object.</param>
        /// <param name="fieldname">The name of the field to check.</param>
        /// <param name="fldval">The value of the field to check.</param>
        /// <returns>True if the field value is not unique, otherwise false.</returns>
        public static EntityValidatorMesseges TrueifNotUnique(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity, object record, string fieldname, object fldval)
        {


            string strval = (string)fldval;
            int intval;
            int cnt = 0;
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource != null && record != null)
                    {
                        if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            Entity = DataSource.GetEntityStructure(Entity.EntityName, false);
                            if (Entity != null)
                            {
                                if (fldval != null)
                                {
                                    if (int.TryParse(strval, out intval))
                                    {
                                        cnt = (int)DataSource.RunQuery($"select count(*)  from {Entity.EntityName} where {fieldname}={intval}");
                                    }
                                    else
                                    {
                                        cnt = (int)DataSource.RunQuery($"select count(*)  from {Entity.EntityName} where {fieldname}='{strval}'");
                                    }
                                    if (cnt == 0)
                                    {
                                        retval = EntityValidatorMesseges.OK;
                                    }
                                    else
                                        retval = EntityValidatorMesseges.DuplicateValue;

                                }
                            }

                        }
                    }
                }



            }

            return retval;
        }
        /// <summary>
        /// Checks if the parent entity exists in the data source based on the provided parameters.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <param name="DataSource">The data source to check for the parent entity.</param>
        /// <param name="Entity">The structure of the entity.</param>
        /// <param name="record">The record object.</param>
        /// <param name="fieldname">The name of the field.</param>
        /// <param name="fldval">The value of the field.</param>
        /// <returns>True if the parent entity exists, otherwise false.</returns>
        public static EntityValidatorMesseges TrueifParentExist(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity, object record, string fieldname, object fldval)
        {


            string strval = (string)fldval;
            int intval;
            int cnt = 0;


            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource.Category == DatasourceCategory.RDBMS)
                    {

                        if (DataSource != null && record != null)
                        {
                            if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                            {
                                Entity = DataSource.GetEntityStructure(Entity.EntityName, false);
                                if (Entity != null)
                                {
                                    List<RelationShipKeys> rels = Entity.Relations.Where(p => p.EntityColumnID == fieldname).ToList();
                                    if (rels.Count > 0)
                                    {
                                        foreach (RelationShipKeys rel in rels)
                                        {
                                            if (int.TryParse(strval, out intval))
                                            {
                                                cnt = (int)DataSource.RunQuery($"select count(*)  from {rel.RelatedEntityID} where {rel.RelatedEntityColumnID}={intval}");
                                            }
                                            else
                                            {

                                                cnt = (int)DataSource.RunQuery($"select count(*)  from {rel.RelatedEntityID} where {rel.RelatedEntityColumnID}='{strval}'");
                                            }
                                            if (cnt == 0)
                                            {
                                                retval = EntityValidatorMesseges.MissingRefernceValue;
                                                break;
                                            }
                                            else
                                            {
                                                retval = EntityValidatorMesseges.OK;
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }




            }
            return retval;
        }
        /// <summary>Checks if an entity exists in a data source.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The data source to check.</param>
        /// <param name="Entity">The entity structure to check.</param>
        /// <returns>True if the entity exists in the data source, false otherwise.</returns>
        public static bool TrueifEntityExist(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity)
        {
            bool retval = false;

            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                    {
                        Entity = DataSource.GetEntityStructure(Entity.EntityName, false);
                        if (Entity == null)
                        {
                            retval = true;
                        }
                    }
                }

            }
            return retval;
        }
        /// <summary>
        /// Compares the structure of two entities from different or the same data sources.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data sources.</param>
        /// <param name="sourceDataSource">The source data source.</param>
        /// <param name="targetDataSource">The target data source.</param>
        /// <param name="sourceEntityName">The name of the source entity.</param>
        /// <param name="targetEntityName">The name of the target entity.</param>
        /// <returns>A list of ComparisonOutput indicating differences between the two entities.</returns>
        public static List<ComparisonOutput> CompareEntityStructuresToList(
            IDMEEditor DMEEditor,
            IDataSource sourceDataSource,
            IDataSource targetDataSource,
            string sourceEntityName,
            string targetEntityName)
        {
            List<ComparisonOutput> results = new List<ComparisonOutput>();

            try
            {
                // Validate Data Sources
                if (sourceDataSource == null || targetDataSource == null)
                {
                    results.Add(new ComparisonOutput
                    {
                        FieldName = "DataSource",
                        SourceDetail = sourceDataSource?.Category.ToString() ?? "NULL",
                        TargetDetail = targetDataSource?.Category.ToString() ?? "NULL",
                        Issue = "One or both data sources are null"
                    });
                    return results;
                }

                // Fetch entity structures
                var sourceEntity = sourceDataSource.GetEntityStructure(sourceEntityName, false);
                var targetEntity = targetDataSource.GetEntityStructure(targetEntityName, false);

                if (sourceEntity == null || targetEntity == null)
                {
                    results.Add(new ComparisonOutput
                    {
                        FieldName = "EntityStructure",
                        SourceDetail = sourceEntity != null ? sourceEntityName : "Not Found",
                        TargetDetail = targetEntity != null ? targetEntityName : "Not Found",
                        Issue = "Entity structure not found"
                    });
                    return results;
                }

                // Compare fields
                CompareFieldsToList(sourceEntity, targetEntity, results);

                // Compare relationships if applicable
                CompareRelationshipsToList(sourceDataSource, targetDataSource, sourceEntity, targetEntity, results);
            }
            catch (Exception ex)
            {
                results.Add(new ComparisonOutput
                {
                    FieldName = "Exception",
                    SourceDetail = "Error",
                    TargetDetail = "Error",
                    Issue = ex.Message
                });
            }

            return results;
        }

        /// <summary>
        /// Compares fields between two entities and adds results to the output list.
        /// </summary>
        private static void CompareFieldsToList(EntityStructure sourceEntity, EntityStructure targetEntity, List<ComparisonOutput> results)
        {
            var sourceFields = sourceEntity.Fields.ToDictionary(f => f.fieldname.ToLower());
            var targetFields = targetEntity.Fields.ToDictionary(f => f.fieldname.ToLower());

            foreach (var sourceField in sourceFields)
            {
                if (targetFields.TryGetValue(sourceField.Key, out var targetField))
                {
                    // Check data types
                    if (!string.Equals(sourceField.Value.fieldtype, targetField.fieldtype, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new ComparisonOutput
                        {
                            FieldName = sourceField.Value.fieldname,
                            SourceDetail = sourceField.Value.fieldtype,
                            TargetDetail = targetField.fieldtype,
                            Issue = "Type Mismatch"
                        });
                    }

                    // Check nullability
                    if (sourceField.Value.AllowDBNull != targetField.AllowDBNull)
                    {
                        results.Add(new ComparisonOutput
                        {
                            FieldName = sourceField.Value.fieldname,
                            SourceDetail = sourceField.Value.AllowDBNull.ToString(),
                            TargetDetail = targetField.AllowDBNull.ToString(),
                            Issue = "Nullability Mismatch"
                        });
                    }

                    // Check unique constraints
                    if (sourceField.Value.IsUnique != targetField.IsUnique)
                    {
                        results.Add(new ComparisonOutput
                        {
                            FieldName = sourceField.Value.fieldname,
                            SourceDetail = sourceField.Value.IsUnique.ToString(),
                            TargetDetail = targetField.IsUnique.ToString(),
                            Issue = "Uniqueness Mismatch"
                        });
                    }
                }
                else
                {
                    results.Add(new ComparisonOutput
                    {
                        FieldName = sourceField.Value.fieldname,
                        SourceDetail = "Exists",
                        TargetDetail = "Does Not Exist",
                        Issue = "Missing in Target"
                    });
                }
            }

            foreach (var targetField in targetFields)
            {
                if (!sourceFields.ContainsKey(targetField.Key))
                {
                    results.Add(new ComparisonOutput
                    {
                        FieldName = targetField.Value.fieldname,
                        SourceDetail = "Does Not Exist",
                        TargetDetail = "Exists",
                        Issue = "Missing in Source"
                    });
                }
            }
        }

        /// <summary>
        /// Compares relationships between two entities and adds results to the output list.
        /// </summary>
        private static void CompareRelationshipsToList(
            IDataSource sourceDataSource,
            IDataSource targetDataSource,
            EntityStructure sourceEntity,
            EntityStructure targetEntity,
            List<ComparisonOutput> results)
        {
            if (sourceDataSource.Category == DatasourceCategory.RDBMS && targetDataSource.Category == DatasourceCategory.RDBMS)
            {
                var sourceRelations = sourceEntity.Relations ?? new List<RelationShipKeys>();
                var targetRelations = targetEntity.Relations ?? new List<RelationShipKeys>();

                foreach (var sourceRel in sourceRelations)
                {
                    var targetRel = targetRelations.FirstOrDefault(rel =>
                        rel.EntityColumnID == sourceRel.EntityColumnID &&
                        rel.RelatedEntityID == sourceRel.RelatedEntityID &&
                        rel.RelatedEntityColumnID == sourceRel.RelatedEntityColumnID);

                    if (targetRel == null)
                    {
                        results.Add(new ComparisonOutput
                        {
                            FieldName = sourceRel.EntityColumnID,
                            SourceDetail = $"{sourceRel.EntityColumnID} -> {sourceRel.RelatedEntityID}.{sourceRel.RelatedEntityColumnID}",
                            TargetDetail = "Missing",
                            Issue = "Relationship Missing in Target"
                        });
                    }
                }

                foreach (var targetRel in targetRelations)
                {
                    var sourceRel = sourceRelations.FirstOrDefault(rel =>
                        rel.EntityColumnID == targetRel.EntityColumnID &&
                        rel.RelatedEntityID == targetRel.RelatedEntityID &&
                        rel.RelatedEntityColumnID == targetRel.RelatedEntityColumnID);

                    if (sourceRel == null)
                    {
                        results.Add(new ComparisonOutput
                        {
                            FieldName = targetRel.EntityColumnID,
                            SourceDetail = "Missing",
                            TargetDetail = $"{targetRel.EntityColumnID} -> {targetRel.RelatedEntityID}.{targetRel.RelatedEntityColumnID}",
                            Issue = "Relationship Missing in Source"
                        });
                    }
                }
            }
        }
        /// <summary>
        /// Generates a detailed string report from a list of ComparisonOutput.
        /// </summary>
        /// <param name="comparisonResults">The list of comparison results.</param>
        /// <returns>A formatted string report.</returns>
        public static string GenerateReportFromComparison(List<ComparisonOutput> comparisonResults)
        {
            StringBuilder report = new StringBuilder();

            report.AppendLine("Comparison Report:");
            foreach (var result in comparisonResults)
            {
                report.AppendLine(result.ToString());
            }

            return report.ToString();
        }
        public static List<ComparisonOutput> ValidateDataConsistency(
       IDMEEditor DMEEditor,
       IDataSource sourceDataSource,
       IDataSource targetDataSource,
       string sourceEntityName,
       string targetEntityName,string PrimaryKey)
        {
            List<ComparisonOutput> results = new List<ComparisonOutput>();

            var sourceData = sourceDataSource.GetEntity(sourceEntityName, null);
            var targetData = targetDataSource.GetEntity(targetEntityName, null);

            if (sourceData == null || targetData == null)
            {
                results.Add(new ComparisonOutput
                {
                    FieldName = "Data",
                    SourceDetail = sourceData == null ? "No Data" : "Exists",
                    TargetDetail = targetData == null ? "No Data" : "Exists",
                    Issue = "Data Missing in Source or Target"
                });
                return results;
            }

            // Ensure data is enumerable and contains rows in dictionary format
            var sourceRows = sourceData as IEnumerable<IDictionary<string, object>>;
            var targetRows = targetData as IEnumerable<IDictionary<string, object>>;

            if (sourceRows == null || targetRows == null)
            {
                results.Add(new ComparisonOutput
                {
                    FieldName = "DataFormat",
                    SourceDetail = sourceRows == null ? "Invalid Format" : "Valid",
                    TargetDetail = targetRows == null ? "Invalid Format" : "Valid",
                    Issue = "Incompatible Data Format"
                });
                return results;
            }

            // Convert data into dictionaries using a unique key like "PrimaryKey"
            var sourceDict = sourceRows.ToDictionary(row => row[PrimaryKey].ToString(), row => row);
            var targetDict = targetRows.ToDictionary(row => row[PrimaryKey].ToString(), row => row);

            foreach (var sourceKey in sourceDict.Keys)
            {
                if (targetDict.TryGetValue(sourceKey, out var targetRow))
                {
                    foreach (var key in sourceDict[sourceKey].Keys)
                    {
                        if (!sourceDict[sourceKey][key].Equals(targetRow[key]))
                        {
                            results.Add(new ComparisonOutput
                            {
                                FieldName = key,
                                SourceDetail = sourceDict[sourceKey][key]?.ToString(),
                                TargetDetail = targetRow[key]?.ToString(),
                                Issue = "Value Mismatch"
                            });
                        }
                    }
                }
                else
                {
                    results.Add(new ComparisonOutput
                    {
                        FieldName = "PrimaryKey",
                        SourceDetail = sourceKey,
                        TargetDetail = "Not Found",
                        Issue = "Missing Record in Target"
                    });
                }
            }

            foreach (var targetKey in targetDict.Keys)
            {
                if (!sourceDict.ContainsKey(targetKey))
                {
                    results.Add(new ComparisonOutput
                    {
                        FieldName = "PrimaryKey",
                        SourceDetail = "Not Found",
                        TargetDetail = targetKey,
                        Issue = "Missing Record in Source"
                    });
                }
            }

            return results;
        }
    }
        /// <summary>
        /// Represents the result of a comparison between two data source entities.
        /// </summary>
        public class ComparisonOutput
    {
        public string FieldName { get; set; }
        public string SourceDetail { get; set; }
        public string TargetDetail { get; set; }
        public string Issue { get; set; }

        public override string ToString()
        {
            return $"{FieldName}: {Issue} | Source: {SourceDetail} | Target: {TargetDetail}";
        }
    }

}