using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TheTechIdea.Beep.Roslyn
{
    public static partial class RoslynCompiler
    {
        private readonly record struct CompilationIdentity(string RequestedType, string SourceHash);
        private static readonly BoundedCompilationCache<CompilationIdentity, Tuple<Type, Assembly>> CompiledTypes = new(256);

        /// <summary>Compiled cache entries, including in-flight entries. Completed retention is bounded at 256.</summary>
        public static int CompiledTypeCacheCount => CompiledTypes.Count;

        internal static string GeneratedSourceHash(string code) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        /// <summary>Compiles by exact requested type and source identity, with single-flight cache admission.</summary>
        /// <remarks>
        /// A simple name must be unique. Full names select exactly; substring matching is not supported.
        /// For legacy assembly-only callers, an absent requested type yields a null Item1 and usable assembly.
        /// Cache eviction does not unload assemblies or invalidate Types already returned to callers.
        /// </remarks>
        public static Tuple<Type, Assembly> CompileClassTypeandAssembly(string classname, string code)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(classname);
            ArgumentNullException.ThrowIfNull(code);
            var identity = new CompilationIdentity(classname, GeneratedSourceHash(code));
            return CompiledTypes.GetOrAdd(identity, () => CompileGeneratedType(classname, code));
        }

        private static Tuple<Type, Assembly> CompileGeneratedType(string requestedType, string code)
        {
            var compilation = CSharpCompilation.Create("BeepGenerated_" + Guid.NewGuid().ToString("N"),
                new[] { CSharpSyntaxTree.ParseText(code) }, GetCommonReferences(includeAdditionalReferences: true),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            if (!result.Success)
            {
                // Diagnostic IDs/positions are useful without echoing source values or compiler messages.
                var failures = result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Take(5).Select(d =>
                    {
                        var position = d.Location.GetLineSpan().StartLinePosition;
                        return $"{d.Id} at {position.Line + 1}:{position.Character + 1}";
                    });
                throw new InvalidOperationException("Could not compile generated type: " + string.Join("; ", failures));
            }
            var assembly = Assembly.Load(stream.ToArray());
            var selected = assembly.GetType(requestedType, false, false);
            if (selected == null && !requestedType.Contains('.') && !requestedType.Contains('+'))
            {
                var matches = assembly.GetTypes().Where(type => string.Equals(type.Name, requestedType, StringComparison.Ordinal)).Take(2).ToArray();
                if (matches.Length > 1) throw new InvalidOperationException("Requested generated type name is ambiguous.");
                selected = matches.SingleOrDefault();
            }
            return new Tuple<Type, Assembly>(selected, assembly);
        }
    }
}
