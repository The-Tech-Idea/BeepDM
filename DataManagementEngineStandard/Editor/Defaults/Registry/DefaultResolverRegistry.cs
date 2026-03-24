using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Attributes;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;

namespace TheTechIdea.Beep.Editor.Defaults.Registry
{
    /// <summary>
    /// Discovers, caches, and auto-registers third-party <see cref="IDefaultValueResolver"/>
    /// plugins that are decorated with <see cref="DefaultResolverAttribute"/>.
    ///
    /// Uses <c>IDMEEditor.assemblyHandler.ConfigEditor.DefaultResolverClasses</c> for
    /// discovery — the same mechanism as <c>[AddinAttribute]</c> connectors,
    /// <c>[PipelinePluginAttribute]</c> ETL plugins, and <c>[FileReaderAttribute]</c>
    /// file-format readers.
    ///
    /// Calling <see cref="Discover"/> also calls <see cref="DefaultsManager.RegisterCustomResolver"/>
    /// for each found resolver so that all existing code that uses
    /// <c>DefaultsManager.Apply(…)</c> picks up the plugin automatically.
    ///
    /// Typical usage:
    /// <code>
    ///   // After assemblyHandler has loaded all plugins:
    ///   var registry = new DefaultResolverRegistry(editor);
    ///   registry.Discover();   // finds all [DefaultResolver(…)]-attributed classes
    ///
    ///   // Look up a descriptor:
    ///   if (registry.TryGet("TenantContextResolver", out var desc))
    ///       Console.WriteLine(desc.Attribute.SupportedTokens);
    /// </code>
    /// </summary>
    public class DefaultResolverRegistry
    {
        private readonly IDMEEditor _editor;
        private readonly Dictionary<string, DefaultResolverDescriptor> _descriptors =
            new Dictionary<string, DefaultResolverDescriptor>(StringComparer.OrdinalIgnoreCase);

        public DefaultResolverRegistry(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        // ── Discovery ────────────────────────────────────────────────────────

        /// <summary>
        /// Scans all assemblies loaded by <c>AssemblyHandler</c> and registers every
        /// concrete class that implements <see cref="IDefaultValueResolver"/> and carries
        /// a <see cref="DefaultResolverAttribute"/>.
        ///
        /// Each discovered resolver is also registered into <see cref="DefaultsManager"/>
        /// (via <c>RegisterCustomResolver</c>) so existing code that calls
        /// <c>DefaultsManager.Apply(…)</c> works without any changes.
        ///
        /// Safe to call multiple times — existing descriptors are updated (re-discovery).
        /// </summary>
        public void Discover()
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var resolverClasses = _editor.assemblyHandler?.ConfigEditor?.DefaultResolverClasses;
                if (resolverClasses == null) return;

                foreach (var cls in resolverClasses)
                {
                    if (cls.type == null || !cls.IsDefaultResolver) continue;

                    var attr = cls.type.GetCustomAttribute<DefaultResolverAttribute>(inherit: false);
                    if (attr == null) continue;

                    _descriptors[attr.ResolverName] = new DefaultResolverDescriptor(attr, cls.type);

                    // Auto-register with DefaultsManager so Apply() picks it up immediately.
                    // Try IDMEEditor ctor first (built-ins), fall back to no-arg ctor (third-party plugins).
                    try
                    {
                        var editorCtor = cls.type.GetConstructor(new[] { typeof(IDMEEditor) });
                        var instance = editorCtor != null
                            ? (IDefaultValueResolver)editorCtor.Invoke(new object[] { _editor })
                            : (IDefaultValueResolver)Activator.CreateInstance(cls.type);
                        if (instance != null)
                            DefaultsManager.RegisterCustomResolver(_editor, instance);
                    }
                    catch (Exception ex)
                    {
                        _editor.AddLogMessage(
                            nameof(Discover),
                            $"Could not instantiate IDefaultValueResolver '{cls.type.FullName}': {ex.Message}",
                            DateTime.Now, -1, null, Errors.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.ErrorObject.Flag = Errors.Failed;
                _editor.AddLogMessage(
                    nameof(Discover),
                    $"DefaultResolverRegistry.Discover failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        // ── Query API ────────────────────────────────────────────────────────

        /// <summary>Returns a read-only view of all discovered resolver descriptors,
        /// keyed by <see cref="DefaultResolverAttribute.ResolverName"/>.</summary>
        public IReadOnlyDictionary<string, DefaultResolverDescriptor> GetDescriptors() =>
            _descriptors;

        /// <summary>
        /// Attempts to find a descriptor by <paramref name="resolverName"/> (case-insensitive).
        /// Returns <c>true</c> and populates <paramref name="descriptor"/> on success.
        /// </summary>
        public bool TryGet(string resolverName, out DefaultResolverDescriptor descriptor) =>
            _descriptors.TryGetValue(resolverName, out descriptor);

        /// <summary>Creates a new instance of the resolver identified by
        /// <paramref name="resolverName"/>, or returns <c>null</c> if not found.</summary>
        public IDefaultValueResolver Create(string resolverName)
        {
            if (!TryGet(resolverName, out var desc)) return null;
            try
            {
                // Try IDMEEditor ctor first (built-ins), fall back to no-arg ctor (third-party plugins).
                var editorCtor = desc.ImplementationType.GetConstructor(new[] { typeof(IDMEEditor) });
                return editorCtor != null
                    ? (IDefaultValueResolver)editorCtor.Invoke(new object[] { _editor })
                    : (IDefaultValueResolver)Activator.CreateInstance(desc.ImplementationType);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(Create),
                    $"Failed to create '{resolverName}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Warning);
                return null;
            }
        }
    }
}
