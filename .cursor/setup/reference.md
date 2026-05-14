# Setup Framework Reference

Complete end-to-end patterns for authoring and using the 8-phase Setup Framework.

## Scenario A: Create a Basic Setup Wizard

```csharp
using TheTechIdea.Beep.SetUp;
using TheTechIdea.Beep.SetUp.Steps;
using TheTechIdea.Beep.SetUp.Seeding;
using TheTechIdea.Beep.ConfigUtil;

public class ProductAppSetup
{
    public ISetupWizard CreateWizard(IDMEEditor editor, ISeederRegistry seeders)
    {
        var connProps = new ConnectionProperties
        {
            ConnectionName = "ProductDB",
            DatabaseType = DataSourceType.SqlLite,
            ConnectionString = "Data Source=./products.db"
        };

        var entityTypes = new[] { typeof(Product), typeof(Category), typeof(Supplier) };

        var wizard = new SetupWizardBuilder()
            .WithId("product-app-v1")
            .WithEnvironment("Development")
            .WithStateFile("./setup-state.json")
            
            // Phase 2: Driver + Connection
            .AddStep(new DriverProvisionStep(new DriverProvisionStepOptions
            {
                PackageName = "TheTechIdea.Beep.DataSources.SQLite"
            }))
            
            .AddStep(new ConnectionConfigStep(new ConnectionConfigStepOptions
            {
                ConnectionProperties = connProps,
                OpenConnection = true
            }))
            
            // Phase 3: Schema
            .AddStep(new SchemaSetupStep(new SchemaSetupStepOptions
            {
                EntityTypes = entityTypes,
                DetectRelationships = true,
                StrictPolicyMode = false
            }))
            
            // Phase 4: Seeding
            .AddStep(new SeedingStep(new SeedingStepOptions
            {
                Registry = seeders
            }))
            
            .Build();

        return wizard;
    }
}
```

## Scenario B: Run with Desktop Adapter (WinForms)

```csharp
using TheTechIdea.Beep.SetUp.Adapters;

public partial class SetupForm : Form
{
    private async void RunSetupAsync()
    {
        var editor = new DMEEditor();
        var seeders = new SeederRegistry();
        
        // Discover seeders from executing assembly
        seeders.DiscoverFromAssemblies(new[] { typeof(SetupForm).Assembly });

        var appSetup = new ProductAppSetup();
        var wizard = appSetup.CreateWizard(editor, seeders);
        var context = new SetupContext { Editor = editor };

        var adapter = new DesktopSetupWizardAdapter(
            progressArgs =>
            {
                progressLabel.Text = progressArgs.Messege;
                progressBar.Value = Math.Min(progressArgs.ParameterInt1, 100);
            },
            report =>
            {
                if (report.Succeeded)
                {
                    MessageBox.Show($"Setup completed in {report.TotalElapsed.TotalSeconds:F1}s");
                    this.Close();
                }
                else
                {
                    var failed = report.StepResults.FirstOrDefault(r => !r.Succeeded);
                    MessageBox.Show($"Setup failed at {failed.StepName}: {failed.Message}", "Error");
                }
            });

        try
        {
            await adapter.RunAsync(wizard, context, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Setup cancelled by user.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error");
        }
    }
}
```

## Scenario C: Resume from Checkpoint

```csharp
public class SetupCoordinator
{
    public async Task<SetupReport> RunWithResume(
        ISetupWizard wizard,
        SetupContext context,
        ISetupWizardAdapter adapter,
        string checkpointPath)
    {
        context.Options.StateFilePath = checkpointPath;

        // First attempt
        var report = await adapter.RunAsync(wizard, context);
        if (report.Succeeded)
            return report;

        // After crash/user restart: resume
        // (assumes SetupWizard still exists or was recreated with same steps)
        var report2 = await wizard.Resume(context);
        return report2;
    }
}
```

## Scenario D: Custom Seeder

```csharp
using TheTechIdea.Beep.SetUp.Seeding;

public class RolesSeeder : ReferenceDataSeederBase<RoleEntity>
{
    public override string SeederId => "roles-seeder";
    public override string SeederName => "System Roles";
    
    // Roles must seed before Users
    public override IReadOnlyList<string> DependsOn => new[] { /* no deps */ };
    protected override string TargetEntityName => "Roles";

    protected override IReadOnlyList<RoleEntity> GetRecords() =>
        new[]
        {
            new RoleEntity { RoleId = 1, RoleName = "Admin", Description = "Administrator" },
            new RoleEntity { RoleId = 2, RoleName = "User", Description = "Regular User" },
            new RoleEntity { RoleId = 3, RoleName = "Guest", Description = "Guest Access" }
        };
}

public class UsersSeeder : ReferenceDataSeederBase<UserEntity>
{
    public override string SeederId => "users-seeder";
    public override string SeederName => "System Users";
    
    // Users depend on Roles
    public override IReadOnlyList<string> DependsOn => new[] { "roles-seeder" };
    protected override string TargetEntityName => "Users";

    protected override IReadOnlyList<UserEntity> GetRecords() =>
        new[]
        {
            new UserEntity { UserId = 1, Username = "admin", RoleId = 1 },
            new UserEntity { UserId = 2, Username = "user1", RoleId = 2 }
        };
}
```

## Scenario E: Blazor Server with SignalR

```csharp
// Shared hub for real-time progress
public class SetupHub : Hub
{
    public async Task RequestSetup(string environment)
    {
        var adapter = new MyBlazorServerSetupAdapter(Clients);
        // ... run wizard via adapter, progress streamed to all clients
    }
}

public class MyBlazorServerSetupAdapter : BlazorServerSetupWizardAdapter
{
    private readonly IHubClients _clients;

    public MyBlazorServerSetupAdapter(IHubClients clients) => _clients = clients;

    protected override void OnProgress(PassedArgs args)
        => _ = _clients.All.SendAsync("ReceiveProgress",
            new { percent = args.ParameterInt1, message = args.Messege });

    protected override void OnComplete(SetupReport report)
        => _ = _clients.All.SendAsync("SetupComplete",
            new { succeeded = report.Succeeded, hash = report.ContentHash });
}
```

## Scenario F: Dry-Run Mode

```csharp
// In desktop app, allow user to preview what setup will do without applying
var options = new SetupOptions
{
    DryRun = true,
    Environment = "Production"
};

var wizard = new SetupWizardBuilder()
    .WithOptions(options)
    .AddStep(new SchemaSetupStep(schemaOpts))
    .Build();

var context = new SetupContext { Options = options };
var report = await adapter.RunAsync(wizard, context);

// report.DryRunReportJson contains DDL preview
var dryRunJson = (SetupContext.Properties["DryRunReportJson"] as string) ?? "";
Console.WriteLine($"Schema DDL preview:\n{dryRunJson}");

// No changes were applied — user can inspect and decide to run without dry-run
```

## Key Patterns

### Idempotency Guard
```csharp
public bool CanSkip(SetupContext context)
{
    // Connection already open and configured
    return context?.DataSource?.ConnectionStatus == ConnectionState.Open
        && context?.Editor?.ConfigEditor?.DataConnections
            ?.Any(c => c.ConnectionName == _opts.ConnectionProperties.ConnectionName) == true;
}
```

### Progress Reporting
```csharp
private static void Report(IProgress<PassedArgs> progress, int percent, string msg) =>
    progress?.Report(new PassedArgs { ParameterInt1 = percent, Messege = msg });

// In Execute
Report(progress, 25, "Building schema…");
// ... work
Report(progress, 50, "Validating constraints…");
```

### Error Handling
```csharp
public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
{
    try
    {
        if (context?.Editor == null)
            return Fail("Editor is required");
        
        // ... work
        
        return Ok("Step completed successfully");
    }
    catch (Exception ex)
    {
        return Fail($"Step failed: {ex.Message}", ex);
    }
}
```

## State Persistence

SetupState is automatically serialized as JSON and written to checkpoint file after each step:
```json
{
  "CompletedStepIds": ["driver-provision", "connection-config"],
  "SkippedStepIds": [],
  "FailedStepId": null,
  "SchemaHash": "abc123def456...",
  "CompletedSeederIds": ["roles-seeder"],
  "StartedAt": "2026-05-14T10:30:00Z",
  "LastUpdatedAt": "2026-05-14T10:35:00Z",
  "Metadata": {
    "MigrationPlanId": "plan-uuid",
    "ExecutionToken": "token-uuid",
    "LastCheckpointId": "checkpoint-uuid"
  }
}
```
