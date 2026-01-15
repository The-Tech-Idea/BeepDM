using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonSchemaHelper
    {
        private static readonly string[] PreferredKeyNames = { "_id", "id", "Id", "ID" };

        public static (List<EntityStructure> entities, List<RelationShipKeys> relations) BuildEntityStructures(
            JToken root,
            string rootEntityName,
            string dataSourceId,
            DataSourceType dsType)
        {
            var entities = new List<EntityStructure>();
            var relations = new List<RelationShipKeys>();

            Traverse(root, rootEntityName, "$", null, entities, relations, dataSourceId, dsType);
            return (entities, relations);
        }

        private static void Traverse(
            JToken token,
            string entityName,
            string path,
            EntityStructure parent,
            List<EntityStructure> entities,
            List<RelationShipKeys> relations,
            string dataSourceId,
            DataSourceType dsType)
        {
            if (token == null) return;

            if (token.Type == JTokenType.Array)
            {
                // Expect array of homogeneous objects
                var arr = (JArray)token;
                var firstObj = arr.FirstOrDefault(t => t.Type == JTokenType.Object) as JObject;
                if (firstObj == null)
                {
                    return; // array of primitives - ignore for entity modeling
                }

                var entity = CreateEntity(entityName, path, parent, dataSourceId, dsType);
                BuildFields(entity, firstObj);
                EnsurePrimaryKey(entity);
                MaybeAddParentRef(entity, parent, relations);
                entities.Add(entity);

                // Recurse into nested object/arrays inside first object to discover children
                foreach (var prop in firstObj.Properties())
                {
                    var childToken = prop.Value;
                    if (childToken.Type == JTokenType.Object || childToken.Type == JTokenType.Array)
                    {
                        string childEntityName = entityName + "_" + prop.Name;
                        Traverse(childToken, childEntityName, path + "." + prop.Name, entity, entities, relations, dataSourceId, dsType);
                    }
                }
            }
            else if (token.Type == JTokenType.Object)
            {
                // Treat object as single-row entity (wrap)
                var jobj = (JObject)token;
                var entity = CreateEntity(entityName, path, parent, dataSourceId, dsType);
                BuildFields(entity, jobj);
                EnsurePrimaryKey(entity);
                MaybeAddParentRef(entity, parent, relations);
                entities.Add(entity);

                foreach (var prop in jobj.Properties())
                {
                    var childToken = prop.Value;
                    if (childToken.Type == JTokenType.Object || childToken.Type == JTokenType.Array)
                    {
                        string childEntityName = entityName + "_" + prop.Name;
                        Traverse(childToken, childEntityName, path + "." + prop.Name, entity, entities, relations, dataSourceId, dsType);
                    }
                }
            }
        }

        private static EntityStructure CreateEntity(string name, string path, EntityStructure parent, string dsId, DataSourceType dsType)
        {
            return new EntityStructure
            {
                EntityName = name,
                DatasourceEntityName = name,
                OriginalEntityName = name,
                EntityPath = path,
                ParentId = parent?.Id ?? 0,
                DataSourceID = dsId,
                DatabaseType = dsType,
                Viewtype = ViewType.File,
                EntityType = EntityType.Table,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>(),
                Relations = new List<RelationShipKeys>()
            };
        }

        private static void BuildFields(EntityStructure entity, JObject sample)
        {
            foreach (var prop in sample.Properties())
            {
                if (entity.Fields.Any(f => f.FieldName.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                entity.Fields.Add(new EntityField
                {
                   FieldName = prop.Name,
                    Originalfieldname = prop.Name,
                    Fieldtype = InferClrType(prop.Value),
                    EntityName = entity.EntityName,
                    AllowDBNull = true
                });
            }
        }

        private static string InferClrType(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => typeof(long).FullName,
                JTokenType.Float => typeof(double).FullName,
                JTokenType.Boolean => typeof(bool).FullName,
                JTokenType.Date => typeof(DateTime).FullName,
                JTokenType.Guid => typeof(Guid).FullName,
                JTokenType.Array => typeof(string).FullName, // simplified
                JTokenType.Object => typeof(string).FullName, // we do not flatten here
                _ => typeof(string).FullName
            };
        }

        private static void EnsurePrimaryKey(EntityStructure entity)
        {
            // Prefer natural keys
            var pk = entity.Fields
                .FirstOrDefault(f => PreferredKeyNames.Contains(f.FieldName, StringComparer.OrdinalIgnoreCase));

            if (pk != null)
            {
                pk.IsKey = true;
                entity.PrimaryKeys.Add(pk);
                return;
            }

            // Create synthetic
            if (!entity.Fields.Any(f => f.FieldName == "_rowId"))
            {
                var synthetic = new EntityField
                {
                   FieldName = "_rowId",
                    Fieldtype = typeof(string).FullName,
                    IsKey = true,
                    IsAutoIncrement = false,
                    AllowDBNull = false,
                    EntityName = entity.EntityName
                };
                entity.Fields.Insert(0, synthetic);
                entity.PrimaryKeys.Add(synthetic);
            }
        }

        private static void MaybeAddParentRef(EntityStructure child, EntityStructure parent, List<RelationShipKeys> relations)
        {
            if (parent == null) return;

            // Parent key
            var parentPk = parent.PrimaryKeys.FirstOrDefault();
            if (parentPk == null)
            {
                parentPk = parent.Fields.First(); // fallback
                parentPk.IsKey = true;
                if (!parent.PrimaryKeys.Contains(parentPk))
                    parent.PrimaryKeys.Add(parentPk);
            }

            // Add foreign key field if not existing
            const string parentRefName = "_parentId";
            if (!child.Fields.Any(f => f.FieldName.Equals(parentRefName, StringComparison.OrdinalIgnoreCase)))
            {
                child.Fields.Add(new EntityField
                {
                   FieldName = parentRefName,
                    Fieldtype = parentPk.Fieldtype,
                    AllowDBNull = true,
                    EntityName = child.EntityName
                });
            }

            // Relationship
            var rel = new RelationShipKeys
            {
                RelatedEntityID = parent.EntityName,
                RelatedEntityColumnID = parentPk.FieldName,
                EntityColumnID = parentRefName
            };
            child.Relations.Add(rel);
            relations.Add(rel);
        }
    }
}