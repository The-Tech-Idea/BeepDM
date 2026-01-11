using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.IO;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for DLL creation and compilation functionality
    /// </summary>
    public partial class ClassCreator
    {
        #region DLL Creation Methods

        /// <summary>
        /// Creates a DLL from a collection of entities
        /// </summary>
        /// <param name="dllname">The name of the DLL to create</param>
        /// <param name="entities">The entities to include</param>
        /// <param name="outputpath">The output path</param>
        /// <param name="progress">Progress reporting interface</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="nameSpacestring">The namespace to use</param>
        /// <param name="generateCSharpCodeFiles">Whether to generate C# code files</param>
        /// <returns>Success message or error details</returns>
        public string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, 
            IProgress<PassedArgs> progress, CancellationToken token, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true)
        {
            if (string.IsNullOrWhiteSpace(dllname))
                throw new ArgumentException("DLL name cannot be null or empty", nameof(dllname));
            
            if (entities == null || entities.Count == 0)
                throw new ArgumentException("Entities list cannot be null or empty", nameof(entities));

            var listOfPaths = new List<string>();
            int currentIndex = 1;
            int totalCount = entities.Count;

            try
            {
                LogMessage($"Starting DLL creation for {totalCount} entities");
                
                // Ensure output directory exists
                outputpath = EnsureOutputDirectory(outputpath);

                // Generate class files for each entity
                foreach (var entity in entities)
                {
                    // Check for cancellation
                    if (token.IsCancellationRequested)
                    {
                        LogMessage("DLL creation cancelled by user", Errors.Failed);
                        return "Operation cancelled";
                    }

                    try
                    {
                        // Validate entity before processing
                        var validationErrors = ValidateEntityStructure(entity);
                        if (validationErrors.Any())
                        {
                            var errorMessage = $"Validation failed for entity {entity.EntityName}: {string.Join(", ", validationErrors)}";
                            LogMessage(errorMessage, Errors.Failed);
                            
                            ReportProgress(progress, $"Validation failed for {entity.EntityName}", 
                                "Error", currentIndex, totalCount, errorMessage);
                            continue;
                        }

                        // Generate class file
                        var classFilePath = Path.Combine(outputpath, entity.EntityName + ".cs");
                        CreateClass(entity.EntityName, entity, outputpath, nameSpacestring, generateCSharpCodeFiles);
                        
                        listOfPaths.Add(classFilePath);

                        // Report progress
                        ReportProgress(progress, $"Created class {entity.EntityName}", 
                            "Update", currentIndex, totalCount);

                        LogMessage($"Successfully created class for entity: {entity.EntityName}");
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Error creating class for {entity.EntityName}: {ex.Message}";
                        LogMessage(errorMessage, Errors.Failed);
                        
                        ReportProgress(progress, $"Error creating class for {entity.EntityName}", 
                            "Error", currentIndex, totalCount, ex.Message);
                    }

                    currentIndex++;
                }

                // Check if we have any files to compile
                if (!listOfPaths.Any())
                {
                    var errorMessage = "No valid class files were generated";
                    LogMessage(errorMessage, Errors.Failed);
                    return errorMessage;
                }

                // Set output file name
                outputFileName = dllname + ".dll";
                var dllPath = Path.Combine(outputpath, outputFileName);

                // Report compilation progress
                ReportProgress(progress, "Creating DLL", "Update", currentIndex, totalCount);

                // Compile to DLL
                LogMessage($"Compiling {listOfPaths.Count} files to DLL: {dllPath}");
                
                if (!RoslynCompiler.CompileCodeToDLL(listOfPaths, dllPath))
                {
                    var errorMessage = "Error compiling code to DLL";
                    LogMessage(errorMessage, Errors.Failed);
                    return errorMessage;
                }

                LogMessage($"Successfully created DLL: {dllPath}");
                return "ok";
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error during DLL creation: {ex.Message}";
                LogMessage(errorMessage, Errors.Failed);
                return errorMessage;
            }
        }

        /// <summary>
        /// Creates a DLL from existing C# files in a directory
        /// </summary>
        /// <param name="dllname">The name of the DLL to create</param>
        /// <param name="filepath">The path containing C# files</param>
        /// <param name="outputpath">The output path for the DLL</param>
        /// <param name="progress">Progress reporting interface</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="nameSpacestring">The namespace to use</param>
        /// <returns>Success message or error details</returns>
        public string CreateDLLFromFilesPath(string dllname, string filepath, string outputpath, 
            IProgress<PassedArgs> progress, CancellationToken token, 
            string nameSpacestring = "TheTechIdea.ProjectClasses")
        {
            if (string.IsNullOrWhiteSpace(dllname))
                throw new ArgumentException("DLL name cannot be null or empty", nameof(dllname));
            
            if (string.IsNullOrWhiteSpace(filepath) || !Directory.Exists(filepath))
                throw new ArgumentException("File path must be a valid directory", nameof(filepath));

            var listOfPaths = new List<string>();
            int currentIndex = 1;

            try
            {
                // Get all C# files in the directory
                var csharpFiles = Directory.GetFiles(filepath, "*.cs", SearchOption.TopDirectoryOnly);
                int totalCount = csharpFiles.Length;

                if (totalCount == 0)
                {
                    var errorMessage = $"No C# files found in directory: {filepath}";
                    LogMessage(errorMessage, Errors.Failed);
                    return errorMessage;
                }

                LogMessage($"Found {totalCount} C# files to compile");

                // Ensure output directory exists
                outputpath = EnsureOutputDirectory(outputpath);

                // Process each C# file
                foreach (var file in csharpFiles)
                {
                    // Check for cancellation
                    if (token.IsCancellationRequested)
                    {
                        LogMessage("DLL creation cancelled by user", Errors.Failed);
                        return "Operation cancelled";
                    }

                    try
                    {
                        // Validate file exists and is readable
                        if (!File.Exists(file))
                        {
                            LogMessage($"File not found: {file}", Errors.Failed);
                            continue;
                        }

                        listOfPaths.Add(file);

                        // Report progress
                        var fileName = Path.GetFileName(file);
                        ReportProgress(progress, $"Added file {fileName}", "Update", currentIndex, totalCount);

                        LogMessage($"Added file to compilation list: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Error processing file {file}: {ex.Message}";
                        LogMessage(errorMessage, Errors.Failed);
                        
                        ReportProgress(progress, $"Error processing {Path.GetFileName(file)}", 
                            "Error", currentIndex, totalCount, ex.Message);
                    }

                    currentIndex++;
                }

                // Check if we have any files to compile
                if (!listOfPaths.Any())
                {
                    var errorMessage = "No valid C# files were found for compilation";
                    LogMessage(errorMessage, Errors.Failed);
                    return errorMessage;
                }

                // Set output file name
                outputFileName = dllname + ".dll";
                var dllPath = Path.Combine(outputpath, outputFileName);

                // Report compilation progress
                ReportProgress(progress, "Creating DLL", "Update", currentIndex, totalCount);

                // Compile to DLL
                LogMessage($"Compiling {listOfPaths.Count} files to DLL: {dllPath}");
                
                if (!RoslynCompiler.CompileCodeToDLL(listOfPaths, dllPath))
                {
                    var errorMessage = "Error compiling files to DLL";
                    LogMessage(errorMessage, Errors.Failed);
                    return errorMessage;
                }

                LogMessage($"Successfully created DLL from files: {dllPath}");
                return "ok";
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error during DLL creation from files: {ex.Message}";
                LogMessage(errorMessage, Errors.Failed);
                return errorMessage;
            }
        }

        #endregion

        #region Progress Reporting

        /// <summary>
        /// Reports progress to the provided progress interface
        /// </summary>
        /// <param name="progress">The progress reporting interface</param>
        /// <param name="message">The progress message</param>
        /// <param name="eventType">The event type</param>
        /// <param name="current">Current item index</param>
        /// <param name="total">Total items count</param>
        /// <param name="errorMessage">Optional error message</param>
        private void ReportProgress(IProgress<PassedArgs> progress, string message, string eventType, 
            int current, int total, string errorMessage = null)
        {
            if (progress == null) return;

            var args = new PassedArgs
            {
                ParameterString1 = message,
                EventType = eventType,
                ParameterInt1 = current,
                ParameterInt2 = total,
                Messege = errorMessage
            };

            progress.Report(args);
        }

        #endregion
    }
}