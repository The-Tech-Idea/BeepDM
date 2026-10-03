using System.Globalization;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class QueryPolicyTests
{
    internal sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly Mock<IUnitofWork> Unit = new();
        internal readonly FormsManager Manager;
        internal readonly List<EntityField> Fields = new()
        {
            new() { FieldName = "TenantId", Fieldtype = "int" },
            new() { FieldName = "Id", Fieldtype = "int" },
            new() { FieldName = "Name", Fieldtype = "string" },
            new() { FieldName = "Amount", Fieldtype = "decimal" }
        };
        internal readonly List<List<AppFilter>> Reads = new();

        internal Harness(bool parameterized = false)
        {
            if (parameterized) Source.As<IParameterizedScalarDataSource>();
            Source.Setup(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
            Source.Setup(s => s.GetScalarAsync(It.IsAny<string>())).ReturnsAsync(3d);
            Unit.SetupProperty(u => u.DataSource, Source.Object);
            Unit.SetupProperty(u => u.EntityName, "Records");
            Unit.Setup(u => u.Get(It.IsAny<List<AppFilter>>()))
                .Callback<List<AppFilter>>(filters => Reads.Add(filters))
                .ReturnsAsync((object)new List<object>());
            Editor.Setup(e => e.GetDataSource(It.IsAny<string>())).Returns(Source.Object);
            var entity = new Mock<IEntityStructure>();
            entity.Setup(e => e.EntityName).Returns("Records");
            entity.Setup(e => e.Fields).Returns(Fields);
            Manager = new FormsManager(Editor.Object);
            Manager.RegisterBlock("ROWS", Unit.Object, entity.Object, "db");
            Manager.GetBlock("ROWS").Mode = DataBlockMode.Query;
            Editor.Invocations.Clear();
            Source.Invocations.Clear();
        }

        internal void Policy(string clause = "TenantId = :tenant", object? tenant = null)
            => Manager.SetBlockSecurity("ROWS", new BlockSecurity
            {
                RowFilterClause = clause,
                RowFilterValues = new() { ["tenant"] = tenant ?? 42 }
            });

        public void Dispose() => Manager.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BasicAndEnhancedQueryComposeCallerDefaultAndPolicyExactlyOnce(bool enhanced)
    {
        using var h = new Harness();
        h.Policy();
        h.Manager.SetDefaultWhere("ROWS", "Id > 0");
        var caller = new List<AppFilter> { new() { FieldName = "Name", Operator = "=", FilterValue = "' OR 1=1 --" } };
        var ok = enhanced ? (await h.Manager.ExecuteQueryEnhancedAsync("ROWS", caller)).Flag == Errors.Ok :
            await h.Manager.ExecuteQueryAsync("ROWS", caller);
        Assert.True(ok);
        var filters = Assert.Single(h.Reads);
        Assert.Equal(3, filters.Count);
        Assert.Single(filters, f => f.FieldName == "TenantId" && f.FilterValue == "42");
        Assert.Single(filters, f => f.FieldName == "Id" && f.Operator == ">");
        Assert.Single(filters, f => f.FilterValue == "' OR 1=1 --" && f.FieldType == typeof(string));
        Assert.Single(caller);
        Assert.Null(caller[0].FieldType);
    }

    [Fact]
    public async Task DeniedPolicyBlocksAllDirectReadRoutesBeforeProviderLookup()
    {
        using var h = new Harness();
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false });
        Assert.False(await h.Manager.ExecuteQueryAsync("ROWS"));
        Assert.Equal(Errors.Failed, (await h.Manager.ExecuteQueryEnhancedAsync("ROWS")).Flag);
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS"));
        Assert.Equal(0, await h.Manager.GetBlockAggregateScalarAsync("ROWS", "COUNT(*)"));
        Assert.Empty(h.Reads);
        h.Unit.Verify(u => u.Get(), Times.Never);
        h.Editor.Verify(e => e.GetDataSource(It.IsAny<string>()), Times.Never);
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("TenantId = :missing")]
    [InlineData("TenantId = 42 OR TenantId = 99")]
    [InlineData("TenantId = 42 -- rest")]
    [InlineData("TenantId = 42; SELECT 1")]
    [InlineData("TenantId = 'oops")]
    [InlineData("ABS(TenantId) = 42")]
    [InlineData("UnknownField = 42")]
    [InlineData("TenantId IN ()")]
    [InlineData("TenantId IN (NULL, 42)")]
    [InlineData("TenantId = 999999999999999999999999")]
    public async Task UnsupportedPolicyFailsClosedInsteadOfDroppingPredicates(string clause)
    {
        using var h = new Harness();
        h.Policy(clause);
        Assert.False(await h.Manager.ExecuteQueryAsync("ROWS"));
        Assert.Equal(Errors.Failed, (await h.Manager.ExecuteQueryEnhancedAsync("ROWS")).Flag);
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS"));
        Assert.Empty(h.Reads);
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("OR")]
    [InlineData("= 42 OR 1=1 --")]
    public async Task UnsupportedCallerOperatorCannotWidenMandatoryPolicy(string op)
    {
        using var h = new Harness();
        h.Policy();
        var filters = new List<AppFilter> { new() { FieldName = "TenantId", Operator = op, FilterValue = "99" } };
        Assert.False(await h.Manager.ExecuteQueryAsync("ROWS", filters));
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS", filters));
        Assert.Empty(h.Reads);
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ParenthesizedPolicyPreservesBetweenNullEmptyAndQuotedCollectionValues()
    {
        using var h = new Harness();
        h.Policy("WHERE (TenantId = :tenant AND (Id BETWEEN 1 AND 9)) AND Name IN ('a,b', '', 'O''Brien') AND Amount IS NOT NULL");
        Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        var filters = Assert.Single(h.Reads);
        Assert.Equal(4, filters.Count);
        Assert.Equal(new[] { "a,b", "", "O'Brien" }, DataSourceAppFilterExtensions.ParseCollectionFilterValues(
            Assert.Single(filters, f => f.Operator == "in").FilterValue));
        var between = Assert.Single(filters, f => f.Operator == "between");
        Assert.Equal("1", between.FilterValue);
        Assert.Equal("9", between.FilterValue1);
        Assert.Contains(filters, f => f.FieldName == "Amount" && f.Operator == "is not null");
    }

    [Theory]
    [InlineData("Name = NULL", "is null", null)]
    [InlineData("Name <> NULL", "is not null", null)]
    [InlineData("Name = ''", "=", "")]
    public async Task NullAndEmptyStringAreDistinct(string clause, string op, string? value)
    {
        using var h = new Harness();
        h.Policy(clause);
        Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        var filter = Assert.Single(Assert.Single(h.Reads));
        Assert.Equal(op, filter.Operator);
        Assert.Equal(value, filter.FilterValue);
    }

    private sealed class ForbiddenValue
    {
        internal bool WasFormatted;
        public override string ToString() { WasFormatted = true; throw new Exception("User formatting must not run"); }
    }

    [Fact]
    public async Task UnsupportedPolicyObjectIsRejectedWithoutInvokingUserFormatting()
    {
        using var h = new Harness();
        var value = new ForbiddenValue();
        h.Policy("Name = :tenant", value);
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS"));
        Assert.False(value.WasFormatted);
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecurityContextAndPolicyAreOwnedSnapshotsAndRoleOrderDoesNotMatter(bool reverse)
    {
        using var h = new Harness();
        var context = new SecurityContext { Roles = reverse ? new() { "reader", "denied" } : new() { "denied", "reader" } };
        var policy = new BlockSecurity
        {
            AllowQuery = false, RowFilterClause = "TenantId = :tenant",
            RowFilterValues = new() { ["tenant"] = 42 },
            RolePermissions = new() { ["reader"] = SecurityPermission.Query, ["denied"] = SecurityPermission.None }
        };
        h.Manager.SetSecurityContext(context);
        h.Manager.SetBlockSecurity("ROWS", policy);
        context.Roles.Clear(); context.IsAdmin = true;
        policy.RowFilterValues["tenant"] = 99;
        policy.RolePermissions.Clear();
        var exported = h.Manager.GetBlockSecurity("ROWS");
        exported.RowFilterValues["tenant"] = 88;
        h.Manager.SecurityContext.Roles.Clear();
        Assert.True(h.Manager.IsBlockAllowed("ROWS", SecurityPermission.Query));
        Assert.False(h.Manager.SecurityContext.IsAdmin);
        Assert.Equal(2, h.Manager.SecurityContext.Roles.Count);
        Assert.Equal(42, h.Manager.GetBlockSecurity("ROWS").RowFilterValues["tenant"]);
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("source")]
    [InlineData("entity")]
    [InlineData("schema")]
    [InlineData("default")]
    public async Task PreQueryMutationInvalidatesReadBeforeUowGet(string mutation)
    {
        using var h = new Harness();
        h.Policy();
        h.Manager.Triggers.RegisterBlockTrigger(TriggerType.PreQuery, "ROWS", _ =>
        {
            switch (mutation)
            {
                case "policy": h.Policy(tenant: 99); break;
                case "source": h.Unit.Object.DataSource = new Mock<IDataSource>().Object; break;
                case "entity": h.Unit.Object.EntityName = "OtherRows"; break;
                case "schema": h.Fields[0].Fieldtype = "string"; break;
                case "default": h.Manager.SetDefaultWhere("ROWS", "Id = 8"); break;
            }
            return TriggerResult.Success;
        });
        Assert.Equal(Errors.Failed, (await h.Manager.ExecuteQueryEnhancedAsync("ROWS")).Flag);
        Assert.Empty(h.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScalarCompletionCannotReturnAStaleCountAfterPolicyChangeOrCancellation(bool cancel)
    {
        using var h = new Harness(true);
        h.Policy();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Source.As<IParameterizedScalarDataSource>().Setup(s => s.GetScalarAsync(It.IsAny<AppFilterQueryDefinition>(), It.IsAny<CancellationToken>()))
            .Callback(() => entered.SetResult()).Returns(release.Task);
        using var ct = new CancellationTokenSource();
        var pending = h.Manager.CountQueryAsync("ROWS", ct: ct.Token);
        await entered.Task;
        if (cancel) ct.Cancel(); else h.Policy(tenant: 99);
        release.SetResult(7L);
        Assert.Equal(-1, await pending);
        Assert.Empty(h.Reads);
    }

    [Fact]
    public async Task ParameterizedCountBindsClosedValuesWithInvariantTypesAndNeverUsesRawScalar()
    {
        using var h = new Harness(true);
        h.Policy("TenantId = :tenant AND Amount >= :amount AND Name = :name");
        var policy = h.Manager.GetBlockSecurity("ROWS");
        policy.RowFilterValues["amount"] = 1.25m;
        policy.RowFilterValues["name"] = "' OR 1=1 --";
        h.Manager.SetBlockSecurity("ROWS", policy);
        AppFilterQueryDefinition? captured = null;
        h.Source.As<IParameterizedScalarDataSource>().Setup(s => s.GetScalarAsync(It.IsAny<AppFilterQueryDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<AppFilterQueryDefinition, CancellationToken>((d, _) => captured = d).ReturnsAsync(6L);
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(6, await h.Manager.CountQueryAsync("ROWS"));
        }
        finally { CultureInfo.CurrentCulture = old; }
        Assert.NotNull(captured);
        Assert.Equal(42, captured.Parameters["p0"]);
        Assert.Equal(1.25m, captured.Parameters["p1"]);
        Assert.Equal("' OR 1=1 --", captured.Parameters["p2"]);
        Assert.DoesNotContain("OR 1=1", captured.QueryText);
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
        Assert.Empty(h.Reads);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(1.5d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(2147483648d)]
    public async Task InvalidCountCannotMasqueradeAsZeroOrTruncate(double scalar)
    {
        using var h = new Harness();
        h.Source.Setup(s => s.GetScalarAsync(It.IsAny<string>())).ReturnsAsync(scalar);
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS"));
    }

    [Theory]
    [InlineData("filters")]
    [InlineData("nesting")]
    [InlineData("value")]
    public async Task OversizedPoliciesFailBeforeProviderExecution(string limit)
    {
        using var h = new Harness();
        h.Policy(limit switch
        {
            "filters" => string.Join(" AND ", Enumerable.Repeat("Id = 1", 129)),
            "nesting" => new string('(', 34) + "Id = 1" + new string(')', 34),
            _ => "Name = :tenant"
        }, limit == "value" ? new string('a', 65537) : 42);
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS"));
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AlreadyCancelledCountDoesNotResolveOrReadProvider()
    {
        using var h = new Harness();
        using var ct = new CancellationTokenSource();
        ct.Cancel();
        Assert.Equal(-1, await h.Manager.CountQueryAsync("ROWS", ct: ct.Token));
        h.Editor.Verify(e => e.GetDataSource(It.IsAny<string>()), Times.Never);
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("SELECT COUNT(*) FROM Records")]
    [InlineData("COUNT(*) FROM Records; --")]
    [InlineData("COUNT(*) + 1")]
    [InlineData("SUM(*)")]
    [InlineData("SUM(Name)")]
    [InlineData("SUM(UnknownField)")]
    [InlineData("AVG((SELECT 1))")]
    public async Task AggregateRejectsRawSqlAndUnsupportedExpressionsBeforeScalar(string expression)
    {
        using var h = new Harness();
        h.Policy();
        Assert.Equal(0, await h.Manager.GetBlockAggregateScalarAsync("ROWS", expression));
        h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PublicQuerySnapshotOwnsItsParameterDictionary()
    {
        var values = new Dictionary<string, object> { ["tenant"] = 42 };
        var snapshot = new QuerySecuritySnapshot(1, true, "TenantId = :tenant", values);
        values["tenant"] = 99;
        Assert.Equal(42, snapshot.RowFilterValues["TENANT"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object>)snapshot.RowFilterValues)["tenant"] = 88);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeniedDetailCannotReadThroughImmediateOrDeferredSynchronization(bool deferred)
    {
        using var h = new Harness();
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false });
        var parent = new Mock<IUnitofWork>();
        parent.Setup(u => u.CurrentItem).Returns(new { Id = 7 });
        var entity = new Mock<IEntityStructure>();
        entity.Setup(e => e.EntityName).Returns("Parents");
        entity.Setup(e => e.Fields).Returns(new List<EntityField> { new() { FieldName = "Id", Fieldtype = "int" } });
        h.Manager.RegisterBlock("PARENT", parent.Object, entity.Object);
        h.Manager.CreateMasterDetailRelation("PARENT", "ROWS", "Id", "Id");
        if (deferred) h.Manager.GetActiveRelationships("PARENT").Single().Coordination = DetailCoordination.Deferred;
        var outcome = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("PARENT");
        if (deferred) outcome = await h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("PARENT", "ROWS");
        Assert.Equal(Errors.Failed, outcome.Flag);
        Assert.Equal(DetailSynchronizationState.Failed, Assert.Single(outcome.Details).State);
        Assert.Empty(h.Reads);
        h.Unit.Verify(u => u.Get(), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImmediateAndDeferredDetailQueriesApplyDetailPolicy(bool deferred)
    {
        using var h = new Harness();
        h.Policy();
        var parent = new Mock<IUnitofWork>();
        parent.Setup(u => u.CurrentItem).Returns(new { Id = 7 });
        var entity = new Mock<IEntityStructure>();
        entity.Setup(e => e.EntityName).Returns("Parents");
        entity.Setup(e => e.Fields).Returns(new List<EntityField> { new() { FieldName = "Id", Fieldtype = "int" } });
        h.Manager.RegisterBlock("PARENT", parent.Object, entity.Object);
        h.Manager.CreateMasterDetailRelation("PARENT", "ROWS", "Id", "Id");
        if (deferred) h.Manager.GetActiveRelationships("PARENT").Single().Coordination = DetailCoordination.Deferred;
        await h.Manager.SynchronizeDetailBlocksAsync("PARENT");
        if (deferred)
        {
            Assert.Empty(h.Reads);
            await h.Manager.SynchronizeDeferredDetailAsync("PARENT", "ROWS");
        }
        var filters = Assert.Single(h.Reads);
        Assert.Equal(2, filters.Count);
        Assert.Contains(filters, f => f.FieldName == "Id" && f.FilterValue == "7");
        Assert.Contains(filters, f => f.FieldName == "TenantId" && f.FilterValue == "42");
    }
}
