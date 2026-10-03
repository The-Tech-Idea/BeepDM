using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests
{
    [Theory]
    [InlineData("replace")]
    [InlineData("remove")]
    [InlineData("priority")]
    public async Task BothDirectionsKeepAdmittedResolverRosterDespiteForwardProviderMutation(string mutation)
    {
        var (manager, schema, destination, source) = Harness();
        schema.SyncDirection = "Bidirectional";
        DefaultsManager.RegisterCustomResolver(manager.Editor, new SyncOwnershipResolver(manager.Editor, "owned", 7));
        var registry = ResolverRegistryAccess.Get(manager.Editor);
        var catalog = new Mock<IConfigEditor>();
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        {
            new() { ConnectionName = "source-ds", DatasourceDefaults = new() { new() { PropertyName = "Id", Rule = ":OWNED()" } } },
            new() { ConnectionName = "target-ds", DatasourceDefaults = new() { new() { PropertyName = "Id", Rule = ":OWNED()" } } }
        });
        Mock.Get(manager.Editor).SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(new object[] { new Dictionary<string, object>() });
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>())).Returns(new object[] { new Dictionary<string, object>() });
        var values = new List<int>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            values.Add((int)((Dictionary<string, object>)row)["Id"]);
            if (mutation == "remove") registry.UnregisterResolver("owned");
            else DefaultsManager.RegisterCustomResolver(manager.Editor, new SyncOwnershipResolver(manager.Editor,
                mutation == "replace" ? "owned" : "higher", 9, mutation == "priority" ? -1 : 100));
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns((string _, object row) =>
        { values.Add((int)((Dictionary<string, object>)row)["Id"]); return new ErrorsInfo { Flag = Errors.Ok }; });
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(ImportOutcome.Completed, result.Outcome); Assert.Equal(2, result.RecordsSucceeded);
        Assert.Equal(new[] { 7, 7 }, values);
    }

    private sealed class SyncOwnershipResolver(IDMEEditor editor, string name, int value, int priority = 100)
        : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => name;
        public override int Priority => priority;
        public override IEnumerable<string> SupportedRuleTypes => new[] { "OWNED" };
        public override bool CanHandle(string rule) => rule.StartsWith("OWNED(", StringComparison.Ordinal);
        public override object ResolveValue(string rule, IPassedArgs parameters) => value;
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
    }
}
