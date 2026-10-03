using System.Diagnostics;
using System.Text.Json.Nodes;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Services.Persistence;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class MigrationOwnershipTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-MigrationOwnership", Guid.NewGuid().ToString("N"));
    private const string Target = "sqlserver/primary.example:1433/customer/dbo";
    private FileMigrationExecutionOwnership Store => new(_folder);

    [Theory]
    [InlineData("first")]
    [InlineData("different")]
    public void LiveOwner_BlocksSameAndDifferentTokensAcrossInstances(string secondToken)
    {
        var first = Store.TryAcquireMigrationExecution(Target, "first", "hash");
        using var lease = first.Lease;
        Assert.Equal(MigrationAdmissionStatus.Acquired, first.Status);
        var before = File.ReadAllBytes(ClaimPath());
        Assert.Equal(MigrationAdmissionStatus.Busy, Store.TryAcquireMigrationExecution(Target, secondToken, "other-hash").Status);
        Assert.False(Store.ReconcileMigrationExecution(Target, lease.Claim.ClaimId, "operator", "incident/123").IsSaved);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
        Assert.True(lease.Finish(MigrationClaimDisposition.Completed).IsSaved);
        Assert.Equal(MigrationAdmissionStatus.Busy, Store.TryAcquireMigrationExecution(Target, secondToken, "hash").Status);
    }

    [Fact]
    public void DifferentTargets_CanHaveIndependentOwners()
    {
        using var first = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease;
        var second = Store.TryAcquireMigrationExecution(Target + "/other", "first", "hash");
        using var secondLease = second.Lease;
        Assert.Equal(MigrationAdmissionStatus.Acquired, second.Status);
        Assert.NotEqual(first.Claim.TargetKey, secondLease.Claim.TargetKey);
    }

    [Fact]
    public void DisposeWithoutFinish_PreservesAbandonedClaimAndRequiresExplicitReconciliation()
    {
        var first = Store.TryAcquireMigrationExecution(Target, "first", "hash");
        var claim = first.Lease.Claim;
        first.Lease.Dispose();
        var denied = Store.TryAcquireMigrationExecution(Target, "different", "different-hash");
        Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, denied.Status);
        Assert.Null(denied.Lease);
        Assert.Equal(claim.ClaimId, denied.ExistingClaim.ClaimId);
        Assert.Equal(MigrationClaimState.Owned, denied.ExistingClaim.State);
        Assert.False(Store.ReconcileMigrationExecution(Target, Guid.NewGuid().ToString("N"), "operator", "incident/123").IsSaved);
        Assert.True(Store.ReconcileMigrationExecution(Target, claim.ClaimId, "operator", "incident/123").IsSaved);
        var released = Store.ReadMigrationExecutionClaim(Target);
        Assert.Equal(MigrationClaimState.Released, released.State);
        Assert.Equal(MigrationClaimDisposition.OperatorReconciled, released.Disposition);
        Assert.Equal(claim.Revision + 1, released.Revision);
        Assert.False(Store.ReconcileMigrationExecution(Target, claim.ClaimId, "operator", "incident/123").IsSaved);
        var next = Store.TryAcquireMigrationExecution(Target, "different", "different-hash");
        using var nextLease = next.Lease;
        Assert.Equal(MigrationAdmissionStatus.Acquired, next.Status);
        Assert.NotEqual(claim.ClaimId, nextLease.Claim.ClaimId);
        Assert.Equal(claim.Revision + 2, nextLease.Claim.Revision);
        var archived = JsonNode.Parse(File.ReadAllText(Assert.Single(Directory.GetFiles(Path.Combine(_folder, "Completed"), "*.json"))))!;
        Assert.Equal(claim.ClaimId, (string?)archived["ClaimId"]);
        Assert.Equal("operator", (string?)archived["Actor"]);
        Assert.Equal("incident/123", (string?)archived["EvidenceReference"]);
    }

    [Theory]
    [InlineData(MigrationClaimDisposition.Completed)]
    [InlineData(MigrationClaimDisposition.SafeToRetry)]
    [InlineData(MigrationClaimDisposition.RequiresReconciliation)]
    public void Finish_IsAcknowledgedAndDispositionControlsReadmission(MigrationClaimDisposition disposition)
    {
        var acquired = Store.TryAcquireMigrationExecution(Target, "first", "hash");
        var original = acquired.Lease.Claim;
        Assert.True(acquired.Lease.Finish(disposition).IsSaved);
        Assert.False(acquired.Lease.Finish(disposition).IsSaved);
        var updated = acquired.Lease.Claim;
        Assert.NotSame(original, updated);
        Assert.Equal(MigrationClaimState.Owned, original.State);
        Assert.Null(original.Disposition);
        Assert.Equal(original.Revision + 1, updated.Revision);
        acquired.Lease.Dispose();
        Assert.False(acquired.Lease.Finish(disposition).IsSaved);
        var next = Store.TryAcquireMigrationExecution(Target, "next", "hash");
        using var lease = next.Lease;
        Assert.Equal(disposition == MigrationClaimDisposition.RequiresReconciliation
            ? MigrationAdmissionStatus.RequiresReconciliation : MigrationAdmissionStatus.Acquired, next.Status);
    }

    [Theory]
    [InlineData(MigrationClaimDisposition.OperatorReconciled)]
    [InlineData((MigrationClaimDisposition)99)]
    public void LeaseCannotFabricateOperatorReconciliationOrUnknownDisposition(MigrationClaimDisposition disposition)
    {
        using var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease;
        var before = File.ReadAllBytes(ClaimPath());
        Assert.False(lease.Finish(disposition).IsSaved);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
        Assert.Equal(MigrationAdmissionStatus.Busy, Store.TryAcquireMigrationExecution(Target, "next", "hash").Status);
    }

    [Fact]
    public void CancellationBeforeAcquisitionOrFinish_DoesNotPublishOrRelease()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Equal(MigrationAdmissionStatus.Cancelled, Store.TryAcquireMigrationExecution(Target, "first", "hash", cancelled.Token).Status);
        Assert.False(Directory.Exists(_folder));
        using var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease;
        var before = File.ReadAllBytes(ClaimPath());
        Assert.Equal(PersistenceWriteStatus.Cancelled, lease.Finish(MigrationClaimDisposition.Completed, cancelled.Token).Status);
        Assert.Equal(PersistenceWriteStatus.Cancelled,
            Store.ReconcileMigrationExecution(Target, lease.Claim.ClaimId, "operator", "incident", cancelled.Token).Status);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
        Assert.True(lease.Finish(MigrationClaimDisposition.SafeToRetry).IsSaved);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData("line\nbreak")]
    public void InvalidIdentities_DoNotCreateOrClearClaims(string invalid)
    {
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(invalid, "token", "hash").Status);
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(Target, invalid, "hash").Status);
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(Target, "token", invalid).Status);
        Assert.False(Directory.Exists(_folder));
    }

    [Fact]
    public void SurrogateIdentity_IsRejectedBeforeStorage()
    {
        // xUnit's discovery transport replaces unpaired surrogate literals in InlineData.
        InvalidIdentities_DoNotCreateOrClearClaims(new string((char)0xD800, 1));
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("evidence")]
    [InlineData("claim")]
    public void ReconciliationRequiresExplicitActorEvidenceAndExpectedIdentity(string missing)
    {
        var acquired = Store.TryAcquireMigrationExecution(Target, "first", "hash");
        var id = acquired.Lease.Claim.ClaimId;
        acquired.Lease.Dispose();
        var before = File.ReadAllBytes(ClaimPath());
        var result = Store.ReconcileMigrationExecution(Target, missing == "claim" ? "" : id,
            missing == "actor" ? "" : "operator", missing == "evidence" ? "" : "incident");
        Assert.False(result.IsSaved);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("broken")]
    [InlineData("trailing")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("unknown-property")]
    [InlineData("version")]
    [InlineData("target")]
    [InlineData("claim")]
    [InlineData("revision")]
    [InlineData("revision-type")]
    [InlineData("state")]
    [InlineData("numeric-state")]
    [InlineData("disposition")]
    [InlineData("disposition-state")]
    [InlineData("time")]
    [InlineData("actor")]
    [InlineData("evidence")]
    [InlineData("size")]
    public void InvalidPersistedEvidence_IsNeverReplacedOrReconciled(string fault)
    {
        var acquired = Store.TryAcquireMigrationExecution(Target, "first", "hash");
        var id = acquired.Lease.Claim.ClaimId;
        acquired.Lease.Dispose();
        var text = File.ReadAllText(ClaimPath());
        var json = JsonNode.Parse(text)!.AsObject();
        switch (fault)
        {
            case "empty": text = ""; break;
            case "broken": text = "{broken"; break;
            case "trailing": text += "{}"; break;
            case "duplicate": text = text.Insert(1, "\"FormatVersion\":1,"); break;
            case "missing": json.Remove("PlanHash"); break;
            case "unknown-property": json["Extra"] = 1; break;
            case "version": json["FormatVersion"] = 2; break;
            case "target": json["TargetKey"] = "other"; break;
            case "claim": json["ClaimId"] = "not-guid"; break;
            case "revision": json["Revision"] = -1; break;
            case "revision-type": json["Revision"] = "1"; break;
            case "state": json["State"] = "Released"; break;
            case "numeric-state": json["State"] = "0"; break;
            case "disposition": json["Disposition"] = "99"; break;
            case "disposition-state": json["Disposition"] = "Completed"; break;
            case "time": json["UpdatedOnUtc"] = "today"; break;
            case "actor": json["Actor"] = 42; break;
            case "evidence": json["EvidenceReference"] = "unexpected"; break;
            case "size": text = new string(' ', 16385); break;
        }
        if (fault is not ("empty" or "broken" or "trailing" or "duplicate" or "size")) text = json.ToJsonString();
        File.WriteAllText(ClaimPath(), text);
        var before = File.ReadAllBytes(ClaimPath());
        var attempt = Store.TryAcquireMigrationExecution(Target, "next", "hash");
        Assert.Equal(fault == "version" ? MigrationAdmissionStatus.Unsupported : MigrationAdmissionStatus.Failed, attempt.Status);
        Assert.Null(attempt.Lease);
        Assert.NotNull(Record.Exception(() => Store.ReadMigrationExecutionClaim(Target)));
        Assert.False(Store.ReconcileMigrationExecution(Target, id, "operator", "incident").IsSaved);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
    }

    [Fact]
    public void NoncooperatingRevisionChange_BlocksOwnerCompletionAndPreservesEvidence()
    {
        using var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease;
        var json = JsonNode.Parse(File.ReadAllText(ClaimPath()))!.AsObject();
        json["Revision"] = 99;
        File.WriteAllText(ClaimPath(), json.ToJsonString());
        var before = File.ReadAllBytes(ClaimPath());
        Assert.False(lease.Finish(MigrationClaimDisposition.Completed).IsSaved);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
        Assert.Equal(MigrationClaimState.Owned, lease.Claim.State);
    }

    [Fact]
    public async Task ConcurrentFinishCalls_ProduceExactlyOneAcknowledgedTransition()
    {
        using var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease;
        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => Task.Run(() => lease.Finish(MigrationClaimDisposition.Completed))));
        Assert.Single(results, result => result.IsSaved);
        Assert.Equal(2, Store.ReadMigrationExecutionClaim(Target).Revision);
    }

    [Fact]
    public void MissingObservation_DoesNotInventClaimOrCreateStorage()
    {
        Assert.Null(Store.ReadMigrationExecutionClaim(Target));
        Assert.False(Directory.Exists(_folder));
    }

    [Fact]
    public void WindowsReplacementDenial_KeepsOwnerAndDurableUnfinishedEvidence()
    {
        if (!OperatingSystem.IsWindows()) return; // Replacement-denial semantics are Windows-specific.
        var admission = Store.TryAcquireMigrationExecution(Target, "first", "hash");
        var before = File.ReadAllBytes(ClaimPath());
        using (var reader = new FileStream(ClaimPath(), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var outcome = admission.Lease.Finish(MigrationClaimDisposition.Completed);
            Assert.Equal(PersistenceWriteStatus.Failed, outcome.Status);
            Assert.Null(outcome.Error.InnerException);
            Assert.DoesNotContain(_folder, outcome.Error.Message);
            Assert.Equal(MigrationClaimState.Owned, admission.Lease.Claim.State);
            Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
            Assert.Equal(MigrationAdmissionStatus.Busy, Store.TryAcquireMigrationExecution(Target, "next", "hash").Status);
        }
        admission.Lease.Dispose();
        Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, Store.TryAcquireMigrationExecution(Target, "next", "hash").Status);
    }

    [Fact]
    public void ReadFailure_DoesNotEchoPersistedPayload()
    {
        using (var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease) { }
        File.WriteAllText(ClaimPath(), "{\"secret-sentinel-payload\":\"broken");
        var error = Assert.Throws<InvalidDataException>(() => Store.ReadMigrationExecutionClaim(Target));
        Assert.DoesNotContain("secret-sentinel", error.ToString());
        Assert.Null(error.InnerException);
    }

    [Fact]
    public void RevisionOverflow_DoesNotReplaceReleasedEvidence()
    {
        using (var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease)
            Assert.True(lease.Finish(MigrationClaimDisposition.Completed).IsSaved);
        var json = JsonNode.Parse(File.ReadAllText(ClaimPath()))!.AsObject();
        json["Revision"] = long.MaxValue;
        File.WriteAllText(ClaimPath(), json.ToJsonString());
        var before = File.ReadAllBytes(ClaimPath());
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(Target, "next", "hash").Status);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArchiveFailure_CannotEraseReleasedEvidenceToAdmitNewWork(bool foreignArchive)
    {
        using var lease = Store.TryAcquireMigrationExecution(Target, "first", "hash").Lease;
        Assert.True(lease.Finish(MigrationClaimDisposition.Completed).IsSaved);
        var claim = lease.Claim;
        lease.Dispose();
        var before = File.ReadAllBytes(ClaimPath());
        var archive = Path.Combine(_folder, "Completed");
        if (foreignArchive)
        {
            Directory.CreateDirectory(archive);
            File.WriteAllText(Path.Combine(archive, claim.TargetKey + "-" + claim.ClaimId + ".json"), "corrupt-evidence");
        }
        else File.WriteAllText(archive, "archive-blocker");
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(Target, "next", "hash").Status);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
        if (foreignArchive) Assert.Equal("corrupt-evidence", File.ReadAllText(Assert.Single(Directory.GetFiles(archive, "*.json"))));
        else Assert.Equal("archive-blocker", File.ReadAllText(archive));
    }

    [Fact]
    public void OversizeIdentityAndEvidence_AreRejectedWithoutPublication()
    {
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(new string('t', 1025), "token", "hash").Status);
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(Target, new string('t', 257), "hash").Status);
        Assert.Equal(MigrationAdmissionStatus.Failed, Store.TryAcquireMigrationExecution(Target, "token", new string('h', 257)).Status);
        Assert.False(Directory.Exists(_folder));
        var admission = Store.TryAcquireMigrationExecution(Target, "token", "hash");
        var claimId = admission.Lease.Claim.ClaimId;
        admission.Lease.Dispose();
        var before = File.ReadAllBytes(ClaimPath());
        Assert.False(Store.ReconcileMigrationExecution(Target, claimId, new string('a', 257), "evidence").IsSaved);
        Assert.False(Store.ReconcileMigrationExecution(Target, claimId, "actor", new string('e', 1025)).IsSaved);
        Assert.Equal(before, File.ReadAllBytes(ClaimPath()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ConfigEditorMissingRoot_DoesNotFallBackToWorkingDirectory(string? root)
    {
        using var editor = new ConfigEditor(Moq.Mock.Of<TheTechIdea.Beep.Logger.IDMLogger>(),
            new ErrorsInfo(), new JsonLoader(), _folder, "missing-root");
        editor.Config.ConfigPath = root;
        var ownership = (IMigrationExecutionOwnership)editor;
        Assert.Equal(MigrationAdmissionStatus.Failed, ownership.TryAcquireMigrationExecution(Target, "token", "hash").Status);
        Assert.False(ownership.ReconcileMigrationExecution(Target, Guid.NewGuid().ToString("N"), "operator", "incident").IsSaved);
        Assert.Throws<InvalidOperationException>(() => ownership.ReadMigrationExecutionClaim(Target));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Equal(MigrationAdmissionStatus.Cancelled, ownership.TryAcquireMigrationExecution(Target, "token", "hash", cancelled.Token).Status);
        Assert.Equal(PersistenceWriteStatus.Cancelled,
            ownership.ReconcileMigrationExecution(Target, "claim", "operator", "incident", cancelled.Token).Status);
    }

    [Fact]
    public void StorageFailure_IsObservableAndDoesNotLeakRawPathOrTarget()
    {
        Directory.CreateDirectory(_folder);
        var blocker = Path.Combine(_folder, "secret-sentinel-file");
        File.WriteAllText(blocker, "previous");
        var admission = new FileMigrationExecutionOwnership(blocker).TryAcquireMigrationExecution("secret-sentinel-target", "token", "hash");
        Assert.Equal(MigrationAdmissionStatus.Failed, admission.Status);
        Assert.DoesNotContain("secret-sentinel", admission.ErrorCode);
        Assert.Equal("previous", File.ReadAllText(blocker));
    }

    [Fact]
    public void ConfigEditor_ExposesTheRealStoreCapabilityAtItsExplicitRoot()
    {
        using var editor = new ConfigEditor(Moq.Mock.Of<TheTechIdea.Beep.Logger.IDMLogger>(),
            new ErrorsInfo(), new JsonLoader(), _folder, "config-owner");
        var ownership = Assert.IsAssignableFrom<IMigrationExecutionOwnership>(editor);
        var admitted = ownership.TryAcquireMigrationExecution(Target, "token", "hash");
        using var lease = admitted.Lease;
        Assert.Equal(MigrationAdmissionStatus.Acquired, admitted.Status);
        Assert.Equal(lease.Claim.ClaimId, ownership.ReadMigrationExecutionClaim(Target).ClaimId);
        var directory = Path.Combine(editor.Config.ConfigPath, "Migrations", "ExecutionOwnership");
        Assert.Single(Directory.GetFiles(directory, "claim-v1-*.json"));
        Assert.True(lease.Finish(MigrationClaimDisposition.Completed).IsSaved);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("different")]
    public async Task ChildOwnerBlocksOtherProcesses_AndCrashNeverClearsDurableWork(string secondToken)
    {
        using var child = StartWorker("migration-claim-hold", _folder, Target, "same", "hash");
        try
        {
            var claimId = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.True(Guid.TryParseExact(claimId, "N", out _), claimId);
            Assert.False(child.HasExited);
            using var competitor = StartWorker("migration-claim-try", _folder, Target, secondToken, "other-hash");
            var status = await competitor.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20));
            await competitor.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(0, competitor.ExitCode);
            Assert.Equal("Busy", status);
            Assert.False(Store.ReconcileMigrationExecution(Target, claimId!, "operator", "incident").IsSaved);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.True(child.HasExited);
            var settle = Stopwatch.StartNew();
            MigrationExecutionAdmission afterCrash;
            do
            {
                afterCrash = Store.TryAcquireMigrationExecution(Target, "fresh-token", "hash");
                Assert.Null(afterCrash.Lease);
                Assert.Equal(claimId, Store.ReadMigrationExecutionClaim(Target).ClaimId);
                if (afterCrash.Status != MigrationAdmissionStatus.Busy) break;
                // A terminal child does not prove the exclusive sidecar is already observable as free.
                Assert.True(settle.Elapsed < TimeSpan.FromSeconds(20), "Owner sidecar did not settle after confirmed process exit.");
                await Task.Delay(20);
            } while (true);
            Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, afterCrash.Status);
            Assert.Equal(claimId, afterCrash.ExistingClaim.ClaimId);
            Assert.True(Store.ReconcileMigrationExecution(Target, claimId!, "operator", "verified-provider-state/123").IsSaved);
            var next = Store.TryAcquireMigrationExecution(Target, "fresh-token", "hash");
            using var lease = next.Lease;
            Assert.Equal(MigrationAdmissionStatus.Acquired, next.Status);
        }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
        }
    }

    private static Process StartWorker(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(worker), worker);
        start.ArgumentList.Add(worker);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private string ClaimPath() => Assert.Single(Directory.GetFiles(_folder, "claim-v1-*.json"));
    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true); }
}
