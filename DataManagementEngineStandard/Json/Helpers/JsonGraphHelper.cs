using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal class JsonGraphHelper
    {
        private readonly JToken _root;
        private readonly List<EntityStructure> _entities;
        public JsonGraphHelper(JToken root, List<EntityStructure> entities, Func<string, Type> resolver)
        {
            _root = root;
            _entities = entities;
        }

        public IEnumerable<object> MaterializeGraph(
            string rootEntityName,
            List<AppFilter> rootFilters,
            int depth,
            Func<EntityStructure, bool> includeChildPredicate = null)
        {
            var options = new GraphHydrationOptions { Depth = depth };
            return MaterializeGraph(rootEntityName, rootFilters, options, includeChildPredicate);
        }

        public IEnumerable<object> MaterializeGraph(
            string rootEntityName,
            List<AppFilter> rootFilters,
            GraphHydrationOptions options,
            Func<EntityStructure, bool> includeChildPredicate = null)
        {
            if (options == null) options = new GraphHydrationOptions();
            if (options.Depth < 0) options.Depth = 0;

            var rootEntity = _entities.FirstOrDefault(e => e.EntityName == rootEntityName);
            if (rootEntity == null) return Enumerable.Empty<object>();

            var rootArray = JsonPathNavigator.ResolveArray(_root, rootEntity);
            if (rootArray == null) return Enumerable.Empty<object>();

            var predicates = JsonCacheManager.GetOrAddPredicates(rootEntity.EntityName, rootFilters, rootEntity);
            var results = new List<object>();

            foreach (var obj in rootArray.OfType<JObject>())
            {
                if (!predicates.All(p => p(obj))) continue;
                var node = BuildNode(obj, rootEntity, options.Depth, options, includeChildPredicate, null);
                results.Add(node);
            }
            return results;
        }

        private object BuildNode(
            JObject current,
            EntityStructure entity,
            int depth,
            GraphHydrationOptions options,
            Func<EntityStructure, bool> includeChildPredicate,
            Dictionary<string, object> parentRef)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Copy fields
            foreach (var p in current.Properties())
            {
                dict[p.Name] = PrimitiveOrString(p.Value);
            }

            // Ancestor chain
            if (options.IncludeAncestorChain && parentRef != null)
            {
                // build up chain list
                if (!dict.ContainsKey(options.AncestorsKey))
                {
                    var chain = new List<Dictionary<string, object>>();
                    if (parentRef.TryGetValue(options.AncestorsKey, out var parentChainObj) &&
                        parentChainObj is IEnumerable<Dictionary<string, object>> parentChainList)
                    {
                        chain.AddRange(parentChainList);
                    }
                    chain.Add(parentRef);
                    dict[options.AncestorsKey] = chain;
                }
            }

            // Parent reference
            if (options.IncludeParentReference && parentRef != null)
            {
                dict[options.ParentReferenceKey] = parentRef;
            }

            if (depth == 0) return dict;

            // Get children with cache
            var children = JsonCacheManager.GetOrAddChildren(entity, _entities)
                .Where(c => (includeChildPredicate == null || includeChildPredicate(c)) &&
                            (options.IncludeEntityNamePredicate == null || options.IncludeEntityNamePredicate(c.EntityName)))
                .ToList();

            if (!children.Any()) return dict;

            var parentPk = entity.PrimaryKeys?.FirstOrDefault();
            string parentPkValue = parentPk != null ? current[parentPk.fieldname]?.ToString() : null;

            foreach (var child in children)
            {
                var childArray = JsonPathNavigator.ResolveArray(_root, child);
                if (childArray == null) continue;

                var rel = child.Relations?.FirstOrDefault(r =>
                    r.RelatedEntityID.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase));

                var childItems = new List<object>();
                foreach (var childObj in childArray.OfType<JObject>())
                {
                    bool belongs = true;
                    if (rel != null && parentPkValue != null)
                    {
                        var fkValue = childObj[rel.EntityColumnID]?.ToString();
                        belongs = string.Equals(fkValue, parentPkValue, StringComparison.OrdinalIgnoreCase);
                    }
                    if (!belongs) continue;

                    var childNode = BuildNode(childObj, child, depth - 1, options, includeChildPredicate, dict);
                    childItems.Add(childNode);
                }

                if (childItems.Count > 0)
                    dict[child.EntityName] = childItems;
            }

            return dict;
        }

        private static object PrimitiveOrString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return null;
            return token.Type switch
            {
                JTokenType.Integer => token.ToObject<long>(),
                JTokenType.Float => token.ToObject<double>(),
                JTokenType.Date => token.ToObject<DateTime>(),
                JTokenType.Boolean => token.ToObject<bool>(),
                JTokenType.Object when token["$oid"] != null => token["$oid"]?.ToString(),
                _ => token.ToString()
            };
        }
    }
}