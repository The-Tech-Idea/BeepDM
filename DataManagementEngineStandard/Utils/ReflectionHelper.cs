using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utils;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Utils
{
    internal static class ReflectionHelper
    {
        internal static T CreateInstance<T>(params object[] args) =>
            (T)Activator.CreateInstance(typeof(T), args);

        internal static Util.ObjectActivator<T> GetActivator<T>(ConstructorInfo ctor)
        {
            var paramsInfo = ctor.GetParameters();
            var param = Expression.Parameter(typeof(object[]), "args");
            var argsExp = new Expression[paramsInfo.Length];
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var accessor = Expression.ArrayIndex(param, index);
                var cast = Expression.Convert(accessor, paramsInfo[i].ParameterType);
                argsExp[i] = cast;
            }
            var newExp = Expression.New(ctor, argsExp);
            var lambda = Expression.Lambda(typeof(Util.ObjectActivator<T>), newExp, param);
            return (Util.ObjectActivator<T>)lambda.Compile();
        }

        internal static bool IsObjectNumeric(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        internal static Type GetListElementType(object list)
        {
            if (list == null) return null;
            var t = list.GetType();
            if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(List<>)) return null;
            return t.GetGenericArguments()[0];
        }

        internal static ExpandoObject ToExpando(object obj)
        {
            var exp = new ExpandoObject();
            var dict = (IDictionary<string, object>)exp;
            foreach (var p in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                dict[p.Name] = p.GetValue(obj);
            }
            return exp;
        }

        internal static object MapObject(IDMEEditor dme, string destEntityName, EntityDataMap_DTL map, object sourceObj)
        {
            object destObj = dme.Utilfunction.GetEntityObject(dme, destEntityName, map.SelectedDestFields);
            foreach (var f in map.FieldMapping)
            {
                try
                {
                    var sp = sourceObj.GetType().GetProperty(f.FromFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var dp = destObj.GetType().GetProperty(f.ToFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (sp != null && dp != null)
                    {
                        var val = sp.GetValue(sourceObj);
                        if (val != null)
                        {
                            if (dp.PropertyType.IsAssignableFrom(val.GetType()))
                                dp.SetValue(destObj, val);
                            else
                                dp.SetValue(destObj, Convert.ChangeType(val, dp.PropertyType));
                        }
                    }
                }
                catch
                {
                    // ignore mapping errors per original silent pattern
                }
            }
            return destObj;
        }

        internal static object GetPropertyValue(object obj, string name) =>
            obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(obj);

        internal static void SetPropertyValue(object obj, string name, object value)
        {
            var pi = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null) return;
            var targetType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
            object safeValue = value == null ? null : Convert.ChangeType(value, targetType);
            pi.SetValue(obj, safeValue);
        }

        internal static List<T> CastList<T>(List<object> source) =>
            source.OfType<T>().ToList();
    }
}