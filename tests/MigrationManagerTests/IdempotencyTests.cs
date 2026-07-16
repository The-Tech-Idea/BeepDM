namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Idempotency gate over per-datasource migration history: <c>RecordMigration</c> writes a named
/// record and <c>IsMigrationApplied</c> queries it so a migration is not re-run. Closes the
/// "history is write-only, never queried" gap.
/// </summary>
public class IdempotencyTests
{
    [Fact]
    public void IsMigrationApplied_False_BeforeRecording()
    {
        var m = new MigrationTestHarness().Build();
        Assert.False(m.IsMigrationApplied("2024-01-add-orders"));
    }

    [Fact]
    public void RecordThenQuery_ReportsApplied()
    {
        var m = new MigrationTestHarness().Build();

        var rec = m.RecordMigration("2024-01-add-orders");
        Assert.Equal(Errors.Ok, rec.Flag);

        Assert.True(m.IsMigrationApplied("2024-01-add-orders"));
        Assert.False(m.IsMigrationApplied("2024-02-add-invoices")); // unrelated name stays unapplied
    }

    [Fact]
    public void FailedRecord_DoesNotCountAsApplied()
    {
        var m = new MigrationTestHarness().Build();

        m.RecordMigration("half-done", success: false);

        // Only successful records gate — a failed attempt must be re-runnable.
        Assert.False(m.IsMigrationApplied("half-done"));
    }
}
