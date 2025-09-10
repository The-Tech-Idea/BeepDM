using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonRelationHelper
    {
        public static IEnumerable<JObject> GetChildren(
            JToken root,
            EntityStructure parent,
            EntityStructure child,
            string parentKeyValue)
        {
            if (parent == null || child == null || string.IsNullOrWhiteSpace(parentKeyValue))
                return Enumerable.Empty<JObject>();

            var childArray = JsonPathNavigator.ResolveArray(root, child);
            if (childArray == null)
                return Enumerable.Empty<JObject>();

            var rel = child.Relations.FirstOrDefault(r =>
                string.Equals(r.RelatedEntityID, parent.EntityName, StringComparison.OrdinalIgnoreCase));

            if (rel == null)
                return Enumerable.Empty<JObject>();

            return childArray
                .OfType<JObject>()
                .Where(o => string.Equals(o[rel.EntityColumnID]?.ToString(), parentKeyValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static IEnumerable<JObject> GetParents(
            JToken root,
            EntityStructure child,
            EntityStructure parent,
            string childRowParentKeyValue)
        {
            if (parent == null || child == null || string.IsNullOrWhiteSpace(childRowParentKeyValue))
                return Enumerable.Empty<JObject>();

            var parentArray = JsonPathNavigator.ResolveArray(root, parent);
            if (parentArray == null)
                return Enumerable.Empty<JObject>();

            var parentPk = parent.PrimaryKeys.FirstOrDefault();
            if (parentPk == null)
                return Enumerable.Empty<JObject>();

            return parentArray
                .OfType<JObject>()
                .Where(o => string.Equals(o[parentPk.fieldname]?.ToString(), childRowParentKeyValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}