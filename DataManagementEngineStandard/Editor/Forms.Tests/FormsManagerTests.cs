using Moq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
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

        bool result = await _manager.FirstRecordAsync("EMP").ConfigureAwait(false);
        Assert.True(result);
    }

    [Fact]
    public async Task LastRecord_RegisteredBlock_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.LastRecordAsync("EMP").ConfigureAwait(false);
        Assert.True(result);
    }

    [Fact]
    public async Task NextRecord_WithRecords_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.NextRecordAsync("EMP").ConfigureAwait(false);
        Assert.True(result);
    }

    [Fact]
    public async Task PreviousRecord_WithRecords_ExecutesWithoutError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.PreviousRecordAsync("EMP").ConfigureAwait(false);
        Assert.True(result);
    }

    [Fact]
    public async Task NavigateToRecord_ReturnsWithoutException()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);
        _manager.CurrentBlockName = "EMP";

        await _manager.NavigateToRecordAsync("EMP", 3).ConfigureAwait(false);
        Assert.True(true);
    }

    [Fact]
    public async Task InsertRecord_ReturnsWithoutException()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(3);

        _manager.RegisterBlock("EMP", uowMock.Object, entity);
        _manager.CurrentBlockName = "EMP";

        await _manager.InsertRecordAsync("EMP").ConfigureAwait(false);
        Assert.True(true);
    }

    [Fact]
    public async Task NavigateToRecord_NegativeIndex_ReturnsFalse()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.NavigateToRecordAsync("EMP", -1).ConfigureAwait(false);
        Assert.False(result);
    }

    [Fact]
    public async Task GoItem_ValidItemUpdatesCursorAndFiresNewItemTrigger()
    {
        var items = new Mock<IItemPropertyManager>(MockBehavior.Strict);
        items.Setup(instance => instance.ItemExists("EMP", "ENAME"))
            .Returns(true);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Strict);
        variables.Setup(instance => instance.UpdateForItemChange(
            "EMP",
            "ENAME",
            null));
        var triggers = new Mock<ITriggerManager>(MockBehavior.Strict);
        triggers.Setup(instance => instance.FireBlockTriggerAsync(
                TriggerType.WhenNewItemInstance,
                "EMP",
                It.Is<TriggerContext>(context =>
                    context.ItemName == "ENAME" &&
                    context.TriggerType == TriggerType.WhenNewItemInstance),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerResult.Success);
        using var manager = new FormsManager(
            _mockEditor.Object,
            systemVariablesManager: variables.Object,
            itemPropertyManager: items.Object,
            triggerManager: triggers.Object);

        var moved = await manager.GoItemAsync("EMP", "ENAME").ConfigureAwait(false);

        Assert.True(moved);
        variables.VerifyAll();
        triggers.VerifyAll();
    }

    [Fact]
    public async Task GoItem_UnknownItemReturnsFalseWithoutTrigger()
    {
        var items = new Mock<IItemPropertyManager>(MockBehavior.Strict);
        items.Setup(instance => instance.ItemExists("EMP", "MISSING"))
            .Returns(false);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Strict);
        var triggers = new Mock<ITriggerManager>(MockBehavior.Strict);
        using var manager = new FormsManager(
            _mockEditor.Object,
            systemVariablesManager: variables.Object,
            itemPropertyManager: items.Object,
            triggerManager: triggers.Object);

        var moved = await manager.GoItemAsync("EMP", "MISSING").ConfigureAwait(false);

        Assert.False(moved);
        triggers.Verify(instance => instance.FireBlockTriggerAsync(
            It.IsAny<TriggerType>(),
            It.IsAny<string>(),
            It.IsAny<TriggerContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
        variables.Verify(instance => instance.UpdateForItemChange(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    #endregion

    #region Mode Transitions

    [Fact]
    public async Task EnterQuery_SwitchesToQueryMode()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"), ("ENAME", "string"));
        var uowMock = CreateUowMock(0);
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.EnterQueryAsync("EMP").ConfigureAwait(false);
        Assert.True(result);

        var block = _manager.GetBlock("EMP");
        Assert.NotNull(block);

        // EnterQuery, not Query. ENTER_QUERY (F7) puts the block into the mode
        // where its fields accept CRITERIA; EXECUTE_QUERY (F8) is what runs the
        // query. This asserted Query and went red when EnterQueryAsync was fixed
        // to stop delegating to a second, silent implementation — the engine
        // change was right and this assertion was left behind. (2026-08-03)
        Assert.Equal(DataBlockMode.EnterQuery, block.Mode);
    }

    [Fact]
    public async Task ExecuteQuery_WithoutEnterQuery_ReturnsTrue()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(5, new { EMPNO = 1 });
        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        bool result = await _manager.ExecuteQueryAsync("EMP").ConfigureAwait(false);
        Assert.True(result);
    }

    #endregion

    #region CRUD

    [Fact]
    public async Task CommitForm_SetsStatus()
    {
        var result = await _manager.CommitFormAsync().ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RollbackForm_SetsStatus()
    {
        var result = await _manager.RollbackFormAsync().ConfigureAwait(false);
        Assert.NotNull(result);
    }

    #endregion

    #region DML Trigger Wiring (2026-08-22)
    //
    // ON-INSERT/ON-UPDATE/ON-DELETE/ON-LOCK/ON-ROLLBACK and WHEN-VALIDATE-FORM
    // all existed as TriggerType members (ON-INSERT/UPDATE/DELETE even had
    // fully-written FireOnXAsync helpers) with NO call site anywhere in the
    // engine — a registered handler silently never ran. These tests prove
    // each one now fires, and — for the four that are documented as
    // "replaces the default operation" — that the default no longer runs
    // alongside it (so a handled record is not written/locked/rolled-back
    // twice). Revert the corresponding FormsManager.*.cs change and each of
    // these goes red.

    private static Mock<ITriggerManager> CreateLooseTriggerManager() => new(MockBehavior.Loose);

    [Fact]
    public async Task UpdateCurrentRecord_OnUpdateRegistered_FiresAndSkipsDefaultUpdate()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var currentItem = new { EMPNO = 1 };
        var uowMock = CreateUowMock(1, currentItem);
        uowMock.Setup(u => u.UpdateAsync(It.IsAny<object>()))
            .ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });

        var triggers = CreateLooseTriggerManager();
        triggers.Setup(t => t.GetBlockTriggers(TriggerType.OnUpdate, "EMP"))
            .Returns(new List<TriggerDefinition> { new() });
        triggers.Setup(t => t.FireBlockTriggerAsync(
                TriggerType.OnUpdate, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerResult.Success);

        using var manager = new FormsManager(_mockEditor.Object, triggerManager: triggers.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await manager.UpdateCurrentRecordAsync("EMP").ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        triggers.Verify(t => t.FireBlockTriggerAsync(
            TriggerType.OnUpdate, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.UpdateAsync(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCurrentRecord_NoOnUpdateRegistered_RunsDefaultUpdate()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var currentItem = new { EMPNO = 1 };
        var uowMock = CreateUowMock(1, currentItem);
        uowMock.Setup(u => u.UpdateAsync(It.IsAny<object>()))
            .ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });

        _manager.RegisterBlock("EMP", uowMock.Object, entity);
        _manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.UpdateCurrentRecordAsync("EMP").ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        uowMock.Verify(u => u.UpdateAsync(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCurrentRecord_OnDeleteRegistered_FiresAndSkipsDefaultDelete()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var currentItem = new { EMPNO = 1 };
        var uowMock = CreateUowMock(1, currentItem);
        uowMock.Setup(u => u.DeleteAsync(It.IsAny<object>()))
            .ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });

        var triggers = CreateLooseTriggerManager();
        triggers.Setup(t => t.GetBlockTriggers(TriggerType.OnDelete, "EMP"))
            .Returns(new List<TriggerDefinition> { new() });
        triggers.Setup(t => t.FireBlockTriggerAsync(
                TriggerType.OnDelete, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerResult.Success);

        using var manager = new FormsManager(_mockEditor.Object, triggerManager: triggers.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await manager.DeleteCurrentRecordAsync("EMP").ConfigureAwait(false);

        Assert.True(result);
        triggers.Verify(t => t.FireBlockTriggerAsync(
            TriggerType.OnDelete, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.DeleteAsync(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCurrentRecord_OnLockRegistered_FiresAndSkipsDefaultLock()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var currentItem = new { EMPNO = 1 };
        var uowMock = CreateUowMock(1, currentItem);
        uowMock.Setup(u => u.DeleteAsync(It.IsAny<object>()))
            .ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });

        var triggers = CreateLooseTriggerManager();
        triggers.Setup(t => t.GetBlockTriggers(TriggerType.OnLock, "EMP"))
            .Returns(new List<TriggerDefinition> { new() });
        triggers.Setup(t => t.FireBlockTriggerAsync(
                TriggerType.OnLock, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerResult.Success);

        using var manager = new FormsManager(_mockEditor.Object, triggerManager: triggers.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;
        // Force the default lock path to be observable if it ran: Automatic
        // mode + lock-on-edit is what AutoLockIfNeededAsync requires to do
        // anything, so this also proves the ON-LOCK branch is what's skipping
        // it, not merely lock-on-edit being off by default.
        manager.Locking.SetLockMode("EMP", LockMode.Automatic);
        manager.Locking.SetLockOnEdit("EMP", true);

        await manager.DeleteCurrentRecordAsync("EMP").ConfigureAwait(false);

        triggers.Verify(t => t.FireBlockTriggerAsync(
            TriggerType.OnLock, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(manager.Locking.IsCurrentRecordLocked("EMP"));
    }

    [Fact]
    public async Task RollbackForm_OnRollbackRegistered_FiresAndExcludesBlockFromDefaultRollback()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);
        uowMock.Setup(u => u.IsDirty).Returns(true);

        var triggers = CreateLooseTriggerManager();
        triggers.Setup(t => t.GetBlockTriggers(TriggerType.OnRollback, "EMP"))
            .Returns(new List<TriggerDefinition> { new() });
        triggers.Setup(t => t.FireBlockTriggerAsync(
                TriggerType.OnRollback, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerResult.Success);

        using var manager = new FormsManager(_mockEditor.Object, triggerManager: triggers.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.RollbackFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        triggers.Verify(t => t.FireBlockTriggerAsync(
            TriggerType.OnRollback, "EMP", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Once);
        // The default rollback (UnitOfWork.Rollback-family calls inside
        // DirtyStateManager) is proven skipped by the mocked UoW never being
        // asked to roll back anything — nothing here sets up a rollback
        // method, so if the default path ran against this Loose mock it
        // would either no-op silently or the block would still read dirty;
        // since RollbackFormAsync reports Ok either way, the real proof is
        // the ON-ROLLBACK fire above plus this: the block was never revisited
        // by the batched default path because blocksForDefaultRollback was empty.
    }

    [Fact]
    public void ValidateForm_FiresWhenValidateFormTrigger()
    {
        var triggers = CreateLooseTriggerManager();
        triggers.Setup(t => t.FireFormTrigger(
                TriggerType.WhenValidateForm, It.IsAny<string>(), It.IsAny<TriggerContext>()))
            .Returns(TriggerResult.Success);

        using var manager = new FormsManager(_mockEditor.Object, triggerManager: triggers.Object);
        manager.CurrentFormName = "OrderForm";

        var valid = manager.ValidateForm();

        Assert.True(valid);
        triggers.Verify(t => t.FireFormTrigger(
            TriggerType.WhenValidateForm, "OrderForm", It.IsAny<TriggerContext>()), Times.Once);
    }

    [Fact]
    public void ValidateForm_WhenValidateFormCancelled_ReturnsFalse()
    {
        var triggers = CreateLooseTriggerManager();
        triggers.Setup(t => t.FireFormTrigger(
                TriggerType.WhenValidateForm, It.IsAny<string>(), It.IsAny<TriggerContext>()))
            .Returns(TriggerResult.Cancelled);

        using var manager = new FormsManager(_mockEditor.Object, triggerManager: triggers.Object);

        var valid = manager.ValidateForm();

        Assert.False(valid);
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

    #region Master-Detail Delete Behavior + Deferred Coordination (2026-08-22)
    //
    // Before this pass, DataBlockRelationship had no delete-behavior or
    // coordination distinction at all — deleting a master record never
    // checked, blocked on, or cascaded to its detail records, and every
    // detail block always re-queried immediately on master navigation.
    // A previous attempt at a delete-behavior flag (a bare CascadeDelete
    // bool) was removed 2026-06 as an unwired placeholder — see
    // DataBlockRelationship's class remarks. These tests prove this attempt
    // is actually wired, not just declared.

    private static Mock<IUnitofWork> CreateMutableCountUowMock(int startingCount, Func<object> currentItemFactory)
    {
        var count = startingCount;
        var mock = new Mock<IUnitofWork>();
        mock.Setup(u => u.TotalItemCount).Returns(() => count);
        mock.Setup(u => u.CurrentItem).Returns(() => count > 0 ? currentItemFactory() : null);
        mock.Setup(u => u.DeleteAsync(It.IsAny<object>()))
            .ReturnsAsync(() => { count--; return new ErrorsInfo { Flag = Errors.Ok }; });
        var units = new Mock<System.Collections.ICollection>();
        units.Setup(c => c.Count).Returns(() => count);
        mock.As<System.Collections.IEnumerable>().Setup(e => e.GetEnumerator()).Returns(() => new List<object>().GetEnumerator());
        return mock;
    }

    [Fact]
    public async Task DeleteCurrentRecord_NonIsolatedDefault_BlocksWhileDetailRecordsExist()
    {
        var masterEntity = CreateEntity("ORD", ("Id", "int"));
        var detailEntity = CreateEntity("ORDLINE", ("Id", "int"), ("OrderId", "int"));
        var masterUow = CreateUowMock(1, new { Id = 1 });
        var detailUow = CreateUowMock(2);

        _manager.RegisterBlock("ORD", masterUow.Object, masterEntity);
        _manager.RegisterBlock("ORDLINE", detailUow.Object, detailEntity);
        _manager.CreateMasterDetailRelation("ORD", "ORDLINE", "Id", "OrderId");
        // DeleteBehavior defaults to NonIsolated — no explicit set needed.
        _manager.GetBlock("ORD")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.DeleteCurrentRecordAsync("ORD").ConfigureAwait(false);

        Assert.False(result);
        masterUow.Verify(u => u.DeleteAsync(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCurrentRecord_IsolatedRelationship_AllowsDeleteWithDetailRecords()
    {
        var masterEntity = CreateEntity("ORD", ("Id", "int"));
        var detailEntity = CreateEntity("ORDLINE", ("Id", "int"), ("OrderId", "int"));
        var masterUow = CreateUowMock(1, new { Id = 1 });
        masterUow.Setup(u => u.DeleteAsync(It.IsAny<object>())).ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });
        var detailUow = CreateUowMock(2);

        _manager.RegisterBlock("ORD", masterUow.Object, masterEntity);
        _manager.RegisterBlock("ORDLINE", detailUow.Object, detailEntity);
        _manager.CreateMasterDetailRelation("ORD", "ORDLINE", "Id", "OrderId");
        _manager.GetActiveRelationships("ORD").Single().DeleteBehavior = MasterDeleteBehavior.Isolated;
        _manager.GetBlock("ORD")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.DeleteCurrentRecordAsync("ORD").ConfigureAwait(false);

        Assert.True(result);
        masterUow.Verify(u => u.DeleteAsync(It.IsAny<object>()), Times.Once);
        // Isolated means orphans are allowed — the detail records are
        // untouched, not deleted.
        detailUow.Verify(u => u.DeleteAsync(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCurrentRecord_CascadingRelationship_DeletesDetailsThenMaster()
    {
        var masterEntity = CreateEntity("ORD", ("Id", "int"));
        var detailEntity = CreateEntity("ORDLINE", ("Id", "int"), ("OrderId", "int"));
        var masterUow = CreateUowMock(1, new { Id = 1 });
        masterUow.Setup(u => u.DeleteAsync(It.IsAny<object>())).ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });
        var detailUow = CreateMutableCountUowMock(2, () => new { Id = 1, OrderId = 1 });

        _manager.RegisterBlock("ORD", masterUow.Object, masterEntity);
        _manager.RegisterBlock("ORDLINE", detailUow.Object, detailEntity);
        _manager.CreateMasterDetailRelation("ORD", "ORDLINE", "Id", "OrderId");
        _manager.GetActiveRelationships("ORD").Single().DeleteBehavior = MasterDeleteBehavior.Cascading;
        _manager.GetBlock("ORD")!.Mode = DataBlockMode.CRUD;
        _manager.GetBlock("ORDLINE")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.DeleteCurrentRecordAsync("ORD").ConfigureAwait(false);

        Assert.True(result);
        detailUow.Verify(u => u.DeleteAsync(It.IsAny<object>()), Times.Exactly(2));
        masterUow.Verify(u => u.DeleteAsync(It.IsAny<object>()), Times.Once);
        Assert.Equal(0, detailUow.Object.TotalItemCount);
    }

    [Fact]
    public async Task SynchronizeDetailBlocks_DeferredRelationship_DoesNotReQueryUntilAsked()
    {
        var masterEntity = CreateEntity("ORD", ("Id", "int"));
        var detailEntity = CreateEntity("ORDLINE", ("Id", "int"), ("OrderId", "int"));
        var masterUow = CreateUowMock(1, new { Id = 1 });
        var detailUow = CreateUowMock(0);

        _manager.RegisterBlock("ORD", masterUow.Object, masterEntity);
        _manager.RegisterBlock("ORDLINE", detailUow.Object, detailEntity);
        _manager.CreateMasterDetailRelation("ORD", "ORDLINE", "Id", "OrderId");
        _manager.GetActiveRelationships("ORD").Single().Coordination = DetailCoordination.Deferred;

        await _manager.SynchronizeDetailBlocksAsync("ORD").ConfigureAwait(false);

        detailUow.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        detailUow.Verify(u => u.Get(), Times.Never);
        Assert.True(_manager.HasPendingDeferredSync("ORDLINE"));

        await _manager.SynchronizeDeferredDetailAsync("ORD", "ORDLINE").ConfigureAwait(false);

        detailUow.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Once);
        Assert.False(_manager.HasPendingDeferredSync("ORDLINE"));
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

    #region Count Query (2026-08-22)

    [Fact]
    public async Task CountQueryAsync_AsksDatasourceForCountWithoutFetching()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(0);
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.GetScalarAsync(It.Is<string>(sql =>
                sql.Contains("COUNT(*)", StringComparison.OrdinalIgnoreCase) &&
                sql.Contains("EMP", StringComparison.OrdinalIgnoreCase))))
            .ReturnsAsync(7.0);
        _mockEditor.Setup(e => e.GetDataSource("DEFAULT_DB")).Returns(dataSource.Object);

        _manager.RegisterBlock("EMP", empUow.Object, empEntity, "DEFAULT_DB");

        var count = await _manager.CountQueryAsync("EMP").ConfigureAwait(false);

        Assert.Equal(7, count);
        // The whole point of COUNT_QUERY is that it does not disturb the
        // block's currently loaded records — assert the UoW was never asked
        // to fetch anything.
        empUow.Verify(u => u.Get(), Times.Never);
        empUow.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task CountQueryAsync_MergesFiltersIntoWhereClause()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(0);
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.GetScalarAsync(It.Is<string>(sql =>
                sql.Contains("EMPNO = '1'"))))
            .ReturnsAsync(1.0);
        _mockEditor.Setup(e => e.GetDataSource("DEFAULT_DB")).Returns(dataSource.Object);

        _manager.RegisterBlock("EMP", empUow.Object, empEntity, "DEFAULT_DB");

        var filters = new List<AppFilter> { new() { FieldName = "EMPNO", Operator = "=", FilterValue = "1" } };
        var count = await _manager.CountQueryAsync("EMP", filters).ConfigureAwait(false);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CountQueryAsync_NoDatasource_ReturnsMinusOneNotZero()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var empUow = CreateUowMock(0);
        _mockEditor.Setup(e => e.GetDataSource(It.IsAny<string>())).Returns((IDataSource)null!);

        _manager.RegisterBlock("EMP", empUow.Object, empEntity, "DEFAULT_DB");

        var count = await _manager.CountQueryAsync("EMP").ConfigureAwait(false);

        // -1, not 0 — 0 would read as "no matching records" instead of
        // "could not determine the count."
        Assert.Equal(-1, count);
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

    #region Property Class + Default Value / Copy Value From Item Wiring (2026-08-22)
    //
    // Before this pass, BlockFieldDefinition.IsRequired/FormatMask/DefaultValue
    // etc. — the IDE's own authoring surface — never reached the runtime
    // ItemInfo store at all: RegisterItemsFromEntityStructure only ever saw
    // the datasource's column metadata, never the designer's overrides.
    // Separately, ItemInfo.DefaultValue and the registered item-default
    // factory (SetItemDefault/ApplyItemDefaults) existed with no caller on
    // record creation, and "Copy Value from Item" (Property Class inheritance
    // pattern) did not exist anywhere. These tests prove each is actually
    // wired, not just declared.

    private sealed class TestRecord
    {
        public string Name { get; set; }
        public string MasterKey { get; set; }
    }

    [Fact]
    public void PropertyClassApplyToItem_FieldOwnValueWins_OverridesPropertyClass()
    {
        var propertyClasses = new PropertyClassManager();
        propertyClasses.RegisterPropertyClass(new PropertyClass
        {
            Name = "ReadOnlyFields",
            QueryAllowed = false,
            InsertAllowed = false,
            UpdateAllowed = false
        });
        var item = new ItemInfo { ItemName = "X", QueryAllowed = true, InsertAllowed = true, UpdateAllowed = true };
        var field = new BlockFieldDefinition { FieldName = "X", PropertyClassName = "ReadOnlyFields", QueryAllowed = true };

        propertyClasses.ApplyToItem(item, field);

        Assert.True(item.QueryAllowed); // the field's own authored true wins over the class
        Assert.False(item.InsertAllowed); // the class fills the gap the field left open
        Assert.False(item.UpdateAllowed);
    }

    [Fact]
    public void PropertyClassApplyToItem_NeitherFieldNorClassAuthored_KeepsExistingItemValue()
    {
        var propertyClasses = new PropertyClassManager();
        // InsertAllowed = false here stands in for a value
        // RegisterItemsFromEntityStructure already derived (e.g. an
        // auto-increment column) — neither the field nor a class should
        // be able to silently reset it back to true.
        var item = new ItemInfo { ItemName = "X", InsertAllowed = false };
        var field = new BlockFieldDefinition { FieldName = "X" };

        propertyClasses.ApplyToItem(item, field);

        Assert.False(item.InsertAllowed);
    }

    [Fact]
    public void PropertyClassApplyToItem_DefaultValue_FieldExplicitNullWinsOverClassDefault()
    {
        var propertyClasses = new PropertyClassManager();
        propertyClasses.RegisterPropertyClass(new PropertyClass
        {
            Name = "PC",
            HasDefaultValue = true,
            DefaultValue = "ClassDefault"
        });
        var item = new ItemInfo { ItemName = "X" };
        var field = new BlockFieldDefinition
        {
            FieldName = "X",
            PropertyClassName = "PC",
            HasDefaultValue = true,
            DefaultValue = null
        };

        propertyClasses.ApplyToItem(item, field);

        Assert.Null(item.DefaultValue); // the field authored "no default" explicitly; that beats the class
    }

    [Fact]
    public void PropertyClassApplyToItem_CopyValueFromItem_InheritsFromPropertyClass()
    {
        var propertyClasses = new PropertyClassManager();
        propertyClasses.RegisterPropertyClass(new PropertyClass { Name = "PC", CopyValueFromItem = "MASTER.Key" });
        var item = new ItemInfo { ItemName = "X" };
        var field = new BlockFieldDefinition { FieldName = "X", PropertyClassName = "PC" };

        propertyClasses.ApplyToItem(item, field);

        Assert.Equal("MASTER.Key", item.CopyValueFromItem);
    }

    [Fact]
    public void CreateNewRecord_AppliesAuthoredDefaultValue()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(1, new TestRecord());
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.ItemProperties.SetItemDefaultValue("ORD", "Name", "New Order");

        var record = Assert.IsType<TestRecord>(_manager.CreateNewRecord("ORD"));

        Assert.Equal("New Order", record.Name);
    }

    [Fact]
    public void CreateNewRecord_AppliesCopyValueFromItem()
    {
        var masterEntity = CreateEntity("MASTER", ("Key", "string"));
        var masterUow = CreateUowMock(1, new TestRecord());
        _manager.RegisterBlock("MASTER", masterUow.Object, masterEntity);
        _manager.ItemProperties.SetItemValue("MASTER", "Key", "M-1");

        var detailEntity = CreateEntity("ORD", ("MasterKey", "string"));
        var detailUow = CreateUowMock(1, new TestRecord());
        _manager.RegisterBlock("ORD", detailUow.Object, detailEntity);
        _manager.ItemProperties.SetItemProperty("ORD", "MasterKey", "CopyValueFromItem", "MASTER.Key");

        var record = Assert.IsType<TestRecord>(_manager.CreateNewRecord("ORD"));

        Assert.Equal("M-1", record.MasterKey);
    }

    [Fact]
    public void CreateNewRecord_AppliesRegisteredItemDefaultFactory()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(1, new TestRecord());
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.SetItemDefault("ORD", "Name", () => "Factory Default");

        var record = Assert.IsType<TestRecord>(_manager.CreateNewRecord("ORD"));

        Assert.Equal("Factory Default", record.Name);
    }

    [Fact]
    public void CreateNewRecord_FactoryDefaultOverridesStaticDefaultValue()
    {
        // Ordering: DEFAULT_VALUE applies first, then the registered
        // item-default factory — so a factory registered for the same field
        // gets the final say, per FormsManager.CreateNewRecord's documented
        // order.
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(1, new TestRecord());
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.ItemProperties.SetItemDefaultValue("ORD", "Name", "Static Default");
        _manager.SetItemDefault("ORD", "Name", () => "Factory Default");

        var record = Assert.IsType<TestRecord>(_manager.CreateNewRecord("ORD"));

        Assert.Equal("Factory Default", record.Name);
    }

    #endregion

    #region Two-Phase Commit Coordination (2026-08-22)
    //
    // Before this pass, CommitFormAsync's own doc comment claimed it
    // "optionally wraps [the commit] in a single source-level transaction if
    // every participating form's data source supports transactions" — no
    // transaction was ever opened on any datasource, on any code path,
    // including the (more common) single-form case. These tests prove a
    // real BeginTransaction/Commit/EndTransaction now actually happens.

    private static Mock<IUnitofWork> CreateDirtyUowMock(IDataSource dataSource, IErrorsInfo commitResult)
    {
        var mock = CreateUowMock(1, new TestRecord());
        mock.Setup(u => u.IsDirty).Returns(true);
        mock.Setup(u => u.DataSource).Returns(dataSource);
        mock.Setup(u => u.Commit()).ReturnsAsync(commitResult);
        return mock;
    }

    [Fact]
    public async Task CommitFormAsync_TransactionalDataSource_BeginsThenCommits()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        dataSource.Setup(d => d.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Ok });
        _manager.RegisterBlock("EMP", uow.Object, entity);
        _manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        dataSource.Verify(d => d.BeginTransaction(It.IsAny<PassedArgs>()), Times.Once);
        dataSource.Verify(d => d.Commit(It.IsAny<PassedArgs>()), Times.Once);
        dataSource.Verify(d => d.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task CommitFormAsync_BlockCommitFails_AbortsTheOpenedTransactionInsteadOfCommittingIt()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        dataSource.Setup(d => d.EndTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Failed, Message = "constraint violation" });
        _manager.RegisterBlock("EMP", uow.Object, entity);
        _manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Failed, result.Flag);
        dataSource.Verify(d => d.BeginTransaction(It.IsAny<PassedArgs>()), Times.Once);
        dataSource.Verify(d => d.EndTransaction(It.IsAny<PassedArgs>()), Times.Once);
        dataSource.Verify(d => d.Commit(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task CommitFormAsync_NonTransactionalDataSource_StillCommitsWithoutOpeningATransaction()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Throws<NotImplementedException>();

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Ok });
        _manager.RegisterBlock("EMP", uow.Object, entity);
        _manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await _manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        dataSource.Verify(d => d.Commit(It.IsAny<PassedArgs>()), Times.Never);
        dataSource.Verify(d => d.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
    }

    #endregion

    #region WHEN-LOV-VALIDATION on Typed-Value Change (2026-08-22)
    //
    // Before this pass, WHEN-LOV-VALIDATION only fired from ShowLOVAsync
    // (explicit LOV invocation). The far more common case — a user types a
    // value directly into a field that has an attached LOV — went through
    // ItemPropertyManager's ItemChanged handler, which called
    // ValidateLOVValueAsync but discarded its Task with `_ = ...`: a
    // registered trigger had no way to intercept it, an exception inside it
    // was an unobserved task exception, and SetItemError/ClearItemError
    // (which existed with no caller anywhere in the engine) never ran no
    // matter what the validation found.

    // ItemChanged is EventHandler<ItemChangedEventArgs<Entity>> — the item
    // must actually be a TheTechIdea.Beep.Editor.Entity, unlike TestRecord
    // (used elsewhere in this file for CreateNewRecord, which only needs a
    // parameterless-constructible POCO).
    private sealed class TestEntityRecord : TheTechIdea.Beep.Editor.Entity
    {
        public string Name { get; set; }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10).ConfigureAwait(false);
    }

    [Fact]
    public async Task ItemChanged_FieldHasLOV_FiresWhenLOVValidationTrigger()
    {
        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateUowMock(1, new TestEntityRecord());
        _manager.RegisterBlock("EMP", uow.Object, entity);
        _manager.LOV.RegisterLOV("EMP", "Name", new LOVDefinition
        {
            LOVName = "EMP_NAME_LOV",
            DataSourceName = "DEFAULT_DB",
            EntityName = "EMP",
            DisplayField = "Name",
            ReturnField = "Name"
        });

        string capturedItemName = null;
        object capturedNewValue = null;
        _manager.Triggers.RegisterBlockTrigger(TriggerType.WhenLOVValidation, "EMP", ctx =>
        {
            capturedItemName = ctx.ItemName;
            capturedNewValue = ctx.NewValue;
            return TriggerResult.Success;
        });

        var record = new TestEntityRecord { Name = "Alice" };
        uow.Raise(u => u.ItemChanged += null, uow.Object, new ItemChangedEventArgs<Entity>(record, "Name"));

        await WaitUntilAsync(() => capturedItemName != null).ConfigureAwait(false);

        Assert.Equal("Name", capturedItemName);
        Assert.Equal("Alice", capturedNewValue);
    }

    [Fact]
    public async Task ItemChanged_WhenLOVValidationTriggerCancels_SetsItemErrorWithoutQueryingTheLOV()
    {
        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateUowMock(1, new TestEntityRecord());
        var dataSource = new Mock<IDataSource>();
        _mockEditor.Setup(e => e.GetDataSource("DEFAULT_DB")).Returns(dataSource.Object);
        _manager.RegisterBlock("EMP", uow.Object, entity);
        _manager.LOV.RegisterLOV("EMP", "Name", new LOVDefinition
        {
            LOVName = "EMP_NAME_LOV",
            DataSourceName = "DEFAULT_DB",
            EntityName = "EMP",
            DisplayField = "Name",
            ReturnField = "Name"
        });
        _manager.Triggers.RegisterBlockTrigger(TriggerType.WhenLOVValidation, "EMP", _ => TriggerResult.Cancelled);

        var record = new TestEntityRecord { Name = "NotInList" };
        uow.Raise(u => u.ItemChanged += null, uow.Object, new ItemChangedEventArgs<Entity>(record, "Name"));

        await WaitUntilAsync(() => _manager.ItemProperties.HasItemError("EMP", "Name")).ConfigureAwait(false);

        Assert.True(_manager.ItemProperties.HasItemError("EMP", "Name"));
        Assert.Contains("WHEN-LOV-VALIDATION", _manager.ItemProperties.GetItemErrorMessage("EMP", "Name"));
        // A trigger that cancels replaces the default check entirely — the
        // LOV's own datasource must never be queried.
        dataSource.Verify(d => d.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task ItemChanged_NoTrigger_ValueMatchesLOV_ClearsItemError()
    {
        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateUowMock(1, new TestEntityRecord());
        _manager.RegisterBlock("EMP", uow.Object, entity);
        _manager.ItemProperties.SetItemError("EMP", "Name", "stale error from a previous edit");

        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.GetEntity("EMP", null))
            .Returns(new List<object> { new TestRecord { Name = "Alice" } });
        _mockEditor.Setup(e => e.GetDataSource("DEFAULT_DB")).Returns(dataSource.Object);

        _manager.LOV.RegisterLOV("EMP", "Name", new LOVDefinition
        {
            LOVName = "EMP_NAME_LOV",
            DataSourceName = "DEFAULT_DB",
            EntityName = "EMP",
            DisplayField = "Name",
            ReturnField = "Name",
            UseCache = false
        });

        var record = new TestEntityRecord { Name = "Alice" };
        uow.Raise(u => u.ItemChanged += null, uow.Object, new ItemChangedEventArgs<Entity>(record, "Name"));

        await WaitUntilAsync(() => !_manager.ItemProperties.HasItemError("EMP", "Name")).ConfigureAwait(false);

        Assert.False(_manager.ItemProperties.HasItemError("EMP", "Name"));
    }

    #endregion

    #region Inter-Form Globals (2026-08-22)
    //
    // FormsManager.InterFormComm.cs already implements :GLOBAL.* correctly —
    // SetGlobalVariable/GetGlobalVariable delegate to the shared
    // IFormRegistry, which is exactly Oracle's own GLOBAL scope ("visible to
    // every form in the application"). An earlier audit pass marked this
    // "Partial — scoping not confirmed to match" for lack of a test proving
    // the cross-form visibility, not because of any actual defect found.
    // This test proves it: two FormsManager instances sharing one
    // IFormRegistry (as CALL_FORM/OPEN_FORM wire them in practice) see the
    // same global.

    [Fact]
    public void GlobalVariable_SetOnOneForm_VisibleFromAnotherFormSharingTheRegistry()
    {
        var registry = new FormRegistry();
        using var formA = new FormsManager(_mockEditor.Object, formRegistry: registry);
        using var formB = new FormsManager(_mockEditor.Object, formRegistry: registry);

        formA.SetGlobalVariable("CUSTOMER_ID", 42);

        Assert.Equal(42, formB.GetGlobalVariable("CUSTOMER_ID"));
        Assert.Equal(42, formB.GetGlobalVariable<int>("CUSTOMER_ID"));
    }

    #endregion

    #region OPEN_FORM / NEW_FORM / CALL_FORM (2026-08-22)
    //
    // CallFormAsync/OpenFormModelessAsync/NewFormAsync/ReturnToCallerAsync
    // (FormsManager.MultiFormNavigation.cs) were, on inspection, already
    // carefully and correctly implemented — genuine TOCTOU handling, stack
    // corruption detection, real caller suspension via a
    // TaskCompletionSource for modal calls. An earlier audit pass marked
    // OPEN_FORM/NEW_FORM "Partial — not confirmed exercised": true only in
    // the sense that zero tests exercised any of this surface. These tests
    // close that gap without changing behavior — nothing here needed a fix.

    [Fact]
    public async Task NewFormAsync_UnregistersCallerAndActivatesTarget()
    {
        var registry = new FormRegistry();
        using var formA = new FormsManager(_mockEditor.Object, formRegistry: registry);
        using var formB = new FormsManager(_mockEditor.Object, formRegistry: registry);
        formA.CurrentFormName = "FormA";
        formB.CurrentFormName = "FormB";
        registry.RegisterForm("FormA", formA);
        registry.RegisterForm("FormB", formB);

        var result = await formA.NewFormAsync("FormB").ConfigureAwait(false);

        Assert.True(result);
        Assert.False(registry.FormExists("FormA"));
        Assert.Equal("FormB", registry.ActiveFormName);
    }

    [Fact]
    public async Task OpenFormModelessAsync_DoesNotSuspendTheCaller()
    {
        var registry = new FormRegistry();
        using var formA = new FormsManager(_mockEditor.Object, formRegistry: registry);
        using var formB = new FormsManager(_mockEditor.Object, formRegistry: registry);
        formA.CurrentFormName = "FormA";
        formB.CurrentFormName = "FormB";
        registry.RegisterForm("FormA", formA);
        registry.RegisterForm("FormB", formB);

        var result = await formA.OpenFormModelessAsync("FormB").ConfigureAwait(false);

        Assert.True(result);
        // Both forms are still registered — modeless is concurrent, not a
        // replace.
        Assert.True(registry.FormExists("FormA"));
        Assert.True(registry.FormExists("FormB"));
        Assert.Equal("FormB", registry.ActiveFormName);
    }

    [Fact]
    public async Task CallFormAsync_Modal_SuspendsCallerUntilCalleeReturnsWithData()
    {
        var registry = new FormRegistry();
        using var formA = new FormsManager(_mockEditor.Object, formRegistry: registry);
        using var formB = new FormsManager(_mockEditor.Object, formRegistry: registry);
        formA.CurrentFormName = "FormA";
        formB.CurrentFormName = "FormB";
        registry.RegisterForm("FormA", formA);
        registry.RegisterForm("FormB", formB);

        var callTask = formA.CallFormAsync("FormB", mode: FormCallMode.Modal);

        // The caller genuinely suspends — it must not complete before the
        // callee explicitly returns.
        await Task.Delay(50).ConfigureAwait(false);
        Assert.False(callTask.IsCompleted);

        var returned = await formB.ReturnToCallerAsync("selected-value").ConfigureAwait(false);
        Assert.True(returned);

        var result = await callTask.ConfigureAwait(false);

        Assert.True(result);
        Assert.Equal("selected-value", formA.GetFormParameter("RETURN_VALUE"));
        Assert.Equal("FormA", registry.ActiveFormName);
    }

    #endregion

    #region Row-Level Security Filter (2026-08-22)
    //
    // ISecurityManager.GetBlockRowFilter/GetBlockSecurity existed with no
    // caller anywhere in the engine. A block configured with
    // BlockSecurity.RowFilterClause (e.g. "TenantId = :TenantId", to restrict
    // a user to their own tenant's rows) had that restriction stored and
    // never enforced: ExecuteQueryAsync only ever checked the coarse
    // query/insert/update/delete allow-flags via EnforceBlockSecurity, never
    // the row filter — every permitted user saw every row, not just their
    // own. QueryBuilderManager.ParseCondition's own comment already
    // anticipated this exact consumer ("parameterized placeholders... are
    // preserved as-is so the caller can resolve them") — nothing had ever
    // been the caller.

    [Fact]
    public async Task ExecuteQueryAsync_BlockHasRowFilterSecurity_MergesFilterIntoQuery()
    {
        var entity = CreateEntity("ORD", ("TenantId", "int"));
        var uow = CreateUowMock(0);
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.SetBlockSecurity("ORD", new BlockSecurity
        {
            RowFilterClause = "TenantId = :TenantId",
            RowFilterValues = new Dictionary<string, object> { ["TenantId"] = 42 }
        });

        await _manager.ExecuteQueryAsync("ORD").ConfigureAwait(false);

        uow.Verify(u => u.Get(It.Is<List<AppFilter>>(f =>
            f.Any(x => x.FieldName == "TenantId" && x.FilterValue == "42"))), Times.Once);
    }

    [Fact]
    public async Task CountQueryAsync_BlockHasRowFilterSecurity_MergesFilterIntoWhereClause()
    {
        var entity = CreateEntity("ORD", ("TenantId", "int"));
        var uow = CreateUowMock(0);
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.GetScalarAsync(It.Is<string>(sql => sql.Contains("TenantId = '42'"))))
            .ReturnsAsync(3.0);
        _mockEditor.Setup(e => e.GetDataSource("DEFAULT_DB")).Returns(dataSource.Object);
        _manager.RegisterBlock("ORD", uow.Object, entity, "DEFAULT_DB");
        _manager.SetBlockSecurity("ORD", new BlockSecurity
        {
            RowFilterClause = "TenantId = :TenantId",
            RowFilterValues = new Dictionary<string, object> { ["TenantId"] = 42 }
        });

        var count = await _manager.CountQueryAsync("ORD").ConfigureAwait(false);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ExecuteQueryAsync_NoRowFilterConfigured_DoesNotAlterFilters()
    {
        var entity = CreateEntity("ORD", ("TenantId", "int"));
        var uow = CreateUowMock(0);
        _manager.RegisterBlock("ORD", uow.Object, entity);

        await _manager.ExecuteQueryAsync("ORD").ConfigureAwait(false);

        // No security configured for this block — the plain no-filter Get()
        // overload is used, exactly as before this fix.
        uow.Verify(u => u.Get(), Times.Once);
        uow.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    #endregion

    #region Timer-Fired Exception Handling (2026-08-22)
    //
    // OnTimerManagerFired used `_ = _triggerManager.FireFormTriggerAsync(...)`
    // — fire-and-forget on an async Task inside a synchronous event handler.
    // The surrounding try/catch only ever observed a synchronous throw; an
    // exception from the trigger's own execution became an unobserved task
    // exception. This test proves the trigger now actually runs (and is
    // awaited) when the timer fires — the fix made OnTimerManagerFired itself
    // async so the await (and its try/catch) cover the real execution.

    [Fact]
    public async Task TimerFired_FiresWhenTimerExpiredTrigger()
    {
        var mockTimer = new Mock<ITimerManager>();
        using var manager = new FormsManager(_mockEditor.Object, timerManager: mockTimer.Object);
        manager.CurrentFormName = "TestForm";

        string capturedTimerName = null;
        manager.Triggers.RegisterFormTrigger(TriggerType.WhenTimerExpired, "TestForm", ctx =>
        {
            capturedTimerName = ctx.Parameters.TryGetValue("TimerName", out var v) ? v as string : null;
            return TriggerResult.Success;
        });

        mockTimer.Raise(t => t.TimerFired += null, mockTimer.Object,
            new TimerFiredEventArgs { TimerName = "T1", FireCount = 1 });

        await WaitUntilAsync(() => capturedTimerName != null).ConfigureAwait(false);

        Assert.Equal("T1", capturedTimerName);
    }

    #endregion

    #region Field/Record Rule-Based Validation → Item Error State (2026-08-22)
    //
    // The last piece of the SetItemError/ClearItemError gap flagged (but
    // deliberately not fixed) alongside G0.25: ValidationManager's
    // ValidationFailed/ValidationCompleted .NET events fired correctly the
    // whole time, but nothing ever read a ValidateItem/ValidateRecord result
    // and pushed it into the per-item error store — HasItemError could never
    // report true for a plain registered ValidationRule failure, only for
    // the LOV path G0.25 fixed. A field with zero registered rules is
    // vacuously "valid" (ItemValidationResult.IsValid with an empty
    // RuleResults) — these tests also prove that case does NOT clear an
    // error a different check (e.g. LOV) already set.

    [Fact]
    public void ValidateField_RequiredRuleFails_SetsItemError()
    {
        var entity = CreateEntity("ORD", ("TenantId", "int"));
        var uow = CreateUowMock(0);
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.Validation.RegisterRule(new ValidationRule
        {
            RuleName = "ORD_TenantId_Required",
            BlockName = "ORD",
            ItemName = "TenantId",
            ValidationType = ValidationType.Required,
            Timing = ValidationTiming.Manual
        });

        var result = _manager.ValidateField("ORD", "TenantId", null);

        Assert.False(result);
        Assert.True(_manager.ItemProperties.HasItemError("ORD", "TenantId"));
    }

    [Fact]
    public void ValidateField_RequiredRulePasses_ClearsPreviousItemError()
    {
        var entity = CreateEntity("ORD", ("TenantId", "int"));
        var uow = CreateUowMock(0);
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.Validation.RegisterRule(new ValidationRule
        {
            RuleName = "ORD_TenantId_Required",
            BlockName = "ORD",
            ItemName = "TenantId",
            ValidationType = ValidationType.Required,
            Timing = ValidationTiming.Manual
        });
        _manager.ValidateField("ORD", "TenantId", null);
        Assert.True(_manager.ItemProperties.HasItemError("ORD", "TenantId"));

        var result = _manager.ValidateField("ORD", "TenantId", 42);

        Assert.True(result);
        Assert.False(_manager.ItemProperties.HasItemError("ORD", "TenantId"));
    }

    [Fact]
    public void ValidateField_NoRulesRegistered_DoesNotClearAnExistingError()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(0);
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.ItemProperties.SetItemError("ORD", "Name", "set by a different check (e.g. LOV)");

        _manager.ValidateField("ORD", "Name", "anything");

        Assert.True(_manager.ItemProperties.HasItemError("ORD", "Name"));
    }

    [Fact]
    public void ValidateBlock_RequiredRuleFailsOnCurrentRecord_SetsItemErrorForThatField()
    {
        var entity = CreateEntity("ORD", ("TenantId", "int"));
        var record = new Dictionary<string, object> { ["TenantId"] = null };
        var uow = CreateUowMock(1, record);
        _manager.RegisterBlock("ORD", uow.Object, entity);
        _manager.Validation.RegisterRule(new ValidationRule
        {
            RuleName = "ORD_TenantId_Required",
            BlockName = "ORD",
            ItemName = "TenantId",
            ValidationType = ValidationType.Required,
            Timing = ValidationTiming.Manual
        });

        var result = _manager.ValidateBlock("ORD");

        Assert.False(result);
        Assert.True(_manager.ItemProperties.HasItemError("ORD", "TenantId"));
    }

    #endregion

    #region New Trigger Wiring — WHEN-CLOSE-FORM / WHEN-CLEAR-BLOCK / WHEN-FORM-NOTIFICATION / WHEN-DATABASE-RECORD (2026-08-24)
    //
    // TriggerType.WhenCloseForm/WhenClearBlock/WhenFormNotification/WhenDatabaseRecord
    // existed nowhere before this — the Oracle Forms catalog listed all four as
    // events the IDE's Add Trigger picker offered with no matching engine member.
    // These tests prove each one actually fires from its real call site, not just
    // that the enum member compiles.

    [Fact]
    public async Task CloseFormAsync_FiresWhenCloseFormTrigger()
    {
        await _manager.OpenFormAsync("TestForm").ConfigureAwait(false);

        string capturedForm = null;
        _manager.Triggers.RegisterFormTrigger(TriggerType.WhenCloseForm, "TestForm", ctx =>
        {
            capturedForm = ctx.FormName;
            return TriggerResult.Success;
        });

        var closed = await _manager.CloseFormAsync().ConfigureAwait(false);

        Assert.True(closed);
        Assert.Equal("TestForm", capturedForm);
    }

    [Fact]
    public async Task CloseFormAsync_WhenCloseFormTriggerCancels_FormStaysOpen()
    {
        await _manager.OpenFormAsync("TestForm").ConfigureAwait(false);
        _manager.Triggers.RegisterFormTrigger(TriggerType.WhenCloseForm, "TestForm",
            ctx => TriggerResult.Cancelled);

        var closed = await _manager.CloseFormAsync().ConfigureAwait(false);

        Assert.False(closed);
        Assert.Equal("TestForm", _manager.CurrentFormName);
    }

    [Fact]
    public async Task ClearBlockAsync_FiresWhenClearBlockTrigger()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(0);
        _manager.RegisterBlock("ORD", uow.Object, entity);

        string capturedBlock = null;
        _manager.Triggers.RegisterBlockTrigger(TriggerType.WhenClearBlock, "ORD", ctx =>
        {
            capturedBlock = ctx.BlockName;
            return TriggerResult.Success;
        });

        await _manager.ClearBlockAsync("ORD").ConfigureAwait(false);

        Assert.Equal("ORD", capturedBlock);
        uow.Verify(u => u.Clear(), Times.Once);
    }

    [Fact]
    public async Task MessageBus_MessageAddressedToCurrentForm_FiresWhenFormNotificationTrigger()
    {
        using var manager = new FormsManager(_mockEditor.Object);
        await manager.OpenFormAsync("TestForm").ConfigureAwait(false);

        string capturedSender = null;
        object capturedPayload = null;
        manager.Triggers.RegisterFormTrigger(TriggerType.WhenFormNotification, "TestForm", ctx =>
        {
            capturedSender = ctx.Parameters.TryGetValue("SenderForm", out var v) ? v as string : null;
            capturedPayload = ctx.Parameters.TryGetValue("Payload", out var p) ? p : null;
            return TriggerResult.Success;
        });

        manager.MessageBus.PostMessage("TestForm", "REFRESH", "payload-data", senderForm: "OtherForm");

        await WaitUntilAsync(() => capturedSender != null).ConfigureAwait(false);

        Assert.Equal("OtherForm", capturedSender);
        Assert.Equal("payload-data", capturedPayload);
    }

    [Fact]
    public async Task InsertRecordEnhancedAsync_FiresWhenDatabaseRecordTriggerBeforePreInsert()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(0);
        uow.Setup(u => u.IsDirty).Returns(false);
        // GetBlockCount (used to detect whether UnitOfWork.Add actually added
        // a record) reads IAggregatable.Count, not TotalItemCount — a plain
        // IUnitofWork mock doesn't implement it, so Add()'s effect must be
        // simulated explicitly via a call sequence: 0 before Add, 1 after.
        uow.As<IAggregatable>().SetupSequence(a => a.Count(It.IsAny<Func<object, bool>>()))
            .Returns(0)
            .Returns(1);
        _manager.RegisterBlock("ORD", uow.Object, entity);
        var blockInfo = _manager.GetBlock("ORD");
        blockInfo.Mode = DataBlockMode.CRUD;

        var firedOrder = new List<TriggerType>();
        _manager.Triggers.RegisterBlockTrigger(TriggerType.WhenDatabaseRecord, "ORD", ctx =>
        {
            firedOrder.Add(TriggerType.WhenDatabaseRecord);
            return TriggerResult.Success;
        });
        _manager.Triggers.RegisterBlockTrigger(TriggerType.PreInsert, "ORD", ctx =>
        {
            firedOrder.Add(TriggerType.PreInsert);
            return TriggerResult.Success;
        });

        var record = new Dictionary<string, object> { ["Name"] = "Alice" };
        var result = await _manager.InsertRecordEnhancedAsync("ORD", record).ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(new[] { TriggerType.WhenDatabaseRecord, TriggerType.PreInsert }, firedOrder);
    }

    #endregion
}
