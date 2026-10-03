# BeepDM — Copilot instructions

**The canonical guidance for this repo is [`/CLAUDE.md`](../CLAUDE.md).** It is verified against the
code and kept current. Read it first; this file is a short orientation for GitHub Copilot only.

An earlier version of this file asserted a number of things that were false against the code
(a `dotnet build BeepDM.sln` workflow that failed, an `Assembly_helpersStandard` project that does
not exist, `DMEEditor.CreateUnitOfWork<T>()` and `uow.AddNew()`/`uow.Modify()` APIs that do not
exist, and an external `Beep.Container` dependency that is actually in-repo). Those claims are gone.
If anything here conflicts with the code, the code wins — verify before repeating a doc claim.

## Orientation

Two shippable projects, both multi-targeting `net8.0;net9.0;net10.0`, root namespace `TheTechIdea.Beep.*`:

- `DataManagementModelsStandard/` → package `TheTechIdea.Beep.DataManagementModels` — contracts/models only.
- `DataManagementEngineStandard/` → package `TheTechIdea.Beep.DataManagementEngine` — all implementation.

`BeepService` (`DataManagementEngineStandard/Services/`) bootstraps the graph; `DMEEditor`
(`Editor/DM/`, `partial` across four files) is the hub every operation flows through; `ConfigEditor`
(`ConfigUtil/`) is a façade over six managers in `ConfigUtil/Managers/`; `AssemblyHandler`
(`AssemblyHandler/AssemblySystem/`) discovers plugins.

## Build and test

```bash
dotnet build BeepDM.sln                 # works; per-project is faster:
dotnet build DataManagementEngineStandard/DataManagementEngine.csproj -f net9.0
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false
```

The local Windows solution baseline has 5,178 passing executions, zero failures/skips:
Forms 221, Setup 232, Studio 66, Migration 160 and FrameworkReliability 1,393 each on
net8/9/10. Compiler/analyzer warnings remain; a local green run does not establish
clean-checkout packaging, external-provider or Unix coverage. See CLAUDE.md and
`.plans/framework/IMPLEMENTATION-LOG.md` for exact evidence and remaining gates.

Read Editor/Importing/REJECT-RECOVERY.md for durable file triage/CAS claims and
actual acknowledged import/sync row replay. Do not use index marking, transform
destination snapshots again, expire uncertain claims or advance failed-run cursors.
Native/provider-run recovery and complete intent/promotion agreement remain open.

Governed migration intent is versioned and schema/target/policy-sensitive. Use
`Editor/Migration/PLAN-INTENT.md` for explicit policy revisions, hash-bound approval
options, persisted snapshot reload and conservative partial-DDL recovery limits.
Do not mutate operations or manually recompute hashes to reuse an approval/token.
Governed execution requires acknowledged history capability; failed checkpoints
block admission or require reconciliation after DDL. Legacy history void saves
now propagate errors. See Services/Persistence/README.md for custom-store/loader
capabilities, legacy promotion and local-filesystem limits.
Connection configuration adds acknowledged saves and per-runtime protection.
Catalog writes/exports use version 2.0, unavailable-key evidence blocks mutation,
and post-save observer failures stay separate from storage outcomes. See
Security/README.md for protected containers, key policy, export and old-reader limits.
BeepSync requires acknowledged start/completion when checkpointing is enabled;
terminal failure retains counts and requires reconciliation. Diagnostics do not
reclassify saved completion. Typed storage does not imply transactional cursor
commit or key replay. Read Editor/BeepSync/STORAGE-AND-OUTCOMES.md for remaining
promotion/DQ/platform boundaries and explicit schema persistence.
Import required-stage failures use optional typed transformation outcomes and
RecordsTransformationFailed; they never admit the original row or advance sync
cursors, and stop blind whole-run retry. Existing helper signatures remain but
built-in methods now surface safe exceptions rather than silent fallback. Read
Editor/Importing/TRANSFORMATION-OUTCOMES.md for custom/default/quality limits.
Required shipped expression/formula ASTs parse bounded syntax before field reads;
Boolean-only conditions, precedence, exact typed comparisons, invariant numbers,
Decimal rounding and lazy branches retain captured nested resolver selection.
Unused branches are syntax-checked only. See Importing/DEFAULTS-ADMISSION.md for
numeric/result bounds and remaining identity/NFEL/plugin/provider gates.
Required shipped query plans validate every filter before callbacks, bind closed
invariant context and retain typed aggregates without skipping malformed values.
Null collections deny; actual empty COUNT/EXISTS are 0/false. Streaming bounds,
root-status/cancellation/disposal do not qualify eager provider allocation, hidden
failures, isolation or explicit query-context ownership. Legacy/custom paths remain.
Required shipped identity/scope rules use explicit string email/application-role
keys and actual supported OS identity/folders, never imported-row or inferred
substitutes. ENV reads only its scope; SYSTEMPATH means Machine PATH. Host strings
are not authorization proof or captured run identity. Read defaults admission for
native ownership, platform limits and legacy/custom separation.
Required shipped dates use Gregorian ISO literals, invariant bounded formatting
and exact tick/calendar offsets. Nested bases resolve through the pinned roster
with actual context and must be DateTime/DateTimeOffset, not reparsed strings.
Kinds/offsets remain intact; dynamic clock leaves are not immutable run time or
named-zone DST qualification. Legacy direct and subclass semantics stay separate.
Required configuration uses explicit named flat string maps or declared Process
prefixes, never inferred editor/row/bare-variable sources. Validate the complete
bounded map before key selection; explicit-source failure and connection alias
conflicts deny. Capture is per resolution, not an automatic host/credential bridge
or immutable run configuration. See defaults admission for source names/limits.
Models EntityMetadataSnapshot.Capture owns supported metadata graphs without
source observers; legacy EntityStructure.Clone stays shallow. Field.Clone is fixed.
Read DataBase/METADATA-SNAPSHOTS.md in Models for graph limits and coordination;
actual sync metadata admission still needs integration. Engine generation now uses
source-sensitive identity and exact type selection. Bare cache seeds no longer
override metadata. Read ConfigUtil/GENERATED-TYPES.md in Engine for compatibility,
single-flight/retention and remaining loaded-assembly/package-consumer limits.

## Rules that matter

NFEL-1 has 99 new cases per TFM for complete bounded grammar and actual execution,
source/token/policy admission, typed/lazy values, lifecycle/timeout and bounded
defensive history. Select RuleEngine(new NfelParser()); other parser profiles stay
separate. Read Engine Rules/NFEL.md before altering semantics or accepting stored
tokens. This is not plugin sandboxing or complete host/provider/adapter qualification.

- Runtime data operations return `IErrorsInfo` (set `Flag` + `Message`) rather than throwing;
  guard clauses and DI misuse still throw.
- Log via `DMEEditor.AddLogMessage(...)` / `Logger.WriteLog(...)`, never `Console.WriteLine`.
- **Never swallow exceptions.** A bare `catch { }` is not allowed — every catch must report the
  exception through the in-scope channel: an `IDMEEditor` (`editor?.AddLogMessage("<Class>",
  $"<context>: {ex.Message}", DateTime.Now, 0, null, Errors.Warning)`), else a class logger, else
  `System.Diagnostics.Debug.WriteLine` + a comment for low-level classes with neither. Don't add an
  editor/logger dependency just to log; the flow may still continue after reporting. See `/CLAUDE.md`.
- Never hard-code a SQL dialect — resolve via `dmEditor.GetDataSourceHelper(DataSourceType.X)`.
- Write config through the `ConfigEditor` façade, never ad-hoc JSON.
- Form saves route through `UnitofWork<T>`; forms never write to a datasource directly.
- Datasource plugins implement `IDataSource` and are enriched (not discovered) by `[AddinAttribute]`,
  which has **no constructor parameters** — use object-initializer syntax.
- `IDMEEditor`, `IDataSource`, `IConfigEditor`, `UnitofWork<T>` ship as NuGet — signature changes are
  breaking releases. Preserve DI registrations and lifetimes (Singleton desktop, Scoped web).
- Some misspellings are baked into shipped contracts (`IErrorsInfo.Fucntion`, `PassedArgs.Messege`,
  `IBeepService.ConfigureationType`) — don't "fix" them casually.

## UnitofWork

```csharp
var uow = new UnitofWork<Product>(dmEditor, "northwind.db", "Products");
var p = uow.New();
uow.Add(p);        // not AddNew
uow.Update(p);     // not Modify
uow.Commit();
```

## Documentation — `Help/*.html` is mirrored to the website

`Help/` is published as the BeepDM product docs on the TheTechIdea website. The published copy lives in a
**separate repository**:

```
C:\Users\f_ald\source\repos\fahadTheTechIdea\MyWebSite\TheTechIdeaWeb\TheTechIdeaWeb.Web\wwwroot\Products Documentation\beepdm\
```

**Any add/edit/delete under `Help/` must be mirrored there in the same session** — otherwise the live site
ships stale docs. Applies to `.html`, `sphinx-style.css` and `navigation.js`; `README.md` and
`NAVIGATION_README.md` stay repo-only.

**Theme is shared** — `sphinx-style.css` and `navigation.js` are byte-identical in both places, pages link
them relatively, and pages are standalone HTML served from `wwwroot` (not wrapped in a Razor layout). A
copied page renders the same; there's no destination CSS to reconcile.

**Not a blind copy — diff before overwriting.** `formsmanager.html` is intentionally different: the public
copy has internal `DataManagementEngineStandard/Editor/Forms/.plans` references **stripped**. Never publish
internal repo paths or `.plans/` links. Run
`diff --strip-trailing-cr Help/<file> "<website>/<file>"` *before* overwriting — if it already differs,
find out why first; the difference may be deliberate, or the website copy may be the better one.

Otherwise the folders are a file-for-file content-identical mirror; only line endings differ (`Help/` = **LF**,
website = **CRLF**) — preserve that. Quote the website path; it contains a space. The mirror is its own git
repo, so it needs a separate commit — tell the user it has uncommitted changes.

See `/CLAUDE.md` for the full architecture, the `classHandler` driver-resolution bridge, and the
current list of known-broken things.
