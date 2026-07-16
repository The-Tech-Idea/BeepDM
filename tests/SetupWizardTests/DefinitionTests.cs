using System.Text.Json;
using TheTechIdea.Beep.SetUp.Definition;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 2 (.plans/setup/PHASE-02-Serializable-SetupDefinition.md) — the keystone:
/// a setup definition must be data, not a C# object graph.
/// </summary>
public class DefinitionTests
{
    private static readonly JsonSetupDefinitionSerializer Serializer = new();
    private static SetupStepFactory NewFactory(ISeederRegistry seeders = null) => new(seeders);

    /// <summary>
    /// Fixed so the fixture is deterministic. <see cref="ConnectionProperties.GuidID"/> defaults to
    /// <c>Guid.NewGuid()</c>, so an authored definition pins it; two definitions with different
    /// GuidIDs describe different connections and are *expected* to hash differently.
    /// </summary>
    private const string FixedConnectionGuid = "11111111-2222-3333-4444-555555555555";

    private static SetupDefinition SampleDefinition() => new()
    {
        Id = "northwind-setup",
        Name = "Northwind demo",
        Environment = "Development",
        Steps =
        {
            new SetupStepDefinition
            {
                StepId = "driver-provision",
                Type = SetupStepFactory.TypeKeys.DriverProvision,
                Options = JsonSerializer.SerializeToElement(new DriverProvisionStepOptions
                {
                    PackageName = "SQLite",
                    Version = "1.0.118"
                }, SetupJson.Options)
            },
            new SetupStepDefinition
            {
                StepId = "connection-config",
                Type = SetupStepFactory.TypeKeys.ConnectionConfig,
                DependsOn = { "driver-provision" },
                Options = JsonSerializer.SerializeToElement(new ConnectionConfigStepOptions
                {
                    ConnectionProperties = new ConnectionProperties
                    {
                        GuidID = FixedConnectionGuid,
                        ConnectionName = "northwind.db",
                        DatabaseType = Utilities.DataSourceType.SqlLite
                    }
                }, SetupJson.Options)
            }
        }
    };

    // ── round-trip + diff stability (2-C, 2-E) ───────────────────────────────

    [Fact]
    public void Definition_RoundTrips_ThroughJson()
    {
        var json = Serializer.Serialize(SampleDefinition());
        var back = Serializer.Deserialize(json);

        Assert.Equal("northwind-setup", back.Id);
        Assert.Equal(2, back.Steps.Count);
        Assert.Equal(SetupStepFactory.TypeKeys.DriverProvision, back.Steps[0].Type);
        Assert.Equal(new[] { "driver-provision" }, back.Steps[1].DependsOn);

        // Options must survive — a nullable JsonElement silently drops without a converter.
        Assert.NotNull(back.Steps[0].Options);
        var opts = back.Steps[0].Options!.Value.Deserialize<DriverProvisionStepOptions>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("SQLite", opts!.PackageName);
    }

    [Fact]
    public void Serialize_IsByteIdentical_AcrossRuns()
    {
        // The artifact is reviewed in a PR; unstable output would churn every diff.
        Assert.Equal(Serializer.Serialize(SampleDefinition()), Serializer.Serialize(SampleDefinition()));
    }

    [Fact]
    public void ContentHash_Changes_WhenStepAdded()
    {
        var a = SampleDefinition();
        var b = SampleDefinition();
        b.Steps.Add(new SetupStepDefinition { StepId = "schema-setup", Type = SetupStepFactory.TypeKeys.SchemaSetup });

        Assert.NotEqual(Serializer.ComputeContentHash(a), Serializer.ComputeContentHash(b));
    }

    [Fact]
    public void ContentHash_Unchanged_ByCosmeticDependsOnReorder()
    {
        var a = SampleDefinition();
        a.Steps[1].DependsOn = new List<string> { "driver-provision", "defaults-setup" };

        var b = SampleDefinition();
        b.Steps[1].DependsOn = new List<string> { "defaults-setup", "driver-provision" };

        // DependsOn is a set, not a sequence — reordering it is semantically identical.
        Assert.Equal(Serializer.ComputeContentHash(a), Serializer.ComputeContentHash(b));
    }

    [Fact]
    public void ContentHash_Differs_WhenConnectionGuidDiffers_DocumentsDiffChurnHazard()
    {
        var a = SampleDefinition();
        var b = SampleDefinition();
        var connB = b.Steps[1].Options!.Value.Deserialize<ConnectionConfigStepOptions>(SetupJson.Options)!;
        connB.ConnectionProperties.GuidID = Guid.NewGuid().ToString();
        b.Steps[1].Options = JsonSerializer.SerializeToElement(connB, SetupJson.Options);

        // Documents a real hazard rather than asserting a wish: ConnectionProperties.GuidID defaults
        // to Guid.NewGuid(), so a definition REGENERATED from code (rather than read from its file)
        // gets a fresh GuidID and therefore a fresh ContentHash — a spurious diff, and an audit
        // record that looks like a different definition. Author the GuidID once and keep it in the
        // file. Revisit if ToDefinition() ever becomes a routine codegen step.
        Assert.NotEqual(Serializer.ComputeContentHash(a), Serializer.ComputeContentHash(b));
    }

    [Fact]
    public void ContentHash_ExcludesItself()
    {
        var a = SampleDefinition();
        var first = Serializer.ComputeContentHash(a);
        a.ContentHash = first;

        Assert.Equal(first, Serializer.ComputeContentHash(a));
    }

    // ── the allow-list (2-D) ─────────────────────────────────────────────────

    [Fact]
    public void Factory_Throws_OnUnknownTypeKey()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            NewFactory().Create(new SetupStepDefinition { StepId = "x", Type = "not-a-real-type" }));

        Assert.Contains("unknown type", ex.Message);
        Assert.Contains(SetupStepFactory.TypeKeys.SchemaSetup, ex.Message); // lists known types
    }

    [Fact]
    public void Factory_DoesNotAccept_AssemblyQualifiedTypeNames()
    {
        // A definition names a registered key, never an arbitrary type — otherwise a definition
        // from a shared store becomes an arbitrary-type-instantiation vector.
        Assert.False(NewFactory().CanCreate(typeof(SchemaSetupStep).AssemblyQualifiedName));
    }

    [Fact]
    public void Factory_InjectsSeederRegistry_FromDi_NotJson()
    {
        var registry = new SeederRegistry();
        var step = (SeedingStep)NewFactory(registry).Create(new SetupStepDefinition
        {
            StepId = "seeding",
            Type = SetupStepFactory.TypeKeys.Seeding,
            Options = JsonSerializer.SerializeToElement(new { seederFilter = new[] { "roles" } })
        });

        Assert.Same(registry, step.Options.Registry);
        Assert.Equal(new[] { "roles" }, step.Options.SeederFilter);
    }

    [Fact]
    public void SeedingOptions_NeverSerializeRegistry()
    {
        var json = JsonSerializer.Serialize(new SeedingStepOptions { Registry = new SeederRegistry() });
        Assert.DoesNotContain("Registry", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── builder interop (2-E) ────────────────────────────────────────────────

    [Fact]
    public void FromDefinition_BuildsRunnableWizard()
    {
        var wizard = SetupWizardBuilder.FromDefinition(SampleDefinition(), NewFactory()).Build();

        Assert.Equal(2, wizard.Steps.Count);
        Assert.Equal("driver-provision", wizard.Steps[0].StepId);
        Assert.Equal("connection-config", wizard.Steps[1].StepId);
    }

    [Fact]
    public void FromDefinition_SkipsDisabledSteps()
    {
        var def = SampleDefinition();
        def.Steps[1].Enabled = false;

        var wizard = SetupWizardBuilder.FromDefinition(def, NewFactory()).Build();

        Assert.Single(wizard.Steps);
    }

    [Fact]
    public void FromDefinition_Rejects_NewerSchemaVersion()
    {
        var def = SampleDefinition();
        def.SchemaVersion = SetupDefinition.CurrentSchemaVersion + 1;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SetupWizardBuilder.FromDefinition(def, NewFactory()));

        Assert.Contains("newer than this build supports", ex.Message);
    }

    [Fact]
    public void ToDefinition_RoundTrips_BackToEquivalentWizard()
    {
        var original = SetupWizardBuilder.FromDefinition(SampleDefinition(), NewFactory());
        var projected = original.ToDefinition();
        var rebuilt = SetupWizardBuilder.FromDefinition(projected, NewFactory()).Build();

        Assert.Equal(2, rebuilt.Steps.Count);
        Assert.Equal("driver-provision", rebuilt.Steps[0].StepId);
        Assert.Equal(new[] { "driver-provision" }, rebuilt.Steps[1].DependsOn);
    }

    [Fact]
    public void ToDefinition_UsesBareTypeKey_ForQualifiedDriverStepIds()
    {
        var builder = new SetupWizardBuilder()
            .AddStep(new DriverProvisionStep(new DriverProvisionStepOptions
            {
                StepId = DriverProvisionStep.BuildStepId("SQLite"),
                PackageName = "SQLite"
            }));

        var def = builder.ToDefinition();

        // Id is qualified; Type must stay the registered key or the factory can't resolve it.
        Assert.Equal("driver-provision:SQLite", def.Steps[0].StepId);
        Assert.Equal(SetupStepFactory.TypeKeys.DriverProvision, def.Steps[0].Type);
        Assert.True(NewFactory().CanCreate(def.Steps[0].Type));
    }

    // ── validation without a datasource (2-G) ────────────────────────────────

    [Fact]
    public void Validator_Passes_ValidDefinition_WithoutEditorOrDataSource()
    {
        // This is the CI gate: it must run on a PR with no database in sight.
        var result = new SetupDefinitionValidator(NewFactory()).Validate(SampleDefinition());

        Assert.NotEqual(Errors.Failed, result.Flag);
    }

    [Fact]
    public void Validator_Detects_UnknownType()
    {
        var def = SampleDefinition();
        def.Steps[0].Type = "bogus";

        var result = new SetupDefinitionValidator(NewFactory()).Validate(def);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("unknown type", result.Message);
    }

    [Fact]
    public void Validator_Detects_DuplicateStepId()
    {
        var def = SampleDefinition();
        def.Steps[1].StepId = def.Steps[0].StepId;

        var result = new SetupDefinitionValidator(NewFactory()).Validate(def);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("Duplicate step id", result.Message);
    }

    [Fact]
    public void Validator_Detects_OutOfOrderDependency()
    {
        var def = SampleDefinition();
        (def.Steps[0], def.Steps[1]) = (def.Steps[1], def.Steps[0]);

        var result = new SetupDefinitionValidator(NewFactory()).Validate(def);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("declared after it", result.Message);
    }

    [Fact]
    public void Validator_Detects_Cycle()
    {
        var def = new SetupDefinition
        {
            Id = "cyclic",
            Steps =
            {
                new SetupStepDefinition { StepId = "a", Type = SetupStepFactory.TypeKeys.DefaultsSetup, DependsOn = { "b" } },
                new SetupStepDefinition { StepId = "b", Type = SetupStepFactory.TypeKeys.DefaultsSetup, DependsOn = { "a" } }
            }
        };

        var result = new SetupDefinitionValidator(NewFactory()).Validate(def);

        Assert.Equal(Errors.Failed, result.Flag);
        // The out-of-order rule fires first for this shape; either is a correct rejection.
        Assert.True(result.Message.Contains("Cycle") || result.Message.Contains("declared after it"));
    }

    [Fact]
    public void Validator_Rejects_NewerSchemaVersion()
    {
        var def = SampleDefinition();
        def.SchemaVersion = SetupDefinition.CurrentSchemaVersion + 1;

        var result = new SetupDefinitionValidator(NewFactory()).Validate(def);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("newer than this build", result.Message);
    }

    [Fact]
    public void Validator_Detects_UnbindableOptions()
    {
        var def = SampleDefinition();
        def.Steps[0].Options = JsonSerializer.SerializeToElement(new { packageName = 12345 });

        var result = new SetupDefinitionValidator(NewFactory()).Validate(def);

        Assert.Equal(Errors.Failed, result.Flag);
    }

    // ── the artifact contract: hand-authored JSON must work ──────────────────

    /// <summary>
    /// The end-to-end promise of Phase 2, and the only test that exercises the actual use case:
    /// a definition a human typed into a file, not one round-tripped from C#.
    /// This is what caught enums binding numerically — round-trip tests cannot.
    /// </summary>
    private const string HandWrittenDefinition = """
    {
      "schemaVersion": 1,
      "id": "northwind-setup",
      "name": "Northwind demo setup",
      "environment": "Development",
      "steps": [
        {
          "stepId": "driver-provision",
          "type": "driver-provision",
          "dependsOn": [],
          "enabled": true,
          "options": { "packageName": "SQLite", "version": "1.0.118" }
        },
        {
          "stepId": "connection-config",
          "type": "connection-config",
          "dependsOn": ["driver-provision"],
          "enabled": true,
          "options": {
            "connectionProperties": {
              "connectionName": "northwind.db",
              "connectionString": "Data Source=./Beep/dbfiles/northwind.db",
              "databaseType": "SqlLite"
            },
            "openConnection": true
          }
        },
        {
          "stepId": "schema-setup",
          "type": "schema-setup",
          "dependsOn": ["connection-config"],
          "enabled": true,
          "options": {
            "entityTypeNames": ["MyApp.Models.Product"],
            "detectRelationships": true
          }
        }
      ]
    }
    """;

    [Fact]
    public void HandWrittenJson_Validates_AndBuildsAWizard()
    {
        var def = Serializer.Deserialize(HandWrittenDefinition);
        var factory = NewFactory();

        var validation = new SetupDefinitionValidator(factory).Validate(def);
        Assert.NotEqual(Errors.Failed, validation.Flag);

        var wizard = SetupWizardBuilder.FromDefinition(def, factory).Build();

        Assert.Equal(new[] { "driver-provision", "connection-config", "schema-setup" },
                     wizard.Steps.Select(s => s.StepId));
    }

    [Fact]
    public void HandWrittenJson_BindsStringEnums()
    {
        var def = Serializer.Deserialize(HandWrittenDefinition);
        var step = (ConnectionConfigStep)NewFactory().Create(def.Steps[1]);

        // "SqlLite" as a NAME. System.Text.Json requires numeric enums by default, so without
        // SetupJson's converter a hand-authored definition simply fails to bind.
        Assert.Equal(Utilities.DataSourceType.SqlLite, step.Options.ConnectionProperties.DatabaseType);
    }

    [Fact]
    public void SerializedDefinition_WritesEnumsAsNames_NotOrdinals()
    {
        var json = Serializer.Serialize(SampleDefinition());

        // Ordinals are positional: inserting a value into DataSourceType would silently re-point
        // every stored definition at a different datasource. Names survive enum edits.
        Assert.Contains("SqlLite", json);
        Assert.DoesNotContain($"\"databaseType\": {(int)Utilities.DataSourceType.SqlLite}", json);
        // Property names are camelCase, matching the documented artifact.
        Assert.Contains("\"schemaVersion\"", json);
        Assert.DoesNotContain("\"SchemaVersion\"", json);
    }

    [Fact]
    public void SchemaSetupOptions_EntityTypeNames_SurviveRoundTrip()
    {
        var def = Serializer.Deserialize(HandWrittenDefinition);
        var step = (SchemaSetupStep)NewFactory().Create(def.Steps[2]);

        // The whole point of 2-B: the portable replacement for IReadOnlyList<Type>.
        Assert.Equal(new[] { "MyApp.Models.Product" }, step.Options.EntityTypeNames);
    }
}
