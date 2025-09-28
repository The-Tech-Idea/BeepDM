using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TheTechIdea.Beep.Editor.Mapping.Helpers;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// AutoObjMapper - Expression Builder functionality
    /// Handles the compilation of mapping expressions
    /// </summary>
    public sealed partial class AutoObjMapper
    {
        /// <summary>
        /// Builds a compiled setter action for mapping between TSource and TDest
        /// </summary>
        private Action<TSource, TDest> BuildSetter<TSource, TDest>()
        {
            var srcType = typeof(TSource);
            var destType = typeof(TDest);

            // Read per-type mapping configuration if any
            var typeMap = _config.GetTypeMap(srcType, destType);

            var srcParam = Expression.Parameter(srcType, "src");
            var destParam = Expression.Parameter(destType, "dest");
            var bodyExpressions = new List<Expression>();

            // Optional before-map hook
            if (typeMap?.BeforeMapDelegate is Delegate before)
            {
                var beforeConst = Expression.Constant(before);
                bodyExpressions.Add(Expression.Invoke(beforeConst, srcParam, destParam));
            }

            // Build property mappings
            var mappingExpressions = BuildPropertyMappings<TSource, TDest>(srcParam, destParam, typeMap);
            bodyExpressions.AddRange(mappingExpressions);

            // Optional after-map hook
            if (typeMap?.AfterMapDelegate is Delegate after)
            {
                var afterConst = Expression.Constant(after);
                bodyExpressions.Add(Expression.Invoke(afterConst, srcParam, destParam));
            }

            var body = bodyExpressions.Count == 1 ? bodyExpressions[0] : Expression.Block(bodyExpressions);
            var lambda = Expression.Lambda<Action<TSource, TDest>>(body, srcParam, destParam);
            return lambda.Compile();
        }

        /// <summary>
        /// Builds property mapping expressions
        /// </summary>
        private List<Expression> BuildPropertyMappings<TSource, TDest>(
            ParameterExpression srcParam, 
            ParameterExpression destParam, 
            Interfaces.ITypeMapBase typeMap)
        {
            var mappingExpressions = new List<Expression>();
            
            // Get property discovery helper
            var propertyHelper = new PropertyDiscoveryHelper(_options);
            var propertyMappings = propertyHelper.GetPropertyMappings<TSource, TDest>();

            foreach (var mapping in propertyMappings)
            {
                if (typeMap != null && typeMap.IsIgnored(mapping.DestProperty.Name))
                    continue;

                Expression valueExpr = null;

                // 1) Custom resolver has precedence
                if (typeMap != null && typeMap.TryGetResolver(mapping.DestProperty.Name, out var resolverDel))
                {
                    var resolverConst = Expression.Constant(resolverDel);
                    valueExpr = Expression.Invoke(resolverConst, srcParam);
                }
                else if (mapping.SourceProperty != null)
                {
                    // 2) Direct property mapping
                    var srcAccess = Expression.Property(srcParam, mapping.SourceProperty);
                    valueExpr = Expression.Convert(srcAccess, typeof(object)); // box for conversion helper
                }
                else
                {
                    // No mapping available, skip
                    continue;
                }

                // Create assignment expression with type conversion
                var assignmentExpr = CreateAssignmentExpression(destParam, mapping.DestProperty, valueExpr);
                
                // Handle IgnoreNullSourceValues
                if (_options.IgnoreNullSourceValues)
                {
                    var nullCheck = Expression.Equal(valueExpr, Expression.Constant(null));
                    mappingExpressions.Add(Expression.IfThen(Expression.Not(nullCheck), assignmentExpr));
                }
                else
                {
                    mappingExpressions.Add(assignmentExpr);
                }
            }

            return mappingExpressions;
        }

        /// <summary>
        /// Creates an assignment expression with type conversion
        /// </summary>
        private Expression CreateAssignmentExpression(ParameterExpression destParam, PropertyInfo destProp, Expression boxedValue)
        {
            // Use TypeConversionHelper for conversion
            var convertHelper = typeof(TypeConversionHelper);
            var convertMethod = convertHelper.GetMethod(nameof(TypeConversionHelper.TryConvert), 
                BindingFlags.Public | BindingFlags.Static);
            
            var targetTypeExpr = Expression.Constant(destProp.PropertyType, typeof(Type));
            var convertedObj = Expression.Call(convertMethod!, boxedValue, targetTypeExpr);
            var convertedCast = Expression.Convert(convertedObj, destProp.PropertyType);
            var destAccess = Expression.Property(destParam, destProp);
            
            return Expression.Assign(destAccess, convertedCast);
        }
    }
}