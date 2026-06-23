using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.AppMap
{
    public class AppEnvironmentDatasource
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string AppName { get; set; } = "";
        public string EnvironmentId { get; set; } = "";
        public string EnvironmentName { get; set; } = "";
        public string DatasourceName { get; set; } = "";
        public string DatasourceCategory { get; set; } = "";
        public bool IsPrimary { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class AppMatrixRow
    {
        public string AppName { get; set; } = "";
        public List<AppMatrixCell> Cells { get; set; } = new();
    }

    public class AppMatrixCell
    {
        public string EnvironmentId { get; set; } = "";
        public string EnvironmentName { get; set; } = "";
        public string? DatasourceName { get; set; }
        public string? DatasourceCategory { get; set; }
        public bool IsPrimary { get; set; }
        public bool HasLink => !string.IsNullOrEmpty(DatasourceName);
    }
}
