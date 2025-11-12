using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Loader extension that scans assemblies for BeepShell command and workflow extensions.
    /// Integrates with AssemblyHandler's ScanExtensions mechanism.
    /// Stores INSTANCES of discovered commands/workflows/extensions, not just types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Extension scanning requires reflection to discover types at runtime")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Extension system requires dynamic type creation")]
    public class ShellExtensionScanner : ILoaderExtention
    {
        private readonly IAssemblyHandler _assemblyHandler;
        private readonly List<IShellCommand> _commands = new();
        private readonly List<IShellWorkflow> _workflows = new();
        private readonly List<IShellExtension> _extensions = new();

        public ShellExtensionScanner(IAssemblyHandler assemblyHandler)
        {
            _assemblyHandler = assemblyHandler;
        }

        /// <summary>
        /// Command instances discovered during scanning
        /// </summary>
        public List<IShellCommand> Commands => _commands;

        /// <summary>
        /// Workflow instances discovered during scanning
        /// </summary>
        public List<IShellWorkflow> Workflows => _workflows;

        /// <summary>
        /// Extension provider instances discovered during scanning
        /// </summary>
        public List<IShellExtension> Extensions => _extensions;

        /// <summary>
        /// Load all assemblies - delegates to AssemblyHandler
        /// </summary>
        public IErrorsInfo LoadAllAssembly()
        {
            // This is called by AssemblyHandler, no-op here
            return _assemblyHandler.ErrorObject;
        }

        /// <summary>
        /// Scan all loaded assemblies
        /// </summary>
        public IErrorsInfo Scan()
        {
            try
            {
                foreach (var assembly in _assemblyHandler.LoadedAssemblies)
                {
                   // Scan(assembly);
                }
                return _assemblyHandler.ErrorObject;
            }
            catch (Exception ex)
            {
                _assemblyHandler.Logger?.WriteLog($"Error scanning assemblies: {ex.Message}");
                _assemblyHandler.ErrorObject.Flag = Errors.Failed;
                _assemblyHandler.ErrorObject.Message = ex.Message;
                return _assemblyHandler.ErrorObject;
            }
        }

        /// <summary>
        /// Scan from assemblies_rep metadata
        /// </summary>
        public IErrorsInfo Scan(assemblies_rep assembly)
        {
            if (assembly?.DllLib != null)
            {
                return Scan(assembly.DllLib);
            }
            return _assemblyHandler.ErrorObject;
        }

        /// <summary>
        /// Scan an assembly for shell extensions and CREATE INSTANCES
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Extension scanning requires reflection")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Extension system requires dynamic creation")]
        public IErrorsInfo Scan(Assembly assembly)
        {
            if (assembly == null)
                return _assemblyHandler.ErrorObject;

            try
            {
                // Skip system assemblies
                if (assembly.FullName?.StartsWith("System") == true || 
                    assembly.FullName?.StartsWith("Microsoft") == true ||
                    assembly.FullName?.StartsWith("netstandard") == true)
                    return _assemblyHandler.ErrorObject;

                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // Skip abstract classes and interfaces
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    try
                    {
                        // Check for IShellCommand implementations
                        if (typeof(IShellCommand).IsAssignableFrom(type))
                        {
                            // Check if this type is already discovered (prevent duplicates)
                            if (_commands.Any(c => c.GetType() == type))
                            {
                                _assemblyHandler.Logger?.WriteLog($"Skipping duplicate shell command: {type.FullName}");
                                continue;
                            }
                            
                            var command = (IShellCommand)Activator.CreateInstance(type)!;
                            _commands.Add(command);
                            _assemblyHandler.Logger?.WriteLog($"Discovered shell command: {type.FullName}");
                        }

                        // Check for IShellWorkflow implementations
                        if (typeof(IShellWorkflow).IsAssignableFrom(type))
                        {
                            // Check if this type is already discovered (prevent duplicates)
                            if (_workflows.Any(w => w.GetType() == type))
                            {
                                _assemblyHandler.Logger?.WriteLog($"Skipping duplicate shell workflow: {type.FullName}");
                                continue;
                            }
                            
                            var workflow = (IShellWorkflow)Activator.CreateInstance(type)!;
                            _workflows.Add(workflow);
                            _assemblyHandler.Logger?.WriteLog($"Discovered shell workflow: {type.FullName}");
                        }

                        // Check for IShellExtension implementations
                        if (typeof(IShellExtension).IsAssignableFrom(type))
                        {
                            // Check if this type is already discovered (prevent duplicates)
                            if (_extensions.Any(e => e.GetType() == type))
                            {
                                _assemblyHandler.Logger?.WriteLog($"Skipping duplicate shell extension provider: {type.FullName}");
                                continue;
                            }
                            
                            var extension = (IShellExtension)Activator.CreateInstance(type)!;
                            _extensions.Add(extension);
                            _assemblyHandler.Logger?.WriteLog($"Discovered shell extension provider: {type.FullName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _assemblyHandler.Logger?.WriteLog($"Error creating instance of {type.FullName}: {ex.Message}");
                    }
                }
                
                return _assemblyHandler.ErrorObject;
            }
            catch (ReflectionTypeLoadException ex)
            {
                _assemblyHandler.Logger?.WriteLog($"Error scanning assembly {assembly.FullName}: {ex.Message}");
                // Log loader exceptions for debugging
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    _assemblyHandler.Logger?.WriteLog($"  Loader exception: {loaderEx?.Message}");
                }
                _assemblyHandler.ErrorObject.Flag = Errors.Failed;
                _assemblyHandler.ErrorObject.Message = ex.Message;
                return _assemblyHandler.ErrorObject;
            }
            catch (Exception ex)
            {
                _assemblyHandler.Logger?.WriteLog($"Error scanning assembly {assembly.FullName}: {ex.Message}");
                _assemblyHandler.ErrorObject.Flag = Errors.Failed;
                _assemblyHandler.ErrorObject.Message = ex.Message;
                return _assemblyHandler.ErrorObject;
            }
        }

        /// <summary>
        /// Get all discovered command, workflow, and extension INSTANCES
        /// </summary>
        public ExtensionScanResult GetResults()
        {
            return new ExtensionScanResult
            {
                Commands = _commands.ToList(),
                Workflows = _workflows.ToList(),
                Extensions = _extensions.ToList()
            };
        }

        /// <summary>
        /// Clear all discovered instances
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
            _workflows.Clear();
            _extensions.Clear();
        }
    }

    /// <summary>
    /// Results from extension scanning - contains INSTANCES not types
    /// </summary>
    public class ExtensionScanResult
    {
        public List<IShellCommand> Commands { get; set; } = new();
        public List<IShellWorkflow> Workflows { get; set; } = new();
        public List<IShellExtension> Extensions { get; set; } = new();

        public int TotalCount => Commands.Count + Workflows.Count + Extensions.Count;
    }
}
