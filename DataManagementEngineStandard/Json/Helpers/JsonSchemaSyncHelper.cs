using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonSchemaSyncHelper
    {
        // Scan data to add missing fields to EntityStructure
        public static bool SyncFieldsFromData(JArray data, EntityStructure es)
        {
            if (data == null || es == null) return false;
            bool changed = false;

            var existing = new HashSet<string>(es.Fields.Select(f => f.FieldName), StringComparer.OrdinalIgnoreCase);

            foreach (var obj in data.OfType<JObject>())
            {
                foreach (var prop in obj.Properties())
                {
                    if (!existing.Contains(prop.Name))
                    {
                        es.Fields.Add(new EntityField
                        {
                           FieldName = prop.Name,
                            Fieldtype = InferClrType(prop.Value),
                            EntityName = es.EntityName,
                            AllowDBNull = true
                        });
                        existing.Add(prop.Name);
                        changed = true;
                    }
                }
            }
            return changed;
        }

        // Ensure synthetic PK still exists
        public static bool EnsurePrimaryKeyIntegrity(EntityStructure es)
        {
            if (es == null) return false;
            if (es.PrimaryKeys == null || es.PrimaryKeys.Count == 0)
                return false;

            var pk = es.PrimaryKeys.First();
            if (!es.Fields.Any(f => f.FieldName.Equals(pk.FieldName, StringComparison.OrdinalIgnoreCase)))
            {
                es.Fields.Insert(0, pk);
                return true;
            }
            return false;
        }

        private static string InferClrType(JToken token) =>
            token.Type switch
            {
                JTokenType.Integer => typeof(long).FullName,
                JTokenType.Float => typeof(double).FullName,
                JTokenType.Boolean => typeof(bool).FullName,
                JTokenType.Date => typeof(DateTime).FullName,
                JTokenType.Guid => typeof(Guid).FullName,
                _ => typeof(string).FullName
            };
    }
}