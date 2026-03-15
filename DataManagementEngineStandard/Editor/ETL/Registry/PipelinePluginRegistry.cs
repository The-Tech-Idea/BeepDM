using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;

namespace TheTechIdea.Beep.Pipelines.Registry
{
    /// <summary>
    /// Discovers, caches, and instantiates pipeline plugins.
    /// Uses <c>IDMEEditor.assemblyHandler</c> for discovery — the same mechanism
    /// as <c>[AddinAttribute]</c> connectors already used throughout BeepDM.
    ///
    /// Typical usage:
    /// <code>
    ///   var registry = new PipelinePluginRegistry(editor);
    ///   registry.Discover();                              // scan loaded assemblies
    ///   var source = registry.Create&lt;IPipelineSource&gt;(PipelineConstants.PluginIds.DbSource);
    /// </code>
    /// </summary>
    public class PipelinePluginRegistry
    {
        private readonly IDMEEditor _editor;
        private readonly Dictionary<string, PipelinePluginDescriptor> _descriptors = new();

        public PipelinePluginRegistry(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Scans all assemblies loaded by <c>AssemblyHandler</c> and registers every
        /// concrete class that implements <see cref="IPipelinePlugin"/> and carries
        /// a <see cref="PipelinePluginAttribute"/>.
        ///
        /// Safe to call multiple times — existing descriptors are updated (re-discovery).
        /// </summary>
        public void Discover()
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                foreach (var cls in _editor.assemblyHandler.ConfigEditor.PipelinePluginClasses)
                {
                    if (cls.type == null || !cls.IsPipelinePlugin) continue;
                    var attr = cls.type.GetCustomAttribute<PipelinePluginAttribute>();
                    if (attr != null)
                        _descriptors[attr.PluginId] = new PipelinePluginDescriptor(attr, cls.type);
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(
                    nameof(Discover),
                    $"Pipeline plugin discovery failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Creates a fresh instance of the plugin identified by <paramref name="pluginId"/>,
        /// injecting <see cref="IDMEEditor"/> as the first constructor argument.
        /// </summary>
        /// <typeparam name="T">Expected plugin interface, e.g. <see cref="IPipelineSource"/>.</typeparam>
        /// <exception cref="KeyNotFoundException">Plugin ID not found in the registry.</exception>
        /// <exception cref="InvalidCastException">Plugin type does not implement <typeparamref name="T"/>.</exception>
        public T Create<T>(string pluginId) where T : IPipelinePlugin
        {
            if (!_descriptors.TryGetValue(pluginId, out var desc))
                throw new KeyNotFoundException($"Pipeline plugin '{pluginId}' not found in registry.");

            var instance = Activator.CreateInstance(desc.ImplementationType, _editor)
                ?? throw new InvalidOperationException(
                    $"Activator.CreateInstance returned null for '{desc.ImplementationType.FullName}'.");

            return (T)instance;
        }

        /// <summary>Returns a snapshot of all registered plugin descriptors.</summary>
        public IReadOnlyList<PipelinePluginDescriptor> GetAll() =>
            _descriptors.Values.ToList().AsReadOnly();

        /// <summary>Returns all plugins with the given functional type.</summary>
        public IReadOnlyList<PipelinePluginDescriptor> GetByType(PipelinePluginType type) =>
            _descriptors.Values
                        .Where(d => d.Attribute.PluginType == type)
                        .ToList()
                        .AsReadOnly();

        /// <summary>Returns true if a plugin with the given ID has been discovered.</summary>
        public bool Contains(string pluginId) => _descriptors.ContainsKey(pluginId);

        /// <summary>Number of registered plugins.</summary>
        public int Count => _descriptors.Count;
    }
}
