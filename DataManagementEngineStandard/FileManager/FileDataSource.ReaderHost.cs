using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.FileManager.Readers;
using TheTechIdea.Beep.FileManager.Registry;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Reader host surface for listing/switching file readers at runtime.
    /// </summary>
    public partial class FileDataSource : IFileDataSourceReaderHost
    {
        private DataSourceType? _selectedReaderTypeOverride;
        private FileReaderDescriptorInfo? _currentReaderDescriptor;
        private FileReaderRegistry? _cachedReaderRegistry;
        private string? _lastSwitchReason;

        public IReadOnlyList<FileReaderDescriptorInfo> GetAvailableReaderDescriptors(bool discover = false)
        {
            var descriptors = new List<FileReaderDescriptorInfo>();

            try
            {
                var registry = GetOrCreateRegistry();
                if (registry != null)
                {
                    if (discover)
                    {
                        registry.Discover();
                        DMEEditor?.AddLogMessage(nameof(GetAvailableReaderDescriptors),
                            $"Discovered {registry.Count} file readers from registry.",
                            DateTime.Now, 0, DatasourceName, Errors.Ok);
                    }

                    descriptors.AddRange(registry.GetAll().Select(d => new FileReaderDescriptorInfo
                    {
                        ReaderId = BuildReaderId(d.ImplementationType.FullName),
                        FormatType = d.FormatType,
                        DisplayName = d.Attribute.DisplayName,
                        DefaultExtension = d.Attribute.DefaultExtension,
                        ImplementationTypeName = d.ImplementationType.FullName ?? string.Empty
                    }));
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage(nameof(GetAvailableReaderDescriptors),
                    $"Reader descriptor discovery failed: {ex.Message}",
                    DateTime.Now, -1, DatasourceName, Errors.Warning);
            }

            if (descriptors.Count == 0)
            {
                // Fallback to factory registrations when registry discovery is unavailable.
                FileReaderFactory.RegisterDefaults();
                foreach (var type in FileReaderFactory.SupportedTypes.Distinct())
                {
                    string extension = string.Empty;
                    try
                    {
                        extension = FileReaderFactory.GetReader(type)?.GetDefaultExtension() ?? string.Empty;
                    }
                    catch
                    {
                        extension = string.Empty;
                    }

                    descriptors.Add(new FileReaderDescriptorInfo
                    {
                        ReaderId = BuildReaderId(type.ToString()),
                        FormatType = type,
                        DisplayName = type.ToString(),
                        DefaultExtension = extension,
                        ImplementationTypeName = string.Empty
                    });
                }

                DMEEditor?.AddLogMessage(nameof(GetAvailableReaderDescriptors),
                    $"Registry unavailable. Returned {descriptors.Count} factory-based reader descriptors.",
                    DateTime.Now, 0, DatasourceName, Errors.Ok);
            }

            return descriptors
                .OrderBy(d => d.FormatType.ToString())
                .ThenBy(d => d.DisplayName)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<DataSourceType> GetAvailableReaderTypes()
        {
            FileReaderFactory.RegisterDefaults();
            return FileReaderFactory.SupportedTypes
                .Distinct()
                .OrderBy(t => t.ToString())
                .ToList()
                .AsReadOnly();
        }

        public FileReaderDescriptorInfo? GetCurrentReaderDescriptor() => _currentReaderDescriptor;

        public object? GetCurrentReader() => _reader;

        public bool TrySwitchReader(DataSourceType targetType, bool reconfigure, out string? reason)
        {
            reason = null;
            _lastSwitchReason = null;

            if (!CanSwitchReader(out reason))
                return false;

            try
            {
                FileReaderFactory.RegisterDefaults();
                if (!FileReaderFactory.IsSupported(targetType))
                {
                    reason = $"No file reader is registered for DataSourceType.{targetType}.";
                    _lastSwitchReason = reason;
                    DMEEditor?.AddLogMessage(nameof(TrySwitchReader), reason, DateTime.Now, -1, DatasourceName, Errors.Warning);
                    return false;
                }

                var selectedReader = FileReaderFactory.GetReader(targetType);
                if (selectedReader == null)
                {
                    reason = $"Reader resolution returned null for DataSourceType.{targetType}.";
                    _lastSwitchReason = reason;
                    DMEEditor?.AddLogMessage(nameof(TrySwitchReader), reason, DateTime.Now, -1, DatasourceName, Errors.Warning);
                    return false;
                }

                var descriptor = GetAvailableReaderDescriptors(false)
                    .FirstOrDefault(d => d.FormatType == targetType)
                    ?? CreateDescriptorFromReader(selectedReader, targetType);

                return TryApplyReader(selectedReader, descriptor, reconfigure, out reason);
            }
            catch (Exception ex)
            {
                reason = $"Reader switch failed for DataSourceType.{targetType}: {ex.Message}";
                _lastSwitchReason = reason;
                DMEEditor?.AddLogMessage(nameof(TrySwitchReader), reason, DateTime.Now, -1, DatasourceName, Errors.Failed);
                return false;
            }
        }

        public bool TrySwitchReader(string implementationTypeName, bool reconfigure, out string? reason)
        {
            reason = null;
            _lastSwitchReason = null;

            if (!CanSwitchReader(out reason))
                return false;

            if (string.IsNullOrWhiteSpace(implementationTypeName))
            {
                reason = "Implementation type name is required.";
                _lastSwitchReason = reason;
                return false;
            }

            var descriptors = GetAvailableReaderDescriptors(true);
            var descriptor = descriptors.FirstOrDefault(d =>
                string.Equals(d.ImplementationTypeName, implementationTypeName, StringComparison.OrdinalIgnoreCase));

            if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.ImplementationTypeName))
            {
                reason = $"Reader implementation '{implementationTypeName}' was not found in discovered registry descriptors.";
                _lastSwitchReason = reason;
                DMEEditor?.AddLogMessage(nameof(TrySwitchReader), reason, DateTime.Now, -1, DatasourceName, Errors.Warning);
                return false;
            }

            return TrySwitchReaderInternalByDescriptor(descriptor, reconfigure, out reason);
        }

        public bool TrySwitchReader(Guid readerId, bool reconfigure, out string? reason)
        {
            reason = null;
            _lastSwitchReason = null;

            if (!CanSwitchReader(out reason))
                return false;

            var descriptor = GetAvailableReaderDescriptors(true)
                .FirstOrDefault(d => d.ReaderId == readerId);

            if (descriptor == null)
            {
                reason = $"Reader with id '{readerId}' was not found.";
                _lastSwitchReason = reason;
                DMEEditor?.AddLogMessage(nameof(TrySwitchReader), reason, DateTime.Now, -1, DatasourceName, Errors.Warning);
                return false;
            }

            return TrySwitchReaderInternalByDescriptor(descriptor, reconfigure, out reason);
        }

        public void ResetReaderSelection()
        {
            _selectedReaderTypeOverride = null;
            _currentReaderDescriptor = null;
            _reader = null;
            _lastSwitchReason = "Reader selection reset to datasource default behavior.";
        }

        public bool TrySwitchReaderByExtension(string extension, bool reconfigure, out string? reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(extension))
            {
                reason = "Extension is required.";
                return false;
            }

            var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
            var match = GetAvailableReaderDescriptors(true)
                .FirstOrDefault(d => string.Equals(d.DefaultExtension, normalized, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                reason = $"No reader found for extension '{normalized}'.";
                return false;
            }

            // Extension can map to multiple readers. Prefer identity-based switch when available.
            if (!string.IsNullOrWhiteSpace(match.ImplementationTypeName))
                return TrySwitchReader(match.ImplementationTypeName, reconfigure, out reason);

            return TrySwitchReader(match.FormatType, reconfigure, out reason);
        }

        private FileReaderRegistry? GetOrCreateRegistry()
        {
            if (_cachedReaderRegistry != null)
                return _cachedReaderRegistry;

            if (DMEEditor == null)
                return null;

            _cachedReaderRegistry = new FileReaderRegistry(DMEEditor);
            return _cachedReaderRegistry;
        }

        private bool TrySwitchReaderInternalByDescriptor(FileReaderDescriptorInfo descriptor, bool reconfigure, out string? reason)
        {
            reason = null;
            try
            {
                IFileFormatReader? selectedReader = null;
                var registry = GetOrCreateRegistry();

                // Prefer exact implementation identity from registry.
                if (registry != null && !string.IsNullOrWhiteSpace(descriptor.ImplementationTypeName))
                {
                    registry.Discover();
                    var regMatch = registry.GetAll().FirstOrDefault(d =>
                        string.Equals(d.ImplementationType.FullName, descriptor.ImplementationTypeName, StringComparison.OrdinalIgnoreCase));
                    if (regMatch != null)
                    {
                        selectedReader = (IFileFormatReader?)Activator.CreateInstance(regMatch.ImplementationType);
                    }
                }

                // Fallback to type-based factory resolution.
                if (selectedReader == null)
                {
                    FileReaderFactory.RegisterDefaults();
                    if (!FileReaderFactory.IsSupported(descriptor.FormatType))
                    {
                        reason = $"No file reader is registered for DataSourceType.{descriptor.FormatType}.";
                        _lastSwitchReason = reason;
                        return false;
                    }
                    selectedReader = FileReaderFactory.GetReader(descriptor.FormatType);
                }

                if (selectedReader == null)
                {
                    reason = $"Could not create reader for '{descriptor.ImplementationTypeName}' ({descriptor.FormatType}).";
                    _lastSwitchReason = reason;
                    return false;
                }

                return TryApplyReader(selectedReader, descriptor, reconfigure, out reason);
            }
            catch (Exception ex)
            {
                reason = $"Reader switch failed: {ex.Message}";
                _lastSwitchReason = reason;
                DMEEditor?.AddLogMessage(nameof(TrySwitchReaderInternalByDescriptor), reason, DateTime.Now, -1, DatasourceName, Errors.Failed);
                return false;
            }
        }

        private bool TryApplyReader(IFileFormatReader selectedReader, FileReaderDescriptorInfo descriptor, bool reconfigure, out string? reason)
        {
            _reader = selectedReader;
            _selectedReaderTypeOverride = descriptor.FormatType;
            DatasourceType = descriptor.FormatType; // update datasource type only on success

            if (reconfigure && Dataconnection?.ConnectionProp != null)
                _reader.Configure(Dataconnection.ConnectionProp);

            _currentReaderDescriptor = string.IsNullOrWhiteSpace(descriptor.ImplementationTypeName)
                ? CreateDescriptorFromReader(selectedReader, descriptor.FormatType)
                : descriptor;

            reason = $"Switched to {_currentReaderDescriptor.DisplayName} ({_currentReaderDescriptor.FormatType}) successfully.";
            _lastSwitchReason = reason;
            DMEEditor?.AddLogMessage(nameof(TryApplyReader), reason, DateTime.Now, 0, DatasourceName, Errors.Ok);
            return true;
        }

        private bool CanSwitchReader(out string? reason)
        {
            reason = null;
            if (!_inTransaction) return true;

            reason = "Cannot switch reader while a transaction/ingestion operation is active.";
            _lastSwitchReason = reason;
            DMEEditor?.AddLogMessage(nameof(CanSwitchReader), reason, DateTime.Now, -1, DatasourceName, Errors.Warning);
            return false;
        }

        private static FileReaderDescriptorInfo CreateDescriptorFromReader(IFileFormatReader reader, DataSourceType formatType)
        {
            var implementationName = reader.GetType().FullName ?? string.Empty;
            return new FileReaderDescriptorInfo
            {
                ReaderId = BuildReaderId(implementationName),
                FormatType = formatType,
                DisplayName = reader.GetType().Name,
                DefaultExtension = reader.GetDefaultExtension(),
                ImplementationTypeName = implementationName
            };
        }

        private static Guid BuildReaderId(string? identity)
        {
            var source = string.IsNullOrWhiteSpace(identity) ? "unknown-reader" : identity.Trim().ToLowerInvariant();
            var bytes = Encoding.UTF8.GetBytes(source);
            var hash = MD5.HashData(bytes);
            return new Guid(hash);
        }
    }
}
