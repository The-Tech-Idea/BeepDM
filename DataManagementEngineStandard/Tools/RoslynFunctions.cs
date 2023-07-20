using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Tools
{
    public static class RoslynFunctions
    {
        public static Tuple<Type,Assembly> CompileAndGetFirstType(string classname,string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
          MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
          MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().Assembly.Location), // for System.ComponentModel
          MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
          MetadataReference.CreateFromFile(typeof(Entity).GetTypeInfo().Assembly.Location),
          MetadataReference.CreateFromFile(Path.Combine( Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Private.CoreLib.dll")),
          MetadataReference.CreateFromFile(Path.Combine( Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"))
                // Add any other references you need...
               
            };
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                    return null;
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    return new Tuple<Type,Assembly>(assembly.GetTypes().FirstOrDefault(p=>p.Name.Contains(classname)), assembly)  ;  // Gets first type. Adjust this if you need to get a specific type.
                }
            }
        }
    }
}
