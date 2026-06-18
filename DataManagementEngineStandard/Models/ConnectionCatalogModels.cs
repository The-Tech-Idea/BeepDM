using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Winform.Controls
{
    public enum ConnectionStoreKind
    {
        ProjectLocal,
        Shared
    }

    public sealed class ConnectionCatalogRecord
    {
        public string Scope { get; set; } = ConnectionStorageScope.Project.ToString();
        public string ProfileName { get; set; } = "Default";
        public string SourceStore { get; set; } = string.Empty;
        public string SourceProfile { get; set; } = string.Empty;
        public string PackageVersion { get; set; } = "1.0";
        public DateTime ExportedOnUtc { get; set; } = DateTime.UtcNow;
        public ConnectionProperties Connection { get; set; } = new ConnectionProperties();
    }

    public sealed class ConnectionCatalogPackage
    {
        public string PackageVersion { get; set; } = "1.0";
        public string ProfileName { get; set; } = "Default";
        public string SourceScope { get; set; } = ConnectionStorageScope.Project.ToString();
        public DateTime ExportedOnUtc { get; set; } = DateTime.UtcNow;
        public List<ConnectionCatalogRecord> Records { get; set; } = new();
    }
}
