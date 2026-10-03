using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SyncRejectRecovery_ReevaluatesDirectionPolicyWithoutAdvancingFailedRun(bool reverse, bool missingEngine)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        destination.SetupGet(x => x.DatasourceName).Returns("target-ds");
        destination.SetupGet(x => x.GuidID).Returns("target-id");
        source.SetupGet(x => x.DatasourceName).Returns("source-ds");
        source.SetupGet(x => x.GuidID).Returns("source-id");
        bool allow = false;
        var seen = new List<string>();
        var engine = QualityEngine(args =>
        {
            var entity = (string)args["entityName"]; seen.Add(entity);
            return reverse && entity == "target" || allow;
        });
        EnableRecordQuality(manager, schema, engine);
        schema.DqPolicy.OnRecordFailure = DataQualityAction.Quarantine;
        if (reverse)
        {
            schema.SyncDirection = "Bidirectional";
            destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
                .Returns(new object[] { new { TargetId = 7, TargetName = "reverse" } });
            source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        }
        var store = new JsonFileImportErrorStore(_folder);
        var before = schema.WatermarkPolicy.LastWatermarkValue;
        var failed = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema, errorStore: store));
        Assert.Equal(Errors.Failed, failed.Flag);
        var config = reverse ? SyncSchemaTranslator.ToReverseImportConfiguration(schema) : SyncSchemaTranslator.ToImportConfiguration(schema);
        var context = DataImportManager.GetRejectContextKey(config);
        var rejected = Assert.Single(await store.LoadAsync(context));
        Assert.Equal(failed.RunId, rejected.Recovery!.RunId);
        await store.PrepareReplayAsync(context, rejected.Recovery.RejectId, 0, "operator");
        allow = true;
        if (missingEngine) manager.IntegrationContext = null;
        var result = await manager.ReplayRejectedRecordAsync(schema, store, context, rejected.Recovery.RejectId, 1, "worker");
        Assert.Equal(missingEngine ? Errors.Failed : Errors.Ok, result.Flag);
        Assert.Equal(missingEngine ? 0 : 1, result.RecordsAcknowledged);
        Assert.Equal(before, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal("Failed", schema.SyncStatus);
        Assert.Equal(missingEngine ? ImportRejectState.Prepared : ImportRejectState.Acknowledged,
            (await store.LoadRejectAsync(context, rejected.Recovery.RejectId)).Recovery!.State);
        source.Verify(x => x.InsertEntity("source", It.IsAny<object>()), reverse && !missingEngine ? Times.Once : Times.Never);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), reverse || !missingEngine ? Times.Once : Times.Never);
        Assert.Equal(reverse ? "source" : "target", seen.Last());
    }
}
