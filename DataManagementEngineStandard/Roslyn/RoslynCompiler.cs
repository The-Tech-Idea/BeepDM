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
using TheTechIdea.Util;


namespace TheTechIdea.Beep.Roslyn
{
    public static class RoslynCompiler
    {
         // Dictionary to store compiled types and their assemblies
          private static readonly Dictionary<string, Tuple<Type, Assembly>> CompiledTypes = new Dictionary<string, Tuple<Type, Assembly>>();

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

    }
}
