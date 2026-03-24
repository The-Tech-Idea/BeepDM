using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.FileManager.Governance
{
    public interface ITenantContext
    {
        string TenantId { get; }
        string ActorId { get; }
        IReadOnlyList<string> Roles { get; }
        string DataRegion { get; }
        DateTimeOffset RequestedAt { get; }
    }

    public sealed record TenantContext(
        string TenantId,
        string ActorId,
        IReadOnlyList<string> Roles,
        string DataRegion,
        DateTimeOffset RequestedAt) : ITenantContext;

    public enum FileOperation
    {
        ReadData,
        WriteData,
        ModifySchema,
        DeleteFile,
        AdminFileSource
    }

    public interface IFileAccessPolicy
    {
        bool IsAllowed(string entityName, FileOperation operation, ITenantContext context);
        void Enforce(string entityName, FileOperation operation, ITenantContext context);
    }

    public interface IRowLevelSecurityFilter
    {
        IReadOnlyList<AppFilter> GetFilters(string entityName, ITenantContext context);
    }

    public interface IDataResidencyPolicy
    {
        void Enforce(string resolvedPath, ITenantContext context);
    }
}
