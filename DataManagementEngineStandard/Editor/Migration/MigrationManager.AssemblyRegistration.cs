using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        #region Assembly Registration

        /// <summary>
        /// Register an additional assembly for entity type discovery.
        /// Use this when entity classes live in separate projects/DLLs that may not be
        /// automatically found by AppDomain scanning (e.g., lazily-loaded assemblies).
        /// </summary>
        public void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) return;
            lock (_assemblyLock)
            {
                if (_registeredAssemblies.Add(assembly))
                {
                    _editor?.AddLogMessage("Beep",
                        $"MigrationManager: Registered assembly '{assembly.GetName().Name}' for entity discovery",
                        DateTime.Now, 0, null, Errors.Ok);
                }
            }
        }

        /// <summary>
        /// Register multiple assemblies for entity type discovery.
        /// </summary>
        public void RegisterAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return;
            foreach (var assembly in assemblies)
            {
                RegisterAssembly(assembly);
            }
        }

        /// <summary>
        /// Gets all currently registered assemblies (manual + auto-discovered).
        /// Useful for diagnostics when entity types are not being found.
        /// </summary>
        public IReadOnlyList<Assembly> GetRegisteredAssemblies()
        {
            lock (_assemblyLock)
            {
                return _registeredAssemblies.ToList().AsReadOnly();
            }
        }

        #endregion
    }
}
