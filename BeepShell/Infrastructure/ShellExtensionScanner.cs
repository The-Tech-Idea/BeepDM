using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Tools;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Loader extension that scans assemblies for BeepShell command and workflow extensions.
    /// Integrates with AssemblyHandler's ScanExtensions mechanism.
    /// </summary>
    public class ShellExtensionScanner : ILoaderExtention
    {
        private readonly IAssemblyHandler _assemblyHandler;
        private readonly List<Type> _commandTypes = new();
        private readonly List<Type> _workflowTypes = new();
        private readonly List<Type> _extensionTypes = new();

        public ShellExtensionScanner(IAssemblyHandler assemblyHandler)
        {
            _assemblyHandler = assemblyHandler;
        }

        /// <summary>
        /// Commands discovered during scanning
        /// </summary>
        public List<Type> CommandTypes => _commandTypes;

        /// <summary>
        /// Workflows discovered during scanning
        /// </summary>
        public List<Type> WorkflowTypes => _workflowTypes;

        /// <summary>
        /// Extension providers discovered during scanning
        /// </summary>
        public List<Type> ExtensionTypes => _extensionTypes;

        /// <summary>
        /// Scan an assembly for shell extensions
        /// </summary>
        public void Scan(Assembly assembly)
        {
            if (assembly == null)
                return;

            try
            {
                // Skip system assemblies
                if (assembly.FullName.StartsWith("System") || 
                    assembly.FullName.StartsWith("Microsoft") ||
                    assembly.FullName.StartsWith("netstandard"))
                    return;

                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // Skip abstract classes and interfaces
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    // Check for IShellCommand implementations
                    if (typeof(IShellCommand).IsAssignableFrom(type))
                    {
                        _commandTypes.Add(type);
                        _assemblyHandler.Logger?.WriteLog($"Discovered shell command: {type.FullName}");
                    }

                    // Check for IShellWorkflow implementations
                    if (typeof(IShellWorkflow).IsAssignableFrom(type))
                    {
                        _workflowTypes.Add(type);
                        _assemblyHandler.Logger?.WriteLog($"Discovered shell workflow: {type.FullName}");
                    }

                    // Check for IShellExtension implementations
                    if (typeof(IShellExtension).IsAssignableFrom(type))
                    {
                        _extensionTypes.Add(type);
                        _assemblyHandler.Logger?.WriteLog($"Discovered shell extension provider: {type.FullName}");
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _assemblyHandler.Logger?.WriteLog($"Error scanning assembly {assembly.FullName}: {ex.Message}");
                // Log loader exceptions for debugging
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    _assemblyHandler.Logger?.WriteLog($"  Loader exception: {loaderEx?.Message}");
                }
            }
            catch (Exception ex)
            {
                _assemblyHandler.Logger?.WriteLog($"Error scanning assembly {assembly.FullName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all discovered commands, workflows, and extensions
        /// </summary>
        public ExtensionScanResult GetResults()
        {
            return new ExtensionScanResult
            {
                Commands = _commandTypes.ToList(),
                Workflows = _workflowTypes.ToList(),
                Extensions = _extensionTypes.ToList()
            };
        }

        /// <summary>
        /// Clear all discovered types
        /// </summary>
        public void Clear()
        {
            _commandTypes.Clear();
            _workflowTypes.Clear();
            _extensionTypes.Clear();
        }
    }

    /// <summary>
    /// Results from extension scanning
    /// </summary>
    public class ExtensionScanResult
    {
        public List<Type> Commands { get; set; } = new();
        public List<Type> Workflows { get; set; } = new();
        public List<Type> Extensions { get; set; } = new();

        public int TotalCount => Commands.Count + Workflows.Count + Extensions.Count;
    }
}
