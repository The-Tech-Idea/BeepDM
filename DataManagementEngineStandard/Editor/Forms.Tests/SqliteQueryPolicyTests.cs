using Microsoft.Data.Sqlite;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class SqliteQueryPolicyTests
{
    public static IEnumerable<object[]> TextCases()
    {
        foreach (var text in new[] { "' OR 1=1 --", "O'Brien", "a,b", "", "back\\slash", "nul\0byte", "\u0645\u0631\u062d\u0628\u0627" })
        {
            yield return new object[] { text, false };
            yield return new object[] { text, true };
        }
    }

    [Theory]
    [MemberData(nameof(TextCases))]
    public async Task ActualSqliteCountKeepsTextAsDataAndNeverCountsAnotherTenant(string text, bool bind)
    {
        using var h = new QueryPolicyTests.Harness(bind);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE Records (Id INTEGER, TenantId INTEGER, Name TEXT, Amount NUMERIC);" +
                "INSERT INTO Records VALUES (1,42,@name,2),(2,99,@name,2),(3,42,'other',2);";
            setup.Parameters.AddWithValue("@name", text);
            setup.ExecuteNonQuery();
        }
        h.Policy();
        if (bind)
            h.Source.As<IParameterizedScalarDataSource>()
                .Setup(s => s.GetScalarAsync(It.IsAny<AppFilterQueryDefinition>(), It.IsAny<CancellationToken>()))
                .Returns<AppFilterQueryDefinition, CancellationToken>(async (definition, ct) =>
                {
                    using var command = connection.CreateCommand();
                    command.ApplyFilterQueryDefinition(definition);
                    return (await command.ExecuteScalarAsync(ct))!;
                });
        else
            h.Source.Setup(s => s.GetScalarAsync(It.IsAny<string>())).Returns<string>(async sql =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                return Convert.ToDouble(await command.ExecuteScalarAsync());
            });
        var caller = new List<AppFilter> { new() { FieldName = "Name", Operator = "=", FilterValue = text } };
        Assert.Equal(1, await h.Manager.CountQueryAsync("ROWS", caller));
        Assert.Empty(h.Reads);
        if (bind) h.Source.Verify(s => s.GetScalarAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ActualSqliteReadAndCountAgreeOnPolicyBetweenNotLikeAndQuotedIn()
    {
        using var h = new QueryPolicyTests.Harness(true);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE Records (Id INTEGER, TenantId INTEGER, Name TEXT, Amount NUMERIC);" +
                "INSERT INTO Records VALUES (1,42,'a,b',2),(2,42,'',8),(3,99,'a,b',100)," +
                "(4,42,'other',2),(5,42,NULL,2),(10,42,'a,b',2);";
            setup.ExecuteNonQuery();
        }
        h.Policy("(TenantId = :tenant) AND Id BETWEEN 1 AND 9 AND Name IN ('a,b','') AND Name NOT LIKE 'other%'");
        var loaded = new List<string>();
        h.Unit.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns<List<AppFilter>>(filters =>
        {
            var definition = h.Source.Object.BuildSelectQueryDefinition("Records", filters);
            using var command = connection.CreateCommand();
            command.ApplyFilterQueryDefinition(definition);
            using var reader = command.ExecuteReader();
            while (reader.Read()) loaded.Add(reader.GetString(2));
            return Task.FromResult<dynamic>(loaded);
        });
        h.Source.As<IParameterizedScalarDataSource>()
            .Setup(s => s.GetScalarAsync(It.IsAny<AppFilterQueryDefinition>(), It.IsAny<CancellationToken>()))
            .Returns<AppFilterQueryDefinition, CancellationToken>(async (definition, ct) =>
            {
                using var command = connection.CreateCommand();
                command.ApplyFilterQueryDefinition(definition);
                return (await command.ExecuteScalarAsync(ct))!;
            });
        Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        Assert.Equal(new[] { "a,b", "" }, loaded);
        Assert.Equal(loaded.Count, await h.Manager.CountQueryAsync("ROWS"));
        Assert.Equal(2, await h.Manager.GetBlockAggregateScalarAsync("ROWS", "COUNT(*)"));
        Assert.Equal(10, await h.Manager.GetBlockAggregateScalarAsync("ROWS", "SUM(Amount)"));
        Assert.Equal(5, await h.Manager.GetBlockAggregateScalarAsync("ROWS", "AVG(Amount)"));
        Assert.Equal(2, await h.Manager.GetBlockAggregateScalarAsync("ROWS", "MIN(Amount)"));
        Assert.Equal(8, await h.Manager.GetBlockAggregateScalarAsync("ROWS", "MAX(Amount)"));
    }
}
