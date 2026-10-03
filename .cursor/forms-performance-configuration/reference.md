# Forms Performance And Configuration Reference

## Local Cursor Paging

Read `DataManagementEngineStandard/Editor/Forms/LOCAL-PAGING.md` and
`Forms.Tests/PagingTests.cs`. Default helper publication is optional, captured and
revision gated. LoadPageAsync projects typed PageStatePublished, not the mere
return of a cursor setter. An ignored setter, invalid buffer, old request,
cancelled pre-move trigger or stale registration cannot publish a loaded page.
After acknowledgement, observer failure retains accepted page state. Do not
blindly replay a null/false result when NavigationEffectsPossible is true.

PageInfo.TotalPagesLong/SkipLong are exact; old int properties throw on overflow.
Explicit zero count is known, not a fallback to old loaded rows. Local acceptance
uses actual loaded count and clamps after shrink. Size zero disables paging.
Virtual/criteria buffers and legacy nonpublication helpers reject. Nested paging
rejects instead of self-waiting; admitted work joins physical manager drain.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

public static class FormsPerformanceExamples
{
    public static async Task<FormQueryResult> FetchProviderPageAsync(
        FormsManager forms, string block, long page, CancellationToken token = default)
    {
        // PageSize is configured at session setup; the UoW/provider must opt in.
        var size = forms.GetBlock(block).Configuration.PageSize;
        var result = await forms.FetchPageWithOutcomeAsync(block,
            new BoundedPageRequest(page, size, 256 * 1024), token);
        if (!result.RecordsPublished)
            return result; // Count shrink/denial/failure does not authorize UI replacement.
        // ProviderPage stays long and separate from local CurrentPage. Host rechecks
        // its captured binding in its dispatcher before displaying accepted rows.
        return result;
    }

    public static async Task<LocalPageResult> PositionLocalPageAsync(
        FormsManager forms, string block, int page, CancellationToken token = default)
    {
        // Configure page size at session setup, not during every navigation request.
        var result = await forms.LoadLocalPageWithOutcomeAsync(block, page, token);
        if (!result.PageStatePublished)
            return result; // Inspect effects/error before deciding on reconciliation.
        if (result.NotificationFailures.Count != 0)
            return result; // Page was accepted; this is not a reason to replay navigation.
        // Host checks its current binding, then dispatches render/focus.
        return result;
    }

    public static void ConfigureEnvironment(ConfigurationManager configuration)
    {
        configuration.LoadConfiguration();
        if (!configuration.ValidateConfiguration()) configuration.ResetToDefaults();
        configuration.Configuration.ValidateBeforeCommit = true;
        configuration.Configuration.ClearCacheOnFormClose = false;
        configuration.SaveConfiguration();
    }
}
```

Compile examples against actual Engine project references on net8/net9/net10;
do not execute configuration persistence merely to test compilation. FormsManager
borrows registered UoWs: keep them alive for the form session and drain/dispose the
manager before their owner disposes them. Never return a form built over using-scoped
UoWs that are already disposed.

## Settings And Caches

Read `DataManagementEngineStandard/Editor/Forms/PROVIDER-PAGING.md`,
`FormsManager.ProviderPaging.cs`, `UOW/UnitofWork.PageRead.cs` and
`Forms.Tests/ProviderPagingTests.cs` for opt-in bounded staged fetch. It requires
IFormsProviderPaging/IStagedUnitofWorkPageRead/IBoundedPagedDataSource, a positive
matching configured PageSize within MaxRecordsPerFetch/MaxRecords, explicit byte
budget, known-column order completed by all primary keys and scalar UTF-8 rows.
The producer must apply every filter before count/page and bound work at production;
receiver envelope/actual-byte checks do not prove arbitrary provider honesty.
Native SQLite test-lane LIMIT/OFFSET/count/policy/byte checks do not qualify plugins.
Out-of-range count shrink retains prior rows with typed PageOutOfRange; no silent
clamp/refetch or unbounded legacy Get fallback. Inspect RecordsPublished separately
from validated ProviderPage observation. Long provider pages do not set local int
CurrentPage, and LastQuery does not pretend an unpaged SELECT is the executed SQL.
FetchAheadDepth, lazy-load mode and TTL do not implement policy/query/registration-
aware bounded cache/prefetch; those Stage F deliverables remain unimplemented.

PerformanceManager cache entries describe blocks, not authorized row pages.
InvalidateBlockCache does not itself re-query or erase rendered text. Cached
buffer authority, queued UI policy repaint and typed local page state are separate:
consult BUFFER-AUTHORIZATION.md, POLICY-REPAINT.md and PERMISSION-PROJECTION.md.
Use telemetry before tuning; arbitrary configuration/graph pinning, shared
injected helpers and real adapters remain unqualified.

