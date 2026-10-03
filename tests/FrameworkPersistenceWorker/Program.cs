using System.Globalization;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Winform.Controls;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Services.Persistence;

if (args.Length < 2) return 2;
switch (args[0])
{
    case "migration-claim-hold":
        var heldAdmission = new FileMigrationExecutionOwnership(args[1]).TryAcquireMigrationExecution(args[2], args[3], args[4]);
        if (heldAdmission.Status != MigrationAdmissionStatus.Acquired) return 20;
        using (heldAdmission.Lease)
        {
            Console.WriteLine(heldAdmission.Lease.Claim.ClaimId);
            Thread.Sleep(Timeout.Infinite);
        }
        break;
    case "migration-claim-try":
        var attemptedAdmission = new FileMigrationExecutionOwnership(args[1]).TryAcquireMigrationExecution(args[2], args[3], args[4]);
        using (attemptedAdmission.Lease) Console.WriteLine(attemptedAdmission.Status);
        break;
    case "migration-execution-hold":
    {
        var executionStorage = new FileMigrationExecutionStorage(args[1], new TheTechIdea.Beep.JsonLoaderService.JsonLoader());
        var executionConfig = new Mock<IConfigEditor>();
        executionConfig.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns<string>(executionStorage.LoadMigrationHistory);
        executionConfig.As<IMigrationExecutionStorageProvider>().Setup(c => c.CaptureMigrationExecutionStorage()).Returns(executionStorage);
        var executionEditor = new Mock<TheTechIdea.Beep.Editor.IDMEEditor>();
        executionEditor.SetupGet(e => e.ConfigEditor).Returns(executionConfig.Object);
        var executionSource = new Mock<TheTechIdea.Beep.IDataSource>();
        executionSource.SetupGet(s => s.DatasourceName).Returns(args[2]);
        executionSource.SetupGet(s => s.GuidID).Returns("test-target");
        executionSource.SetupGet(s => s.DatasourceType).Returns(TheTechIdea.Beep.Utilities.DataSourceType.SqlServer);
        executionSource.SetupGet(s => s.Category).Returns(TheTechIdea.Beep.Utilities.DatasourceCategory.RDBMS);
        executionSource.SetupGet(s => s.ConnectionStatus).Returns(System.Data.ConnectionState.Open);
        executionSource.Setup(s => s.CheckEntityExist(It.IsAny<string>())).Returns(false);
        executionSource.Setup(s => s.CreateEntityAs(It.IsAny<TheTechIdea.Beep.DataBase.EntityStructure>())).Returns(() =>
        {
            Console.WriteLine("provider-entered");
            Console.Out.Flush();
            Thread.Sleep(Timeout.Infinite);
            return true;
        });
        var executionManager = new TheTechIdea.Beep.Editor.Migration.MigrationManager(executionEditor.Object, executionSource.Object)
            { ExecutionTargetIdentity = "test-target/sqlserver" };
        var executionPlan = executionManager.LoadMigrationPlan(args[3]);
        var executionResult = executionManager.ExecuteMigrationPlan(executionPlan, executionToken: args[4]);
        Console.Error.WriteLine(executionResult.Message);
        return 21;
    }
    case "reject-claim":
        var claimStore = new JsonFileImportErrorStore(args[1]);
        try
        {
            await claimStore.ClaimReplayAsync(args[2], args[3], 1, args[4]);
            Console.WriteLine("claimed");
        }
        catch (InvalidDataException) { Console.WriteLine("denied"); }
        break;
    case "reject-reload":
        var rejectReload = await new JsonFileImportErrorStore(args[1]).LoadRejectAsync(args[2], args[3]);
        if (rejectReload.Recovery?.State != ImportRejectState.Claimed || rejectReload.Replayed || rejectReload.Recovery.Revision != 2 ||
            rejectReload.Recovery.RejectId != args[3] || rejectReload.Recovery.OriginalDestinationPayload != rejectReload.Recovery.PreparedDestinationPayload)
            return 12;
        using (var payload = System.Text.Json.JsonDocument.Parse(rejectReload.Recovery.PreparedDestinationPayload))
            if (payload.RootElement.GetProperty("Fields").GetProperty("Id").GetProperty("Kind").GetString() != "i32") return 13;
        Console.WriteLine("reloaded");
        break;
    case "increment":
        for (int i = 0; i < int.Parse(args[2], CultureInfo.InvariantCulture); i++)
            await AtomicFileStore.UpdateTextAsync(args[1], current =>
                (int.Parse(current ?? "0", CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture));
        break;
    case "sync-save":
        var syncStore = new TheTechIdea.Beep.Editor.BeepSync.Helpers.SchemaPersistenceHelper(Mock.Of<TheTechIdea.Beep.Editor.IDMEEditor>(), args[1]);
        var syncOffset = int.Parse(args[2], CultureInfo.InvariantCulture);
        for (int i = 0; i < 10; i++)
            await syncStore.SaveSchemaAsync(new TheTechIdea.Beep.Editor.DataSyncSchema { Id = "worker-" + (syncOffset + i) });
        break;
    case "sync-reload":
        var syncReload = new TheTechIdea.Beep.Editor.BeepSync.Helpers.SchemaPersistenceHelper(Mock.Of<TheTechIdea.Beep.Editor.IDMEEditor>(), args[1]);
        var syncRows = await syncReload.LoadSchemasAsync();
        var syncCheckpoint = await syncReload.LoadCheckpointAsync("schema");
        if (syncRows.Count != 1 || syncRows[0].WatermarkPolicy.LastWatermarkValue is not DateTimeOffset syncTime ||
            syncTime.Offset != TimeSpan.FromHours(3) || syncCheckpoint?.LastProcessedKeyValue is not long syncKey || syncKey != 42L) return 10;
        break;
    case "sync-failure-reload":
        var failureStore = new TheTechIdea.Beep.Editor.BeepSync.Helpers.SchemaPersistenceHelper(Mock.Of<TheTechIdea.Beep.Editor.IDMEEditor>(), args[1]);
        var failureCheckpoint = await failureStore.LoadCheckpointAsync("schema");
        var failureEvidence = failureCheckpoint?.FailureEvidence;
        if (failureCheckpoint?.Status != "Failed" || !failureCheckpoint.RequiresReconciliation || failureCheckpoint.ProcessedOffset != 1 ||
            failureEvidence?.FormatVersion != 1 || failureEvidence.RecordsAttempted != 2 || failureEvidence.RecordsAcknowledged != 1 ||
            failureEvidence.RecordsQualityRejected != 1 || failureEvidence.RecordsBlocked != 1 || failureEvidence.WriteAttempts != 1 ||
            failureEvidence.Threshold?.Outcome != TheTechIdea.Beep.Editor.BeepSync.SyncBatchThresholdOutcome.Rejected ||
            failureEvidence.Threshold.FailureMode != QualityFailureMode.Required || failureEvidence.Threshold.RejectRate != 0.5) return 11;
        break;
    case "hold":
        await AtomicFileStore.UpdateTextAsync(args[1], current =>
        {
            Console.WriteLine("lease-acquired");
            Thread.Sleep(Timeout.Infinite);
            return current!;
        });
        break;
    case "errors":
        var store = new JsonFileImportErrorStore(args[1]);
        int count = int.Parse(args[2], CultureInfo.InvariantCulture);
        int offset = int.Parse(args[3], CultureInfo.InvariantCulture);
        for (int i = 0; i < count; i++)
            await store.SaveAsync(new ImportErrorRecord
            {
                ContextKey = "process-context", BatchNumber = 2, RecordIndex = offset + i, Reason = "worker"
            });
        break;
    case "catalog":
        var service = new Mock<IBeepService>();
        service.SetupGet(s => s.BeepDirectory).Returns(args[1]);
        service.SetupGet(s => s.AppRepoName).Returns("catalog-test");
        using (var catalog = new JsonConnectionStorageProvider(service.Object))
        {
            int catalogCount = int.Parse(args[2], CultureInfo.InvariantCulture);
            int catalogOffset = int.Parse(args[3], CultureInfo.InvariantCulture);
            for (int i = 0; i < catalogCount; i++)
                await catalog.AddOrUpdateAsync(ConnectionStorageScope.Project, "Default", new ConnectionProperties
                {
                    ConnectionName = $"worker-{catalogOffset + i}", GuidID = Guid.NewGuid().ToString("D")
                }, persist: true);
        }
        break;
    case "protected-catalog":
        var protectedService = new Mock<IBeepService>();
        protectedService.SetupGet(s => s.BeepDirectory).Returns(args[1]);
        protectedService.SetupGet(s => s.AppRepoName).Returns("protected-test");
        var suppliedKey = Environment.GetEnvironmentVariable("BEEP_TEST_CREDENTIAL_KEY");
        if (suppliedKey == null) return 8;
        var protectedKeys = new Mock<IConnectionCredentialKeyProvider>();
        protectedKeys.SetupGet(k => k.CurrentKeyId).Returns("first");
        protectedKeys.Setup(k => k.GetKey("first")).Returns(new ReadOnlyMemory<byte>(Convert.FromBase64String(suppliedKey)));
        var protectedPolicy = new TheTechIdea.Beep.Security.ConnectionCredentialProtection(
            new TheTechIdea.Beep.Security.AesGcmConnectionCredentialCipher(protectedKeys.Object));
        using (var protectedStore = new JsonConnectionStorageProvider(protectedService.Object, protectedPolicy))
        {
            var protectedRows = protectedStore.LoadConnections(ConnectionStorageScope.Project, "Default", false);
            if (protectedRows.Count != 1 || protectedRows[0].Password != "sentinel-credential-not-for-storage-3974" ||
                protectedRows[0].Headers[0].Headervalue != protectedRows[0].Password ||
                protectedRows[0].ProtectedCredentialPayload != null) return 9;
        }
        break;
    case "migration-history":
        var migrationStore = new TheTechIdea.Beep.ConfigUtil.Managers.MigrationHistoryManager(null,
            new TheTechIdea.Beep.JsonLoaderService.JsonLoader(), new ConfigandSettings { ConfigPath = args[1] }, null);
        int migrationCount = int.Parse(args[2], CultureInfo.InvariantCulture);
        int migrationOffset = int.Parse(args[3], CultureInfo.InvariantCulture);
        for (int i = 0; i < migrationCount; i++)
            migrationStore.AppendRecord("process-db", TheTechIdea.Beep.Utilities.DataSourceType.SqlServer,
                new MigrationRecord { MigrationId = $"record-{migrationOffset + i}" });
        break;
    case "migration-resume":
        var restartStore = new TheTechIdea.Beep.ConfigUtil.Managers.MigrationHistoryManager(null,
            new TheTechIdea.Beep.JsonLoaderService.JsonLoader(), new ConfigandSettings { ConfigPath = args[1] }, null);
        var restartConfig = new Mock<IConfigEditor>();
        restartConfig.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns<string>(restartStore.Load);
        restartConfig.As<IMigrationExecutionStorageProvider>().Setup(c => c.CaptureMigrationExecutionStorage())
            .Returns(new TheTechIdea.Beep.Services.Persistence.FileMigrationExecutionStorage(args[1],
                new TheTechIdea.Beep.JsonLoaderService.JsonLoader()));
        var restartEditor = new Mock<TheTechIdea.Beep.Editor.IDMEEditor>();
        restartEditor.SetupGet(e => e.ConfigEditor).Returns(restartConfig.Object);
        var restartSource = new Mock<TheTechIdea.Beep.IDataSource>();
        restartSource.SetupGet(s => s.DatasourceName).Returns(args[2]);
        restartSource.SetupGet(s => s.GuidID).Returns("test-target");
        restartSource.SetupGet(s => s.DatasourceType).Returns(TheTechIdea.Beep.Utilities.DataSourceType.SqlServer);
        restartSource.SetupGet(s => s.Category).Returns(TheTechIdea.Beep.Utilities.DatasourceCategory.RDBMS);
        var restartManager = new TheTechIdea.Beep.Editor.Migration.MigrationManager(restartEditor.Object, restartSource.Object)
            { ExecutionTargetIdentity = "test-target/sqlserver" };
        var loadedPlan = restartManager.LoadMigrationPlan(args[3]);
        var loadedCheckpoint = restartManager.GetExecutionCheckpoint(args[4]);
        if (loadedPlan.PlanHash != args[5] || loadedCheckpoint?.ApprovedPlan.PlanHash != args[5] ||
            loadedPlan.Operations[0].SchemaSnapshot.DesiredSchema.Fields[0].Size1 != 77 ||
            loadedCheckpoint.ApprovedPlan.Operations[0].SchemaSnapshot.DesiredSchema.Fields[0].Size1 != 77) return 3;
        var resumed = restartManager.ResumeMigrationPlan(args[4]);
        if (!resumed.Success || !resumed.ResumedFromCheckpoint || !resumed.CheckpointPersisted) return 4;
        restartSource.Verify(s => s.CreateEntityAs(It.IsAny<TheTechIdea.Beep.DataBase.EntityStructure>()), Times.Never);
        break;
    case "bounded-pool-catalog":
        ThreadPool.GetMaxThreads(out _, out var ioThreads);
        if (!ThreadPool.SetMinThreads(2, 2) || !ThreadPool.SetMaxThreads(2, ioThreads)) return 5;
        var poolService = new Mock<IBeepService>();
        poolService.SetupGet(s => s.BeepDirectory).Returns(args[1]);
        poolService.SetupGet(s => s.AppRepoName).Returns("catalog-test");
        using (var poolCatalog = new JsonConnectionStorageProvider(poolService.Object))
        {
            poolCatalog.SaveConnections(ConnectionStorageScope.Project, "Default", Array.Empty<ConnectionProperties>());
            var catalogPath = Path.Combine(args[1], "ConnectionCatalogs", "catalog-test", "project.connections.json");
            using var poolLease = new FileStream(catalogPath + ".beep.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using var admitted = new CountdownEvent(2);
            var releaser = new Thread(() => { if (admitted.Wait(TimeSpan.FromSeconds(10))) poolLease.Dispose(); }) { IsBackground = true };
            releaser.Start();
            var writes = Enumerable.Range(0, 4).Select(index => Task.Run(async () =>
            {
                var connection = new ConnectionProperties { ConnectionName = $"bounded-{index}", GuidID = $"bounded-{index}" };
                if (index < 2)
                {
                    admitted.Signal();
                    return poolCatalog.AddOrUpdate(ConnectionStorageScope.Project, "Default", connection, true);
                }
                return await poolCatalog.AddOrUpdateAsync(ConnectionStorageScope.Project, "Default", connection, true);
            })).ToArray();
            if ((await Task.WhenAll(writes)).Any(saved => !saved)) return 6;
            releaser.Join();
            if (poolCatalog.LoadConnections(ConnectionStorageScope.Project, "Default", false).Count != 4) return 7;
        }
        break;
    default: return 2;
}
return 0;
