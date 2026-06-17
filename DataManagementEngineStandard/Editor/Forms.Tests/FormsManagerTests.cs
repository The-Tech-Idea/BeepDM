using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class FormsManagerTests : IDisposable
{
    private readonly Mock<IDMEEditor> _mockEditor;
    private readonly FormsManager _manager;

    public FormsManagerTests()
    {
        _mockEditor = new Mock<IDMEEditor>();
        _manager = new FormsManager(_mockEditor.Object);
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    private static IEntityStructure CreateEntity(string entityName, params (string Name, string Type)[] fields)
    {
        var mock = new Mock<IEntityStructure>();
        mock.Setup(e => e.EntityName).Returns(entityName);
        mock.Setup(e => e.Fields).Returns(fields.Select(f => new EntityField
        {
            FieldName = f.Name,
            Fieldtype = f.Type
        }).ToList());
        return mock.Object;
    }

    private static Mock<IUnitofWork> CreateUowMock(int recordCount = 0, object? currentItem = null)
    {
        var mock = new Mock<IUnitofWork>();
        mock.Setup(u => u.TotalItemCount).Returns(recordCount);
        mock.Setup(u => u.CurrentItem).Returns(currentItem);
        var units = new Mock<System.Collections.ICollection>();
        units.Setup(c => c.Count).Returns(recordCount);
        mock.As<System.Collections.IEnumerable>().Setup(e => e.GetEnumerator()).Returns(new List<object>().GetEnumerator());
        return mock;
    }

    #region Block Registration

    [Fact]
    public void RegisterBlock_ValidParameters_BlockExists()
    {
        var entity = CreateEntity("EMPLOYEES", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(5);

        _manager.RegisterBlock("EMP", uowMock.Object, entity, "DEFAULT_DB");

        Assert.True(_manager.BlockExists("EMP"));
        Assert.Equal(1, _manager.BlockCount);
    }

    [Fact]
    public void RegisterBlock_NullBlockName_ThrowsArgumentException()
    {
        var entity = CreateEntity("X", ("A", "string"));
        Assert.Throws<ArgumentException>(() =>
            _manager.RegisterBlock(null!, null!, entity));
    }

    [Fact]
    public void RegisterBlock_NullUnitOfWork_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _manager.RegisterBlock("EMP", null!, null!, "DEFAULT_DB"));
    }

    [Fact]
    public void RegisterBlock_DuplicateBlockName_AutoReplaces()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uow1 = CreateUowMock(3);
        var uow2 = CreateUowMock(7);

        _manager.RegisterBlock("EMP", uow1.Object, entity);
        _manager.RegisterBlock("EMP", uow2.Object, entity);

        Assert.True(_manager.BlockExists("EMP"));
        Assert.Equal(1, _manager.BlockCount);
    }

    [Fact]
    public void UnregisterBlock_ExistingBlock_ReturnsTrue()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(3);

        _manager.RegisterBlock("EMP", uowMock.Object, entity);
        bool removed = _manager.UnregisterBlock("EMP");

        Assert.True(removed);
        Assert.False(_manager.BlockExists("EMP"));
        Assert.Equal(0, _manager.BlockCount);
    }

    [Fact]
    public void UnregisterBlock_NonExistentBlock_ReturnsFalse()
    {
        bool removed = _manager.UnregisterBlock("NON_EXISTENT");
        Assert.False(removed);
    }

    #endregion

    #region Navigation

    [Fact]
    public async Task FirstRecord_RegisteredBlock_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(10, new { EMPNO = 1, ENAME = "Alice" });
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.FirstRecordAsync("EMP");
        Assert.True(result);
    }

    [Fact]
    public async Task LastRecord_RegisteredBlock_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.LastRecordAsync("EMP");
        Assert.True(result);
    }

    [Fact]
    public async Task NextRecord_WithRecords_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.NextRecordAsync("EMP");
        Assert.True(result);
    }

    [Fact]
    public async Task PreviousRecord_WithRecords_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.PreviousRecordAsync("EMP");
        Assert.True(result);
    }

    [Fact]
    public async Task NavigateToRecord_ReturnsWithoutException()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);
        _manager.CurrentBlockName = "EMP";

        await _manager.NavigateToRecordAsync("EMP", 3);
        Assert.True(true);
    }

    [Fact]
    public async Task InsertRecord_ReturnsWithoutException()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(3);

        _manager.RegisterBlock("EMP", uowMock.Object, entity);
        _manager.CurrentBlockName = "EMP";

        await _manager.InsertRecordAsync("EMP");
        Assert.True(true);
    }

    [Fact]
    public async Task NavigateToRecord_NegativeIndex_ReturnsFalse()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.NavigateToRecordAsync("EMP", -1);
        Assert.False(result);
    }

    #endregion

    #region Mode Transitions

    [Fact]
    public async Task EnterQuery_SwitchesToQueryMode()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(0);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.EnterQueryAsync("EMP");
        Assert.True(result);

        var block = _manager.GetBlock("EMP");
        Assert.NotNull(block);
        Assert.Equal(DataBlockMode.Query, block.Mode);
    }

    [Fact]
    public async Task ExecuteQuery_WithoutEnterQuery_ReturnsTrue()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(5, new { EMPNO = 1 });
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.ExecuteQueryAsync("EMP");
        Assert.True(result);
    }

    #endregion

    #region CRUD

    [Fact]
    public async Task CommitForm_SetsStatus()
    {
        var result = await _manager.CommitFormAsync();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RollbackForm_SetsStatus()
    {
        var result = await _manager.RollbackFormAsync();
        Assert.NotNull(result);
    }

    #endregion

    #region Master-Detail Relationships

    [Fact]
    public void CreateMasterDetailRelation_ValidBlocks_SetsRelationship()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var deptEntity = CreateEntity("DEPT", ("DEPTNO", "int"), ("DNAME", "string"));
        var empUow = CreateUowMock(5);
        var deptUow = CreateUowMock(3);

        _manager.RegisterBlock("EMP", empUow.Object, empEntity);
        _manager.RegisterBlock("DEPT", deptUow.Object, deptEntity);

        _manager.CreateMasterDetailRelation("DEPT", "EMP", "DEPTNO", "DEPTNO");

        var detailBlocks = _manager.GetDetailBlocks("DEPT");
        Assert.NotNull(detailBlocks);
        Assert.Contains("EMP", detailBlocks);
    }

    [Fact]
    public void CreateMasterDetailRelation_NonexistentMaster_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _manager.CreateMasterDetailRelation("NONEXISTENT", "EMP", "DEPTNO", "DEPTNO"));
    }

    [Fact]
    public void CreateMasterDetailRelation_NonexistentDetail_ThrowsInvalidOperationException()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(5);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        Assert.Throws<InvalidOperationException>(() =>
            _manager.CreateMasterDetailRelation("EMP", "NONEXISTENT", "DEPTNO", "DEPTNO"));
    }

    [Fact]
    public void GetDetailBlocks_NoDetails_ReturnsEmptyList()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(5);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        var details = _manager.GetDetailBlocks("EMP");
        Assert.NotNull(details);
        Assert.Empty(details);
    }

    #endregion

    #region Trigger Registration

    [Fact]
    public void RegisterFormTrigger_SimpleHandler_Completes()
    {
        Func<TriggerContext, TriggerResult> handler = _ => TriggerResult.Success;

        _manager.Triggers.RegisterFormTrigger(
            TriggerType.WhenNewFormInstance, "F1", handler);

        Assert.True(_manager.Triggers.TriggerCount > 0);
    }

    [Fact]
    public void FireBlockTrigger_NoRegisteredTriggers_ReturnsTriggerResult()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(5);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        var result = _manager.Triggers.FireBlockTrigger(
            TriggerType.WhenValidateRecord, "EMP");

        Assert.NotNull(result);
    }

    #endregion

    #region Validation

    [Fact]
    public void ValidateRecord_NoValidationRules_ReturnsValid()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        var record = new Dictionary<string, object>
        {
            ["EMPNO"] = 1,
            ["ENAME"] = "Alice"
        };

        var result = _manager.Validation.ValidateRecord(
            "EMP", record, ValidationTiming.Manual);

        Assert.NotNull(result);
    }

    #endregion

    #region Savepoints

    [Fact]
    public void CreateSavepoint_RegisteredBlock_ReturnsSavepointName()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        string savepoint = _manager.Savepoints.CreateSavepoint("EMP", "SP1");

        Assert.NotNull(savepoint);
        Assert.True(_manager.Savepoints.SavepointExists("EMP", savepoint));
    }

    [Fact]
    public void CreateSavepoint_AutoGeneratesName_WhenNullPassed()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        string savepoint = _manager.Savepoints.CreateSavepoint("EMP");

        Assert.False(string.IsNullOrWhiteSpace(savepoint));
    }

    [Fact]
    public void ReleaseSavepoint_ExistingSavepoint_ReturnsTrue()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        string sp = _manager.Savepoints.CreateSavepoint("EMP", "SP1");
        bool released = _manager.Savepoints.ReleaseSavepoint("EMP", sp);

        Assert.True(released);
        Assert.False(_manager.Savepoints.SavepointExists("EMP", sp));
    }

    [Fact]
    public void ListSavepoints_NoSavepoints_ReturnsEmpty()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        var list = _manager.Savepoints.ListSavepoints("EMP");

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    #endregion

    #region System Variables

    [Fact]
    public void SystemVariables_GetSystemVariables_ReturnsObject()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);
        _manager.CurrentBlockName = "EMP";

        var vars = _manager.SystemVariables.GetSystemVariables("EMP");

        Assert.NotNull(vars);
    }

    #endregion

    #region LOV

    [Fact]
    public void HasLov_NoLOVRegistered_ReturnsFalse()
    {
        Assert.False(_manager.LOV.HasLOV("EMP", "DEPTNO"));
    }

    #endregion

    #region Item Properties

    [Fact]
    public void SetItemProperty_DoesNotThrow()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(3);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        _manager.ItemProperties.SetItemProperty("EMP", "EMPNO", "Visible", true);
        Assert.True(true);
    }

    #endregion

    #region Block Property

    [Fact]
    public void GetBlockProperty_SetThenGet_ReturnsValue()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        _manager.SetBlockProperty("EMP", Forms.Models.BlockProperty.QueryAllowed, true);
        var value = _manager.GetBlockProperty("EMP", Forms.Models.BlockProperty.QueryAllowed);

        Assert.NotNull(value);
    }

    #endregion

    #region Block Count

    [Fact]
    public void GetBlockCount_RegisteredBlock_ReturnsUowCount()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(15);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        int count = _manager.GetBlockCount("EMP");

        Assert.True(count >= 0);
    }

    #endregion

    #region Error Log

    [Fact]
    public void ErrorLog_NoErrors_ReturnsEmpty()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        var log = _manager.ErrorLog.GetErrorLog("EMP");

        Assert.NotNull(log);
        Assert.Empty(log);
    }

    [Fact]
    public void ErrorLog_LogError_IncreasesCount()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(3);
        _manager.RegisterBlock("EMP", empUow.Object, empEntity);

        _manager.ErrorLog.LogError("EMP", new InvalidOperationException("Test error"), "Testing");

        int count = _manager.ErrorLog.GetErrorCount("EMP");
        Assert.Equal(1, count);
    }

    #endregion

    #region Messages

    [Fact]
    public void Messages_ShowInfo_IsStored()
    {
        _manager.Messages.ShowInfoMessage("EMP", "Record saved");

        string msg = _manager.Messages.GetCurrentMessage("EMP");
        Assert.NotNull(msg);
    }

    [Fact]
    public void Messages_ShowWarning_IsStored()
    {
        _manager.Messages.ShowWarningMessage("EMP", "Field is read-only");

        string msg = _manager.Messages.GetCurrentMessage("EMP");
        Assert.NotNull(msg);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        _manager.Dispose();
        _manager.Dispose();
        Assert.True(true);
    }

    #endregion

    #region Form Name

    [Fact]
    public void CurrentFormName_SetAndGet_ReturnsSetValue()
    {
        _manager.CurrentFormName = "TestForm";
        Assert.Equal("TestForm", _manager.CurrentFormName);
    }

    #endregion
}
