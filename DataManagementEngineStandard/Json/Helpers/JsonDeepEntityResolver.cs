using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonDeepEntityResolver
    {
        // Returns the JArray representing a named entity anywhere in the tree (by EntityPath)
        public static JArray ResolveEntityArray(JToken root, List<EntityStructure> entities, string entityName)
        {
            var es = entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null) return null;
            return JsonPathNavigator.ResolveArray(root, es);
        }

        // Enumerate all entities under a parent (direct children)
        public static IEnumerable<EntityStructure> GetChildEntities(EntityStructure parent, List<EntityStructure> all)
            => all.Where(e => e.ParentId == parent?.Id);
    }
}