using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Resolves master-detail key mappings from explicit settings and metadata fallbacks.
    /// </summary>
    public sealed class MasterDetailKeyResolver
    {
        /// <summary>Resolves the effective key mapping between a master and detail block.</summary>
        public MasterDetailKeyResolution Resolve(
            DataBlockInfo masterBlock,
            DataBlockInfo detailBlock,
            string explicitMasterKeyField = null,
            string explicitDetailForeignKeyField = null)
        {
            if (masterBlock == null)
            {
                return Unresolved("Master block information is required.");
            }

            if (detailBlock == null)
            {
                return Unresolved("Detail block information is required.");
            }

            var resolution = new MasterDetailKeyResolution();

            if (!string.IsNullOrWhiteSpace(explicitMasterKeyField) || !string.IsNullOrWhiteSpace(explicitDetailForeignKeyField))
            {
                if (string.IsNullOrWhiteSpace(explicitMasterKeyField) || string.IsNullOrWhiteSpace(explicitDetailForeignKeyField))
                {
                    resolution.Warnings.Add(
                        $"Ignoring incomplete explicit relationship mapping between '{masterBlock.BlockName}' and '{detailBlock.BlockName}'.");
                }
                else if (TryParseMappings(explicitMasterKeyField, explicitDetailForeignKeyField, out var explicitMappings, out var parseError))
                {
                    return Resolved(MasterDetailKeyResolutionSource.ExplicitConfiguration, explicitMappings, resolution.Warnings);
                }
                else
                {
                    return Unresolved(parseError);
                }
            }

            if (TryResolveFromRelations(masterBlock, detailBlock, detailBlock.EntityStructure?.Relations, MasterDetailKeyResolutionSource.EntityRelations, out var relationResolution))
            {
                MergeWarnings(relationResolution, resolution.Warnings);
                return relationResolution;
            }

            if (TryResolveFromDataSource(masterBlock, detailBlock, resolution.Warnings, out var datasourceResolution))
            {
                MergeWarnings(datasourceResolution, resolution.Warnings);
                return datasourceResolution;
            }

            if (TryResolveByPrimaryKeyNames(masterBlock, detailBlock, out var fallbackResolution))
            {
                MergeWarnings(fallbackResolution, resolution.Warnings);
                return fallbackResolution;
            }

            resolution.ErrorMessage =
                $"Unable to resolve a master-detail key mapping between '{masterBlock.BlockName}' and '{detailBlock.BlockName}'. " +
                "Provide explicit key fields or metadata that identifies the relationship.";
            return resolution;
        }

        /// <summary>Parses explicit master/detail key strings into field mappings.</summary>
        public static bool TryParseMappings(
            string masterKeyField,
            string detailForeignKeyField,
            out List<DataBlockFieldMapping> mappings,
            out string errorMessage)
        {
            mappings = new List<DataBlockFieldMapping>();
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(masterKeyField) || string.IsNullOrWhiteSpace(detailForeignKeyField))
            {
                errorMessage = "Master and detail key fields must both be provided.";
                return false;
            }

            var masterFields = SplitFields(masterKeyField);
            var detailFields = SplitFields(detailForeignKeyField);

            if (masterFields.Length != detailFields.Length)
            {
                errorMessage =
                    $"Master/detail key field counts do not match ('{masterKeyField}' vs '{detailForeignKeyField}').";
                return false;
            }

            for (var index = 0; index < masterFields.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(masterFields[index]) || string.IsNullOrWhiteSpace(detailFields[index]))
                {
                    errorMessage = "Master/detail key mappings cannot contain empty field names.";
                    return false;
                }

                mappings.Add(new DataBlockFieldMapping
                {
                    MasterField = masterFields[index],
                    DetailField = detailFields[index]
                });
            }

            return mappings.Count > 0;
        }

        private static bool TryResolveFromRelations(
            DataBlockInfo masterBlock,
            DataBlockInfo detailBlock,
            IEnumerable<RelationShipKeys> relations,
            MasterDetailKeyResolutionSource source,
            out MasterDetailKeyResolution resolution)
        {
            resolution = null;
            var candidates = relations?
                .Where(IsUsableRelation)
                .ToList();

            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            var masterEntityNames = GetEntityIdentifiers(masterBlock.EntityStructure);
            if (masterEntityNames.Count > 0)
            {
                var matchingCandidates = candidates
                    .Where(relation =>
                        !string.IsNullOrWhiteSpace(relation.RelatedEntityID) &&
                        masterEntityNames.Contains(relation.RelatedEntityID.Trim()))
                    .ToList();

                if (matchingCandidates.Count > 0)
                {
                    candidates = matchingCandidates;
                }
            }

            var relationGroups = BuildRelationGroups(candidates);
            if (relationGroups.Count == 0)
            {
                return false;
            }

            if (relationGroups.Count > 1)
            {
                resolution = Unresolved(
                    $"Multiple candidate relationships were found between '{masterBlock.BlockName}' and '{detailBlock.BlockName}'. " +
                    "Specify MasterKeyPropertyName and ForeignKeyPropertyName explicitly.");
                return true;
            }

            var mappings = relationGroups[0]
                .OrderBy(relation => relation.RelatedColumnSequenceID)
                .ThenBy(relation => relation.EntityColumnSequenceID)
                .ThenBy(relation => relation.RelatedEntityColumnID, StringComparer.OrdinalIgnoreCase)
                .Select(relation => new DataBlockFieldMapping
                {
                    MasterField = relation.RelatedEntityColumnID.Trim(),
                    DetailField = relation.EntityColumnID.Trim()
                })
                .ToList();

            resolution = Resolved(source, mappings);
            return true;
        }

        private static bool TryResolveFromDataSource(
            DataBlockInfo masterBlock,
            DataBlockInfo detailBlock,
            ICollection<string> warnings,
            out MasterDetailKeyResolution resolution)
        {
            resolution = null;

            var dataSource = detailBlock.UnitOfWork?.DataSource;
            var entityStructure = detailBlock.EntityStructure;
            if (dataSource == null || entityStructure == null)
            {
                return false;
            }

            try
            {
                var concreteEntityStructure = entityStructure as EntityStructure;
                var entityName =
                    entityStructure.DatasourceEntityName ??
                    concreteEntityStructure?.OriginalEntityName ??
                    entityStructure.EntityName;

                var relations = dataSource.GetEntityforeignkeys(entityName, entityStructure.SchemaOrOwnerOrDatabase);
                if (TryResolveFromRelations(
                    masterBlock,
                    detailBlock,
                    relations,
                    MasterDetailKeyResolutionSource.DataSourceForeignKeys,
                    out resolution))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                warnings?.Add(
                    $"Failed to resolve foreign-key metadata from the data source for '{detailBlock.BlockName}': {ex.Message}");
                return false;
            }

            return false;
        }

        private static bool TryResolveByPrimaryKeyNames(
            DataBlockInfo masterBlock,
            DataBlockInfo detailBlock,
            out MasterDetailKeyResolution resolution)
        {
            resolution = null;

            var primaryKeys = GetPrimaryKeyNames(masterBlock.EntityStructure).ToList();
            if (primaryKeys.Count == 0)
            {
                return false;
            }

            var detailFields = detailBlock.EntityStructure?.Fields?
                .Where(field => !string.IsNullOrWhiteSpace(field.FieldName))
                .Select(field => field.FieldName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (detailFields == null || detailFields.Count == 0)
            {
                return false;
            }

            if (primaryKeys.Any(primaryKey => !detailFields.Contains(primaryKey)))
            {
                return false;
            }

            resolution = Resolved(
                MasterDetailKeyResolutionSource.MatchingPrimaryKeyNames,
                primaryKeys.Select(primaryKey => new DataBlockFieldMapping
                {
                    MasterField = primaryKey,
                    DetailField = primaryKey
                }));
            return true;
        }

        private static List<List<RelationShipKeys>> BuildRelationGroups(List<RelationShipKeys> candidates)
        {
            if (candidates.Count == 1)
            {
                return new List<List<RelationShipKeys>> { candidates };
            }

            var namedGroups = candidates
                .Where(relation => !string.IsNullOrWhiteSpace(relation.RalationName))
                .GroupBy(relation => relation.RalationName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.ToList())
                .ToList();

            if (namedGroups.Count > 0)
            {
                return namedGroups;
            }

            return new List<List<RelationShipKeys>>();
        }

        private static HashSet<string> GetEntityIdentifiers(IEntityStructure entityStructure)
        {
            var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (entityStructure == null)
            {
                return identifiers;
            }

            AddIdentifier(identifiers, entityStructure.EntityName);
            AddIdentifier(identifiers, entityStructure.DatasourceEntityName);
            if (entityStructure is EntityStructure concreteEntityStructure)
            {
                AddIdentifier(identifiers, concreteEntityStructure.OriginalEntityName);
            }
            return identifiers;
        }

        private static IEnumerable<string> GetPrimaryKeyNames(IEntityStructure entityStructure)
        {
            if (entityStructure == null)
            {
                return Enumerable.Empty<string>();
            }

            var keys = entityStructure.PrimaryKeys?
                .Where(key => !string.IsNullOrWhiteSpace(key.FieldName))
                .Select(key => key.FieldName.Trim())
                .ToList();

            if (keys != null && keys.Count > 0)
            {
                return keys;
            }

            if (!string.IsNullOrWhiteSpace(entityStructure.PrimaryKeyString))
            {
                return SplitFields(entityStructure.PrimaryKeyString);
            }

            return Enumerable.Empty<string>();
        }

        private static string[] SplitFields(string fields)
        {
            return fields?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(field => field.Trim())
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .ToArray() ?? Array.Empty<string>();
        }

        private static bool IsUsableRelation(RelationShipKeys relation)
        {
            return relation != null &&
                   !string.IsNullOrWhiteSpace(relation.RelatedEntityColumnID) &&
                   !string.IsNullOrWhiteSpace(relation.EntityColumnID);
        }

        private static void AddIdentifier(HashSet<string> identifiers, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                identifiers.Add(value.Trim());
            }
        }

        private static MasterDetailKeyResolution Resolved(
            MasterDetailKeyResolutionSource source,
            IEnumerable<DataBlockFieldMapping> mappings,
            IEnumerable<string> warnings = null)
        {
            var resolution = new MasterDetailKeyResolution
            {
                IsResolved = true,
                Source = source,
                Mappings = mappings.ToList()
            };

            if (warnings != null)
            {
                foreach (var warning in warnings.Where(warning => !string.IsNullOrWhiteSpace(warning)))
                {
                    resolution.Warnings.Add(warning);
                }
            }

            return resolution;
        }

        private static MasterDetailKeyResolution Unresolved(string errorMessage)
        {
            return new MasterDetailKeyResolution
            {
                IsResolved = false,
                Source = MasterDetailKeyResolutionSource.Unresolved,
                ErrorMessage = errorMessage
            };
        }

        private static void MergeWarnings(MasterDetailKeyResolution resolution, IEnumerable<string> warnings)
        {
            if (resolution == null || warnings == null)
            {
                return;
            }

            foreach (var warning in warnings.Where(warning => !string.IsNullOrWhiteSpace(warning)))
            {
                resolution.Warnings.Add(warning);
            }
        }
    }
}