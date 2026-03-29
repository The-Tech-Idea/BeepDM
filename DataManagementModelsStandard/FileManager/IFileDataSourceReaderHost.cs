using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Exposes file-reader discovery and runtime selection behavior for file datasource hosts.
    /// </summary>
    public interface IFileDataSourceReaderHost
    {
        /// <summary>
        /// Returns available reader descriptors.
        /// When <paramref name="discover"/> is <c>true</c>, implementations should trigger
        /// registry discovery before returning descriptors (if a registry is available).
        /// When <c>false</c>, implementations should return cached/discovered data and
        /// may fall back to factory-registered readers.
        /// </summary>
        IReadOnlyList<FileReaderDescriptorInfo> GetAvailableReaderDescriptors(bool discover = false);

        /// <summary>
        /// Returns all available format types known by the host.
        /// </summary>
        IReadOnlyList<DataSourceType> GetAvailableReaderTypes();

        /// <summary>
        /// Returns metadata for the currently active reader selection, or <c>null</c>
        /// when no reader is currently selected.
        /// </summary>
        FileReaderDescriptorInfo? GetCurrentReaderDescriptor();

        /// <summary>
        /// Returns the currently active reader instance as an opaque object, or <c>null</c>
        /// when no reader has been resolved yet.
        /// </summary>
        object? GetCurrentReader();

        /// <summary>
        /// Attempts to switch the active reader to <paramref name="targetType"/>.
        /// Implementations should return <c>false</c> with a reason when the switch
        /// cannot be completed and should avoid throwing for expected failures.
        /// On success, the datasource type should be updated to match the new reader.
        /// </summary>
        bool TrySwitchReader(DataSourceType targetType, bool reconfigure, out string? reason);

        /// <summary>
        /// Attempts to switch the active reader to a specific implementation type name.
        /// This enables selecting between multiple readers for the same datasource type.
        /// </summary>
        bool TrySwitchReader(string implementationTypeName, bool reconfigure, out string? reason);

        /// <summary>
        /// Attempts to switch the active reader by stable reader identifier.
        /// </summary>
        bool TrySwitchReader(Guid readerId, bool reconfigure, out string? reason);

        /// <summary>
        /// Clears any manual reader override and returns selection behavior to datasource default.
        /// </summary>
        void ResetReaderSelection();

        /// <summary>
        /// Attempts to switch the active reader by file extension (without leading dot).
        /// </summary>
        bool TrySwitchReaderByExtension(string extension, bool reconfigure, out string? reason);
    }
}
