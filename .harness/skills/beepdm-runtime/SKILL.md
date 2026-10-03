---
name: beepdm-runtime
description: Configure BeepDM dependency injection, provider/scoped runtime ownership, keyed component aliases, or migrate eager AddBeepServices/Build startup to deferred AddBeepRuntime. Use for hosting and datasource lifecycle isolation, not setup wizard steps or provider CRUD.
---

# BeepDM Runtime Registration

Ground changes in the current Engine Services and DMEEditor sources. The
`Beep.Container` spelling is a namespace, not a separate package.

## Deferred Hosting

`AddBeepRuntime(Action<BeepServiceOptions>)` registers factories without creating
a runtime or touching storage. Options are captured at registration; each provider
creates its own graph. Singleton is one graph per provider, Scoped one per scope,
Transient one per IBeepService resolution.

```csharp
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Services;

var services = new ServiceCollection();
services.AddBeepRuntime(options =>
{
    options.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "app-data");
    options.AppRepoName = "orders";
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.EnableAssemblyLoading = false; // Enable when plugin discovery is required.
});
using var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
using var scope = provider.CreateScope();
var runtime = scope.ServiceProvider.GetRequiredService<IBeepService>();
var editor = runtime.DMEEditor;
```

EnableAssemblyLoading defaults to true. Explicit invalid/uncreatable roots fail;
they do not rebind to global/temp configuration. Default driver/query/type metadata
is initialized per runtime, not via process-wide once-only flags.

## Graph And Ownership Rules

- Resolve scoped services inside a scope. Do not dispose DI-owned components
  manually; the provider/scope owns their lifetime.
- Keyed aliases are Editor, ConfigEditor, Logger, Util, JsonLoader, AssemblyHandler,
  and Errors. Unkeyed aliases exist too. For Singleton/Scoped, default aliases
  refer to the same graph within their lifetime.
- Transient aliases each resolve a fresh IBeepService. For a coherent transient
  graph resolve IBeepService once and access its properties; don't resolve an
  editor and config alias separately and assume they belong together.
- Pre-existing component descriptors are preserved, but do not become injected
  components inside the factory-created runtime. Resolve the runtime's properties
  when internal graph consistency matters.
- Duplicate IBeepService registration in one collection is rejected. Two providers
  built from that collection still own different singletons. Same path options
  intentionally refer to the same persisted files, not independent disk state.
- Configure once. Reconfiguration and post-disposal initialization are rejected.
  Cleanup attempts remaining resources even after close/dispose/logger errors.
- InitializationTimeout is currently captured/validated, not an enforced deadline.
  Don't promise cancellation of a legacy constructor or assembly loader.

## Legacy Migration

`AddBeepServices(Action)`, `Register(...)`, and builder `Build()` return an eager,
caller-owned runtime separate from subsequently DI-resolved runtimes. Dispose
that returned concrete BeepService. Ignoring it can retain startup resources.
Migrate normal hosts to AddBeepRuntime; do not capture an eager runtime in factories.

Real builder methods include WithDirectory, WithAppRepo, WithAssemblyLoading,
WithViewDiscovery, WithAssembliesToScan, AsSingleton/AsScoped/AsTransient.
Don't generate WithDirectoryPath/WithAppRepoName or AddBeepForWeb/Desktop/Blazor:
those shortcuts are not implemented runtime APIs here.

`editor.GetBeepService()` resolves that editor's owning service, not the last
process-global runtime. No-context static validation/reset helpers are obsolete;
ResetRegistrationState is a no-op. Legacy RegisterBeep.Services and
EnvironmentService folder state remain single-host compatibility surfaces.
Static file ConnectionHelper methods require explicit legacy Initialize(editor);
DMEEditor no longer implicitly binds every host to the first process editor.

## Datasource Lifecycle

Normal helper/editor creation uses an editor-owned registry, including local
creation. Name/GUID/case variants converge on one published source per generation.
Framework creation does not automatically open the connection, though a provider
constructor may perform I/O. Call OpenDataSource and verify ConnectionState.Open.
Metadata/driver/properties are set before publication.

Removal detaches both aliases, closes/disposes the runtime source, and invalidates
pending creation. A later lookup may recreate it if its connection remains configured.
Normal removal doesn't delete the stored connection; historical CreateLocal
metadata cleanup remains distinct for name/GUID removal. Failed cleanup returns
false, but still detaches and attempts disposal.

Dispose during construction rejects late publication and eventually releases the
constructed instance; it cannot interrupt or await every legacy constructor.
Open/close operations admitted through the editor coordinate with removal.
A provider's synchronous removal callback defers source release until that
operation returns. Recursive construction of the same identity fails rather
than deadlocking; resolving a different dependency is supported.

The public DataSources list is a compatibility view, not a thread-safe collection.
Serialize external list/config mutation. Direct IDataSource CRUD and
CreateDataSourceFromDefinition's caller-owned raw instances do not gain universal
thread safety. Do not directly dispose an editor-owned source; use removal or
DataSourceLifecycleHelper.DisposeDataSourceAsync. Ownerless legacy helper cache
overloads are obsolete and aren't used for automatic runtime registration.

## Working Set And Proof

- DataManagementEngineStandard/Services/RegisterBeepinServiceCollection.cs
- DataManagementEngineStandard/Services/BeepService.cs
- DataManagementEngineStandard/Services/EnvironmentService.cs
- DataManagementEngineStandard/Editor/DM/DMEEditor.cs
- DataManagementEngineStandard/Helpers/EditorDataSourceRegistry.cs
- DataManagementEngineStandard/Helpers/DataSourceLifecycleHelper.cs
- tests/FrameworkReliabilityTests/RuntimeIsolationTests.cs
- tests/FrameworkReliabilityTests/DataSourceLifecycleTests.cs

Run the reliability project on all supported TFMs and the solution after changes.
Mocks/in-memory lifecycle fixtures prove orchestration, not external-provider
transaction, plugin unloading, timer drain, secret protection or distributed storage.
