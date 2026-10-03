using System.Security.Principal;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    private static Task<TheTechIdea.Beep.Editor.Importing.ImportExecutionResult> IdentityRun(string rule, IPassedArgs context,
        Mock<IDataSource> destination, IDMEEditor? editor = null, CancellationToken token = default)
    {
        editor ??= Mock.Of<IDMEEditor>();
        var owner = editor;
        DefaultsManager.RegisterCustomResolver(owner, new ProbeResolver(owner, _ => DefaultsManager.Resolve(owner, rule, context)));
        Mock.Get(owner).Invocations.Clear();
        return new DataImportBatchHelper(owner, new DataImportTransformationHelper(owner), Mock.Of<IDataImportProgressHelper>())
            .ProcessBatchDetailedAsync(new object[] { new Dictionary<string, object>() }, Config(destination, ":PROBE()"), null!, token);
    }

    [Theory]
    [InlineData(":USEREMAIL")]
    [InlineData(":USERROLE")]
    [InlineData(":USERROLE(Company)")]
    [InlineData(":USERPROFILE(NotAKnownFolder)")]
    public async Task RequiredIdentityCannotInventEmailRoleOrProfile(string rule)
    {
        var editor = new Mock<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var result = await IdentityRun(rule, new PassedArgs(), destination, editor.Object);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.False(result.HasUncertainWrites);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Fact]
    public async Task RequiredApplicationRoleDoesNotFallBackToGenericRole()
    {
        var result = await IdentityRun(":USERROLE(Company)", new IdentityContext { UserRole = "GenericRole" }, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData(":USEREMAIL", "explicit@example.test")]
    [InlineData(":USERROLE", "Reviewer")]
    [InlineData(":USERROLE(Company)", "CompanyReviewer")]
    public async Task RequiredIdentityReadsExplicitHostPropertiesWithoutFabrication(string rule, string expected)
    {
        var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await IdentityRun(rule, new IdentityContext
        { UserEmail = "explicit@example.test", UserRole = "Reviewer", CompanyRole = "CompanyReviewer" }, destination)).Flag);
        var write = Assert.Single(destination.Invocations.Where(call => call.Method.Name == nameof(IDataSource.InsertEntity)));
        Assert.Equal(expected, ((Dictionary<string, object>)write.Arguments[1])["Value"]);
    }

    private sealed class IdentityContext : PassedArgs
    {
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string? CompanyRole { get; set; }
    }

    [Theory]
    [InlineData(":USEREMAIL", "UserEmail", "provided@example.test")]
    [InlineData(":USERROLE", "UserRole", "ProvidedRole")]
    [InlineData(":USERROLE('Company,(EU)')", "Company,(EU)Role", "ExactRole")]
    public async Task RequiredIdentitySupportsExactNamedHostValues(string rule, string name, string expected)
    {
        var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await IdentityRun(rule, new PassedArgs
        { Objects = new() { new() { Name = name, obj = expected } } }, destination)).Flag);
        var write = Assert.Single(destination.Invocations.Where(call => call.Method.Name == nameof(IDataSource.InsertEntity)));
        Assert.Equal(expected, ((Dictionary<string, object>)write.Arguments[1])["Value"]);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("object")]
    [InlineData("null")]
    [InlineData("blank")]
    [InlineData("control")]
    [InlineData("oversized")]
    [InlineData("ambiguous")]
    public async Task RequiredIdentityDeniesInvalidOrAmbiguousNamedValuesWithoutObserverConversion(string kind)
    {
        var observer = new IdentityObserverValue();
        object? value = kind switch { "number" => 17, "object" => observer, "null" => null,
            "blank" => " ", "control" => "role\r\nother", "oversized" => new string('a', 4097), _ => "First" };
        var context = new PassedArgs { Objects = new() { new() { Name = "UserRole", obj = value! } } };
        if (kind == "ambiguous") context.Objects.Add(new() { Name = "userrole", obj = "Second" });
        var result = await IdentityRun(":USERROLE", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, observer.Reads);
    }

    private sealed class IdentityObserverValue
    {
        public int Reads { get; private set; }
        public override string ToString() { Reads++; throw new IOException(Secret); }
    }

    [Fact]
    public async Task RequiredIdentityNeverInfersTrustedRoleOrEmailFromImportPayload()
    {
        var context = new PassedArgs { ReturnData = new Dictionary<string, object>
        { ["UserEmail"] = "row-spoof@example.test", ["UserRole"] = "Administrator" } };
        var result = await IdentityRun(":USEREMAIL", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Machine")]
    [InlineData("System")]
    public async Task RequiredEnvironmentNeverFallsBackFromRequestedScopeToProcess(string scope)
    {
        var key = "BEEP_REQUIRED_SCOPE_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, "process-only-value", EnvironmentVariableTarget.Process);
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":ENV('{key}',{scope})"), row);
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.False(row.ContainsKey("Value"));
        }
        finally { Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process); }
    }

    [Theory]
    [InlineData("ENV")]
    [InlineData("ENVIRONMENT")]
    [InlineData("ENVVAR")]
    [InlineData("ENVIRONMENTVARIABLE")]
    public async Task RequiredEnvironmentRetainsExplicitProcessValuesAndAliases(string op)
    {
        var key = "BEEP_REQUIRED_PROCESS_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, "actual-process-value");
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, $":{op}('{key}',Process)"), row)).Flag);
            Assert.Equal("actual-process-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    [Theory]
    [InlineData(":TEMP")]
    [InlineData(":TEMPPATH")]
    [InlineData(":USERPROFILE(TEMP)")]
    public async Task RequiredTemporaryFolderAliasesUseTheActualTemporaryDirectory(string rule)
    {
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(Path.GetTempPath(), row["Value"]);
    }

    [Theory]
    [InlineData(":USERNAME")]
    [InlineData(":CURRENTUSER")]
    [InlineData(":USERLOGIN")]
    public async Task RequiredUsernameAliasesKeepActualOsAccountMeaning(string rule)
    {
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(Environment.UserName, row["Value"]);
    }

    [Theory]
    [InlineData(":USERID")]
    [InlineData(":USERPRINCIPAL")]
    public async Task RequiredWindowsIdentityRetainsActualSidOrNameWithoutUsernameFallback(string rule)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var identity = WindowsIdentity.GetCurrent();
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(rule == ":USERID" ? identity.User!.Value : identity.Name, row["Value"]);
    }

    [Fact]
    public async Task RequiredSystemPathReadsMachineScopeNotInheritedProcessPath()
    {
        var expected = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":SYSTEMPATH"), row);
        Assert.Equal(expected == null ? Errors.Failed : Errors.Ok, result.Flag);
        if (expected != null) Assert.Equal(expected, row["Value"]);
        else Assert.Equal(0, result.WriteAttempts);
    }

    [Fact]
    public async Task RequiredIdentityRejectsConflictingPropertyAndNamedContextBeforeReadingGetter()
    {
        var context = new IdentityGetterContext(() => "PropertyRole")
        { Objects = new() { new() { Name = "UserRole", obj = "NamedRole" } } };
        var result = await IdentityRun(":USERROLE", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, context.Reads);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("object")]
    [InlineData("blank")]
    public async Task RequiredIdentityPropertyRequiresActualStringNotCoercion(string kind)
    {
        var observer = new IdentityObserverValue();
        var context = new IdentityGetterContext(() => kind switch { "number" => 17, "object" => observer, _ => " " });
        var result = await IdentityRun(":USERROLE", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(1, context.Reads); Assert.Equal(0, observer.Reads);
    }

    [Fact]
    public async Task RequiredIdentityGetterFailureCannotLogOrReturnHostRoleFallback()
    {
        var context = new IdentityGetterContext(() => throw new IOException(Secret));
        var editor = new Mock<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var result = await IdentityRun(":USERROLE", context, destination, editor.Object);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(1, context.Reads);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Fact]
    public async Task RequiredIdentityCancellationAfterGetterAdmitsNoProviderWrite()
    {
        using var cancellation = new CancellationTokenSource();
        var context = new IdentityGetterContext(() => { cancellation.Cancel(); return "ActualRole"; });
        var result = await IdentityRun(":USERROLE", context, new Mock<IDataSource>(), token: cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome); Assert.Equal(0, result.WriteAttempts); Assert.Equal(1, context.Reads);
        Assert.False(result.HasUncertainWrites);
    }

    [Fact]
    public async Task RequiredIdentityInvalidRuleStopsBeforeContextGetter()
    {
        var context = new IdentityGetterContext(() => "ActualRole");
        var result = await IdentityRun(":USERROLE(Company,extra)", context, new Mock<IDataSource>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, context.Reads);
    }

    private sealed class IdentityGetterContext(Func<object> read) : PassedArgs
    {
        public int Reads { get; private set; }
        public object UserRole { get { Reads++; return read(); } }
    }

    [Theory]
    [InlineData(":USERROLE.'Company,(EU)'")]
    [InlineData(":USERROLE('Company,(EU)')")]
    public async Task RequiredApplicationRoleDotAndQuotedCallsPreserveExactHostKey(string rule)
    {
        var result = await IdentityRun(rule, new PassedArgs
        { Objects = new() { new() { Name = "Company,(EU)Role", obj = "ScopedReviewer" } } }, new Mock<IDataSource>());
        Assert.Equal(Errors.Ok, result.Flag); Assert.Equal(1, result.WriteAttempts);
    }

    [Theory]
    [InlineData(":ENV:")]
    [InlineData(":ENVIRONMENT:")]
    public async Task RequiredColonEnvironmentKeyPreservesQuotedParenthesesAndCommas(string prefix)
    {
        var key = "BEEP_REQUIRED_COLON_" + Guid.NewGuid().ToString("N") + "(a,b)";
        Environment.SetEnvironmentVariable(key, "exact-colon-value");
        try
        {
            var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
            Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, prefix + "'" + key + "'"), row)).Flag);
            Assert.Equal("exact-colon-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    [Theory]
    [InlineData("DOCUMENTS", Environment.SpecialFolder.MyDocuments)]
    [InlineData("APPDATA", Environment.SpecialFolder.ApplicationData)]
    [InlineData("LOCALAPPDATA", Environment.SpecialFolder.LocalApplicationData)]
    [InlineData("DESKTOP", Environment.SpecialFolder.Desktop)]
    public async Task RequiredProfileUsesTheNamedOsFolderOrDeniesItsAbsence(string name, Environment.SpecialFolder folder)
    {
        var expected = Environment.GetFolderPath(folder); var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":USERPROFILE('{name}')"), row);
        Assert.Equal(string.IsNullOrWhiteSpace(expected) ? Errors.Failed : Errors.Ok, result.Flag);
        if (!string.IsNullOrWhiteSpace(expected)) Assert.Equal(expected, row["Value"]);
        else Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData(ApartmentState.STA)]
    [InlineData(ApartmentState.MTA)]
    public void RequiredDownloadsUsesActualKnownFolderAndPreservesCallerApartment(ApartmentState apartment)
    {
        if (!OperatingSystem.IsWindows()) return;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
                ApartmentState? during = null;
                DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ =>
                {
                    during = Thread.CurrentThread.GetApartmentState();
                    return DefaultsManager.Resolve(editor, ":USERPROFILE(Downloads)");
                }));
                var result = Run(editor, Config(destination, ":PROBE()"), row).GetAwaiter().GetResult();
                Assert.Equal(apartment, during);
                Assert.Equal(apartment, Thread.CurrentThread.GetApartmentState());
                Assert.Equal(Errors.Ok, result.Flag);
                var path = Assert.IsType<string>(row["Value"]); Assert.True(Path.IsPathFullyQualified(path));
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
                if (key?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") is string registered)
                    Assert.Equal(Environment.ExpandEnvironmentVariables(registered).TrimEnd('\\'), path.TrimEnd('\\'), ignoreCase: true);
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(apartment); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Known-folder worker did not finish.");
        if (failure != null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    [Fact]
    public async Task RequiredUserGroupReturnsOnlyAVerifiedBuiltinMembership()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var identity = WindowsIdentity.GetCurrent(); var principal = new WindowsPrincipal(identity);
        var expected = principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Administrators" :
            principal.IsInRole(WindowsBuiltInRole.PowerUser) ? "Power Users" : principal.IsInRole(WindowsBuiltInRole.User) ? "Users" : null;
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":USERGROUP"), row);
        Assert.Equal(expected == null ? Errors.Failed : Errors.Ok, result.Flag);
        if (expected != null) Assert.Equal(expected, row["Value"]);
        else Assert.Equal(0, result.WriteAttempts);
    }

    [Fact]
    public async Task RequiredUserPathRetainsUserScopeWithoutProcessFallback()
    {
        var expected = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":USERPATH"), row);
        Assert.Equal(expected == null ? Errors.Failed : Errors.Ok, result.Flag);
        if (expected != null) Assert.Equal(expected, row["Value"]);
        else Assert.Equal(0, result.WriteAttempts);
    }

    [Fact]
    public async Task RequiredCustomUserResolverStillOwnsItsSemanticContract()
    {
        var editor = Mock.Of<IDMEEditor>(); DefaultsManager.RegisterCustomResolver(editor, new CustomUserResolver(editor));
        var row = new Dictionary<string, object>(); var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":USEREMAIL"), row)).Flag);
        Assert.Equal("plugin-defined-user", row["Value"]);
    }

    private sealed class CustomUserResolver(IDMEEditor editor) : TheTechIdea.Beep.Editor.Defaults.Resolvers.UserContextResolver(editor)
    {
        public override object ResolveValue(string rule, IPassedArgs parameters) => "plugin-defined-user";
    }

    [Fact]
    public void LegacyIdentityStillHasItsDemonstrationFallbacksOutsideRequiredScope()
    {
        var editor = Mock.Of<IDMEEditor>();
        Assert.IsType<string>(DefaultsManager.Resolve(editor, ":USEREMAIL"));
        Assert.IsType<string>(DefaultsManager.Resolve(editor, ":USERROLE"));
    }
}
