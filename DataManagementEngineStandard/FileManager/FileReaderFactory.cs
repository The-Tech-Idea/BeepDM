using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.FileManager.Registry;
using TheTechIdea.Beep.FileManager.Readers;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Registry that maps a <see cref="DataSourceType"/> to its
    /// <see cref="IFileFormatReader"/> implementation.
    ///
    /// Call <see cref="RegisterDefaults"/> once at application start
    /// (delegates to <see cref="FileReaderRegistry.RegisterAttributedReadersFromAssembly"/>),
    /// then call <see cref="GetReader"/> to resolve the correct reader
    /// for a given datasource type.
    ///
    /// Third-party readers can be registered at any time via <see cref="Register"/>
    /// (including via <see cref="FileReaderRegistry.Discover"/>).
    /// </summary>
    public static class FileReaderFactory
    {
        private static readonly Dictionary<DataSourceType, IFileFormatReader> _registry
            = new Dictionary<DataSourceType, IFileFormatReader>();

        private static bool _defaultsRegistered;

        // ── Registration ─────────────────────────────────────────────────────

        /// <summary>Registers a reader, replacing any existing entry for the same type.</summary>
        public static void Register(IFileFormatReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            _registry[reader.SupportedType] = reader;
        }

        /// <summary>
        /// Registers attributed core file readers if not already registered.
        /// Uses <see cref="FileReaderRegistry.RegisterAttributedReadersFromAssembly"/>.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public static void RegisterDefaults()
        {
            if (_defaultsRegistered) return;
            _defaultsRegistered = true;

            FileReaderRegistry.RegisterAttributedReadersFromAssembly(typeof(IFileFormatReader).Assembly);
        }

        // ── Resolution ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the reader registered for <paramref name="type"/>.
        /// Falls back to the CSV reader for unknown types so that
        /// legacy flat-file connections continue to work.
        /// </summary>
        public static IFileFormatReader GetReader(DataSourceType type)
        {
            if (_registry.TryGetValue(type, out var reader))
                return reader;

            if (_registry.TryGetValue(DataSourceType.CSV, out var csvFallback))
                return csvFallback;

            throw new NotSupportedException(
                $"No IFileFormatReader registered for DataSourceType.{type}. " +
                $"Call FileReaderFactory.Register(...) before opening a connection.");
        }

        /// <summary>Returns true if a reader for <paramref name="type"/> is registered.</summary>
        public static bool IsSupported(DataSourceType type) => _registry.ContainsKey(type);

        /// <summary>Returns all currently registered format types.</summary>
        public static IEnumerable<DataSourceType> SupportedTypes => _registry.Keys;

        /// <summary>Clears all registrations (intended for unit tests only).</summary>
        internal static void Reset()
        {
            _registry.Clear();
            _defaultsRegistered = false;
        }
    }
}
