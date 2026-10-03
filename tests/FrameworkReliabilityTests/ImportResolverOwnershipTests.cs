using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using Xunit;
using DefaultValue = TheTechIdea.Beep.ConfigUtil.DefaultValue;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    [Theory]
    [InlineData("replace")]
    [InlineData("remove")]
    [InlineData("priority")]
    public async Task ManagerPinsResolverRosterBeforeSourceReadAndRefreshesNextRun(string mutation)
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 7));
        var registry = ResolverRegistryAccess.Get(editor);
        var config = Config(destination, ":OWNED()");
        bool change = true;
        Mock.Get(config.SourceData!).Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            if (change)
            {
                change = false;
                if (mutation == "remove") registry.UnregisterResolver("owned");
                else DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor,
                    mutation == "replace" ? "owned" : "higher", _ => 9, priority: mutation == "priority" ? -1 : 100));
            }
            return new object[] { new Dictionary<string, object>() };
        });
        var values = new List<int>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { values.Add((int)((Dictionary<string, object>)row)["Value"]); return new ErrorsInfo { Flag = Errors.Ok }; });
        using var manager = new DataImportManager(editor);
        Assert.Equal(Errors.Ok, (await manager.RunImportAsync(config, null!, default)).Flag);
        Assert.Equal(new[] { 7 }, values);
        var next = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        if (mutation == "remove") { Assert.Equal(1, next.RecordsTransformationFailed); Assert.Equal(new[] { 7 }, values); }
        else { Assert.Equal(Errors.Ok, next.Flag); Assert.Equal(new[] { 7, 9 }, values); }
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("remove")]
    [InlineData("priority")]
    public async Task DirectBatchPinsRosterAcrossProviderCallbacksAndFreshBatchUsesChanges(string mutation)
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 7));
        var registry = ResolverRegistryAccess.Get(editor);
        var config = Config(destination, ":OWNED()"); var values = new List<int>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            values.Add((int)((Dictionary<string, object>)row)["Value"]);
            if (values.Count == 1)
            {
                if (mutation == "remove") registry.UnregisterResolver("owned");
                else DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor,
                    mutation == "replace" ? "owned" : "higher", _ => 9, priority: mutation == "priority" ? -1 : 100));
            }
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        Assert.Equal(Errors.Ok, (await Run(editor, config, new Dictionary<string, object>(), new Dictionary<string, object>())).Flag);
        Assert.Equal(new[] { 7, 7 }, values);
        var next = await Run(editor, config, new Dictionary<string, object>());
        if (mutation == "remove") { Assert.Equal(1, next.RecordsTransformationFailed); Assert.Equal(new[] { 7, 7 }, values); }
        else { Assert.Equal(Errors.Ok, next.Flag); Assert.Equal(new[] { 7, 7, 9 }, values); }
    }

    [Theory]
    [InlineData("facade")]
    [InlineData("manager")]
    [InlineData("dictionary")]
    [InlineData("telemetry")]
    public async Task NestedRequiredCallsKeepPinnedRosterAndBypassLegacyCache(string route)
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        bool seed = true;
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => seed ? 99 : 7));
        var registry = ResolverRegistryAccess.Get(editor);
        Assert.Equal(99, registry.ResolveValue(":OWNED()", new PassedArgs()));
        seed = false;
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "outer", args =>
        {
            DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 9));
            return route switch
            {
                "manager" => registry.ResolveValue(":OWNED()", args),
                "dictionary" => registry.ResolveValue(":OWNED()", new Dictionary<string, object>()),
                "telemetry" => registry.ResolveWithTelemetry(":OWNED()", args).ResolvedValue,
                _ => DefaultsManager.Resolve(editor, ":OWNED()", args)
            };
        }, operation: "OUTER"));
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":OUTER()"), row)).Flag);
        Assert.Equal(7, row["Value"]);
        Assert.Equal(99, DefaultsManager.Resolve(editor, ":OWNED()"));
        registry.InvalidateValueCache();
        Assert.Equal(9, DefaultsManager.Resolve(editor, ":OWNED()"));
    }

    [Fact]
    public async Task NestedEditorRebindCannotBeHiddenByResolverFallback()
    {
        var editor = Mock.Of<IDMEEditor>(); var foreign = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(foreign, new OwnershipResolver(foreign, "owned", _ => 9));
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "outer", args =>
        {
            try { return DefaultsManager.Resolve(foreign, ":OWNED()", args); }
            catch { return 99; }
        }, operation: "OUTER"));
        var destination = new Mock<IDataSource>();
        var result = await Run(editor, Config(destination, ":OUTER()"), new Dictionary<string, object>());
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
    }

    [Fact]
    public async Task ResolverSentDataMutationCannotRetargetAssignmentOrLaterRows()
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var definitions = new List<DefaultValue>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", args =>
        {
            var definition = Assert.IsType<DefaultValue>(args.SentData);
            definitions.Add(definition);
            definition.PropertyName = "Other"; definition.Rule = "changed"; definition.IsEnabled = false;
            return 7;
        }));
        var config = Config(destination, ":OWNED()");
        var first = new Dictionary<string, object>(); var second = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, config, first, second)).Flag);
        Assert.Equal(7, first["Value"]); Assert.Equal(7, second["Value"]);
        Assert.False(first.ContainsKey("Other")); Assert.False(second.ContainsKey("Other"));
        Assert.Equal(2, definitions.Count); Assert.NotSame(definitions[0], definitions[1]);
        Assert.Equal(":OWNED()", config.DefaultValues[0].Rule); Assert.True(config.DefaultValues[0].IsEnabled);
    }

    [Fact]
    public void StandaloneDefaultsOwnRosterAndDefinitionsAcrossFields()
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 7));
        var defaults = new List<DefaultValue>
        { new() { PropertyName = "First", Rule = ":OWNED()" }, new() { PropertyName = "Second", Rule = ":OWNED()" } };
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ =>
        {
            defaults[1].Rule = "changed";
            DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 9));
            return 7;
        }));
        var metadata = new TheTechIdea.Beep.DataBase.EntityStructure { Fields = new()
            { new() { FieldName = "First", Fieldtype = "System.Object" }, new() { FieldName = "Second", Fieldtype = "System.Object" } } };
        var row = new Dictionary<string, object>();
        new DataImportTransformationHelper(editor).ApplyDefaultValues(row, defaults, metadata, "target-connection");
        Assert.Equal(7, row["First"]); Assert.Equal(7, row["Second"]);
        Assert.Equal(9, DefaultsManager.Resolve(editor, ":OWNED()"));
        var next = new Dictionary<string, object>();
        new DataImportTransformationHelper(editor).ApplyDefaultValues(next,
            new() { new() { PropertyName = "First", Rule = ":OWNED()" } }, metadata, "target-connection");
        Assert.Equal(9, next["First"]);
    }

    [Fact]
    public async Task RequiredBuiltInInitializationDoesNotInvokeRegistrationLogObservers()
    {
        var editor = new Mock<IDMEEditor>();
        editor.Setup(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>())).Throws(new IOException(Secret));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor.Object, Config(destination, ":NEWGUID()"), row)).Flag);
        Assert.True(Guid.TryParse(Assert.IsType<string>(row["Value"]), out var guid)); Assert.NotEqual(Guid.Empty, guid);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    public static IEnumerable<object[]> NestedFailureCases() =>
        from route in new[] { "facade", "manager", "dictionary", "telemetry", "default-wrapper" }
        from failure in new[] { "null", "throw", "missing" }
        select new object[] { route, failure };

    [Theory]
    [MemberData(nameof(NestedFailureCases))]
    public async Task SwallowedNestedFailureCannotBecomeSuccessfulOuterDefault(string route, string failure)
    {
        var editor = new Mock<IDMEEditor>();
        var logger = new Mock<TheTechIdea.Beep.Logger.IDMLogger>();
        editor.SetupGet(x => x.Logger).Returns(logger.Object);
        DefaultsManager.RegisterCustomResolver(editor.Object, new OwnershipResolver(editor.Object, "owned", _ =>
            failure == "throw" ? throw new IOException(Secret) : null!));
        var registry = ResolverRegistryAccess.Get(editor.Object);
        string rule = failure == "missing" ? $":MISSING('{Secret}')" : $":OWNED('{Secret}')";
        TheTechIdea.Beep.Editor.Defaults.Interfaces.ResolverExecutionResult? telemetry = null;
        DefaultsManager.RegisterCustomResolver(editor.Object, new OwnershipResolver(editor.Object, "outer", args =>
        {
            try
            {
                _ = route switch
                {
                    "manager" => registry.ResolveValue(rule, args),
                    "dictionary" => registry.ResolveValue(rule, new Dictionary<string, object>()),
                    "telemetry" => telemetry = registry.ResolveWithTelemetry(rule, args),
                    "default-wrapper" => DefaultsManager.ResolveDefaultValue(editor.Object,
                        new DefaultValue { PropertyName = "Value", Rule = rule, PropertyValue = 99 }, args),
                    _ => DefaultsManager.Resolve(editor.Object, rule, args)
                };
            }
            catch { }
            return 99;
        }, operation: "OUTER"));
        var destination = new Mock<IDataSource>();
        var result = await Run(editor.Object, Config(destination, ":OUTER()"), new Dictionary<string, object>());
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites); Assert.Null(result.Ex);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.Is<string>(message => message.Contains(Secret)),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
        logger.Verify(x => x.WriteLog(It.Is<string>(message => message.Contains(Secret))), Times.Never);
        if (route == "telemetry")
        {
            Assert.NotNull(telemetry); Assert.False(telemetry.Succeeded);
            Assert.Equal("Required defaults resolution failed.", telemetry.ErrorMessage);
            Assert.Null(telemetry.OriginalRule); Assert.Null(telemetry.NormalizedRule);
            Assert.Null(telemetry.ResolverName); Assert.Null(telemetry.RuleFingerprint);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOrCancelledStandaloneDefaultRestoresPriorContext(bool cancel)
    {
        var editor = Mock.Of<IDMEEditor>(); using var cancellation = new CancellationTokenSource();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ =>
        {
            DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 9));
            if (!cancel) throw new IOException(Secret);
            cancellation.Cancel(); return 7;
        }));
        var destination = new Mock<IDataSource>(); var config = Config(destination, ":OWNED()");
        var helper = new DataImportTransformationHelper(editor);
        if (cancel) Assert.Throws<OperationCanceledException>(() => helper.TransformRecord(new Dictionary<string, object>(), config, cancellation.Token));
        else Assert.False(helper.TransformRecord(new Dictionary<string, object>(), config, default).Succeeded);
        var row = new Dictionary<string, object>();
        helper.ApplyDefaultValues(row, config.DefaultValues, config.DestEntityStructure!, "target-connection");
        Assert.Equal(9, row["Value"]);
    }

    [Fact]
    public async Task ConcurrentRunsOnSameEditorOwnIndependentRosters()
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 7));
        var config = Config(destination, ":OWNED()");
        using var ready = new ManualResetEventSlim(); using var resume = new ManualResetEventSlim();
        int reads = 0;
        Mock.Get(config.SourceData!).Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            if (Interlocked.Increment(ref reads) == 1)
            { ready.Set(); if (!resume.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); }
            return new object[] { new Dictionary<string, object>() };
        });
        var values = new System.Collections.Concurrent.ConcurrentBag<int>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { values.Add((int)((Dictionary<string, object>)row)["Value"]); return new ErrorsInfo { Flag = Errors.Ok }; });
        using var firstManager = new DataImportManager(editor); using var secondManager = new DataImportManager(editor);
        var first = Task.Run(() => firstManager.RunImportAsync(config, null!, default));
        try
        {
            Assert.True(ready.Wait(TimeSpan.FromSeconds(10)));
            DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ => 9));
            Assert.Equal(Errors.Ok, (await secondManager.RunImportAsync(config, null!, default)).Flag);
        }
        finally { resume.Set(); }
        Assert.Equal(Errors.Ok, (await first).Flag);
        Assert.Equal(new[] { 7, 9 }, values.OrderBy(value => value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NamedNestedDefaultUsesAdmittedDefinitionsWithoutCatalogRelookup(bool columnWrapper)
    {
        var editor = new Mock<IDMEEditor>(); var catalog = new Mock<IConfigEditor>();
        catalog.SetupGet(x => x.DataConnections).Throws(new IOException(Secret));
        editor.SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        var seed = new DefaultValue { PropertyName = columnWrapper ? "target.Seed" : "Seed", PropertyValue = 7 };
        DefaultsManager.RegisterCustomResolver(editor.Object, new OwnershipResolver(editor.Object, "outer", args =>
        {
            seed.PropertyValue = 9;
            return columnWrapper ? DefaultsManager.GetColumnDefault(editor.Object, "TARGET-CONNECTION", "target", "Seed", args)
                : DefaultsManager.ResolveDefaultValue(editor.Object, "TARGET-CONNECTION", "seed", args);
        }, operation: "OUTER"));
        var destination = new Mock<IDataSource>(); var config = Config(destination, ":OUTER()");
        config.DefaultValues.Add(seed);
        config.DestEntityStructure!.Fields.Add(new() { FieldName = seed.PropertyName, Fieldtype = "System.Int32" });
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor.Object, config, row)).Flag);
        Assert.Equal(7, row["Value"]); Assert.Equal(7, row[seed.PropertyName]);
        catalog.VerifyGet(x => x.DataConnections, Times.Never);
    }

    [Theory]
    [InlineData("unknown-ds")]
    [InlineData("missing-field")]
    [InlineData("foreign-editor")]
    public async Task UndeclaredNestedDefaultLookupCannotAdmitFallback(string defect)
    {
        var editor = Mock.Of<IDMEEditor>(); var foreign = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "outer", args =>
        {
            try
            {
                return DefaultsManager.ResolveDefaultValue(defect == "foreign-editor" ? foreign : editor,
                    defect == "unknown-ds" ? "another-connection" : "target-connection",
                    defect == "missing-field" ? "missing" : "Value", args) ?? 99;
            }
            catch { return 99; }
        }, operation: "OUTER"));
        var destination = new Mock<IDataSource>();
        var result = await Run(editor, Config(destination, ":OUTER()"), new Dictionary<string, object>());
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
    }

    [Fact]
    public async Task RecursiveNamedDefaultLookupFailsAtRequiredDepthBound()
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "outer",
            args => DefaultsManager.ResolveDefaultValue(editor, "target-connection", "Value", args), operation: "OUTER"));
        var destination = new Mock<IDataSource>();
        var result = await Run(editor, Config(destination, ":OUTER()"), new Dictionary<string, object>());
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectorCancellationOrReportedFailureStopsFurtherPluginCallbacks(bool cancel)
    {
        var editor = Mock.Of<IDMEEditor>(); using var cancellation = new CancellationTokenSource();
        DefaultsManager.RegisterCustomResolver(editor, new SelectionBlocker(editor, cancellation, cancel));
        int selections = 0, resolutions = 0;
        DefaultsManager.RegisterCustomResolver(editor, new OwnershipResolver(editor, "owned", _ =>
            { resolutions++; return 7; }, priority: 0, canHandle: _ => { selections++; return true; }));
        var destination = new Mock<IDataSource>(); var config = Config(destination, ":OWNED()");
        var batch = new DataImportBatchHelper(editor, new DataImportTransformationHelper(editor), Mock.Of<IDataImportProgressHelper>());
        var result = await batch.ProcessBatchDetailedAsync(new object[] { new Dictionary<string, object>() }, config, null!, cancellation.Token);
        Assert.Equal(cancel ? ImportOutcome.Cancelled : ImportOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, selections); Assert.Equal(0, resolutions);
    }

    private sealed class SelectionBlocker(IDMEEditor editor, CancellationTokenSource cancellation, bool cancel)
        : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => "blocker";
        public override int Priority => -1;
        public override IEnumerable<string> SupportedRuleTypes => new[] { "OWNED" };
        public override bool CanHandle(string rule)
        { if (cancel) cancellation.Cancel(); else LogWarning(Secret); return false; }
        public override object ResolveValue(string rule, IPassedArgs parameters) => throw new InvalidOperationException();
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
    }

    private sealed class OwnershipResolver(IDMEEditor editor, string name, Func<IPassedArgs, object> resolve,
        int priority = 100, string operation = "OWNED", Func<string, bool>? canHandle = null) : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => name;
        public override int Priority => priority;
        public override bool SupportsCaching => true;
        public override IEnumerable<string> SupportedRuleTypes => new[] { operation };
        public override bool CanHandle(string rule) => canHandle?.Invoke(rule) ?? rule.StartsWith(operation + "(", StringComparison.Ordinal);
        public override object ResolveValue(string rule, IPassedArgs parameters) => resolve(parameters);
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
    }
}

internal sealed class ResolverRegistryAccess : DefaultsManager
{
    internal static DefaultValueResolverManager Get(IDMEEditor editor)
    {
        // The legacy accessor is process-wide; coordinate lookup, then retain the actual editor's instance.
        lock (_lockObject)
        {
            Initialize(editor);
            return (DefaultValueResolverManager)ResolverManager;
        }
    }
}
