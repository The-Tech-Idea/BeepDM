using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Tools.ClassHandler
{
    public class AnonymousAssemblyFactory
    {
        private static readonly Lazy<AnonymousAssemblyFactory> Lazy = new Lazy<AnonymousAssemblyFactory>(() => new AnonymousAssemblyFactory(), LazyThreadSafetyMode.ExecutionAndPublication);

        private ModuleBuilder moduleBinder;
        private ConcurrentDictionary<AnonymousClassSignature, Type> classes;
        private long classCount;
        private ReaderWriterLockSlim readerWriterLockSlim;

        private AnonymousAssemblyFactory()
        {
            var assemblyName = new AssemblyName("SomeDll.AnonymousClasses");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

#if ENABLE_LINQ_PARTIAL_TRUST
        new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
            try
            {
                this.moduleBinder = assemblyBuilder.DefineDynamicModule("SomeDll.DynamicModule");
            }
            finally
            {
#if ENABLE_LINQ_PARTIAL_TRUST
            PermissionSet.RevertAssert();
#endif
            }

            this.classes = new ConcurrentDictionary<AnonymousClassSignature, Type>();
            this.classCount = 1;
            this.readerWriterLockSlim = new ReaderWriterLockSlim();
        }

        public static AnonymousAssemblyFactory Instance => Lazy.Value;

        public Type GetAnonymousClass(IEnumerable<AnonymousProperty> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            this.readerWriterLockSlim.EnterUpgradeableReadLock();

            try
            {
                var signature = new AnonymousClassSignature(properties);
                Type outType;

                if (!this.classes.TryGetValue(signature, out outType))
                {
#if ENABLE_LINQ_PARTIAL_TRUST
        new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
                    this.readerWriterLockSlim.EnterWriteLock();
                    if (!this.classes.TryGetValue(signature, out outType))
                    {
                        var className = $"SomeDll_AnonymousClass_{this.classCount++}";
                        var typeBuilder = this.moduleBinder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public, typeof(AnonymousClass));
                        List<FieldInfo> fields;
                        this.CreateAnonymousProperties(typeBuilder, properties.ToList(), out fields);
                        this.CreateEqualsMethod(typeBuilder, fields);
                        this.CreateGetHashCodeMethod(typeBuilder, fields);
                        var result = typeBuilder.CreateType();
                        this.classes.TryAdd(signature, result);
                       // Interlocked.CompareExchange(ref this.classCount, this.classes.Count, this.classes.Count);
                        return result;
                    }

                    return outType;
                }
                else
                    return outType;
            }
            finally
            {
#if ENABLE_LINQ_PARTIAL_TRUST
            PermissionSet.RevertAssert();
#endif

                if (this.readerWriterLockSlim.IsWriteLockHeld)
                {
                    this.readerWriterLockSlim.ExitWriteLock();
                }

                this.readerWriterLockSlim.ExitUpgradeableReadLock();
            }
        }

        private void CreateAnonymousProperties(TypeBuilder typeBuilder, List<AnonymousProperty> properties, out List<FieldInfo> fields)
        {
            if (typeBuilder == null) throw new ArgumentNullException(nameof(typeBuilder));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            fields = new List<FieldInfo>(properties.Count);

            foreach (var anonProperty in properties)
            {
                var field = typeBuilder.DefineField($"_{anonProperty.Name}", anonProperty.Type, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(anonProperty.Name, PropertyAttributes.HasDefault, anonProperty.Type, null);
                var getter = typeBuilder.DefineMethod($"get_{anonProperty.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { anonProperty.Type });
                var setter = typeBuilder.DefineMethod($"set_{anonProperty.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { anonProperty.Type });

                var getterGenerator = getter.GetILGenerator();
                getterGenerator.Emit(OpCodes.Ldarg_0);
                getterGenerator.Emit(OpCodes.Ldfld, field);
                getterGenerator.Emit(OpCodes.Ret);

                var setterGenerator = setter.GetILGenerator();
                setterGenerator.Emit(OpCodes.Ldarg_0);
                setterGenerator.Emit(OpCodes.Ldarg_1);
                setterGenerator.Emit(OpCodes.Stfld, field);
                setterGenerator.Emit(OpCodes.Ret);

                property.SetGetMethod(getter);
                property.SetSetMethod(setter);

                fields.Add(field);
            }
        }

        private void CreateEqualsMethod(TypeBuilder typeBuilder, List<FieldInfo> fields)
        {
            if (typeBuilder == null) throw new ArgumentNullException(nameof(typeBuilder));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            var method = typeBuilder.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(bool), new[] { typeof(object) });

            var methodGenerator = method.GetILGenerator();
            var other = methodGenerator.DeclareLocal(typeBuilder);
            var next = methodGenerator.DefineLabel();
            methodGenerator.Emit(OpCodes.Ldarg_1);
            methodGenerator.Emit(OpCodes.Isinst, typeBuilder);
            methodGenerator.Emit(OpCodes.Stloc, other);
            methodGenerator.Emit(OpCodes.Ldloc, other);
            methodGenerator.Emit(OpCodes.Brtrue_S, next);
            methodGenerator.Emit(OpCodes.Ldc_I4_0);
            methodGenerator.Emit(OpCodes.Ret);
            methodGenerator.MarkLabel(next);
            foreach (var field in fields)
            {
                var comparerType = typeof(EqualityComparer<>).MakeGenericType(field.FieldType);
                next = methodGenerator.DefineLabel();
                methodGenerator.EmitCall(OpCodes.Call, comparerType.GetMethod("get_Default"), null);
                methodGenerator.Emit(OpCodes.Ldarg_0);
                methodGenerator.Emit(OpCodes.Ldfld, field);
                methodGenerator.Emit(OpCodes.Ldloc, other);
                methodGenerator.EmitCall(OpCodes.Callvirt, comparerType.GetMethod("Equals", new[] { field.FieldType, field.FieldType }), null);
                methodGenerator.Emit(OpCodes.Brtrue_S, next);
                methodGenerator.Emit(OpCodes.Ldc_I4);
                methodGenerator.Emit(OpCodes.Ret);
                methodGenerator.MarkLabel(next);
            }

            methodGenerator.Emit(OpCodes.Ldc_I4_1);
            methodGenerator.Emit(OpCodes.Ret);
        }

        private void CreateGetHashCodeMethod(TypeBuilder typeBuilder, List<FieldInfo> fields)
        {
            if (typeBuilder == null) throw new ArgumentNullException(nameof(typeBuilder));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            var method = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(int), null);

            var methodGenerator = method.GetILGenerator();
            methodGenerator.Emit(OpCodes.Ldc_I4_0);
            foreach (var field in fields)
            {
                var comparerType = typeof(EqualityComparer<>).MakeGenericType(field.FieldType);
                methodGenerator.EmitCall(OpCodes.Call, comparerType.GetMethod("get_Default"), null);
                methodGenerator.Emit(OpCodes.Ldarg_0);
                methodGenerator.Emit(OpCodes.Ldfld, field);
                methodGenerator.EmitCall(OpCodes.Callvirt, comparerType.GetMethod("GetHashCode", new[] { field.FieldType }), null);
                methodGenerator.Emit(OpCodes.Xor);
            }

            methodGenerator.Emit(OpCodes.Ret);
        }
    }
}
