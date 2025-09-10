using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonPathNavigator
    {
        // Basic path resolution: EntityStructure.EntityPath expected like $.Root.Child.Sub
        public static JToken ResolveToken(JToken root, string jsonPath)
        {
            if (root == null || string.IsNullOrWhiteSpace(jsonPath))
                return null;
            if (jsonPath == "$")
                return root;

            // Very lightweight segment walk (no full JSONPath support)
            var segments = jsonPath.Trim().TrimStart('$', '.').Split('.', StringSplitOptions.RemoveEmptyEntries);
            JToken current = root;
            foreach (var seg in segments)
            {
                if (current == null)
                    return null;

                if (current.Type == JTokenType.Array)
                {
                    // Treat segment as property inside first element if homogeneous
                    var firstObj = current.FirstOrDefault(t => t.Type == JTokenType.Object) as JObject;
                    if (firstObj == null)
                        return null;
                    current = firstObj[seg];
                }
                else if (current.Type == JTokenType.Object)
                {
                    current = current[seg];
                }
                else return null;
            }
            return current;
        }

        public static JArray ResolveArray(JToken root, EntityStructure entity)
        {
            var token = ResolveToken(root, entity?.EntityPath);
            return token as JArray;
        }
    }
}