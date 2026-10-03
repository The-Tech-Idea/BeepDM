using System.Collections;
using System.Globalization;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

[CollectionDefinition("Checkpoint restart", DisableParallelization = true)]
public sealed class CheckpointRestartCollection { }

[Collection("Checkpoint restart")]
public class PlanIntentTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }
    private sealed class Order { public int Id { get; set; } }

    private static (MigrationTestHarness Harness, MigrationManager Manager, MigrationPlanArtifact Plan) Build(EntityStructure schema = null)
    {
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), schema ?? Schema());
        var manager = harness.Build();
        return (harness, manager, manager.BuildMigrationPlanForTypes(new[] { typeof(Product) }));
    }

    private static EntityStructure Schema() => MigrationTestHarness.Entity("Product", "Id", "Name");

    [Theory]
    [InlineData("Fieldtype", "System.Int64")]
    [InlineData("Size", 72)]
    [InlineData("Size1", 72)]
    [InlineData("Size2", 12)]
    [InlineData("NumericPrecision", 12)]
    [InlineData("NumericScale", 4)]
    [InlineData("AllowDBNull", false)]
    [InlineData("IsKey", true)]
    [InlineData("IsIdentity", true)]
    [InlineData("IsAutoIncrement", true)]
    [InlineData("IsUnique", true)]
    [InlineData("IsRequired", true)]
    [InlineData("IsUnicode", false)]
    [InlineData("DefaultValue", "0|O:Name:C:")]
    [InlineData("Expression", "Id + 1")]
    [InlineData("ColumnName", "physical_id")]
    [InlineData("ColumnTypeName", "decimal(12,4)")]
    [InlineData("DatabaseGeneratedOptionName", "Computed")]
    [InlineData("OrdinalPosition", 2)]
    public void FieldDefinitions_ChangePlanHash(string propertyName, object value)
    {
        var original = Build().Plan;
        var changed = Schema();
        var property = typeof(EntityField).GetProperty(propertyName)!;
        property.SetValue(changed.Fields[0], Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture));
        Assert.NotEqual(original.PlanHash, Build(changed).Plan.PlanHash);
    }

    [Fact]
    public void CanonicalHash_IgnoresUnorderedFields_Guids_AndCulture()
    {
        var first = Build().Plan;
        var reordered = Schema();
        reordered.Fields.Reverse();
        reordered.GuidID = Guid.NewGuid().ToString();
        reordered.Fields[0].GuidID = Guid.NewGuid().ToString();
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal(first.PlanHash, Build(reordered).Plan.PlanHash);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void CanonicalHashAndSnapshot_DoNotUseHostJsonDefaults()
    {
        var baseline = Build().Plan.PlanHash;
        var settings = Newtonsoft.Json.JsonConvert.DefaultSettings;
        try
        {
            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new()
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };
            var (harness, manager, plan) = Build();
            Assert.Equal(baseline, plan.PlanHash);
            var changed = Schema();
            changed.Fields[0].IsIdentity = true;
            Assert.NotEqual(baseline, Build(changed).Plan.PlanHash);
            var loaded = manager.LoadMigrationPlan(plan.PlanId);
            Assert.Equal(baseline, loaded.PlanHash);
            Assert.True(manager.ExecuteMigrationPlan(loaded).Success);
            Assert.Single(harness.ReceivedEntities);
        }
        finally { Newtonsoft.Json.JsonConvert.DefaultSettings = settings; }
    }

    [Fact]
    public void CosmeticWorkflowChanges_DoNotInvalidateIntent()
    {
        var (harness, manager, plan) = Build();
        plan.LifecycleState = MigrationPlanLifecycleState.Approved;
        plan.Operations[0].Note = "reviewer wording changed";
        plan.ProviderCapabilities.PortabilityWarning = "different wording";
        foreach (var issue in plan.ReadinessIssues) issue.Message = "different wording";
        Assert.True(manager.ExecuteMigrationPlan(plan).Success);
        Assert.Single(harness.ReceivedEntities);
    }

    [Fact]
    public void IndexAndRelationDefinitions_ChangeHash_EvenWithoutApplyOptIn()
    {
        var baseline = Build().Plan.PlanHash;
        var indexed = Schema();
        indexed.Indexes.Add(new EntityIndex { Name = "IX_Product_Name", Columns = new() { "Name" }, IsUnique = true });
        Assert.NotEqual(baseline, Build(indexed).Plan.PlanHash);
        var related = Schema();
        related.Relations.Add(new RelationShipKeys
        {
            RalationName = "FK_Product_Customer", EntityColumnID = "Id", RelatedEntityID = "Customer",
            RelatedEntityColumnID = "Id", OnDeleteBehavior = "Cascade"
        });
        Assert.NotEqual(baseline, Build(related).Plan.PlanHash);
        var cascade = Build(related).Plan.PlanHash;
        related.Relations[0].OnDeleteBehavior = "Restrict";
        Assert.NotEqual(cascade, Build(related).Plan.PlanHash);
    }

    [Fact]
    public void CompositeKeyAndIndexColumnOrder_IsSemantic()
    {
        var schema = Schema();
        schema.PrimaryKeys.AddRange(schema.Fields);
        schema.Indexes.Add(new EntityIndex { Name = "IX_Product", Columns = new() { "Id", "Name" } });
        var original = Build(schema).Plan.PlanHash;
        schema.PrimaryKeys.Reverse();
        Assert.NotEqual(original, Build(schema).Plan.PlanHash);
        schema.PrimaryKeys.Reverse();
        schema.Indexes[0].Columns.Reverse();
        Assert.NotEqual(original, Build(schema).Plan.PlanHash);
    }

    [Fact]
    public void OriginalMetadataMutation_DoesNotChangePreviewOrProviderPayload()
    {
        var desired = Schema();
        desired.Fields[1].Size1 = 73;
        var (harness, manager, plan) = Build(desired);
        var preview = manager.GenerateDryRunReport(plan).Operations.SelectMany(item => item.DdlPreview).ToArray();
        desired.Fields[1].Size1 = 999;
        desired.Fields.Add(new EntityField { FieldName = "Unapproved", Fieldtype = "System.String" });
        Assert.Equal(preview, manager.GenerateDryRunReport(plan).Operations.SelectMany(item => item.DdlPreview).ToArray());
        var conversions = harness.ConversionCount;
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.True(result.Success, result.Message);
        var actual = Assert.Single(harness.ReceivedEntities);
        Assert.Equal(73, actual.Fields.Single(field => field.FieldName == "Name").Size1);
        Assert.DoesNotContain(actual.Fields, field => field.FieldName == "Unapproved");
        Assert.Equal(conversions, harness.ConversionCount);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("operation")]
    [InlineData("policy")]
    [InlineData("version")]
    [InlineData("governance")]
    [InlineData("performance")]
    public void MutatedPlan_IsRejectedBeforeProviderOrCheckpointWrites(string mutation)
    {
        var (harness, manager, plan) = Build();
        if (mutation == "schema") plan.Operations[0].SchemaSnapshot.DesiredSchema.Fields[0].IsIdentity = true;
        if (mutation == "operation") plan.Operations[0].TargetName = "Different";
        if (mutation == "policy") plan.ExecutionPolicy.AbortOnStepFailure = false;
        if (mutation == "version") plan.PlanHashVersion = 0;
        if (mutation == "governance") plan.GovernancePolicy.RequireApprovalForHighRisk = false;
        if (mutation == "performance") plan.PerformancePlan.Policy.EnableThrottledMode = true;
        var records = harness.History.Migrations.Count;
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Empty(harness.ProviderCalls);
        Assert.Equal(records, harness.History.Migrations.Count);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("guid")]
    [InlineData("connection")]
    public void ChangedTarget_IsRejected(string mutation)
    {
        var (harness, manager, plan) = Build();
        if (mutation == "name") harness.TargetName = "other";
        if (mutation == "guid") harness.TargetGuid = "other";
        if (mutation == "connection") harness.TargetConnectionProperties = new() { ConnectionString = "Server=other;Database=other" };
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Contains("target", result.Message);
        Assert.Empty(harness.ProviderCalls);
    }

    [Fact]
    public void ChangedLiveFieldDefinition_IsDrift_WithoutReDerivingDesiredMetadata()
    {
        var current = Schema();
        var desired = Schema();
        desired.Fields.Add(new EntityField { FieldName = "Price", Fieldtype = "System.Decimal" });
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), desired).WithExisting(current);
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        current.Fields[0].AllowDBNull = false;
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Empty(harness.ProviderCalls);
        Assert.True(manager.RunPreflightChecks(plan).SchemaDriftDetected);
    }

    [Fact]
    public void PolicyRevision_HasNewIdentity_AndRejectsOldTokenOrApproval()
    {
        var (harness, manager, plan) = Build();
        var token = plan.ExecutionCheckpoint.ExecutionToken;
        var policy = new MigrationExecutionPolicy { AbortOnStepFailure = false };
        Assert.False(manager.ExecuteMigrationPlan(plan, policy).Success);
        var revision = manager.CreateMigrationPlanRevision(plan, policy);
        Assert.NotEqual(plan.PlanHash, revision.PlanHash);
        Assert.NotEqual(plan.PlanId, revision.PlanId);
        Assert.False(manager.ExecuteMigrationPlan(revision, executionToken: token).Success);
        Assert.False(manager.ExecuteMigrationPlan(revision, policyOptions: new()
            { Approver = "operator", OverrideReason = "review", ApprovedPlanHash = plan.PlanHash }).Success);
        Assert.Empty(harness.ProviderCalls);
        var result = manager.ExecuteMigrationPlan(revision, policy);
        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public void CallerCannotChangeGovernanceContext_WithoutRevision()
    {
        var (harness, manager, plan) = Build();
        var options = new MigrationPolicyOptions { EnvironmentTier = MigrationEnvironmentTier.Production };
        var result = manager.ExecuteMigrationPlan(plan, policyOptions: options);
        Assert.False(result.Success);
        Assert.Contains("Governance", result.Message);
        Assert.Empty(harness.ProviderCalls);
        var revision = manager.CreateMigrationPlanRevision(plan, plan.ExecutionPolicy, options);
        Assert.NotEqual(plan.PlanHash, revision.PlanHash);
        Assert.Equal(MigrationEnvironmentTier.Production, revision.GovernancePolicy.EnvironmentTier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ModelPlan_CapturesSchema_ForResolvedAndUnresolvedTypes(bool resolveType)
    {
        var typeName = resolveType ? typeof(Product).FullName! : "No.Loaded.Model.Product";
        var entity = new MigrationModelEntity
        {
            ClrTypeFullName = typeName, TableName = "Product",
            Properties = new()
            {
                new() { PropertyName = "Id", FieldType = "System.Int32", IsPrimaryKey = true, IsNullable = false },
                new() { PropertyName = "Name", FieldType = "System.String", MaxLength = 73 }
            }
        };
        var model = new MigrationModel { Entities = new() { [typeName] = entity } };
        var harness = new MigrationTestHarness();
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForModel(model);
        Assert.NotNull(plan.Operations[0].SchemaSnapshot.DesiredSchema);
        entity.Properties[1].MaxLength = 999;
        var loaded = harness.Build().LoadMigrationPlan(plan.PlanId);
        Assert.Equal(plan.PlanHash, loaded.PlanHash);
        var result = harness.Build().ExecuteMigrationPlan(loaded);
        Assert.True(result.Success, result.Message);
        Assert.Equal(73, Assert.Single(harness.ReceivedEntities).Fields.Single(field => field.FieldName == "Name").Size);
    }

    [Fact]
    public void CompletedCheckpoint_StillRejectsChangedTarget()
    {
        var (harness, manager, plan) = Build();
        var completed = manager.ExecuteMigrationPlan(plan);
        Assert.True(completed.Success, completed.Message);
        harness.TargetGuid = "other";
        Assert.False(manager.ResumeMigrationPlan(completed.ExecutionToken).Success);
    }

    [Fact]
    public void ChangedPreviewCheckpointStep_CannotChangePrivateExecutionIntent()
    {
        var (harness, manager, plan) = Build();
        var checkpoint = plan.ExecutionCheckpoint;
        checkpoint.Steps[0].TargetName = "Unapproved";
        var records = harness.History.Migrations.Count;
        var result = manager.ExecuteMigrationPlan(plan, executionToken: checkpoint.ExecutionToken);
        Assert.True(result.Success, result.Message);
        Assert.True(harness.History.Migrations.Count > records);
        Assert.DoesNotContain(harness.ProviderCalls, call => call.Contains("Unapproved"));
    }

    [Fact]
    public void PreProviderColumnAdmissionFailure_ReloadsCapturedSchemaAndPolicy_InNewManager()
    {
        var desired = Schema();
        desired.Fields[1].Size1 = 73;
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), desired)
            .WithExisting(MigrationTestHarness.Entity("Product", "Id"));
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" &&
            Newtonsoft.Json.JsonConvert.DeserializeObject<MigrationExecutionCheckpoint>(record.Notes)!.Steps
                .Any(step => step.Status == MigrationExecutionStepStatus.Running)
            ? new(TheTechIdea.Beep.ConfigUtil.PersistenceWriteStatus.Failed) : new(TheTechIdea.Beep.ConfigUtil.PersistenceWriteStatus.Saved);
        var failed = manager.ExecuteMigrationPlan(plan);
        Assert.False(failed.Success);
        Assert.Empty(harness.ProviderCalls);
        harness.HistoryWrite = null;
        harness.ReceivedColumns.Clear();
        desired.Fields[1].Size1 = 999;
        var restarted = harness.Build();
        var resumed = restarted.ResumeMigrationPlan(failed.ExecutionToken);
        Assert.True(resumed.Success, resumed.Message);
        Assert.Equal(73, Assert.Single(harness.ReceivedColumns).Size1);
        Assert.Equal(plan.PlanHash, resumed.Checkpoint.ApprovedPlan.PlanHash);
        Assert.Equal(plan.ExecutionPolicy.TransientErrorMarkers, resumed.Checkpoint.ApprovedPlan.ExecutionPolicy.TransientErrorMarkers);
    }

    [Fact]
    public void PreProviderIndexAdmissionFailure_ReloadsOrderedColumnsAndBooleanOptions()
    {
        var desired = Schema();
        desired.Indexes.Add(new EntityIndex
        {
            Name = "IX_Product", Columns = new() { "Name", "Id" }, IsUnique = true,
            Options = new() { ["unique"] = true, ["fillfactor"] = 80 }
        });
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), desired).WithExisting(Schema());
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) }, applyIndexes: true);
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" &&
            Newtonsoft.Json.JsonConvert.DeserializeObject<MigrationExecutionCheckpoint>(record.Notes)!.Steps
                .Any(step => step.Status == MigrationExecutionStepStatus.Running)
            ? new(TheTechIdea.Beep.ConfigUtil.PersistenceWriteStatus.Failed) : new(TheTechIdea.Beep.ConfigUtil.PersistenceWriteStatus.Saved);
        var failed = manager.ExecuteMigrationPlan(plan);
        Assert.False(failed.Success);
        harness.HistoryWrite = null;
        harness.ReceivedIndexes.Clear();
        Assert.Empty(harness.ProviderCalls);
        desired.Indexes.Clear();
        var resumed = harness.Build().ResumeMigrationPlan(failed.ExecutionToken);
        Assert.True(resumed.Success, resumed.Message);
        var actual = Assert.Single(harness.ReceivedIndexes);
        Assert.Equal(new[] { "Name", "Id" }, actual.Columns);
        Assert.Equal(true, actual.Options["unique"]);
        Assert.Equal(80, Convert.ToInt32(actual.Options["fillfactor"]));
        var rollback = harness.Build().RollbackFailedExecution(failed.ExecutionToken);
        Assert.True(rollback.Success, rollback.Message);
        Assert.Single(rollback.ExecutedActions);
    }

    [Fact]
    public void LegacyCompletedCheckpoint_IsNotReportedAsSafeResume()
    {
        var (harness, manager, plan) = Build();
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.True(result.Success, result.Message);
        var record = harness.History.Migrations.Last(item => item.Name == "ExecuteMigrationPlan.Checkpoint");
        var old = Newtonsoft.Json.JsonConvert.DeserializeObject<MigrationExecutionCheckpoint>(record.Notes)!;
        old.ApprovedPlan = null;
        record.Notes = Newtonsoft.Json.JsonConvert.SerializeObject(old);
        var resumed = harness.Build().ResumeMigrationPlan(result.ExecutionToken);
        Assert.False(resumed.Success);
        Assert.Contains("Legacy", resumed.Message);
    }

    [Fact]
    public void ContinueAfterFailure_DoesNotPublishSuccessOrCompletedCheckpoint()
    {
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), Schema())
            .WithDesired(typeof(Order), MigrationTestHarness.Entity("SalesOrder", "Id"));
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product), typeof(Order) });
        plan = manager.CreateMigrationPlanRevision(plan, new MigrationExecutionPolicy { AbortOnStepFailure = false });
        harness.FailOps.Add("CreateEntityAs:Product");
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Equal(1, result.AppliedCount);
        Assert.Single(result.FailedSteps);
        Assert.Contains("CreateEntityAs:SalesOrder", harness.ProviderCalls);
        Assert.True(result.Checkpoint.HasFailed);
        Assert.False(result.Checkpoint.IsCompleted);
        var persisted = harness.Build().GetExecutionCheckpoint(result.ExecutionToken);
        Assert.True(persisted.HasFailed);
        Assert.False(persisted.IsCompleted);
    }

    [Fact]
    public void ProgressCallback_CannotRerouteLaterProviderPayloads()
    {
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), Schema())
            .WithDesired(typeof(Order), MigrationTestHarness.Entity("SalesOrder", "Id"));
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product), typeof(Order) });
        var checkpoint = plan.ExecutionCheckpoint;
        var progress = new InlineProgress(() => checkpoint.Steps[1].EntityName = "Unapproved");
        var result = manager.ExecuteMigrationPlan(plan, executionToken: checkpoint.ExecutionToken, progress: progress);
        Assert.True(result.Success, result.Message);
        Assert.Contains("CreateEntityAs:SalesOrder", harness.ProviderCalls);
        Assert.DoesNotContain("CreateEntityAs:Unapproved", harness.ProviderCalls);
        Assert.Equal(new[] { "Product", "SalesOrder" }, harness.ReceivedEntities.Select(entity => entity.EntityName));
    }

    private sealed class InlineProgress(Action action) : IProgress<PassedArgs>
    {
        public void Report(PassedArgs value) => action();
    }
}
