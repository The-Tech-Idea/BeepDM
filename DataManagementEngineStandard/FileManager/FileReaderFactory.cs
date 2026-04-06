using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.FileManager.Registry;
using TheTechIdea.Beep.FileManager.Readers;
using TheTechIdea.Beep.Utilities;

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

        // ── ConfigEditor integration ─────────────────────────────────────────

        /// <summary>
        /// Builds a <see cref="ConnectionDriversConfig"/> from a file format reader,
        /// deriving DisplayName, extension, version and icon path from
        /// <see cref="FileReaderAttribute"/> when the attribute is present.
        /// The <c>classHandler</c> is always set to <c>"FileDataSource"</c> so the
        /// format-agnostic <c>FileDataSource</c> is instantiated and then routes to
        /// the correct <see cref="IFileFormatReader"/> at connection-open time.
        /// </summary>
        public static ConnectionDriversConfig BuildDriverConfig(IFileFormatReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var attr        = reader.GetType().GetCustomAttribute<FileReaderAttribute>(inherit: false);
            string name     = attr?.DisplayName      ?? reader.SupportedType.ToString();
            string ext      = attr?.DefaultExtension ?? reader.GetDefaultExtension() ?? string.Empty;
            string version  = attr?.Version          ?? "1.0.0";
            string icon     = !string.IsNullOrEmpty(attr?.IconPath) ? attr.IconPath : $"{ext}.svg";
            string typeName = reader.GetType().FullName ?? reader.GetType().Name;
            string asmName  = reader.GetType().Assembly.GetName().Name ?? string.Empty;

            return new ConnectionDriversConfig
            {
                GuidID             = GuidFromType(reader.GetType()),
                PackageName        = name,
                DriverClass        = typeName,
                version            = version,
                dllname            = asmName,
                AdapterType        = "DEFAULT",
                iconname           = icon,
                classHandler       = "FileDataSource",
                ADOType            = false,
                CreateLocal        = false,
                InMemory           = false,
                extensionstoHandle = ext,
                Favourite          = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType     = reader.SupportedType,
                IsMissing          = false,
                NuggetVersion      = version,
                NuggetSource       = name,
                NuggetMissing      = false,
                NeedDrivers        = false
            };
        }

        /// <summary>
        /// Registers a reader in <see cref="FileReaderFactory"/> and simultaneously
        /// upserts the matching <see cref="ConnectionDriversConfig"/> into
        /// <paramref name="configEditor"/>.DataDriversClasses so the format appears
        /// as an available connection type in the BeepDM connection manager.
        /// If <paramref name="configEditor"/> is <c>null</c> the method behaves
        /// identically to <see cref="Register"/>.
        /// </summary>
        public static void RegisterWithConfig(IFileFormatReader reader, IConfigEditor configEditor)
        {
            Register(reader);

            if (configEditor == null) return;

            var cfg      = BuildDriverConfig(reader);
            var existing = configEditor.DataDriversClasses
                .FirstOrDefault(d => d.DatasourceType == cfg.DatasourceType);

            if (existing == null)
                configEditor.DataDriversClasses.Add(cfg);
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Derives a deterministic <see cref="Guid"/> from a type's full name using MD5
        /// so that repeated registrations do not flood the saved JSON config with
        /// duplicate entries.
        /// </summary>
        private static string GuidFromType(Type type)
        {
            byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(type.FullName ?? type.Name));
            return new Guid(bytes).ToString();
        }
    }
}
