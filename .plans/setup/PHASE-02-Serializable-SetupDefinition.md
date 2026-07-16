# Phase 2 — Serializable `SetupDefinition` *(keystone)*

**Goal:** Make a setup definition **data** instead of a C# object graph, so it can be versioned in
Git, diffed, reviewed, shipped to another app, driven from a CLI, validated in CI, stored remotely,
and authorized.

**Pre-condition:** Phase 1 complete (the default wizard actually runs).

**Files touched:** `DataManagementModelsStandard/SetUp/`, `DataManagementEngineStandard/SetUp/`

**Exit criteria:** a wizard round-trips C# → JSON → C# and produces an identical run; the JSON is
stable enough to diff.

---

## ✅ Status: complete

All items P2-01..08 landed (see the master tracker for the per-item summary). 161/161 tests green;
`tests/SetupWizardTests/DefinitionTests.cs` (26 tests) covers it.

**The exit criterion was too weak.** "Round-trips C# → JSON → C#" passed while a hand-written
definition still failed to load — because a round-trip writes and reads with the *same* settings, so
it cannot catch a settings bug. The real acceptance test is `HandWrittenJson_Validates_AndBuildsAWizard`:
a JSON string typed by a human, deserialized, validated, and built into a runnable wizard. It caught
two bugs the round-trip tests were blind to:

1. **Enums bound numerically.** `System.Text.Json` requires integer enum values by default, so
   `"databaseType": "SqlLite"` threw on bind. Numeric enum values are also *positional* — inserting a
   member into `DataSourceType` would silently re-point every stored definition at a different
   datasource. Fixed with named enum values.
2. **Output was PascalCase**, not the camelCase of the documented artifact.

Both were fixed by one shared `SetupJson.Options` (`DataManagementModelsStandard/SetUp/Definition/`)
that every serialization path uses — the serializer, the factory's binder, and each step's
`SerializeOptions`. If you add a step type or a serializer, route it through `SetupJson` or a
hand-written definition won't bind.

Also worth carrying forward: `ContentHash` depends on `ConnectionProperties.GuidID`, which defaults to
`Guid.NewGuid()`. A definition **read from its file** is stable; one **regenerated via `ToDefinition()`**
gets a fresh GuidID and thus a new hash — a spurious diff and a misleading audit record. Pinned by
`ContentHash_Differs_WhenConnectionGuidDiffers_DocumentsDiffChurnHazard`; revisit if `ToDefinition()`
ever becomes routine codegen.

---

## Why this is the keystone

Every other phase is blocked on it:

| Phase | Why it needs a data definition |
|---|---|
| P3 remote state | a shared store needs a portable artifact to key state against |
| P5 RBAC | you authorize *a definition*; you can't authorize a compiled object graph |
| P6 audit | "what was applied" must be a hash of something serializable |
| P7 multi-app | N apps means N definitions — today N compilations |
| P8 CLI/CI | a CLI can't `new SchemaSetupStepOptions { EntityTypes = ... }` |

The single blocking fact:

```csharp
// DataManagementModelsStandard/SetUp/Steps/SchemaSetupStepOptions.cs:16
public IReadOnlyList<Type> EntityTypes { get; set; }   // ← CLR Types. Not portable. Not serializable.
```

---

## 2-A  `SetupDefinition` model

**New:** `DataManagementModelsStandard/SetUp/Definition/SetupDefinition.cs`

```csharp
public sealed class SetupDefinition
{
    /// <summary>Schema version of THIS document. Bump on any breaking shape change.</summary>
    public int SchemaVersion { get; set; } = 1;

    public string Id { get; set; } = "default-setup";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }

    /// <summary>Free-form; resolved to a real environment in P7.</summary>
    public string Environment { get; set; } = "Development";

    public List<SetupStepDefinition> Steps { get; set; } = new();

    /// <summary>Stable SHA-256 over the canonical JSON. Audit (P6) binds to this.</summary>
    public string ContentHash { get; set; }
}

public sealed class SetupStepDefinition
{
    /// <summary>Unique within the definition. Matches ISetupStep.StepId.</summary>
    public string StepId { get; set; }

    /// <summary>Registered step-type key (e.g. "driver-provision"), NOT an assembly-qualified name.</summary>
    public string Type { get; set; }

    public List<string> DependsOn { get; set; } = new();
    public bool Enabled { get; set; } = true;

    /// <summary>Step-specific options, shape owned by the step type. See 2-B.</summary>
    public JsonElement? Options { get; set; }
}
```

**`Type` is a registry key, not an assembly-qualified type name.** An AQN in a definition file would
(a) leak internal namespaces into a user-editable artifact, (b) break on any refactor or version
bump, and (c) be an arbitrary-type-instantiation vector once definitions can come from a shared store
(P3). The registry (2-D) is the allow-list.

---

## 2-B  Retire `IReadOnlyList<Type>`

**File:** `DataManagementModelsStandard/SetUp/Steps/SchemaSetupStepOptions.cs`

```csharp
[Obsolete("Use EntityTypeNames. CLR Types cannot be serialized into a SetupDefinition. " +
          "Will be removed in the next major.")]
public IReadOnlyList<Type> EntityTypes { get; set; }

/// <summary>Assembly-qualified or simple type names, resolved via IAssemblyHandler at run time.</summary>
public IReadOnlyList<string> EntityTypeNames { get; set; }

/// <summary>Extra assemblies to probe when resolving EntityTypeNames.</summary>
public IReadOnlyList<string> ExtraAssemblyNames { get; set; }
```

**This is a shipped NuGet contract** (`TheTechIdea.Beep.DataManagementModels` v3.1.1) — additive +
`[Obsolete]`, never an edit. `SchemaSetupStep` resolves in priority order:

```csharp
private IReadOnlyList<Type> ResolveEntityTypes(SetupContext ctx)
{
    if (_options.EntityTypes?.Count > 0)                       // legacy path still works
        return _options.EntityTypes;

    if (_options.EntityTypeNames == null || _options.EntityTypeNames.Count == 0)
        return Array.Empty<Type>();

    var handler = ctx.Editor.assemblyHandler;
    var resolved = new List<Type>();
    var missing  = new List<string>();

    foreach (var name in _options.EntityTypeNames)
    {
        var t = handler.GetType(name);                         // existing type-cache path
        if (t != null) resolved.Add(t); else missing.Add(name);
    }

    if (missing.Count > 0)
        throw new SetupDefinitionException(
            $"Could not resolve entity type(s): {string.Join(", ", missing)}. " +
            $"Ensure the declaring assembly is loaded (LoadAllAssembly) or listed in ExtraAssemblyNames.");

    return resolved;
}
```

Unresolvable types must **fail loudly**. Silently seeding an empty type list would make
`SchemaSetupStep` create no schema and report success — exactly the failure mode Phase 1 exists to
eliminate. Note `SchemaSetupStep.CanSkip` hashes the entity list into `SetupState.SchemaHash`, so an
empty list would also poison the skip check on the next run.

---

## 2-C  `ISetupDefinitionSerializer`

**New:** `DataManagementModelsStandard/SetUp/Definition/ISetupDefinitionSerializer.cs`
**New:** `DataManagementEngineStandard/SetUp/Definition/JsonSetupDefinitionSerializer.cs`

```csharp
public interface ISetupDefinitionSerializer
{
    string Serialize(SetupDefinition definition);
    SetupDefinition Deserialize(string json);
    string ComputeContentHash(SetupDefinition definition);
}
```

**Diff-stability is a requirement, not a nicety** — the artifact is meant for code review:

- `WriteIndented = true`
- properties emitted in declaration order; `Steps` in declared order; `DependsOn` sorted ordinal
- `ContentHash` excluded from its own hash computation
- no `DateTime` / machine paths / GUIDs in the serialized shape

The engine already takes `Newtonsoft.Json` **and** uses `System.Text.Json` in
`SetupCheckpointStore`. Use **`System.Text.Json`** here to match the neighbouring setup code.

---

## 2-D  `ISetupStepFactory` — the allow-list

**New:** `DataManagementModelsStandard/SetUp/Definition/ISetupStepFactory.cs`
**New:** `DataManagementEngineStandard/SetUp/Definition/SetupStepFactory.cs`

```csharp
public interface ISetupStepFactory
{
    /// <summary>Register a step type key + how to build it from an options payload.</summary>
    void Register(string typeKey, Func<JsonElement?, ISetupStep> factory);

    bool CanCreate(string typeKey);
    ISetupStep Create(SetupStepDefinition definition);
    IReadOnlyCollection<string> RegisteredTypes { get; }
}
```

Built-in registrations (mirrors `DefaultSetupWizardFactory`'s current hardcoded sequence):

| `Type` key | Step | Options |
|---|---|---|
| `driver-provision` | `DriverProvisionStep` | `DriverProvisionStepOptions` |
| `connection-config` | `ConnectionConfigStep` | `ConnectionConfigStepOptions` |
| `schema-setup` | `SchemaSetupStep` | `SchemaSetupStepOptions` |
| `defaults-setup` | `DefaultsSetupStep` | `DefaultsSetupStepOptions` |
| `seeding` | `SeedingStep` | `SeedingStepOptions` |
| `data-import` | `DataImportStep` | `DataImportStepOptions` |

Two options classes are **declared inline in the Engine** today (`DefaultsSetupStepOptions` in
`DefaultsSetupStep.cs:12`, `DataImportStepOptions` in `DataImportStep.cs:12`) while the other four
live in Models. Move both to `DataManagementModelsStandard/SetUp/Steps/` — a definition's option
shapes belong with the contracts, and P8's CLI needs them without an Engine reference. Leave
`[Obsolete]` type-forwards if anything references them.

Non-serializable options need explicit handling — `SeedingStepOptions.Registry` is an
`ISeederRegistry` **instance**. It must be marked `[JsonIgnore]` and injected at build time from DI,
not carried in the definition:

```csharp
factory.Register("seeding", opts => new SeedingStep(new SeedingStepOptions
{
    Registry = _serviceProvider.GetService<ISeederRegistry>(),   // from DI, never from JSON
    // ... remaining options deserialized from `opts`
}));
```

---

## 2-E  Builder ⇄ definition

**File:** `SetUp/SetupWizardBuilder.cs`

```csharp
public static SetupWizardBuilder FromDefinition(SetupDefinition def, ISetupStepFactory factory);
public SetupDefinition ToDefinition();
```

`FromDefinition` validates before building: unknown `Type` key → throw naming the key and listing
`factory.RegisteredTypes`; duplicate `StepId` → throw (this is 1-A's crash, surfaced as a real
message); `DependsOn` targeting an unknown step → throw.

`ToDefinition()` requires each step to describe itself. Add to `ISetupStep` as a **default interface
method** so existing steps compile unchanged:

```csharp
// ISetupStep
string TypeKey => StepId;                          // built-ins override where id != type
JsonElement? SerializeOptions() => null;           // steps that opt in return their options
```

Round-tripping a step that returns `null` from `SerializeOptions()` loses its options — so
`ToDefinition()` warns for any step that hasn't opted in, rather than silently emitting a lossy
definition.

---

## 2-F  `SchemaVersion` + upgrade path

`SetupState` has **no version field**, and `SetupCheckpointStore.LoadPersistedState` swallows
deserialization failure with a bare `catch { return; }` — so a shape change **silently resets to a
fresh run and re-runs every step**. On a live database that's not a reset; it's a re-migration.

Add to both `SetupState` and `SetupDefinition`:

```csharp
public int SchemaVersion { get; set; } = 1;
```

**New:** `SetUp/Definition/SetupStateUpgrader.cs`

```csharp
public interface ISetupStateUpgrader
{
    int CurrentVersion { get; }
    bool CanUpgrade(int fromVersion);
    SetupState Upgrade(SetupState state);
}
```

`LoadPersistedState` then: read version → if `< Current` and upgradable, upgrade + persist → if
unknown/newer, **fail loudly** (don't silently start fresh) → if deserialization fails, log the
exception rather than swallowing it.

---

## 2-G  Validate without executing

**New:** `SetUp/Definition/SetupDefinitionValidator.cs`

```csharp
public interface ISetupDefinitionValidator
{
    IErrorsInfo Validate(SetupDefinition definition);   // structural — no IDMEEditor, no datasource
}
```

Checks: `SchemaVersion` supported; `StepId`s unique and non-empty; every `Type` registered; `DependsOn`
resolvable, acyclic, and (per 1-F) correctly ordered; options payloads deserialize against their
declared shapes; `EntityTypeNames` non-empty when `schema-setup` is present.

Returns `IErrorsInfo` per repo convention — data errors don't throw. Must run **without** a live
datasource: this is what P8's CI gate calls.

---

## 2-H  Tests

| Test | Guards |
|---|---|
| `Definition_RoundTrips_CSharp_To_Json_To_CSharp` | 2-C, 2-E |
| `Serialize_IsStable_AcrossRuns` (byte-identical ×2) | 2-C diff-stability |
| `ContentHash_Changes_WhenStepAdded` | 2-C |
| `ContentHash_Unchanged_ByCosmeticReorderOfDependsOn` | 2-C |
| `Factory_Throws_OnUnknownTypeKey` | 2-D allow-list |
| `Factory_InjectsSeederRegistry_FromDi_NotJson` | 2-D |
| `SchemaSetupStep_Resolves_EntityTypeNames_ViaAssemblyHandler` | 2-B |
| `SchemaSetupStep_Fails_Loudly_OnUnresolvableTypeName` | 2-B |
| `SchemaSetupStep_LegacyEntityTypes_StillWorks` | 2-B back-compat |
| `Validator_Detects_Cycle_And_DuplicateStepId` | 2-G |
| `LoadPersistedState_Fails_OnUnknownSchemaVersion` | 2-F |

## Files summary

| Action | File | Est. |
|---|---|---|
| New | `Models/SetUp/Definition/SetupDefinition.cs` | ~60 |
| New | `Models/SetUp/Definition/ISetupDefinitionSerializer.cs` | ~15 |
| New | `Models/SetUp/Definition/ISetupStepFactory.cs` | ~15 |
| New | `Models/SetUp/Definition/ISetupStateUpgrader.cs` | ~12 |
| Move | `DefaultsSetupStepOptions`, `DataImportStepOptions` → Models | ~30 |
| Modify | `Models/SetUp/Steps/SchemaSetupStepOptions.cs` | ~12 |
| Modify | `Models/SetUp/ISetupStep.cs` (2 DIMs) | ~6 |
| Modify | `Models/SetUp/SetupState.cs` (+`SchemaVersion`) | ~3 |
| New | `Engine/SetUp/Definition/JsonSetupDefinitionSerializer.cs` | ~90 |
| New | `Engine/SetUp/Definition/SetupStepFactory.cs` | ~120 |
| New | `Engine/SetUp/Definition/SetupDefinitionValidator.cs` | ~90 |
| New | `Engine/SetUp/Definition/SetupStateUpgrader.cs` | ~50 |
| Modify | `Engine/SetUp/SetupWizardBuilder.cs` | ~70 |
| Modify | `Engine/SetUp/Steps/SchemaSetupStep.cs` | ~40 |
| Modify | `Engine/SetUp/SetupCheckpointStore.cs` | ~25 |
| New | `tests/SetupWizardTests/DefinitionTests.cs` | ~250 |

---

## Example artifact

The deliverable — reviewable in a PR, diffable, portable across apps:

```json
{
  "schemaVersion": 1,
  "id": "northwind-setup",
  "name": "Northwind demo setup",
  "environment": "Development",
  "steps": [
    {
      "stepId": "driver-provision:SQLite",
      "type": "driver-provision",
      "dependsOn": [],
      "enabled": true,
      "options": { "packageName": "SQLite", "version": "1.0.118" }
    },
    {
      "stepId": "connection-config",
      "type": "connection-config",
      "dependsOn": ["driver-provision:SQLite"],
      "enabled": true,
      "options": {
        "connectionName": "northwind.db",
        "connectionString": "Data Source=./Beep/dbfiles/northwind.db",
        "databaseType": "SqlLite",
        "openConnection": true
      }
    },
    {
      "stepId": "schema-setup",
      "type": "schema-setup",
      "dependsOn": ["connection-config"],
      "enabled": true,
      "options": {
        "entityTypeNames": ["MyApp.Models.Product", "MyApp.Models.Category"],
        "detectRelationships": true,
        "strictPolicyMode": false
      }
    }
  ],
  "contentHash": "sha256:…"
}
```

Note `stepId: "driver-provision:SQLite"` — the 1-A fix is what makes N drivers expressible here at
all.
