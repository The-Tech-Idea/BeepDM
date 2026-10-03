using Microsoft.Data.Sqlite;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class SqliteCommitOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualSqliteTransactionCommitsBothBlocksOrPersistsNeither(bool failSecondWrite)
    {
        // This qualifies the engine against an actual SQLite ADO.NET transaction,
        // not the separately distributed Beep SQLite plugin or desktop UI adapters.
        var path = Path.Combine(Path.GetTempPath(), "BeepDM-forms-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE A(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL); CREATE TABLE B(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL);";
                command.ExecuteNonQuery();
            }
            SqliteTransaction? transaction = null;
            var source = new Mock<IDataSource>();
            source.SetupGet(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
            source.SetupGet(s => s.DatasourceName).Returns(path);
            var begins = 0;
            var commits = 0;
            var aborts = 0;
            var writes = 0;
            source.Setup(s => s.BeginTransaction(It.IsAny<PassedArgs>())).Returns(() =>
            {
                if (transaction != null) throw new InvalidOperationException("Nested SQLite transaction denied");
                transaction = connection.BeginTransaction(); begins++;
                return new ErrorsInfo { Flag = Errors.Ok };
            });
            source.Setup(s => s.Commit(It.IsAny<PassedArgs>())).Returns(() =>
            {
                transaction!.Commit(); transaction.Dispose(); transaction = null; commits++;
                return new ErrorsInfo { Flag = Errors.Ok };
            });
            source.Setup(s => s.EndTransaction(It.IsAny<PassedArgs>())).Returns(() =>
            {
                transaction!.Rollback(); transaction.Dispose(); transaction = null; aborts++;
                return new ErrorsInfo { Flag = Errors.Ok };
            });
            source.Setup(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>())).Returns((string entity, object record) =>
            {
                if (entity != "A" && entity != "B") throw new InvalidOperationException("Unexpected entity");
                var row = (CommitOwnershipTests.Row)record;
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"INSERT INTO [{entity}](Name) VALUES(@name); SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("@name", failSecondWrite && ++writes == 2 ? DBNull.Value : row.Name);
                try
                {
                    row.Id = Convert.ToInt32(command.ExecuteScalar());
                    return new ErrorsInfo { Flag = Errors.Ok };
                }
                catch (SqliteException ex) { return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex }; }
            });
            var editor = new Mock<IDMEEditor>().Object;
            using var manager = new FormsManager(editor);
            var units = new List<UnitofWork<CommitOwnershipTests.Row>>();
            var wrappers = new List<UnitOfWorkWrapper>();
            try
            {
                foreach (var name in new[] { "A", "B" })
                {
                    var structure = new EntityStructure { EntityName = name,
                        Fields = new List<EntityField> { new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true, IsAutoIncrement = true } } };
                    var unit = new UnitofWork<CommitOwnershipTests.Row>(editor, "database", name, structure, "Id")
                    { DataSource = source.Object, Units = new ObservableBindingList<CommitOwnershipTests.Row>() };
                    unit.Units.Add(new CommitOwnershipTests.Row());
                    var wrapper = new UnitOfWorkWrapper(unit);
                    units.Add(unit); wrappers.Add(wrapper);
                    manager.RegisterBlock(name, wrapper, structure);
                    manager.GetBlock(name).Mode = DataBlockMode.CRUD;
                }
                var result = await manager.CommitFormWithOutcomeAsync();
                Assert.Equal(failSecondWrite ? Errors.Failed : Errors.Ok, result.Flag);
                Assert.Equal(1, begins);
                Assert.Equal(failSecondWrite ? 0 : 1, commits);
                Assert.Equal(failSecondWrite ? 1 : 0, aborts);
                Assert.Null(transaction);
                Assert.All(units, unit => Assert.Equal(failSecondWrite, unit.IsDirty));
                if (failSecondWrite) Assert.All(units, unit => Assert.Equal(0, unit.Units[0].Id));
                using var verifier = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
                verifier.Open();
                using var query = verifier.CreateCommand();
                query.CommandText = "SELECT (SELECT COUNT(*) FROM A) + (SELECT COUNT(*) FROM B)";
                Assert.Equal(failSecondWrite ? 0L : 2L, (long)query.ExecuteScalar()!);
            }
            finally
            {
                manager.Dispose();
                foreach (var wrapper in wrappers) wrapper.Dispose();
                foreach (var unit in units) unit.Dispose();
                transaction?.Dispose();
            }
        }
        finally { File.Delete(path); }
    }
}
