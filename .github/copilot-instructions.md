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
dotnet test BeepDM.sln                  # runs SetupWizardTests + FormsManager.Tests
```

The engine emits ~9k CA1416 warnings — normal; look for `0 Error(s)`. `SetupWizardTests`
has one known pre-existing failure (`Build_Throws_WhenStepsOutOfOrder`).

## Rules that matter

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

See `/CLAUDE.md` for the full architecture, the `classHandler` driver-resolution bridge, and the
current list of known-broken things.
