using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Abstracts assembly loading operations for NuGet package management.
    /// Implemented by both AssemblyHandler and SharedContextAssemblyHandler
    /// to allow NuGetPackageManager to work with either implementation.
    /// </summary>
    public interface IAssemblyLoadContext
    {
        /// <summary>
        /// Loads a NuGet package from the specified path.
        /// </summary>
        /// <param name="packagePath">The directory path containing the package.</param>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>NuggetInfo containing loaded assembly information, or null if failed.</returns>
        Task<NuggetInfo> LoadPackageAsync(string packagePath, string packageId);

        /// <summary>
        /// Unloads a previously loaded package by its identifier.
        /// </summary>
        /// <param name="packageId">The package identifier to unload.</param>
        /// <returns>True if unloaded successfully; otherwise, false.</returns>
        bool UnloadPackage(string packageId);

        /// <summary>
        /// Gets all currently loaded assemblies.
        /// </summary>
        /// <returns>Collection of loaded assemblies.</returns>
        IEnumerable<Assembly> GetLoadedAssemblies();

        /// <summary>
        /// Checks if a package is currently loaded.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>True if the package is loaded; otherwise, false.</returns>
        bool IsPackageLoaded(string packageId);

        /// <summary>
        /// Gets whether this load context supports shared context mode.
        /// </summary>
        bool SupportsSharedContext { get; }
    }
}
