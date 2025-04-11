using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using TheTechIdea.Beep.Editor;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using System.Threading.Tasks;
using System.Collections.Concurrent;




namespace TheTechIdea.Beep.Roslyn
{
    public static class RoslynCompiler
    {
         // Dictionary to store compiled types and their assemblies
          private static readonly Dictionary<string, Tuple<Type, Assembly>> CompiledTypes = new Dictionary<string, Tuple<Type, Assembly>>();
        // Create a central method for managing references instead of duplicating across multiple methods
        private static List<MetadataReference> GetCommonReferences(bool includeAdditionalReferences = false)
        {
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Entity).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"))
    };

            if (includeAdditionalReferences)
            {
                // Add commonly used references for your application
                references.Add(MetadataReference.CreateFromFile(typeof(System.Data.DataTable).Assembly.Location));
                references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
                references.Add(MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location));
                // Add more as needed
            }

            return references;
        }

        public static bool CompileCodeToDLL(string sourceFile, string outputFile)
        {
            // Read the code from the file
            string code = File.ReadAllText(sourceFile);

            // Create a syntax tree from the code
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Set up assembly references
            // Note: You may need to adjust the paths or use MetadataReference.CreateFromFile
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
    };

            // Add additional necessary references
            // For example, if you use System.Linq in your code, add its reference as well
            // references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputFile),
                new[] { syntaxTree },
                references,
                compilationOptions);

            // Emit the compilation to a DLL
            EmitResult result;
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                result = compilation.Emit(outputStream);
            }

            // Handle and display errors
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine($"Compilation successful! Assembly generated at '{outputFile}'");
                return true;
            }
        }
        public static bool CompileCodeToDLL(IEnumerable<string> sourceFiles, string outputFile)
        {
            // List to hold the syntax trees
            var syntaxTrees = new List<SyntaxTree>();

            // Read each source file and create a syntax tree
            foreach (var sourceFile in sourceFiles)
            {
                string code = File.ReadAllText(sourceFile);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                syntaxTrees.Add(syntaxTree);
            }

            // Set up assembly references
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        // Add additional necessary references
        // For example, if you use System.Linq in your code, add its reference as well
        // references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));
    };

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputFile),
                syntaxTrees,
                references,
                compilationOptions);

            // Emit the compilation to a DLL
            EmitResult result;
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                result = compilation.Emit(outputStream);
            }

            // Handle and display errors
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine($"Compilation successful! Assembly generated at '{outputFile}'");
                return true;
            }
        }
        public  static bool CompileCodeFromStringsToDLL(IEnumerable<string> sourceCodes, string outputFile)
        {
            // List to hold the syntax trees
            var syntaxTrees = new List<SyntaxTree>();

            // Create a syntax tree for each source code string
            foreach (var code in sourceCodes)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                syntaxTrees.Add(syntaxTree);
            }

            // Set up assembly references
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        // Add additional necessary references
        // For example, if you use System.Linq in your code, add its reference as well
        // references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));
    };

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputFile),
                syntaxTrees,
                references,
                compilationOptions);

            // Emit the compilation to a DLL
            EmitResult result;
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                result = compilation.Emit(outputStream);
            }

            // Handle and display errors
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine($"Compilation successful! Assembly generated at '{outputFile}'");
                return true;
            }
        }
        public static Tuple<Type, Assembly> CompileClassTypeandAssembly(string classname, string code)
        {
            if (CompiledTypes.TryGetValue(classname, out var existingType))
            {
                return existingType;
            }
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
          MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
          MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().Assembly.Location), // for System.ComponentModel
          MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
          MetadataReference.CreateFromFile(typeof(Entity).GetTypeInfo().Assembly.Location),
          //MetadataReference.CreateFromFile(Path.Combine( Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Private.CoreLib.dll")),
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
                    var compiledType = assembly.GetTypes().FirstOrDefault(p => p.Name.Contains(classname));

                    // Store the compiled type and assembly in the dictionary
                    if (compiledType != null)
                    {
                        CompiledTypes[classname] = new Tuple<Type, Assembly>(compiledType, assembly);
                    }
                    return new Tuple<Type, Assembly>(assembly.GetTypes().FirstOrDefault(p => p.Name.Contains(classname)), assembly);  // Gets first type. Adjust this if you need to get a specific type.
                }
            }
        }
        public static Type CompileGetClassType(string filepath, string classname, string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            string assemblyName = Path.GetFileNameWithoutExtension(filepath);
            MetadataReference[] references = new MetadataReference[]
            {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
     //   MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Private.CoreLib.dll")),
        MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"))
            };
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            string filename = Path.Combine(filepath, classname, ".cs");
            EmitResult result = compilation.Emit(filename);  // Emit to file instead of MemoryStream

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
                Assembly assembly = Assembly.LoadFrom(filename);  // Load the assembly from file

                return assembly.GetTypes().FirstOrDefault(p => p.Name.Contains(classname));  // Gets first type. Adjust this if you need to get a specific type.
            }
        }
        public static bool CompileClassFromFileToDLL(string filePath, string outputFile)
        {
            string sourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CompileToDLL(new[] { syntaxTree }, outputFile);
        }
        public static bool CompileClassFromStringToDLL(string sourceCode, string outputFile)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CompileToDLL(new[] { syntaxTree }, outputFile);
        }
        public static bool CompileFiles(IEnumerable<string> filePaths)
        {
            // List to hold the syntax trees
            var syntaxTrees = new List<SyntaxTree>();

            // Read and parse each file into a syntax tree
            foreach (var filePath in filePaths)
            {
                string fileContent = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
                syntaxTrees.Add(syntaxTree);
            }
            // Set up assembly references
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        // Add other necessary references as needed
    };

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                syntaxTrees,
                references,
                compilationOptions);

            // Emit the compilation to an in-memory assembly
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly.Load(ms.ToArray());
                    return true;
                }
                else
                {
                    // Handle and display errors
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            Console.WriteLine(diagnostic.ToString());
                        }
                    }
                    return false;
                }
            }
        }
        
        // Compile from file path
        public static bool CompileFile(string filePath)
        {
            string sourceCode = File.ReadAllText(filePath);
            return CompileSource(sourceCode);
        }

        // Compile from source code string
        public static bool CompileSource(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CompileInMemory(new[] { syntaxTree });
        }

        // In-memory compilation
       
        public static Assembly CreateAssembly(IDMEEditor DMEEditor, string code)
        {
            try
            {
                // Parse the code into a syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                // Set up assembly references
                // The list of necessary references might need to be adjusted based on your code's requirements
                var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            // Additional references can be added here
        };

                // Define compilation options
                var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Release)
                    .WithPlatform(Platform.AnyCpu);

                // Create a compilation
                var compilation = CSharpCompilation.Create(
                    "InMemoryAssembly",
                    new[] { syntaxTree },
                    references,
                    compilationOptions);

                // Emit the compilation to an in-memory assembly
                Assembly assembly;
                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var diagnostic in result.Diagnostics)
                        {
                            if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                            {
                                sb.AppendLine(diagnostic.ToString());
                                DMEEditor.AddLogMessage("Error", diagnostic.ToString(), DateTime.Now, 0, null, Errors.Failed);
                            }
                        }
                        throw new InvalidOperationException(sb.ToString());
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }

                return assembly;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error compiling code: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
        public static Assembly CreateAssembly( string code)
        {
            try
            {
                // Parse the code into a syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                // Set up assembly references
                // The list of necessary references might need to be adjusted based on your code's requirements
                var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            // Additional references can be added here
        };

                // Define compilation options
                var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Release)
                    .WithPlatform(Platform.AnyCpu);

                // Create a compilation
                var compilation = CSharpCompilation.Create(
                    "InMemoryAssembly",
                    new[] { syntaxTree },
                    references,
                    compilationOptions);

                // Emit the compilation to an in-memory assembly
                Assembly assembly;
                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var diagnostic in result.Diagnostics)
                        {
                            if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                            {
                                sb.AppendLine(diagnostic.ToString());
                               // DMEEditor.AddLogMessage("Error", diagnostic.ToString(), DateTime.Now, 0, null, Errors.Failed);
                            }
                        }
                        throw new InvalidOperationException(sb.ToString());
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }

                return assembly;
            }
            catch (Exception ex)
            {
                //DMEEditor.AddLogMessage("Error", $"Error compiling code: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
        private static bool CompileToDLL(IEnumerable<SyntaxTree> syntaxTrees, string outputFile)
        {
            // Set up assembly references
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        // Add other necessary references
    };

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputFile),
                syntaxTrees,
                references,
                compilationOptions);

            // Emit the compilation to a DLL
            EmitResult result;
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                result = compilation.Emit(outputStream);
            }

            // Handle and display errors
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine($"Compilation successful! Assembly generated at '{outputFile}'");
                return true;
            }
        }
        private static bool CompileInMemory(IEnumerable<SyntaxTree> syntaxTrees)
        {
            // Set up assembly references
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        // Add other necessary references
    };

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                syntaxTrees,
                references,
                compilationOptions);

            // Emit the compilation to an in-memory assembly
            EmitResult result;
            using (var ms = new MemoryStream())
            {
                result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly.Load(ms.ToArray());
                }
            }

            // Handle and display errors
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine("Compilation successful! The assembly has been loaded in memory.");
                return true;
            }
        }
        private static bool Compile(IEnumerable<SyntaxTree> syntaxTrees)
        {
            // Set up assembly references
            var references = new List<MetadataReference>
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        // Add additional necessary references as needed
    };

            // Define compilation options
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Create a compilation
            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                syntaxTrees,
                references,
                compilationOptions);

            // Emit the compilation to an in-memory assembly
            EmitResult result;
            using (var ms = new MemoryStream())
            {
                result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly.Load(ms.ToArray());
                }
            }

            // Handle and display errors
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine("Compilation successful! The assembly has been loaded in memory.");
                return true;
            }
        }
        // Add async compilation methods for better UI responsiveness
        public static async Task<Assembly> CreateAssemblyAsync(IDMEEditor DMEEditor, string code)
        {
            return await Task.Run(() => CreateAssembly(DMEEditor, code));
        }

        public static async Task<bool> CompileCodeToDLLAsync(string sourceFile, string outputFile)
        {
            return await Task.Run(() => CompileCodeToDLL(sourceFile, outputFile));
        }

        // Improved error reporting that returns structured information
        public static (bool Success, IEnumerable<Diagnostic> Errors, Assembly Assembly) CompileAndGetDiagnostics(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = GetCommonReferences();

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                new[] { syntaxTree },
                references,
                compilationOptions);

            Assembly resultAssembly = null;
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    resultAssembly = Assembly.Load(ms.ToArray());
                }

                return (result.Success,
                        result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error),
                        resultAssembly);
            }
        }
        // Improved type caching with thread safety
      
        // Add methods to manage the cache
        public static void ClearCompiledTypeCache()
        {
            CompiledTypes.Clear();
        }

        public static bool RemoveFromCache(string className)
        {
            return CompiledTypes.Remove(className, out _);
        }
        // Add ability to generate PDB files for debugging capabilities
        public static bool CompileWithDebuggingInfo(string sourceCode, string outputFile)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = GetCommonReferences();

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug) // Use Debug optimization
                .WithPlatform(Platform.AnyCpu);

            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputFile),
                new[] { syntaxTree },
                references,
                compilationOptions);

            // Create both DLL and PDB files
            EmitResult result;
            using (var dllStream = new FileStream(outputFile, FileMode.Create))
            using (var pdbStream = new FileStream(Path.ChangeExtension(outputFile, "pdb"), FileMode.Create))
            {
                result = compilation.Emit(dllStream, pdbStream);
            }

            return result.Success;
        }
        // Add support for using Source Generators
        public static Assembly CompileWithSourceGenerators(string code, IEnumerable<ISourceGenerator> generators)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var references = GetCommonReferences();

            var compilation = CSharpCompilation.Create(
                "GeneratedAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Apply source generators
            var driver = CSharpGeneratorDriver.Create(generators);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Emit the compilation
            using (var ms = new MemoryStream())
            {
                var result = outputCompilation.Emit(ms);
                if (!result.Success)
                {
                    return null;
                }

                ms.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(ms.ToArray());
            }
        }
        // Compile multiple classes in one go, handling their dependencies
        public static Dictionary<string, Type> CompileMultipleClassTypes(Dictionary<string, string> classesToCompile)
        {
            if (classesToCompile == null || !classesToCompile.Any())
                return new Dictionary<string, Type>();

            // Parse all source codes
            var syntaxTrees = classesToCompile.Values.Select(code => CSharpSyntaxTree.ParseText(code)).ToArray();

            // Set up compilation
            var references = GetCommonReferences(true);
            var compilation = CSharpCompilation.Create(
                "MultipleClassesAssembly",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Emit assembly
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                    return new Dictionary<string, Type>();

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                // Map class names to their compiled types
                var types = new Dictionary<string, Type>();
                foreach (var className in classesToCompile.Keys)
                {
                    var type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
                    if (type != null)
                        types[className] = type;
                }

                return types;
            }
        }

    }
}
