# Runtime And Datasource Ownership

## Deferred Registration

Use `services.AddBeepRuntime(options => ...)` for normal hosting. Registration
copies options and stores factories; it does not configure a runtime or create
directories. Resolving IBeepService initializes the full graph. Invalid explicit
roots fail instead of falling back to global/temp state.

Singleton is one runtime per provider; Scoped is one per scope; Transient is one
per IBeepService resolution. Two providers built from the same collection still
own different singleton graphs. Default metadata lists and items are per-runtime.
Using the same disk path intentionally shares persisted state; memory isolation
is not filesystem/tenant authorization isolation.

Default unkeyed/keyed component aliases follow the runtime lifetime. Key names:
Editor, ConfigEditor, Logger, Util, JsonLoader, AssemblyHandler and Errors.
Singleton/Scoped aliases point to their graph. Transient alias resolutions each
resolve a new runtime: resolve IBeepService once and use its properties for a
coherent transient graph. Pre-existing component descriptors are preserved but
are not injected into factory-created runtime internals.

DI owns resolved runtimes and components. Do not dispose them manually. Configure
once; replacement and initialization after disposal are rejected. Configure and
assembly/configuration loading coordinate with cleanup. A loading exception does
not mark the attempt complete; individual driver scan diagnostics remain separate.
Failed direct configuration releases its partial graph; create a new service to
retry rather than reusing the failed object.
Cleanup cancels/releases the token source and attempts remaining owned resources
after failures, logging them without replacing the primary operation failure.
Concrete editor config/loader ownership is not duplicated by the service facade.

InitializationTimeout is captured and validated, but is not yet an enforced
deadline. AdditionalProperties values are shallow-copied compatibility options,
not a guarantee of deep immutability or arbitrary dependency injection.

## Legacy Startup Migration

AddBeepServices(Action), Register and builder Build return an eager usable runtime
owned by the caller, separate from subsequently DI-resolved runtimes. The return
type remains IBeepService; cast the known concrete BeepService to dispose it.
Ignoring the eager return can retain resources. Prefer AddBeepRuntime for hosts
that only need registration. Direct new BeepService().Configure also remains
caller-owned; its service registrations create independent deferred graphs.

This is a behavioral compatibility change, not just a factory refactor. Hosts
must not expect the eager return to be identical to the DI runtime or assume
configuration added only to that eager object's mutable lists reaches DI.
Move such configuration into the host's explicit initialization path after its
runtime is resolved. Bootstrap/plugin consumer samples remain a release gate.

editor.GetBeepService resolves its own runtime owner. Process-state reset and
no-context registration validation are obsolete; reset is a no-op. Legacy
RegisterBeep.Services and EnvironmentService path APIs remain explicit single-host
compatibility surfaces. Static file ConnectionHelper APIs require explicit
Initialize(editor); new editor construction no longer binds all hosts to the
first process editor. They are not multi-tenant helpers.

## Datasource Registry

Framework editor/helper creation uses a weakly editor-keyed registry. Names are
case-insensitive and a configured GUID is preserved on the created source.
Concurrent name/GUID/helper/synchronous/asynchronous paths share one pending
creation for the connection identity. Completed sources appear in DataSources;
driver/connection/entity metadata is configured before publication.

Framework creation doesn't open the connection; provider constructors may still
perform I/O. OpenDataSource verifies provider state separately. Same-name sources
in different editors do not share cache, connection, removal or disposal state.

Failed configuration releases the unpublished instance and permits explicit
retry. A constructor that throws before returning owns its own partial resources.
No registry lock is held across plugin construction or awaits. Recursive waiting
on the same identity is rejected instead of deadlocking; different dependencies
can be resolved. Provider constructor cycles that discard ExecutionContext or
perform unrelated external waits are outside this recursion detection.

Removal detaches name/GUID references and invalidates a pending generation,
then closes/disposes the source. Later lookup may create a replacement if the
connection remains configured. Normal removal does not delete persisted connection
configuration. Existing local name-removal metadata cleanup and GUID-removal
connection-list cleanup remain distinct compatibility behaviors.

Closed maps to successful close, not Open. Close/Dispose failures are observable
through false removal/close results and diagnostics; even a failed close doesn't
prevent disposal. Detachment is not rolled back on cleanup failure. A metadata
cleanup failure also doesn't skip source release.

Editor-routed open/close operations coordinate with removal/disposal. A synchronous
provider removal callback defers disposal until its admitted operation returns.
Direct helper disposal removes only that instance's generation, never a newer
replacement with the same name. Ownerless global cache overloads are obsolete;
they remain a manual compatibility cache, not the normal runtime registry.

## Concurrency And Shutdown Limits

- DataSources and configuration lists remain mutable public compatibility views.
  Serialize external mutations and don't share source instances between editors.
- ErrorObject is legacy mutable diagnostic state, not a thread-local operation
  result. Lifecycle coordination doesn't make all editor services, provider CRUD,
  transactions or UI-bound state universally thread-safe.
- CreateDataSourceFromDefinition returns a raw caller-owned instance. The registry
  doesn't own it until the caller explicitly adds it to the compatibility view.
- Disposal rejects new registry work and late publication. Legacy constructors
  cannot be forcibly cancelled. Disposal may return before an already-running
  constructor finishes; its returned instance is eventually released, not cached.
- The winning concurrent Dispose call performs cleanup once; another concurrent
  Dispose may return before that cleanup finishes. Stop mutating owned state once
  shutdown begins. General async draining is not yet a public contract.
- Plugin unload, Forms/health timer draining, editor service subscriptions,
  cancellation of arbitrary provider CRUD, and complete secret protection are
  separate unfinished audits. Don't infer them from these lifecycle tests.

## Verification

RuntimeIsolationTests covers provider/options/lifetime isolation, aliases,
legacy startup ownership and service cleanup. DataSourceLifecycleTests covers
single-flight construction, generation invalidation, name/GUID removal, close
coordination, recursive construction and independent cleanup failures. The
fixtures use recording mocks and an in-memory implementation, not live external
providers. Run all reliability TFMs and the solution after changes; consult the
framework execution log for platform/version/results and remaining release gates.
