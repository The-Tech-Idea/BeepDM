using System.Collections;
using System.Collections.ObjectModel;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    private static PassedArgs ConfigurationContext(string name, object value) => new()
    { Objects = new() { new() { Name = name, obj = value } } };

    private static object WrittenConfiguration(Mock<IDataSource> destination)
    {
        var write = Assert.Single(destination.Invocations, call => call.Method.Name == nameof(IDataSource.InsertEntity));
        return ((Dictionary<string, object>)write.Arguments[1])["Value"];
    }

    [Theory]
    [InlineData("CONFIG")]
    [InlineData("CONFIGURATIONVALUE")]
    [InlineData("APPSETTING")]
    [InlineData("SETTING")]
    public async Task RequiredConfigurationCannotUseBareEnvironmentNamespaceFallback(string operation)
    {
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, Secret);
        try
        {
            var result = await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), $":{operation}('{key}')"), new Dictionary<string, object>());
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
            Assert.Equal(1, result.RecordsTransformationFailed);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    [Theory]
    [InlineData("CONFIG")]
    [InlineData("CONFIGURATIONVALUE")]
    [InlineData("APPSETTING")]
    [InlineData("SETTING")]
    public async Task RequiredConfigurationFunctionAndColonAliasesReadDeclaredEnvironmentNamespace(string operation)
    {
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N") + ":comma,(group)";
        Environment.SetEnvironmentVariable("APPSETTING_" + key, "declared value");
        try
        {
            foreach (var rule in new[] { $":{operation}('{key}')", $":{operation}:'{key}'" })
            {
                var row = new Dictionary<string, object>();
                Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), rule), row)).Flag);
                Assert.Equal("declared value", row["Value"]);
            }
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Theory]
    [InlineData("CONFIG", "AppSettings")]
    [InlineData("CONFIGURATIONVALUE", "AppSettings")]
    [InlineData("APPSETTING", "AppSettings")]
    [InlineData("SETTING", "AppSettings")]
    [InlineData("APPCONFIG", "AppConfig")]
    [InlineData("WEBCONFIG", "WebConfig")]
    [InlineData("CONNECTIONSTRING", "ConnectionStrings")]
    public async Task RequiredConfigurationReadsActualExplicitNamedMapForEachAlias(string operation, string source)
    {
        foreach (var rule in new[] { $":{operation}('Db:Value,(EU)')", $":{operation}:'Db:Value,(EU)'" })
        {
            var destination = new Mock<IDataSource>();
            var context = ConfigurationContext(source, new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                { ["Db:Value,(EU)"] = "actual host value" }));
            var result = await IdentityRun(rule, context, destination);
            Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("actual host value", WrittenConfiguration(destination));
        }
    }

    [Theory]
    [InlineData("AppSettings", ":APPSETTING(Key)")]
    [InlineData("AppConfig", ":APPCONFIG(Key)")]
    [InlineData("WebConfig", ":WEBCONFIG(Key)")]
    [InlineData("ConnectionStrings", ":CONNECTIONSTRING(Key)")]
    public async Task RequiredConfigurationRetainsGenuineEmptyStringFromExplicitSource(string source, string rule)
    {
        var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await IdentityRun(rule, ConfigurationContext(source,
            new Dictionary<string, string> { ["Key"] = "" }), destination)).Flag);
        Assert.Equal("", WrittenConfiguration(destination));
    }

    [Theory]
    [InlineData("APPCONFIG")]
    [InlineData("WEBCONFIG")]
    public async Task RequiredApplicationAndWebConfigCannotConsumeUnrelatedAppSettingEnvironment(string operation)
    {
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("APPSETTING_" + key, Secret);
        try
        {
            var result = await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), $":{operation}('{key}')"), new Dictionary<string, object>());
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("duplicate")]
    [InlineData("null")]
    public async Task RequiredExplicitConfigurationSourceNeverFallsBackToEnvironment(string kind)
    {
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("APPSETTING_" + key, Secret);
        try
        {
            var context = ConfigurationContext("AppSettings", kind switch
            { "missing" => new Dictionary<string, string>(), "null" => null!, "invalid" => new object(),
                _ => new Dictionary<string, string> { [key] = "first" } });
            if (kind == "duplicate") context.Objects.Add(new() { Name = "appsettings", obj = new Dictionary<string, string> { [key] = "second" } });
            var result = await IdentityRun($":CONFIG('{key}')", context, new Mock<IDataSource>());
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("nullvalue")]
    [InlineData("duplicatekey")]
    [InlineData("blankkey")]
    [InlineData("controlkey")]
    [InlineData("keylimit")]
    [InlineData("valuelimit")]
    [InlineData("entrylimit")]
    [InlineData("budget")]
    public async Task RequiredConfigurationDeniesMalformedOrUnboundedHostMaps(string kind)
    {
        var values = new Dictionary<string, string> { ["Key"] = "value" };
        switch (kind)
        {
            case "nullvalue": values["Other"] = null!; break;
            case "duplicatekey": values["key"] = "second"; break;
            case "blankkey": values[" "] = "bad"; break;
            case "controlkey": values["other\r\nkey"] = "bad"; break;
            case "keylimit": values[new string('a', 1025)] = "bad"; break;
            case "valuelimit": values["Other"] = new string('a', 1048577); break;
            case "entrylimit": for (var i = 0; i < 10000; i++) values["Other" + i] = "v"; break;
            case "budget": for (var i = 0; i < 9; i++) values["Other" + i] = new string('a', 1048576); break;
        }
        var observer = new ConfigurationObserverValue();
        object map = kind == "unsupported" ? new Dictionary<string, object> { ["Key"] = observer } : values;
        var result = await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(0, observer.Reads);
    }

    [Fact]
    public async Task RequiredConfigurationCannotInferHostMapFromImportedRow()
    {
        var context = new PassedArgs { ReturnData = new Dictionary<string, object>
            { ["AppSettings"] = new Dictionary<string, string> { ["Key"] = "row spoof" } } };
        var result = await IdentityRun(":CONFIG(Key)", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData("ConnectionStrings__")]
    [InlineData("CONNECTIONSTRING_")]
    public async Task RequiredConnectionStringAcceptsEitherDeclaredProcessAlias(string prefix)
    {
        var name = "BEEP_REQUIRED_CONN_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(prefix + name, "actual connection");
        try
        {
            var row = new Dictionary<string, object>();
            Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), $":CONNECTIONSTRING('{name}')"), row)).Flag);
            Assert.Equal("actual connection", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable(prefix + name, null); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredConnectionStringAmbiguousAliasesCannotSilentlySelectFirst(bool same)
    {
        var name = "BEEP_REQUIRED_CONN_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("ConnectionStrings__" + name, "first");
        Environment.SetEnvironmentVariable("CONNECTIONSTRING_" + name, same ? "first" : "second");
        try
        {
            var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), $":CONNECTIONSTRING('{name}')"), row);
            Assert.Equal(same ? Errors.Ok : Errors.Failed, result.Flag);
            Assert.Equal(same ? 1 : 0, result.WriteAttempts);
            if (same) Assert.Equal("first", row["Value"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__" + name, null);
            Environment.SetEnvironmentVariable("CONNECTIONSTRING_" + name, null);
        }
    }

    [Fact]
    public async Task RequiredConfigurationCaseInsensitiveHostKeyMustBeUnambiguous()
    {
        var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await IdentityRun(":CONFIG(key)", ConfigurationContext("appsettings",
            new Dictionary<string, string> { ["Key"] = "actual" }), destination)).Flag);
        Assert.Equal("actual", WrittenConfiguration(destination));
    }

    [Fact]
    public void LegacyConfigurationDirectCallRetainsBareEnvironmentFallback()
    {
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, "legacy value");
        try { Assert.Equal("legacy value", new ConfigurationResolver(Mock.Of<IDMEEditor>()).ResolveValue($"CONFIG('{key}')", new PassedArgs())); }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    private sealed class ConfigurationObserverValue
    {
        public int Reads { get; private set; }
        public override string ToString() { Reads++; throw new IOException(Secret); }
    }

    [Theory]
    [InlineData(":CONFIG(Key,ignored)")]
    [InlineData(":APPCONFIG(Key,ignored)")]
    [InlineData(":WEBCONFIG()")]
    [InlineData(":CONNECTIONSTRING(Key)trailing")]
    [InlineData(":CONFIG('Key' 'Other')")]
    [InlineData(":CONFIG('Key\r\nOther')")]
    public async Task RequiredConfigurationInvalidOuterGrammarPrecedesHostCallbacks(string rule)
    {
        var map = new ConfigurationMap();
        var result = await IdentityRun(rule, ConfigurationContext("AppSettings", map), new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(0, map.CountReads); Assert.Equal(0, map.EnumeratorReads);
    }

    [Fact]
    public async Task RequiredConfigurationLookupKeyLimitPrecedesHostCallbacks()
    {
        var map = new ConfigurationMap();
        var result = await IdentityRun(":CONFIG('" + new string('a', 1025) + "')", ConfigurationContext("AppSettings", map), new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(0, map.CountReads); Assert.Equal(0, map.EnumeratorReads);
    }

    [Theory]
    [InlineData("count")]
    [InlineData("enumerator")]
    [InlineData("move")]
    [InlineData("current")]
    [InlineData("dispose")]
    public async Task RequiredConfigurationCallbackExceptionsDenyWithoutSecretLogs(string stage)
    {
        var map = new ConfigurationMap(); var editor = Mock.Of<IDMEEditor>();
        Action fail = () => throw new IOException(Secret);
        switch (stage)
        {
            case "count": map.OnCount = fail; break;
            case "enumerator": map.OnEnumerator = fail; break;
            case "move": map.OnMove = fail; break;
            case "current": map.OnCurrent = fail; break;
            default: map.OnDispose = fail; break;
        }
        var result = await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>(), editor);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.DoesNotContain(Secret, result.Message ?? ""); Assert.Equal(0, map.LookupReads);
        Mock.Get(editor).Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
        if (stage is "move" or "current" or "dispose") Assert.Equal(1, map.DisposeReads);
    }

    [Theory]
    [InlineData("count")]
    [InlineData("enumerator")]
    [InlineData("move")]
    [InlineData("current")]
    [InlineData("dispose")]
    public async Task RequiredConfigurationCancellationAtEverySourceBoundaryStopsBusinessCallbacks(string stage)
    {
        using var cancellation = new CancellationTokenSource(); var map = new ConfigurationMap();
        switch (stage)
        {
            case "count": map.OnCount = cancellation.Cancel; break;
            case "enumerator": map.OnEnumerator = cancellation.Cancel; break;
            case "move": map.OnMove = cancellation.Cancel; break;
            case "current": map.OnCurrent = cancellation.Cancel; break;
            default: map.OnDispose = cancellation.Cancel; break;
        }
        var result = await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>(), token: cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, map.LookupReads);
        if (stage == "count") { Assert.Equal(0, map.EnumeratorReads); Assert.Equal(0, map.DisposeReads); }
        if (stage == "enumerator") { Assert.Equal(0, map.MoveReads); Assert.Equal(1, map.DisposeReads); }
        if (stage == "move") { Assert.Equal(0, map.CurrentReads); Assert.Equal(1, map.DisposeReads); }
        if (stage == "current") { Assert.Equal(1, map.MoveReads); Assert.Equal(1, map.DisposeReads); }
    }

    [Theory]
    [InlineData("count")]
    [InlineData("move")]
    [InlineData("dispose")]
    public async Task RequiredConfigurationReportedFailureCannotAcknowledgeCapturedValue(string stage)
    {
        var editor = Mock.Of<IDMEEditor>(); var map = new ConfigurationMap();
        Action warning = () => new ProbeResolver(editor, _ => 1, warn: true).ResolveValue("PROBE()", new PassedArgs());
        if (stage == "count") map.OnCount = warning;
        else if (stage == "move") map.OnMove = warning;
        else map.OnDispose = warning;
        var result = await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>(), editor);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        if (stage == "count") Assert.Equal(0, map.EnumeratorReads);
        if (stage == "move") Assert.Equal(0, map.CurrentReads);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(10001)]
    public async Task RequiredConfigurationDeclaredCountMustAgreeWithCompleteCapture(int declared)
    {
        var map = new ConfigurationMap { DeclaredCount = declared };
        var result = await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        if (declared is -1 or 10001) Assert.Equal(0, map.EnumeratorReads);
    }

    [Fact]
    public async Task RequiredConfigurationLyingCountCannotCauseUnboundedConsumption()
    {
        var map = new ConfigurationMap { DeclaredCount = 1 };
        map.Rows = Enumerable.Range(0, 10001).Select(index => new KeyValuePair<string, string>("Key" + index, "value")).ToList();
        var result = await IdentityRun(":CONFIG(Key0)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(10000, map.CurrentReads); Assert.Equal(10001, map.MoveReads); Assert.Equal(1, map.DisposeReads);
    }

    [Fact]
    public async Task RequiredConfigurationCapturesStringsBeforeSourceDisposalMutation()
    {
        var map = new ConfigurationMap(); map.OnDispose = () => map.Rows[0] = new("Key", "changed after capture");
        var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), destination)).Flag);
        Assert.Equal("actual value", WrittenConfiguration(destination)); Assert.Equal(0, map.LookupReads);
    }

    [Fact]
    public async Task RequiredConfigurationSourceFailureAfterAcknowledgedPrefixRemainsPartial()
    {
        var editor = Mock.Of<IDMEEditor>(); var map = new ConfigurationMap { OnDispose = () => throw new IOException(Secret) };
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ => DefaultsManager.Resolve(editor,
            ":CONFIG(Key)", ConfigurationContext("AppSettings", map))));
        var first = new Dictionary<string, object> { ["Value"] = "acknowledged" }; var destination = new Mock<IDataSource>();
        var result = await Run(editor, Config(destination, ":PROBE()"), first, new Dictionary<string, object>());
        Assert.Equal(ImportOutcome.Partial, result.Outcome); Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.Equal(1, result.WriteAttempts); Assert.False(result.HasUncertainWrites);
        destination.Verify(x => x.InsertEntity("target", first), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CONFIG(Key,ignored)")]
    public async Task RequiredConfigurationDirectCallsCannotBypassRequiredGrammar(string? rule)
    {
        var editor = Mock.Of<IDMEEditor>(); var map = new ConfigurationMap();
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ => new ConfigurationResolver(editor)
            .ResolveValue(rule, ConfigurationContext("AppSettings", map))));
        var result = await Run(editor, Config(new Mock<IDataSource>(), ":PROBE()"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, map.CountReads);
    }

    [Fact]
    public async Task RequiredConfigurationSubclassKeepsItsExplicitSemantics()
    {
        var editor = Mock.Of<IDMEEditor>(); var row = new Dictionary<string, object>();
        DefaultsManager.RegisterCustomResolver(editor, new CustomConfigurationResolver(editor));
        Assert.Equal(Errors.Ok, (await Run(editor, Config(new Mock<IDataSource>(), ":CONFIG(Key)"), row)).Flag);
        Assert.Equal("custom configuration", row["Value"]);
    }

    [Fact]
    public async Task RequiredConfigurationDuplicateNamespaceDeniesBeforeSourceCallbacks()
    {
        var map = new ConfigurationMap(); var context = ConfigurationContext("AppSettings", map);
        context.Objects.Add(new() { Name = "APPSETTINGS", obj = map });
        var result = await IdentityRun(":CONFIG(Key)", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, map.CountReads);
    }

    [Fact]
    public async Task RequiredConfigurationContextEntryLimitDeniesBeforeSourceCallbacks()
    {
        var map = new ConfigurationMap(); var context = ConfigurationContext("AppSettings", map);
        for (var i = 0; i < 10000; i++) context.Objects.Add(new() { Name = "Other" + i, obj = "unused" });
        var result = await IdentityRun(":CONFIG(Key)", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, map.CountReads);
    }

    [Fact]
    public async Task RequiredConfigurationNullEnumeratorCannotReturnCapturedOrEnvironmentValue()
    {
        var map = new ConfigurationMap { NullEnumerator = true };
        var result = await IdentityRun(":CONFIG(Key)", ConfigurationContext("AppSettings", map), new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, map.MoveReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredExplicitConnectionStringSourceCannotFallThroughToProcessAlias(bool invalid)
    {
        var name = "BEEP_REQUIRED_CONN_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("ConnectionStrings__" + name, Secret);
        try
        {
            var context = ConfigurationContext("ConnectionStrings", invalid ? (object)new ConfigurationObserverValue() : new Dictionary<string, string>());
            var result = await IdentityRun($":CONNECTIONSTRING('{name}')", context, new Mock<IDataSource>());
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        }
        finally { Environment.SetEnvironmentVariable("ConnectionStrings__" + name, null); }
    }

    [Theory]
    [InlineData("CONFIG", "AppSettings")]
    [InlineData("APPCONFIG", "AppConfig")]
    [InlineData("WEBCONFIG", "WebConfig")]
    [InlineData("CONNECTIONSTRING", "ConnectionStrings")]
    public async Task RequiredConfigurationDotLiteralKeysRetainPunctuationAndExplicitNamespace(string operation, string source)
    {
        var destination = new Mock<IDataSource>();
        var result = await IdentityRun($":{operation}.'Key.part,(EU)'", ConfigurationContext(source,
            new Dictionary<string, string> { ["Key.part,(EU)"] = "actual punctuation key" }), destination);
        Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("actual punctuation key", WrittenConfiguration(destination));
    }

    [Fact]
    public async Task RequiredConfigurationNeverGuessesAHostEditorOrCredentialBackend()
    {
        var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ConfigEditor).Throws(new IOException(Secret));
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N");
        var result = await Run(editor.Object, Config(new Mock<IDataSource>(), $":CONFIG('{key}')"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        editor.VerifyGet(x => x.ConfigEditor, Times.Never);
    }

    [Fact]
    public async Task RequiredConfigurationEnvironmentValuesAreDynamicNotMetadataCached()
    {
        var key = "BEEP_REQUIRED_CONFIG_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("APPSETTING_" + key, "first value");
        try
        {
            var destination = new Mock<IDataSource>(); var configuration = Config(destination, $":CONFIG('{key}')"); var writes = 0;
            destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Callback<string, object>((_, _) =>
            { if (++writes == 1) Environment.SetEnvironmentVariable("APPSETTING_" + key, "second value"); })
                .Returns(new ErrorsInfo { Flag = Errors.Ok });
            var first = new Dictionary<string, object>(); var second = new Dictionary<string, object>();
            Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), configuration, first, second)).Flag);
            Assert.Equal("first value", first["Value"]); Assert.Equal("second value", second["Value"]);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    private sealed class CustomConfigurationResolver(IDMEEditor editor) : ConfigurationResolver(editor)
    {
        public override string ResolverName => "CustomConfiguration";
        public override int Priority => 0;
        public override object ResolveValue(string rule, IPassedArgs parameters) => "custom configuration";
    }

    private sealed class ConfigurationMap : IReadOnlyDictionary<string, string>
    {
        public List<KeyValuePair<string, string>> Rows { get; set; } = new() { new("Key", "actual value") };
        public int? DeclaredCount { get; set; }
        public Action? OnCount { get; set; }
        public Action? OnEnumerator { get; set; }
        public Action? OnMove { get; set; }
        public Action? OnCurrent { get; set; }
        public Action? OnDispose { get; set; }
        public int CountReads { get; private set; }
        public int EnumeratorReads { get; private set; }
        public int MoveReads { get; private set; }
        public int CurrentReads { get; private set; }
        public int DisposeReads { get; private set; }
        public int LookupReads { get; private set; }
        public bool NullEnumerator { get; set; }
        public int Count { get { CountReads++; OnCount?.Invoke(); return DeclaredCount ?? Rows.Count; } }
        public IEnumerable<string> Keys => throw new InvalidOperationException(Secret);
        public IEnumerable<string> Values => throw new InvalidOperationException(Secret);
        public string this[string key] { get { LookupReads++; throw new InvalidOperationException(Secret); } }
        public bool ContainsKey(string key) { LookupReads++; throw new InvalidOperationException(Secret); }
        public bool TryGetValue(string key, out string value) { value = null!; LookupReads++; throw new InvalidOperationException(Secret); }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        { EnumeratorReads++; OnEnumerator?.Invoke(); return NullEnumerator ? null! : new Cursor(this); }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private sealed class Cursor(ConfigurationMap owner) : IEnumerator<KeyValuePair<string, string>>
        {
            private int index = -1;
            public KeyValuePair<string, string> Current { get { owner.CurrentReads++; owner.OnCurrent?.Invoke(); return owner.Rows[index]; } }
            object IEnumerator.Current => Current;
            public bool MoveNext() { owner.MoveReads++; owner.OnMove?.Invoke(); return ++index < owner.Rows.Count; }
            public void Dispose() { owner.DisposeReads++; owner.OnDispose?.Invoke(); }
            public void Reset() => throw new NotSupportedException();
        }
    }
}
