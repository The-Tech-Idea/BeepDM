using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.FileManager.Readers;

namespace TheTechIdea.Beep.FileManager.Registry
{
    /// <summary>
    /// Discovers, caches, and auto-registers <see cref="IFileFormatReader"/>
    /// implementations that are decorated with <see cref="FileReaderAttribute"/>.
    ///
    /// Uses <c>IDMEEditor.assemblyHandler.ConfigEditor.FileReaderClasses</c> for discovery —
    /// the same mechanism as <c>[AddinAttribute]</c> connectors and
    /// <c>[PipelinePluginAttribute]</c> ETL plugins.
    ///
    /// Calling <see cref="Discover"/> also calls <see cref="FileReaderFactory.Register"/>
    /// for each found reader so that all existing code that calls
    /// <c>FileReaderFactory.GetReader(type)</c> continues to work transparently.
    ///
    /// <see cref="FileReaderFactory.RegisterDefaults"/> uses
    /// <see cref="RegisterAttributedReadersFromAssembly"/> for core readers.
    ///
    /// Typical usage:
    /// <code>
    ///   var registry = new FileReaderRegistry(editor);
    ///   registry.Discover();   // finds all [FileReader(…)]-attributed classes
    ///   var reader = registry.Create(DataSourceType.Parquet);
    /// </code>
    /// </summary>
    public class FileReaderRegistry
    {
        private readonly IDMEEditor _editor;
        private readonly Dictionary<DataSourceType, FileReaderDescriptor> _descriptors = new();

        public FileReaderRegistry(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Registers all attributed file readers from a given assembly.
        /// Used by <see cref="FileReaderFactory.RegisterDefaults"/> before an editor exists.
        /// </summary>
        public static void RegisterAttributedReadersFromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract) continue;
                if (!typeof(IFileFormatReader).IsAssignableFrom(type)) continue;

                var attr = type.GetCustomAttribute<FileReaderAttribute>(inherit: false);
                if (attr == null) continue;

                var instance = (IFileFormatReader?)Activator.CreateInstance(type);
                if (instance != null)
                    FileReaderFactory.Register(instance);
            }
        }

        // ── Discovery ────────────────────────────────────────────────────────

        /// <summary>
        /// Scans all assemblies loaded by <c>AssemblyHandler</c> and registers every
        /// concrete class that implements <see cref="IFileFormatReader"/> and carries
        /// a <see cref="FileReaderAttribute"/>.
        ///
        /// Each discovered reader is also registered in <see cref="FileReaderFactory"/>
        /// so existing code that calls <c>FileReaderFactory.GetReader(type)</c> works
        /// without changes.
        ///
        /// Safe to call multiple times — existing descriptors are updated (re-discovery).
        /// </summary>
        public void Discover()
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var readerClasses = _editor.assemblyHandler?.ConfigEditor?.FileReaderClasses;
                if (readerClasses == null) return;

                foreach (var cls in readerClasses)
                {
                    if (cls.type == null || !cls.IsFileReader) continue;

                    var attr = cls.type.GetCustomAttribute<FileReaderAttribute>(inherit: false);
                    if (attr == null) continue;

                    _descriptors[attr.FormatType] = new FileReaderDescriptor(attr, cls.type);

                    // Auto-register with the static factory and also ensure a ConnectionDriversConfig
                    // entry exists in ConfigEditor so the format appears in the connection manager.
                    try
                    {
                        var instance = (IFileFormatReader)Activator.CreateInstance(cls.type);
                        if (instance != null)
                            FileReaderFactory.RegisterWithConfig(
                                instance, _editor?.ConfigEditor);
                    }
                    catch (Exception ex)
                    {
                        _editor.AddLogMessage(
                            nameof(Discover),
                            $"Could not instantiate IFileFormatReader '{cls.type.FullName}': {ex.Message}",
                            DateTime.Now, -1, null, Errors.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(
                    nameof(Discover),
                    $"File reader plugin discovery failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        // ── Instantiation ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fresh instance of the reader registered for
        /// <paramref name="formatType"/>. The reader is created with its default
        /// (no-argument) constructor — readers do not require <c>IDMEEditor</c>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// No plugin was discovered for the given <paramref name="formatType"/>.
        /// </exception>
        public IFileFormatReader Create(DataSourceType formatType)
        {
            if (!_descriptors.TryGetValue(formatType, out var desc))
                throw new KeyNotFoundException(
                    $"No IFileFormatReader plugin registered for DataSourceType.{formatType}. " +
                    $"Ensure the assembly is loaded and decorated with [FileReader({formatType}, …)].");

            var instance = Activator.CreateInstance(desc.ImplementationType)
                ?? throw new InvalidOperationException(
                    $"Activator.CreateInstance returned null for '{desc.ImplementationType.FullName}'.");

            return (IFileFormatReader)instance;
        }

        // ── Query ────────────────────────────────────────────────────────────

        /// <summary>Returns all currently discovered descriptors.</summary>
        public IReadOnlyList<FileReaderDescriptor> GetAll()
        {
            var result = new List<FileReaderDescriptor>(_descriptors.Values);
            return result.AsReadOnly();
        }

        /// <summary>Returns <c>true</c> if a plugin for <paramref name="formatType"/> is registered.</summary>
        public bool Contains(DataSourceType formatType) => _descriptors.ContainsKey(formatType);

        /// <summary>Number of discovered plugins.</summary>
        public int Count => _descriptors.Count;
    }
}
