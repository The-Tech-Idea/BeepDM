using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Cross-platform reflection-based discovery and registration of Beep add-in
    /// views (<see cref="IDM_Addin"/>) and view models (<see cref="IBeepViewModel"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions live in the cross-platform <c>DataManagementEngineStandard</c>
    /// assembly and carry no Windows / WinForms dependency. They can be used from any
    /// host (Desktop / Web / Blazor / Console on any OS) and are also reachable from the
    /// fluent builder via <see cref="IBeepServiceBuilder.WithViewDiscovery"/>.
    /// </para>
    /// <para>
    /// Discovered types are registered with keyed DI so they can be resolved by their
    /// C# type name - matching the existing add-in lookup convention. A single failing
    /// type never aborts the whole scan: the error is logged and the next type is tried.
    /// </para>
    /// </remarks>
    public static class BeepServiceAddinDiscoveryExtensions
    {
        /// <summary>
        /// Delegate invoked when additional view models are being registered.
        /// Receives the service collection so subscribers can register extra types.
        /// </summary>
        /// <param name="services">The service collection add-ins are being registered into.</param>
        public delegate void RegisterBeepViewModelEventHandler(IServiceCollection services);

        /// <summary>
        /// Delegate invoked when additional add-in views are being registered.
        /// Receives the service collection so subscribers can register extra types.
        /// </summary>
        /// <param name="services">The service collection views are being registered into.</param>
        public delegate void RegisterAddinEventHandler(IServiceCollection services);

        /// <summary>
        /// Raised from <see cref="AddBeepViewModels"/> after auto-discovery completes,
        /// so subscribers can register further view models imperatively. This event was
        /// previously declared (but never raised) on the desktop-only
        /// <c>BeepDesktopServices</c>; it now fires here for every host.
        /// </summary>
        public static event RegisterBeepViewModelEventHandler OnRegisterViewModels;

        /// <summary>
        /// Raised from <see cref="AddBeepViews"/> after auto-discovery completes,
        /// so subscribers can register further add-ins imperatively. This event was
        /// previously declared (but never raised) on the desktop-only
        /// <c>BeepDesktopServices</c>; it now fires here for every host.
        /// </summary>
        public static event RegisterAddinEventHandler OnRegisterAddins;

        /// <summary>
        /// Discovers concrete (non-abstract, non-interface) <see cref="IBeepViewModel"/>
        /// types across the supplied assemblies - or all relevant loaded assemblies when
        /// <paramref name="assembliesToScan"/> is <c>null</c> - and registers each as a
        /// keyed transient keyed on its type name.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="assembliesToScan">
        /// Optional explicit set of assemblies to scan. When <c>null</c>, the scanner
        /// inspects the executing / calling / entry assemblies, the
        /// <see cref="DependencyContext"/> runtime libraries, and every assembly already
        /// loaded into the current <see cref="AppDomain"/> (system assemblies excluded).
        /// </param>
        /// <param name="logger">
        /// Optional logger that receives per-type failures. When <c>null</c>, failures are
        /// written to <see cref="Debug"/>.
        /// </param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBeepViewModels(
            this IServiceCollection services,
            IEnumerable<Assembly> assembliesToScan = null,
            IDMLogger logger = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var assemblies = assembliesToScan?.ToList() ?? BeepAssemblyScanner.GetRelevantAssemblies();

            RegisterViewModels(services, assemblies, logger);
            return services;
        }

        /// <summary>
        /// Discovers concrete <see cref="IBeepViewModel"/> types using the assemblies a
        /// <see cref="IAssemblyHandler"/> has loaded. Prefer this overload in hosts that
        /// have already called <c>IBeepService.LoadAssemblies()</c> (directly or via the
        /// fluent <c>.WithAssemblyLoading()</c>): it scans exactly what the handler loaded
        /// and is robust against isolated <c>AssemblyLoadContext</c>s where loaded
        /// assemblies may not appear in <see cref="AppDomain.CurrentDomain"/>.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="loader">
        /// The assembly handler whose <see cref="IAssemblyHandler.LoadedAssemblies"/> should
        /// be scanned. When <c>null</c>, falls back to a full AppDomain scan.
        /// </param>
        /// <param name="logger">Optional logger that receives per-type failures.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBeepViewModels(
            this IServiceCollection services,
            IAssemblyHandler loader,
            IDMLogger logger = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var assemblies = loader?.LoadedAssemblies?.ToList()
                             ?? BeepAssemblyScanner.GetRelevantAssemblies();

            RegisterViewModels(services, assemblies, logger);
            return services;
        }

        /// <summary>
        /// Shared worker for the <see cref="AddBeepViewModels(IServiceCollection, IEnumerable{Assembly}, IDMLogger)"/>
        /// overloads. Performs the reflection scan and the keyed-transient registrations.
        /// </summary>
        private static void RegisterViewModels(
            IServiceCollection services,
            List<Assembly> assemblies,
            IDMLogger logger)
        {
            var viewModelTypes = assemblies
                .SelectMany(BeepAssemblyScanner.GetTypesFromAssembly)
                .Where(t => typeof(IBeepViewModel).IsAssignableFrom(t)
                            && !t.IsInterface
                            && !t.IsAbstract)
                .ToList();

            foreach (var viewModelType in viewModelTypes)
            {
                // Capture for closure. Per-type try/catch keeps one bad VM from failing the whole scan.
                var type = viewModelType;
                try
                {
                    services.AddKeyedTransient<IBeepViewModel>(type.Name, (serviceProvider, key) =>
                        (IBeepViewModel)ActivatorUtilities.CreateInstance(serviceProvider, type));
                }
                catch (Exception ex)
                {
                    LogAddinError(logger, $"Error registering view model '{type.Name}': {ex.Message}");
                }
            }

            // Fire the hook so callers can register additional VMs imperatively.
            try
            {
                OnRegisterViewModels?.Invoke(services);
            }
            catch (Exception ex)
            {
                LogAddinError(logger, $"OnRegisterViewModels subscriber threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Discovers <see cref="IDM_Addin"/> types decorated with
        /// <see cref="AddinAttribute"/> across the supplied assemblies - or all relevant
        /// loaded assemblies when <paramref name="assembliesToScan"/> is <c>null</c> -
        /// and registers each one with a lifetime driven by
        /// <see cref="AddinAttribute.ScopeCreateType"/>:
        /// <see cref="AddinScopeCreateType.Single"/> becomes singleton (plus a keyed
        /// singleton on the type name); anything else becomes transient (plus a keyed
        /// transient).
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="assembliesToScan">
        /// Optional explicit set of assemblies to scan. When <c>null</c>, the scanner
        /// inspects the executing / calling / entry assemblies, the
        /// <see cref="DependencyContext"/> runtime libraries, and every assembly already
        /// loaded into the current <see cref="AppDomain"/> (system assemblies excluded).
        /// </param>
        /// <param name="logger">
        /// Optional logger that receives per-type failures. When <c>null</c>, failures are
        /// written to <see cref="Debug"/>.
        /// </param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBeepViews(
            this IServiceCollection services,
            IEnumerable<Assembly> assembliesToScan = null,
            IDMLogger logger = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var assemblies = assembliesToScan?.ToList() ?? BeepAssemblyScanner.GetRelevantAssemblies();

            RegisterViews(services, assemblies, logger);
            return services;
        }

        /// <summary>
        /// Discovers <see cref="IDM_Addin"/> views using the assemblies a
        /// <see cref="IAssemblyHandler"/> has loaded. Prefer this overload in hosts that
        /// have already called <c>IBeepService.LoadAssemblies()</c> (directly or via the
        /// fluent <c>.WithAssemblyLoading()</c>): it scans exactly what the handler loaded
        /// and is robust against isolated <c>AssemblyLoadContext</c>s.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="loader">
        /// The assembly handler whose <see cref="IAssemblyHandler.LoadedAssemblies"/> should
        /// be scanned. When <c>null</c>, falls back to a full AppDomain scan.
        /// </param>
        /// <param name="logger">Optional logger that receives per-type failures.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBeepViews(
            this IServiceCollection services,
            IAssemblyHandler loader,
            IDMLogger logger = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var assemblies = loader?.LoadedAssemblies?.ToList()
                             ?? BeepAssemblyScanner.GetRelevantAssemblies();

            RegisterViews(services, assemblies, logger);
            return services;
        }

        /// <summary>
        /// Shared worker for the <see cref="AddBeepViews"/> overloads.
        /// </summary>
        private static void RegisterViews(
            IServiceCollection services,
            List<Assembly> assemblies,
            IDMLogger logger)
        {
            var viewTypes = assemblies
                .SelectMany(BeepAssemblyScanner.GetTypesFromAssembly)
                .Where(t => typeof(IDM_Addin).IsAssignableFrom(t) && !t.IsInterface)
                .ToList();

            foreach (var viewType in viewTypes)
            {
                var addinAttribute = viewType.GetCustomAttribute<AddinAttribute>();
                if (addinAttribute == null)
                    continue;

                var addinName = viewType.Name;

                if (addinAttribute.ScopeCreateType == AddinScopeCreateType.Single)
                {
                    try
                    {
                        services.AddSingleton(viewType);
                        services.AddKeyedSingleton<IDM_Addin>(addinName, (serviceProvider, key) =>
                            (IDM_Addin)serviceProvider.GetRequiredService(viewType));
                    }
                    catch (Exception ex)
                    {
                        LogAddinError(logger, $"Error registering view '{addinName}': {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        services.AddTransient(viewType);
                        services.AddKeyedTransient<IDM_Addin>(addinName, (serviceProvider, key) =>
                            (IDM_Addin)serviceProvider.GetRequiredService(viewType));
                    }
                    catch (Exception ex)
                    {
                        LogAddinError(logger, $"Error registering view '{addinName}': {ex.Message}");
                    }
                }
            }

            // Fire the hook so callers can register additional add-ins imperatively.
            try
            {
                OnRegisterAddins?.Invoke(services);
            }
            catch (Exception ex)
            {
                LogAddinError(logger, $"OnRegisterAddins subscriber threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers a single add-in view. Inspects its <see cref="AddinAttribute"/> and
        /// uses a singleton lifetime when <see cref="AddinAttribute.ScopeCreateType"/> is
        /// <see cref="AddinScopeCreateType.Single"/>, otherwise transient. Each registration
        /// is also keyed on the type name against <see cref="IDM_Addin"/>.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="addin">The add-in instance whose type should be registered.</param>
        /// <param name="logger">Optional logger that receives registration failures.</param>
        public static void AddBeepView(
            this IServiceCollection services,
            IDM_Addin addin,
            IDMLogger logger = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (addin == null)
                throw new ArgumentNullException(nameof(addin));

            try
            {
                var viewType = addin.GetType();
                var addinName = viewType.Name;
                var addinAttribute = viewType.GetCustomAttribute<AddinAttribute>();
                if (addinAttribute == null)
                    return;

                if (addinAttribute.ScopeCreateType == AddinScopeCreateType.Single)
                {
                    services.AddSingleton(viewType);
                    services.AddKeyedSingleton<IDM_Addin>(addinName, (serviceProvider, key) =>
                        (IDM_Addin)serviceProvider.GetRequiredService(viewType));
                }
                else
                {
                    services.AddTransient(viewType);
                    services.AddKeyedTransient<IDM_Addin>(addinName, (serviceProvider, key) =>
                        (IDM_Addin)serviceProvider.GetRequiredService(viewType));
                }
            }
            catch (Exception ex)
            {
                LogAddinError(logger, $"Error registering additional view: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers a single view model by its concrete type as a keyed transient keyed
        /// on the type name. The supplied <paramref name="viewModel"/> instance is used
        /// only to obtain the type; resolution still goes through the DI container.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="viewModel">The view model whose type should be registered.</param>
        /// <param name="logger">Optional logger that receives registration failures.</param>
        public static void AddBeepViewModel(
            this IServiceCollection services,
            IBeepViewModel viewModel,
            IDMLogger logger = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (viewModel == null)
                return;

            try
            {
                var viewModelType = viewModel.GetType();
                var type = viewModelType;
                services.AddKeyedTransient<IBeepViewModel>(viewModelType.Name, (serviceProvider, key) =>
                    (IBeepViewModel)ActivatorUtilities.CreateInstance(serviceProvider, type));
            }
            catch (Exception ex)
            {
                LogAddinError(logger, $"Error adding view model: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes an add-in discovery error to the supplied logger when present,
        /// otherwise to <see cref="Debug"/>.
        /// </summary>
        private static void LogAddinError(IDMLogger logger, string message)
        {
            if (logger != null)
            {
                logger.LogError(message);
            }
            else
            {
                Debug.WriteLine($"[BeepServiceAddinDiscovery] {message}");
            }
        }
    }

    /// <summary>
    /// Assembly scanning helpers used by the add-in / view-model discovery extensions.
    /// Centralised here so the discovery surface and the scanning strategy evolve together,
    /// and so the original desktop behaviour is preserved exactly when the helpers moved
    /// out of <c>BeepDesktopServices</c>.
    /// </summary>
    internal static class BeepAssemblyScanner
    {
        // Prefixes for assemblies that are never interesting for Beep add-in discovery.
        // Note: WPF assemblies (WindowsBase / PresentationCore / PresentationFramework)
        // are intentionally listed here so a WPF host does not try to instantiate XAML types.
        private static readonly string[] SystemPrefixes =
        {
            "System",
            "Microsoft",
            "mscorlib",
            "netstandard",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework"
        };

        /// <summary>
        /// Collects the executing, calling and entry assemblies, every non-system runtime
        /// library from <see cref="DependencyContext.Default"/>, and every assembly
        /// already loaded into the current <see cref="AppDomain"/>. Duplicates are removed.
        /// </summary>
        public static List<Assembly> GetRelevantAssemblies()
        {
            var assemblies = new List<Assembly>();

            // Core assemblies.
            var coreAssemblies = new[]
            {
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly(),
                Assembly.GetEntryAssembly()
            }.Where(a => a != null);

            assemblies.AddRange(coreAssemblies);

            try
            {
                // Assemblies declared in the dependency graph.
                var dependencyAssemblies = DependencyContext.Default?.RuntimeLibraries
                    .SelectMany(lib => lib.GetDefaultAssemblyNames(DependencyContext.Default))
                    .Where(name => !IsSystemAssembly(name.Name))
                    .Select(name =>
                    {
                        try { return Assembly.Load(name); }
                        catch { return null; }
                    })
                    .Where(a => a != null);

                if (dependencyAssemblies != null)
                {
                    assemblies.AddRange(dependencyAssemblies);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading dependency assemblies: {ex.Message}");
            }

            // Assemblies already loaded into the AppDomain at this point.
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !IsSystemAssembly(a.FullName));

            assemblies.AddRange(loadedAssemblies);

            return assemblies.Distinct().ToList();
        }

        /// <summary>
        /// Returns the types defined by <paramref name="assembly"/>. A
        /// <see cref="ReflectionTypeLoadException"/> is swallowed and the partial set of
        /// types it managed to surface is returned; any other exception yields an empty
        /// sequence so scanning continues with the next assembly.
        /// </summary>
        public static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return Enumerable.Empty<Type>();

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="assemblyName"/> starts with one of the
        /// well-known system / framework prefixes (case-insensitive), or is empty.
        /// </summary>
        public static bool IsSystemAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return true;

            return SystemPrefixes.Any(prefix =>
                assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
