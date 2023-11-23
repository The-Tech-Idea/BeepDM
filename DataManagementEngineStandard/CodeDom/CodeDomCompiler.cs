using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TheTechIdea.Beep.CodeDom
{
    public static class CodeDomCompiler
    {
       
        public static bool CompileCode(CodeDomProvider provider,
             String sourceFile,
             String exeFile)
        {

            CompilerParameters cp = new CompilerParameters();

            // Generate an executable instead of
            // a class library.
            cp.GenerateExecutable = false;

            // Set the assembly file name to generate.
            cp.OutputAssembly = exeFile;

            // Generate debug information.
            cp.IncludeDebugInformation = false;

            // Add an assembly reference.
            cp.ReferencedAssemblies.Add("System.dll");

            // Save the assembly as a physical file.
            cp.GenerateInMemory = false;

            // Set the level at which the compiler
            // should start displaying warnings.
            cp.WarningLevel = 3;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            // Set compiler argument to optimize output.
            cp.CompilerOptions = "/optimize";

            // Set a temporary files collection.
            // The TempFileCollection stores the temporary files
            // generated during a build in the current directory,
            // and does not delete them after compilation.
            cp.TempFiles = new TempFileCollection(".", true);

            //if (provider.Supports(GeneratorSupport.EntryPointMethod))
            //{
            //    // Specify the class that contains
            //    // the main method of the executable.
            //    cp.MainClass = "Samples.Class1";
            //}

            if (Directory.Exists("Resources"))
            {
                if (provider.Supports(GeneratorSupport.Resources))
                {
                    // Set the embedded resource file of the assembly.
                    // This is useful for culture-neutral resources,
                    // or default (fallback) resources.
                    cp.EmbeddedResources.Add("Resources\\Default.resources");

                    // Set the linked resource reference files of the assembly.
                    // These resources are included in separate assembly files,
                    // typically localized for a specific language and culture.
                    cp.LinkedResources.Add("Resources\\nb-no.resources");
                }
            }

            // Invoke compilation.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceFile);

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    sourceFile, cr.PathToAssembly);
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Source {0} built into {1} successfully.",
                    sourceFile, cr.PathToAssembly);
                Console.WriteLine("{0} temporary files created during the compilation.",
                    cp.TempFiles.Count.ToString());
            }

            // Return the results of compilation.
            if (cr.Errors.Count > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static List<string> CompileCode(CodeDomProvider provider,
           List<string> sourceFiles,
           String exeFile)
        {
            List<string> retval = new List<string>();
            CompilerParameters cp = new CompilerParameters();

            // Generate an executable instead of
            // a class library.
            cp.GenerateExecutable = false;

            // Set the assembly file name to generate.
            cp.OutputAssembly = exeFile;

            // Generate debug information.
            cp.IncludeDebugInformation = false;

            // Add an assembly reference.
            //  cp.ReferencedAssemblies.Add("System.dll");

            // Save the assembly as a physical file.
            cp.GenerateInMemory = false;

            // Set the level at which the compiler
            // should start displaying warnings.
            cp.WarningLevel = 3;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            // Set compiler argument to optimize output.
            cp.CompilerOptions = "/optimize";

            // Set a temporary files collection.
            // The TempFileCollection stores the temporary files
            // generated during a build in the current directory,
            // and does not delete them after compilation.
            cp.TempFiles = new TempFileCollection(".", true);

            //if (provider.Supports(GeneratorSupport.EntryPointMethod))
            //{
            //    // Specify the class that contains
            //    // the main method of the executable.
            //    cp.MainClass = "Samples.Class1";
            //}

            if (Directory.Exists("Resources"))
            {
                if (provider.Supports(GeneratorSupport.Resources))
                {
                    // Set the embedded resource file of the assembly.
                    // This is useful for culture-neutral resources,
                    // or default (fallback) resources.
                    cp.EmbeddedResources.Add("Resources\\Default.resources");

                    // Set the linked resource reference files of the assembly.
                    // These resources are included in separate assembly files,
                    // typically localized for a specific language and culture.
                    cp.LinkedResources.Add("Resources\\nb-no.resources");
                }
            }

            // Invoke compilation.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceFiles.ToArray());

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    exeFile, cr.PathToAssembly);
                retval.Add($"Errors building {exeFile} into {cr.PathToAssembly}");
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    retval.Add(ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                retval.Add("ok");
                Console.WriteLine("Source {0} built into {1} successfully.",
                    exeFile, cr.PathToAssembly);
                Console.WriteLine("{0} temporary files created during the compilation.",
                    cp.TempFiles.Count.ToString());
            }

            //// Return the results of compilation.
            //if (cr.Errors.Count > 0)
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
            return retval;
        }
        public static List<string> CompileCodeFromStrings(CodeDomProvider provider,
          List<string> sourceFiles,
          String exeFile)
        {
            List<string> retval = new List<string>();
            CompilerParameters cp = new CompilerParameters();

            // Generate an executable instead of
            // a class library.
            cp.GenerateExecutable = false;

            // Set the assembly file name to generate.
            cp.OutputAssembly = exeFile;

            // Generate debug information.
            cp.IncludeDebugInformation = false;

            // Add an assembly reference.
            //  cp.ReferencedAssemblies.Add("System.dll");

            // Save the assembly as a physical file.
            cp.GenerateInMemory = false;

            // Set the level at which the compiler
            // should start displaying warnings.
            cp.WarningLevel = 3;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            // Set compiler argument to optimize output.
            cp.CompilerOptions = "/optimize";

            // Set a temporary files collection.
            // The TempFileCollection stores the temporary files
            // generated during a build in the current directory,
            // and does not delete them after compilation.
            cp.TempFiles = new TempFileCollection(".", true);

            //if (provider.Supports(GeneratorSupport.EntryPointMethod))
            //{
            //    // Specify the class that contains
            //    // the main method of the executable.
            //    cp.MainClass = "Samples.Class1";
            //}

            if (Directory.Exists("Resources"))
            {
                if (provider.Supports(GeneratorSupport.Resources))
                {
                    // Set the embedded resource file of the assembly.
                    // This is useful for culture-neutral resources,
                    // or default (fallback) resources.
                    cp.EmbeddedResources.Add("Resources\\Default.resources");

                    // Set the linked resource reference files of the assembly.
                    // These resources are included in separate assembly files,
                    // typically localized for a specific language and culture.
                    cp.LinkedResources.Add("Resources\\nb-no.resources");
                }
            }

            // Invoke compilation.
            CompilerResults cr = provider.CompileAssemblyFromSource(cp, sourceFiles.ToArray());

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    exeFile, cr.PathToAssembly);
                retval.Add($"Errors building {exeFile} into {cr.PathToAssembly}");
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    retval.Add(ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                retval.Add("ok");
                Console.WriteLine("Source {0} built into {1} successfully.",
                    exeFile, cr.PathToAssembly);
                Console.WriteLine("{0} temporary files created during the compilation.",
                    cp.TempFiles.Count.ToString());
            }

            //// Return the results of compilation.
            //if (cr.Errors.Count > 0)
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
            return retval;
        }
    }
}
