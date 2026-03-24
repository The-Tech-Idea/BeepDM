# Phase 15 — Multi-Tenancy and Governance

| Attribute      | Value                                      |
|----------------|--------------------------------------------|
| Phase          | 15                                         |
| Status         | planned                                    |
| Priority       | High                                       |
| Dependencies   | Phase 7 (security governance), Phase 11 (ingestion contracts), Phase 14 (DLP) |
| Est. Effort    | 4 days                                     |

---

## 1. Goal

Make the FileManager a **first-class multi-tenant component**:
- Each tenant operates in a strictly isolated file namespace.
- Tenant A cannot read, write, or infer schema from Tenant B's files.
- Row-level security ensures multi-tenant CSVs (where one file contains rows for multiple tenants) are filtered at read time.
- Role-based access control gates all file operations.
- Data residency and region controls prevent cross-border data movement.

---

## 2. Motivation

| Current state | Enterprise / SaaS requirement |
|---------------|-------------------------------|
| Single global `FilePath` property | Each tenant must have an isolated root directory |
| No caller identity on reads | Every operation must be tagged with a `TenantId` and `ActorId` |
| `GetEntity` returns all rows | Row-level security must filter rows to the requesting tenant |
| No region controls | GDPR / data residency rules prohibit EU data from leaving EU |

---

## 3. Tenant Context

### 3.1 `ITenantContext`

```csharp
namespace TheTechIdea.Beep.FileManager.Governance
{
    /// <summary>
    /// Ambient context for the current file operation.
    /// Passed through all layers explicitly — never via thread-local or static state.
    /// </summary>
    public interface ITenantContext
    {
        string TenantId { get; }
        string ActorId { get; }          // User or service principal making the request
        IReadOnlyList<string> Roles { get; } // e.g. ["FileReader", "FileAdmin"]
        string DataRegion { get; }       // e.g. "EU", "US-EAST", "APAC"
        DateTimeOffset RequestedAt { get; }
    }
}
```

### 3.2 `TenantContext` immutable record (reference implementation)

```csharp
public sealed record TenantContext(
    string TenantId,
    string ActorId,
    IReadOnlyList<string> Roles,
    string DataRegion,
    DateTimeOffset RequestedAt) : ITenantContext;
```

---

## 4. Tenant-Scoped File Path Policy

### 4.1 Path resolver contract

```csharp
namespace TheTechIdea.Beep.FileManager.Governance
{
    public interface ITenantFilePathResolver
    {
        /// <summary>
        /// Returns the fully-qualified, tenant-scoped file path for the given
        /// logical entity name and tenant context.
        /// </summary>
        string Resolve(string logicalEntityName, ITenantContext context);

        /// <summary>
        /// Validates that <paramref name="absolutePath"/> is within the tenant's
        /// allowed root. Throws <see cref="PathTraversalException"/> on violation.
        /// MUST be called on every externally-supplied path before any IO operation.
        /// </summary>
        void ValidatePathBoundary(string absolutePath, ITenantContext context);
    }
}
```

### 4.2 Default resolver: directory-per-tenant

```
FileRootBase/
    {TenantId}/
        {DataRegion}/
            {EntityName}.csv
```

Example: `C:\Beep\Files\ACME\EU\customers.csv`

```csharp
public sealed class DirectoryPerTenantPathResolver : ITenantFilePathResolver
{
    private readonly string _fileRootBase;

    public string Resolve(string logicalEntityName, ITenantContext context)
    {
        var safeTenantId  = SanitizePathSegment(context.TenantId);
        var safeRegion    = SanitizePathSegment(context.DataRegion);
        var safeEntity    = SanitizePathSegment(logicalEntityName);
        return Path.GetFullPath(
            Path.Combine(_fileRootBase, safeTenantId, safeRegion, safeEntity + ".csv"));
    }

    public void ValidatePathBoundary(string absolutePath, ITenantContext context)
    {
        var safeTenantId = SanitizePathSegment(context.TenantId);
        var tenantRoot = Path.GetFullPath(Path.Combine(_fileRootBase, safeTenantId)) + Path.DirectorySeparatorChar;
        if (!absolutePath.StartsWith(tenantRoot, StringComparison.OrdinalIgnoreCase))
            throw new PathTraversalException(absolutePath, tenantRoot, context.TenantId);
    }

    private static string SanitizePathSegment(string segment)
    {
        // Remove path separators, null bytes, and parent-traversal sequences
        if (string.IsNullOrWhiteSpace(segment))
            throw new ArgumentException("Path segment must not be empty.", nameof(segment));
        var invalid = Path.GetInvalidFileNameChars().Concat(new[]{'/', '\\', '\0'}).ToHashSet();
        var safe = new string(segment.Where(c => !invalid.Contains(c)).ToArray());
        if (safe.Contains(".."))
            throw new PathTraversalException(segment, segment, "N/A");
        return safe;
    }
}
```

---

## 5. Role-Based Access Control

### 5.1 Roles and permissions

| Role | Read File | Write File | Modify Schema | Delete File | Admin Config |
|------|-----------|------------|---------------|-------------|--------------|
| `FileReader` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `FileWriter` | ✅ | ✅ | ❌ | ❌ | ❌ |
| `FileSchemaAdmin` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `FileAdmin` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Auditor` | ✅ | ❌ | ❌ | ❌ | ❌ |

### 5.2 `IFileAccessPolicy`

```csharp
namespace TheTechIdea.Beep.FileManager.Governance
{
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
        /// <summary>
        /// Returns true if the actor in <paramref name="context"/> is allowed
        /// to perform <paramref name="operation"/> on <paramref name="entityName"/>.
        /// </summary>
        bool IsAllowed(string entityName, FileOperation operation, ITenantContext context);

        /// <summary>
        /// Throws <see cref="FileAccessDeniedException"/> if not allowed.
        /// </summary>
        void Enforce(string entityName, FileOperation operation, ITenantContext context);
    }
}
```

### 5.3 Default role-based policy implementation

```csharp
public sealed class RoleBasedFileAccessPolicy : IFileAccessPolicy
{
    private static readonly Dictionary<string, HashSet<FileOperation>> _rolePermissions = new()
    {
        ["FileReader"]      = new() { FileOperation.ReadData },
        ["FileWriter"]      = new() { FileOperation.ReadData, FileOperation.WriteData },
        ["FileSchemaAdmin"] = new() { FileOperation.ReadData, FileOperation.WriteData, FileOperation.ModifySchema },
        ["FileAdmin"]       = new() { FileOperation.ReadData, FileOperation.WriteData, FileOperation.ModifySchema, FileOperation.DeleteFile, FileOperation.AdminFileSource },
        ["Auditor"]         = new() { FileOperation.ReadData },
    };

    public bool IsAllowed(string entityName, FileOperation operation, ITenantContext context) =>
        context.Roles.Any(role =>
            _rolePermissions.TryGetValue(role, out var ops) && ops.Contains(operation));

    public void Enforce(string entityName, FileOperation operation, ITenantContext context)
    {
        if (!IsAllowed(entityName, operation, context))
            throw new FileAccessDeniedException(context.ActorId, entityName, operation, context.TenantId);
    }
}
```

---

## 6. Row-Level Security

### 6.1 Use case

A single CSV file may contain rows for multiple tenants, joined by a tenant-key column (e.g. `organization_id`).  
Row-level security (RLS) ensures `TenantA` reading `orders.csv` only receives rows where `organization_id = 'TenantA'`.

### 6.2 `IRowLevelSecurityFilter`

```csharp
namespace TheTechIdea.Beep.FileManager.Governance
{
    public interface IRowLevelSecurityFilter
    {
        /// <summary>
        /// Returns additional <see cref="AppFilter"/> instances to append to every query
        /// for the given entity and tenant context.
        /// Returns an empty list if RLS is not configured for this entity.
        /// </summary>
        IReadOnlyList<AppFilter> GetFilters(string entityName, ITenantContext context);
    }
}
```

### 6.3 Configuration

```csharp
// Register RLS configuration per entity
rlsFilter.Configure("orders.csv", new RlsRule
{
    TenantKeyColumn = "organization_id",    // column in the CSV that holds the tenant key
    TenantValueSource = RlsValueSource.TenantContextId  // bind to TenantContext.TenantId
});
```

### 6.4 Integration in `CSVDataSource.GetEntity`

```csharp
// Before executing the query:
var additionalFilters = _rlsFilter?.GetFilters(entityName, _tenantContext) ?? [];
var allFilters = filter.Concat(additionalFilters).ToList();
// Pass allFilters to the existing filter evaluation logic
```

---

## 7. Data Residency Controls

### 7.1 Region-enforcement rule

Files in region `EU` must only be opened when `TenantContext.DataRegion == "EU"`.  
Prevent EU data from being read by a `US-EAST` scoped context.

```csharp
public interface IDataResidencyPolicy
{
    /// <summary>
    /// Validates that the file at <paramref name="resolvedPath"/> may be accessed
    /// from the region declared in <paramref name="context"/>.
    /// Throws <see cref="DataResidencyViolationException"/> on violation.
    /// </summary>
    void Enforce(string resolvedPath, ITenantContext context);
}
```

### 7.2 Default implementation: directory-region mapping

Derive allowed region from the path:  
`/files/ACME/EU/orders.csv` → region `EU` → only allow context with `DataRegion == "EU"`.

---

## 8. Governance Audit Trail

Every file operation (read, write, schema change, delete) must produce an audit event:

```csharp
public sealed record FileGovernanceEvent(
    string EventId,
    string TenantId,
    string ActorId,
    string EntityName,
    FileOperation Operation,
    bool WasAllowed,
    string DenialReason,          // null if allowed
    string DataRegion,
    DateTimeOffset OccurredAt,
    string JobId);               // null for non-ingestion reads
```

The audit event is written to:
1. `IDMEEditor.Logger` at severity `Audit`.
2. An append-only `IGovernanceAuditStore` (separate database table or write-once file).

---

## 9. Acceptance Criteria

| # | Criterion | Test |
|---|-----------|------|
| 1 | `ValidatePathBoundary` throws `PathTraversalException` for `../../secret.csv` | Unit |
| 2 | `FileReader` role cannot call `InsertEntity` — `FileAccessDeniedException` thrown | Unit |
| 3 | `FileAdmin` role can call all operations | Unit |
| 4 | RLS filter appends `organization_id = 'ACME'` for tenant `ACME` on `orders.csv` | Unit |
| 5 | EU-scoped file read with `DataRegion = "US-EAST"` context throws `DataResidencyViolationException` | Unit |
| 6 | Every allowed and denied operation produces a `FileGovernanceEvent` in the audit store | Integration |
| 7 | `SanitizePathSegment` strips `..`, `\`, `/`, and null bytes | Unit |

---

## 10. Deliverables

| Artifact | Location |
|----------|----------|
| `Governance/ITenantContext.cs` | `FileManager/Governance/` |
| `Governance/ITenantFilePathResolver.cs` | `FileManager/Governance/` |
| `Governance/DirectoryPerTenantPathResolver.cs` | `FileManager/Governance/Implementations/` |
| `Governance/IFileAccessPolicy.cs` | `FileManager/Governance/` |
| `Governance/RoleBasedFileAccessPolicy.cs` | `FileManager/Governance/Implementations/` |
| `Governance/IRowLevelSecurityFilter.cs` | `FileManager/Governance/` |
| `Governance/IDataResidencyPolicy.cs` | `FileManager/Governance/` |
| `Governance/FileGovernanceEvent.cs` | `FileManager/Governance/` |
| `Governance/PathTraversalException.cs` | `FileManager/Governance/` |
| `Governance/FileAccessDeniedException.cs` | `FileManager/Governance/` |
| `Governance/DataResidencyViolationException.cs` | `FileManager/Governance/` |
| Unit tests | `tests/FileManager/GovernanceTests.cs` |

---

## 11. Enterprise Standards Traceability

| Standard | Clause | Addressed |
|----------|--------|-----------|
| ISO/IEC 27001 | A.9 — Access Control | `IFileAccessPolicy`, RBAC |
| GDPR Art. 25 | Data Protection by Design | `ITenantFilePathResolver`, path boundary |
| GDPR Art. 44-49 | Cross-border data transfer | `IDataResidencyPolicy` |
| SOC 2 Type II | Logical access controls | Role table, audit trail |
| CIS Control 14 | Controlled access based on need to know | RLS filter |
| OWASP Path Traversal | OWASP A01:2021 | `SanitizePathSegment`, `ValidatePathBoundary` |
