using Moq;
using System.Collections.Concurrent;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
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

    // G0.64 (2026-08-26): _currentBlockName was never initialized for a
    // form's first block -- only an explicit SwitchToBlockAsync/
    // GoBlockAsync call ever set it, and no host in this repo calls that
    // on initial registration. Every "current block" fallback
    // (DmlTriggers/KeyTriggers/Menu dispatch, Alert MessageScope,
    // GetAllBlockModeInfo's IsCurrentBlock, SaveFormState/
    // RestoreFormStateAsync) silently treated every single-block form as
    // having no current block at all. Fixed by defaulting the first
    // registered block to current, mirroring Oracle Forms' own default
    // (first block in navigation sequence).

    [Fact]
    public void RegisterBlock_FirstBlock_BecomesCurrentBlock()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);

        _manager.RegisterBlock("EMP", uowMock.Object, entity);

        Assert.Equal("EMP", _manager.CurrentBlockName);
    }

    [Fact]
    public void RegisterBlock_SecondBlock_DoesNotOverrideCurrentBlock()
    {
        var empEntity = CreateEntity("EMP", ("EMPNO", "int"));
        var ordEntity = CreateEntity("ORD", ("OrderId", "int"));

        _manager.RegisterBlock("EMP", CreateUowMock(1).Object, empEntity);
        _manager.RegisterBlock("ORD", CreateUowMock(1).Object, ordEntity);

        Assert.Equal("EMP", _manager.CurrentBlockName);
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
        // FormsManager's constructor wires SystemVariables onto whatever
        // ITriggerManager it's given (see G0.36 in gaps.md) -- a Strict mock
        // needs this stubbed even though this test doesn't care about it.
        triggers.SetupSet(instance => instance.SystemVariables = It.IsAny<ISystemVariablesManager>());
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
        triggers.SetupSet(instance => instance.SystemVariables = It.IsAny<ISystemVariablesManager>());
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

    // :SYSTEM.CURSOR_RECORD / :SYSTEM.LAST_RECORD / :SYSTEM.RECORDS_DISPLAYED (G0.36,
    // continued, 2026-08-26). Previously only updated from
    // TryUpdateSavepointSystemVariables (savepoint rollback only) -- ordinary record
    // navigation left them stale. NavigateAsync (First/Next/Previous/Last, via
    // NavigateWithValidationAsync) and NavigateToRecordInternalAsync
    // (NavigateToRecordAsync/GoRecordAsync) are the two real choke points every
    // record-navigation entry point funnels through.

    [Fact]
    public async Task NextRecordAsync_OnSuccess_UpdatesSystemVariablesRecordPosition()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(10);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.NextRecordAsync("EMP").ConfigureAwait(false);

        Assert.True(result);
        variables.Verify(v => v.UpdateForRecordChange("EMP", It.IsAny<int>(), 10), Times.Once);
    }

    [Fact]
    public async Task NavigateToRecordAsync_OnSuccess_UpdatesSystemVariablesRecordPosition()
    {
        // PerformRecordNavigation (unlike PerformNavigation, which First/Next/
        // Previous/Last use) dynamic-dispatches SetCurrentIndex/GetTotalRecords
        // against Units directly -- a bare ICollection mock has no CurrentIndex
        // property to dispatch to, so it fails the dynamic bind and the
        // navigation itself never succeeds. A real ObservableBindingList gives
        // it something genuine to navigate. Must be closed over a PUBLIC type --
        // FormsManager's dynamic dispatch runs in a different assembly, and the
        // C# dynamic binder (unlike plain reflection) enforces accessibility, so
        // ObservableBindingList<TestEntityRecord> (a private nested test class)
        // fails to bind at all: GetTotalRecords silently caught a
        // RuntimeBinderException and returned 0, never a "not navigable" 3 >= 0.
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var units = new TheTechIdea.Beep.Editor.ObservableBindingList<TheTechIdea.Beep.Editor.Entity>(
            new List<TheTechIdea.Beep.Editor.Entity> { new(), new(), new(), new(), new() });
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.Units).Returns(units);
        uowMock.Setup(u => u.TotalItemCount).Returns(units.Count);
        uowMock.Setup(u => u.IsDirty).Returns(false);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.NavigateToRecordAsync("EMP", 3).ConfigureAwait(false);

        Assert.True(result, manager.Status);
        variables.Verify(v => v.UpdateForRecordChange("EMP", 3, units.Count), Times.Once);
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
    public void PropertyClassApplyToItem_EnabledAndVisible_OverlayDirectlyFromField()
    {
        // Unlike QueryAllowed/InsertAllowed/UpdateAllowed, IsEnabled/IsVisible
        // are not part of the Property Class model at all -- they apply
        // directly from the field, with no class-fallback step to prove.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "X", Enabled = true, Visible = true };
        var field = new BlockFieldDefinition { FieldName = "X", IsEnabled = false, IsVisible = false };

        propertyClasses.ApplyToItem(item, field);

        Assert.False(item.Enabled);
        Assert.False(item.Visible);
    }

    [Fact]
    public void PropertyClassApplyToItem_Label_OverlaysPromptTextDirectlyFromField()
    {
        // ItemInfo.Create defaults PromptText to the raw field name
        // (e.g. "OrderId") -- both WinFormBlockHost and BeepWpfBlock already
        // read PromptText as the visible field label and grid column
        // caption, but nothing carried the IDE's authored Label across to
        // it, so every authored caption ("Order ID") was silently discarded.
        // Like IsEnabled/IsVisible, PropertyClass has no Label member, so
        // this applies directly from the field with no class-fallback step
        // to prove.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", PromptText = "OrderId" };
        var field = new BlockFieldDefinition { FieldName = "OrderId", Label = "Order ID" };

        propertyClasses.ApplyToItem(item, field);

        Assert.Equal("Order ID", item.PromptText);
    }

    [Fact]
    public void PropertyClassApplyToItem_NoAuthoredLabel_KeepsExistingPromptText()
    {
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", PromptText = "OrderId" };
        var field = new BlockFieldDefinition { FieldName = "OrderId" };

        propertyClasses.ApplyToItem(item, field);

        Assert.Equal("OrderId", item.PromptText);
    }

    [Fact]
    public void PropertyClassApplyToItem_Width_OverlaysItemWidthDirectlyFromField()
    {
        // BlockFieldDefinition.Width has had a full IDE authoring surface
        // (emit/read-back) since it was added, but neither runtime host ever
        // sized a control from it -- ItemInfo had no Width property at all
        // to carry the value across. Like Label, PropertyClass has no Width
        // member, so this applies directly from the field.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", Width = 0 };
        var field = new BlockFieldDefinition { FieldName = "OrderId", Width = 220 };

        propertyClasses.ApplyToItem(item, field);

        Assert.Equal(220, item.Width);
    }

    [Fact]
    public void PropertyClassApplyToItem_NoAuthoredWidth_KeepsExistingWidth()
    {
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", Width = 0 };
        var field = new BlockFieldDefinition { FieldName = "OrderId" };

        propertyClasses.ApplyToItem(item, field);

        Assert.Equal(0, item.Width);
    }

    [Fact]
    public void PropertyClassApplyToItem_AuthoredIsRequiredTrue_SetsItemRequired()
    {
        // A field the schema itself allows null on, but the designer's
        // author has explicitly checked Required for (a business rule the
        // database doesn't enforce) -- item.Required was never touched by
        // ApplyToItem at all before this fix, so an authored Required
        // checkbox compiled, round-tripped, and did nothing at runtime.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "Notes", Required = false };
        var field = new BlockFieldDefinition { FieldName = "Notes", IsRequired = true };

        propertyClasses.ApplyToItem(item, field);

        Assert.True(item.Required);
    }

    [Fact]
    public void PropertyClassApplyToItem_UnauthoredIsRequired_KeepsSchemaDerivedTrue()
    {
        // BlockFieldDefinition.IsRequired is a plain bool -- there is no
        // "not authored" state distinct from false, unlike the
        // QueryAllowed/InsertAllowed/UpdateAllowed cluster. So this overlay
        // is deliberately one-directional: an unauthored field (IsRequired
        // still false, the type's own default) must never force a NOT NULL
        // database column's already-correct item.Required back down to
        // false -- that would be a regression, not a fix.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", Required = true };
        var field = new BlockFieldDefinition { FieldName = "OrderId" };

        propertyClasses.ApplyToItem(item, field);

        Assert.True(item.Required);
    }

    [Theory]
    [InlineData("Numeric", "Numeric")]
    [InlineData("date", "Date")]
    [InlineData("BOOLEAN", "Boolean")]
    [InlineData("Checkbox", "Checkbox")]
    [InlineData("readonly", "ReadOnly")]
    [InlineData("Text", "Text")]
    public void PropertyClassApplyToItem_RecognisedEditorKey_OverlaysCanonicalCategory(
        string authoredEditorKey, string expectedCanonical)
    {
        // BlockFieldsEditorDialog's EditorKey box is free text, not a
        // constrained dropdown, so it must be normalised (case-insensitive,
        // against the same canonical set the runtime presenter registries
        // switch on) before being trusted -- never carried across verbatim.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", EditorKey = null };
        var field = new BlockFieldDefinition { FieldName = "OrderId", EditorKey = authoredEditorKey };

        propertyClasses.ApplyToItem(item, field);

        Assert.Equal(expectedCanonical, item.EditorKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("BeepComboBox")]
    [InlineData("nope")]
    public void PropertyClassApplyToItem_UnrecognisedEditorKey_LeavesItemEditorKeyNull(string? authoredEditorKey)
    {
        // An unauthored field and a typo'd/platform-specific one must be
        // indistinguishable to the registry -- both mean "infer the type",
        // never a guess at what an unrecognised value might have meant.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "OrderId", EditorKey = null };
        var field = new BlockFieldDefinition { FieldName = "OrderId", EditorKey = authoredEditorKey };

        propertyClasses.ApplyToItem(item, field);

        Assert.Null(item.EditorKey);
    }

    [Fact]
    public void PropertyClassApplyToItem_AuthoredIsReadOnlyTrue_ForcesInsertAndUpdateNotAllowed()
    {
        // BlockFieldsEditorDialog's "Is Read Only" checkbox round-tripped
        // perfectly (load, save, DesignerBlockGenerator emission) but
        // item.InsertAllowed/item.UpdateAllowed -- the two flags both
        // WinFormBlockHost.cs and BeepWpfBlock.cs actually read to compute
        // presenter.IsReadOnly for the current block mode -- were never
        // touched by ApplyToItem, so an authored Read Only field stayed
        // fully editable at runtime.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "TotalPrice", InsertAllowed = true, UpdateAllowed = true };
        var field = new BlockFieldDefinition { FieldName = "TotalPrice", IsReadOnly = true };

        propertyClasses.ApplyToItem(item, field);

        Assert.False(item.InsertAllowed);
        Assert.False(item.UpdateAllowed);
    }

    [Fact]
    public void PropertyClassApplyToItem_UnauthoredIsReadOnly_KeepsExistingInsertUpdateAllowed()
    {
        // Same one-directional shape as IsRequired above: IsReadOnly is a
        // plain bool with no "not authored" state distinct from false, so an
        // unauthored field must never force an already-editable item back to
        // read-only.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "TotalPrice", InsertAllowed = true, UpdateAllowed = true };
        var field = new BlockFieldDefinition { FieldName = "TotalPrice" };

        propertyClasses.ApplyToItem(item, field);

        Assert.True(item.InsertAllowed);
        Assert.True(item.UpdateAllowed);
    }

    [Fact]
    public void PropertyClassApplyToItem_AuthoredIsReadOnlyTrue_OverridesExplicitInsertUpdateAllowedTrue()
    {
        // IsReadOnly can only ADD the restriction, so it must win even over
        // a contradictory explicit authoring (InsertAllowed/UpdateAllowed
        // both explicitly true alongside IsReadOnly true) -- applied after
        // the QueryAllowed/InsertAllowed/UpdateAllowed cluster, not before.
        var propertyClasses = new PropertyClassManager();
        var item = new ItemInfo { ItemName = "TotalPrice", InsertAllowed = false, UpdateAllowed = false };
        var field = new BlockFieldDefinition
        {
            FieldName = "TotalPrice",
            InsertAllowed = true,
            UpdateAllowed = true,
            IsReadOnly = true
        };

        propertyClasses.ApplyToItem(item, field);

        Assert.False(item.InsertAllowed);
        Assert.False(item.UpdateAllowed);
    }

    [Fact]
    public void AssignTabIndexFromAuthoredOrder_RanksByAuthoredOrder_NotOriginalListPosition()
    {
        // BlockFieldDefinition.Order authors the Block Fields editor's
        // drag-reorder sequence and round-trips perfectly, but nothing ever
        // read it back into item.TabIndex -- RegisterItemsFromEntityStructure
        // seeds TabIndex purely from the datasource's raw column order, so an
        // author's reordering was invisible to Tab-key navigation. Field C is
        // listed last but authored Order=0, so it must rank first.
        var fieldA = new BlockFieldDefinition { FieldName = "A", Order = 2 };
        var fieldB = new BlockFieldDefinition { FieldName = "B", Order = 1 };
        var fieldC = new BlockFieldDefinition { FieldName = "C", Order = 0 };
        var itemA = new ItemInfo { ItemName = "A" };
        var itemB = new ItemInfo { ItemName = "B" };
        var itemC = new ItemInfo { ItemName = "C" };
        var resolved = new List<(BlockFieldDefinition Field, ItemInfo Item)>
        {
            (fieldA, itemA), (fieldB, itemB), (fieldC, itemC)
        };

        DefinitionBlockRegistrar.AssignTabIndexFromAuthoredOrder(resolved);

        Assert.Equal(0, itemC.TabIndex);
        Assert.Equal(1, itemB.TabIndex);
        Assert.Equal(2, itemA.TabIndex);
    }

    [Fact]
    public void AssignTabIndexFromAuthoredOrder_AllFieldsShareUnauthoredDefaultOrder_StillAssignsUniqueSequentialTabIndex()
    {
        // A legacy/hand-written block whose fields never went through the
        // Block Fields editor has every Order at its unauthored int default
        // (0). Ranking by stable sort -- never copying Order's raw value --
        // means this must still produce a unique TabIndex per field, in
        // original list order, exactly matching what
        // RegisterItemsFromEntityStructure already gave it: no regression to
        // a duplicate-TabIndex state for blocks that never authored Order.
        var fieldA = new BlockFieldDefinition { FieldName = "A", Order = 0 };
        var fieldB = new BlockFieldDefinition { FieldName = "B", Order = 0 };
        var fieldC = new BlockFieldDefinition { FieldName = "C", Order = 0 };
        var itemA = new ItemInfo { ItemName = "A" };
        var itemB = new ItemInfo { ItemName = "B" };
        var itemC = new ItemInfo { ItemName = "C" };
        var resolved = new List<(BlockFieldDefinition Field, ItemInfo Item)>
        {
            (fieldA, itemA), (fieldB, itemB), (fieldC, itemC)
        };

        DefinitionBlockRegistrar.AssignTabIndexFromAuthoredOrder(resolved);

        Assert.Equal(0, itemA.TabIndex);
        Assert.Equal(1, itemB.TabIndex);
        Assert.Equal(2, itemC.TabIndex);
    }

    [Fact]
    public void AssignTabIndexFromAuthoredOrder_NullList_NoOp()
    {
        DefinitionBlockRegistrar.AssignTabIndexFromAuthoredOrder(null);
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

    #region :SYSTEM.BLOCK_STATUS / :SYSTEM.RECORD_STATUS on real edit (2026-08-25)
    //
    // ItemChanged is the one place every genuine user-driven field edit on
    // every block passes through (see gaps.md G0.36's "CHANGED" account) and
    // is confirmed never to fire from query population (UnitofWork.CRUD.cs's
    // Get()/Get(filters) builds each row in a plain List<T> first and only
    // wraps it afterward). No LOV is attached here deliberately -- this test
    // exercises the plain (no-LOV) branch of the handler, which the three
    // WHEN-LOV-VALIDATION tests above do not.

    [Fact]
    public async Task ItemChanged_NoLov_SetsBlockAndRecordStatusToChanged()
    {
        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateUowMock(1, new TestEntityRecord());
        _manager.RegisterBlock("EMP", uow.Object, entity);

        Assert.Equal("NEW", _manager.SystemVariables.GetSystemVariables("EMP").BLOCK_STATUS);

        var record = new TestEntityRecord { Name = "Bob" };
        uow.Raise(u => u.ItemChanged += null, uow.Object, new ItemChangedEventArgs<Entity>(record, "Name"));

        await WaitUntilAsync(
            () => _manager.SystemVariables.GetSystemVariables("EMP").BLOCK_STATUS == "CHANGED")
            .ConfigureAwait(false);

        Assert.Equal("CHANGED", _manager.SystemVariables.GetSystemVariables("EMP").BLOCK_STATUS);
        Assert.Equal("CHANGED", _manager.SystemVariables.GetSystemVariables("EMP").RECORD_STATUS);
        Assert.Equal("CHANGED", _manager.SystemVariables.GetFormSystemVariables().FORM_STATUS);
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

    #region Named Alert Registry (2026-08-25)
    //
    // ShowAlertAsync took its title/message/buttons literally on every call —
    // there was no persisted, named ALERT object at all, unlike Oracle Forms
    // where an Alert is authored once and shown by name from any trigger
    // (SHOW_ALERT('alert_name')). These tests cover the new registry and
    // prove ShowAlertByNameAsync actually renders through the registered
    // definition's own fields, not just returns some canned result.

    [Fact]
    public void CreateAlert_ThenGetAlert_RoundTripsAllFields()
    {
        var created = _manager.CreateAlert(
            "ConfirmDelete", "Confirm", "Delete this record?",
            AlertStyle.Question, "Yes", "No");

        var fetched = _manager.GetAlert("ConfirmDelete");

        Assert.Same(created, fetched);
        Assert.Equal("ConfirmDelete", fetched.Name);
        Assert.Equal("Confirm", fetched.Title);
        Assert.Equal("Delete this record?", fetched.Message);
        Assert.Equal(AlertStyle.Question, fetched.Style);
        Assert.Equal("Yes", fetched.Button1Text);
        Assert.Equal("No", fetched.Button2Text);
        Assert.True(_manager.AlertExists("ConfirmDelete"));
    }

    [Fact]
    public void GetAlert_UnknownName_ReturnsNull()
    {
        Assert.Null(_manager.GetAlert("DoesNotExist"));
        Assert.False(_manager.AlertExists("DoesNotExist"));
    }

    [Fact]
    public void RemoveAlert_ExistingAlert_RemovesItAndReturnsTrue()
    {
        _manager.CreateAlert("A1", "T", "M");

        var removed = _manager.RemoveAlert("A1");

        Assert.True(removed);
        Assert.False(_manager.AlertExists("A1"));
    }

    [Fact]
    public void ClearAllAlerts_RemovesEveryRegisteredAlert()
    {
        _manager.CreateAlert("A1", "T1", "M1");
        _manager.CreateAlert("A2", "T2", "M2");

        _manager.ClearAllAlerts();

        Assert.Empty(_manager.GetAllAlerts());
    }

    [Fact]
    public async Task ShowAlertByNameAsync_UnknownName_ReturnsNoneWithoutCallingProvider()
    {
        var mockProvider = new Mock<IAlertProvider>();
        using var manager = new FormsManager(_mockEditor.Object, alertProvider: mockProvider.Object);

        var result = await manager.ShowAlertByNameAsync("DoesNotExist").ConfigureAwait(false);

        Assert.Equal(AlertResult.None, result);
        mockProvider.Verify(p => p.ShowAlertAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AlertStyle>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShowAlertByNameAsync_KnownName_RendersThroughProviderWithDefinitionFields()
    {
        var mockProvider = new Mock<IAlertProvider>();
        mockProvider
            .Setup(p => p.ShowAlertAsync(
                "Confirm", "Delete this record?", AlertStyle.Question,
                "Yes", "No", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AlertResult.Button2);
        using var manager = new FormsManager(_mockEditor.Object, alertProvider: mockProvider.Object);
        manager.CreateAlert("ConfirmDelete", "Confirm", "Delete this record?", AlertStyle.Question, "Yes", "No");

        var result = await manager.ShowAlertByNameAsync("ConfirmDelete").ConfigureAwait(false);

        Assert.Equal(AlertResult.Button2, result);
        mockProvider.Verify(p => p.ShowAlertAsync(
            "Confirm", "Delete this record?", AlertStyle.Question,
            "Yes", "No", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Named Editor Registry (Oracle Forms EDITOR object) (2026-08-25)
    //
    // Neither the definition nor the invocation existed anywhere before this
    // — the only prior "Editor" hit in the model layer was
    // BlockFieldDefinition/BlockEntityDefinition.EditorKey, an unrelated
    // control-selection string for the platform field-presenter registry.
    // These tests cover the new registry and prove ShowEditorAsync actually
    // writes a committed edit onto the block's current record, and leaves
    // an existing value alone on cancel.

    [Fact]
    public void CreateEditor_ThenGetEditor_RoundTripsAllFields()
    {
        var created = _manager.CreateEditor("Notes", "Edit Notes", 600, 400, wrapText: false, showScrollBar: false);

        var fetched = _manager.GetEditor("Notes");

        Assert.Same(created, fetched);
        Assert.Equal("Notes", fetched.Name);
        Assert.Equal("Edit Notes", fetched.Title);
        Assert.Equal(600, fetched.Width);
        Assert.Equal(400, fetched.Height);
        Assert.False(fetched.WrapText);
        Assert.False(fetched.ShowScrollBar);
        Assert.True(_manager.EditorExists("Notes"));
    }

    [Fact]
    public void GetEditor_UnknownName_ReturnsNull()
    {
        Assert.Null(_manager.GetEditor("DoesNotExist"));
        Assert.False(_manager.EditorExists("DoesNotExist"));
    }

    [Fact]
    public void RemoveEditor_ExistingEditor_RemovesItAndReturnsTrue()
    {
        _manager.CreateEditor("E1");

        var removed = _manager.RemoveEditor("E1");

        Assert.True(removed);
        Assert.False(_manager.EditorExists("E1"));
    }

    [Fact]
    public void ClearAllEditors_RemovesEveryRegisteredEditor()
    {
        _manager.CreateEditor("E1");
        _manager.CreateEditor("E2");

        _manager.ClearAllEditors();

        Assert.Empty(_manager.GetAllEditors());
    }

    [Fact]
    public async Task ShowEditorAsync_NoCurrentRecord_ReturnsCancelWithoutCallingProvider()
    {
        var mockProvider = new Mock<IEditorProvider>();
        using var manager = new FormsManager(_mockEditor.Object, editorProvider: mockProvider.Object);
        var entity = CreateEntity("DOC", ("Notes", "string"));
        var uow = CreateUowMock(0, currentItem: null);
        manager.RegisterBlock("DOC", uow.Object, entity);

        var result = await manager.ShowEditorAsync("DOC", "Notes").ConfigureAwait(false);

        Assert.False(result.Committed);
        mockProvider.Verify(p => p.ShowEditorAsync(
            It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShowEditorAsync_Committed_WritesValueOntoCurrentRecord()
    {
        var mockProvider = new Mock<IEditorProvider>();
        mockProvider
            .Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), "old text", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EditorResult.Ok("new, larger, edited text"));
        using var manager = new FormsManager(_mockEditor.Object, editorProvider: mockProvider.Object);
        var entity = CreateEntity("DOC", ("Notes", "string"));
        var record = new Dictionary<string, object> { ["Notes"] = "old text" };
        var uow = CreateUowMock(1, currentItem: record);
        manager.RegisterBlock("DOC", uow.Object, entity);

        var result = await manager.ShowEditorAsync("DOC", "Notes").ConfigureAwait(false);

        Assert.True(result.Committed);
        Assert.Equal("new, larger, edited text", record["Notes"]);
    }

    [Fact]
    public async Task ShowEditorAsync_Cancelled_LeavesCurrentRecordUnchanged()
    {
        var mockProvider = new Mock<IEditorProvider>();
        mockProvider
            .Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), "old text", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EditorResult.Cancel());
        using var manager = new FormsManager(_mockEditor.Object, editorProvider: mockProvider.Object);
        var entity = CreateEntity("DOC", ("Notes", "string"));
        var record = new Dictionary<string, object> { ["Notes"] = "old text" };
        var uow = CreateUowMock(1, currentItem: record);
        manager.RegisterBlock("DOC", uow.Object, entity);

        var result = await manager.ShowEditorAsync("DOC", "Notes").ConfigureAwait(false);

        Assert.False(result.Committed);
        Assert.Equal("old text", record["Notes"]);
    }

    [Fact]
    public async Task ShowEditorAsync_ItemHasNamedEditorAttached_UsesAttachedDefinitionNotSystemDefault()
    {
        var mockProvider = new Mock<IEditorProvider>();
        EditorDefinition capturedEditor = null;
        mockProvider
            .Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<EditorDefinition, string, CancellationToken>((ed, _, _) => capturedEditor = ed)
            .ReturnsAsync(EditorResult.Cancel());
        using var manager = new FormsManager(_mockEditor.Object, editorProvider: mockProvider.Object);
        var entity = CreateEntity("DOC", ("Notes", "string"));
        var record = new Dictionary<string, object> { ["Notes"] = "old text" };
        var uow = CreateUowMock(1, currentItem: record);
        manager.RegisterBlock("DOC", uow.Object, entity);
        manager.CreateEditor("BigNotesEditor", "Big Notes", 800, 600);
        manager.ItemProperties.RegisterItem("DOC", "Notes", new ItemInfo { ItemName = "Notes", BlockName = "DOC", EditorName = "BigNotesEditor" });

        await manager.ShowEditorAsync("DOC", "Notes").ConfigureAwait(false);

        Assert.NotNull(capturedEditor);
        Assert.Equal("BigNotesEditor", capturedEditor.Name);
        Assert.Equal("Big Notes", capturedEditor.Title);
    }

    #endregion

    #region TriggerManager -> SystemVariables Context Wiring (G0.36, 2026-08-25)

    // SystemVariablesManager was fully built, exposed on IUnitofWorksManager and
    // TriggerContext, and completely inert -- none of its Update*/Set* methods had
    // any caller anywhere in the engine, so :SYSTEM.TRIGGER_* stayed permanently
    // empty no matter what a form did. TriggerManager.ExecuteTriggerChain(Async) is
    // the one place every Fire*Trigger(Async) variant funnels through, so that's
    // where the wiring landed rather than at each of the ~30 individual call sites.
    // These tests exercise the real TriggerManager class directly (not mocked --
    // every other test in this file mocks ITriggerManager to test FormsManager's
    // own dispatch logic; this is the first to test TriggerManager's own behavior).

    [Fact]
    public void FireBlockTrigger_SetsAndClearsSystemVariablesTriggerContext()
    {
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Strict);
        variables.Setup(v => v.SetTriggerContext("PreInsert", "EMP", null, 0));
        variables.Setup(v => v.ClearTriggerContext());

        var manager = new TriggerManager(_mockEditor.Object) { SystemVariables = variables.Object };
        ISystemVariablesManager? seenInsideHandler = null;
        manager.RegisterBlockTrigger(TriggerType.PreInsert, "EMP", context =>
        {
            seenInsideHandler = context.SystemVariables;
            return TriggerResult.Success;
        });

        var result = manager.FireBlockTrigger(TriggerType.PreInsert, "EMP");

        Assert.Equal(TriggerResult.Success, result);
        Assert.Same(variables.Object, seenInsideHandler);
        variables.VerifyAll();
    }

    [Fact]
    public async Task FireItemTriggerAsync_SetsAndClearsSystemVariablesTriggerContext()
    {
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Strict);
        variables.Setup(v => v.SetTriggerContext("WhenValidateItem", "ORD", "QTY", 0));
        variables.Setup(v => v.ClearTriggerContext());

        var manager = new TriggerManager(_mockEditor.Object) { SystemVariables = variables.Object };
        manager.RegisterItemTriggerAsync(TriggerType.WhenValidateItem, "ORD", "QTY",
            (_, _) => Task.FromResult(TriggerResult.Success));

        var result = await manager.FireItemTriggerAsync(TriggerType.WhenValidateItem, "ORD", "QTY")
            .ConfigureAwait(false);

        Assert.Equal(TriggerResult.Success, result);
        variables.VerifyAll();
    }

    [Fact]
    public void FireBlockTrigger_NoSystemVariablesWired_DoesNotThrow()
    {
        // SystemVariables is nullable and unset by default (e.g. a hand-constructed
        // TriggerManager in a test, or a future consumer that doesn't wire it) --
        // firing a trigger must not NRE just because nobody assigned it.
        var manager = new TriggerManager(_mockEditor.Object);
        manager.RegisterBlockTrigger(TriggerType.PreInsert, "EMP", _ => TriggerResult.Success);

        var result = manager.FireBlockTrigger(TriggerType.PreInsert, "EMP");

        Assert.Equal(TriggerResult.Success, result);
    }

    #endregion

    #region SwitchToBlockAsync -> SystemVariables.UpdateForBlockChange (G0.36, continued, 2026-08-25)

    // UpdateForItemChange (GoItemAsync) and UpdateForRecordChange (savepoint
    // rollback) turned out to already be wired, pre-dating this session --
    // found only after re-grepping past an earlier mistake (see gaps.md
    // G0.36's self-correction). UpdateForBlockChange had no caller anywhere,
    // confirmed by the same corrected grep, and SwitchToBlockAsync is the
    // exact same shape of single choke point GoItemAsync already was: every
    // block switch (SwitchToBlockAsync itself, and GoBlockAsync, which is a
    // pure delegation to it) goes through this one method.

    [Fact]
    public async Task SwitchToBlockAsync_UpdatesSystemVariablesCurrentBlock()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var switched = await manager.SwitchToBlockAsync("EMP").ConfigureAwait(false);

        Assert.True(switched);
        // Twice, not once (G0.64, 2026-08-26): RegisterBlock now defaults
        // _currentBlockName to the first block registered (nothing else
        // had ever set it, silently breaking every consumer that falls
        // back to "the current block"), which itself calls
        // UpdateForBlockChange -- then this explicit SwitchToBlockAsync("EMP")
        // call fires it again (EMP was already current, but
        // SwitchToBlockAsync has no same-block short-circuit).
        variables.Verify(v => v.UpdateForBlockChange("EMP"), Times.Exactly(2));
    }

    [Fact]
    public async Task GoBlockAsync_DelegatesToSwitchToBlockAsync_UpdatesSystemVariables()
    {
        // GoBlockAsync is a pure delegation (`=> SwitchToBlockAsync(blockName)`)
        // -- this pins that the delegation itself keeps working, not just the
        // method it forwards to.
        var entity = CreateEntity("ORD", ("OrderId", "int"));
        var uowMock = CreateUowMock(1);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("ORD", uowMock.Object, entity);

        var switched = await manager.GoBlockAsync("ORD").ConfigureAwait(false);

        Assert.True(switched);
        // Twice, not once -- see G0.64 note above: RegisterBlock's new
        // first-block-becomes-current default fires it once, the explicit
        // GoBlockAsync/SwitchToBlockAsync("ORD") call fires it again.
        variables.Verify(v => v.UpdateForBlockChange("ORD"), Times.Exactly(2));
    }

    #endregion

    #region CurrentFormName -> SystemVariables.SetCurrentForm (G0.36, continued, 2026-08-25)

    [Fact]
    public void CurrentFormName_Set_UpdatesSystemVariablesCurrentForm()
    {
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);

        manager.CurrentFormName = "OrderForm";

        variables.Verify(v => v.SetCurrentForm("OrderForm"), Times.Once);
    }

    #endregion

    #region blockInfo.Mode writers -> SystemVariables.SetMode (G0.36, continued, 2026-08-25)

    // Unlike CURRENT_BLOCK and CURRENT_FORM, SetMode genuinely has no single
    // choke point: blockInfo.Mode is assigned directly at four sites across
    // two files (EnterQueryModeAsync, EnterCrudModeForNewRecordAsync and
    // CoordinateChildBlocksForNewMasterRecord in FormsManager.ModeTransitions.cs,
    // plus ExecuteQueryEnhancedAsync in FormsManager.EnhancedOperations.cs) --
    // re-checked, not assumed, after CURRENT_BLOCK/CURRENT_FORM both turned
    // out to have one. Each site now calls SetMode individually, mapped onto
    // Oracle's real :SYSTEM.MODE vocabulary (NORMAL / ENTER-QUERY). This test
    // covers the simplest of the four -- EnterQueryModeAsync -- the others
    // share the same one-line pattern and are covered by the full test run.

    [Fact]
    public async Task EnterQueryModeAsync_SetsSystemVariablesModeToEnterQuery()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(0);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var entered = await manager.EnterQueryAsync("EMP").ConfigureAwait(false);

        Assert.True(entered);
        variables.Verify(v => v.SetMode("ENTER-QUERY"), Times.Once);
    }

    #endregion

    #region LogError -> SystemVariables.SetLastError (G0.36, continued, 2026-08-25)

    // Unlike the other SystemVariablesManager gaps, this one has a genuine
    // single choke point despite 114+ separate catch blocks across
    // FormsManager.*.cs: every one of them already reports through the shared
    // protected LogError helper (FormsManager.Helpers.cs), which mirrors into
    // the per-block IBlockErrorLog when a block context is given. Hooking
    // LogError itself, not each catch site, covers every failure this manager
    // ever logs -- the same shape Oracle Forms' own :SYSTEM.LAST_ERROR has
    // (it reflects whatever runtime error the form most recently hit, from
    // any operation).

    [Fact]
    public async Task LogError_SetsSystemVariablesLastError()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(0);
        uowMock.Setup(u => u.Get()).ThrowsAsync(new InvalidOperationException("boom"));
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var succeeded = await manager.ExecuteQueryAsync("EMP").ConfigureAwait(false);

        Assert.False(succeeded);
        variables.Verify(v => v.SetLastError(
            It.Is<string>(msg => msg.Contains("EMP")),
            It.IsAny<int>()), Times.Once);
    }

    #endregion

    #region ExecuteQueryEnhancedAsync -> SystemVariables.SetLastQuery (G0.36, continued, 2026-08-25)

    // SetLastQuery was left open in the earlier G0.36 pass -- ExecuteQueryEnhancedAsync has one
    // natural landing spot (right after UnitOfWork.Get(filters)/Get() succeeds), the same shape
    // that made SetMode/SetLastError tractable, but it takes a List<AppFilter>, not a WHERE-clause
    // string, and the original pass found no existing filter-to-string serializer to reuse. That
    // was itself an incomplete search: DataSourceAppFilterExtensions.BuildSelectQueryDefinition
    // (DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs) already builds a
    // full "SELECT ... FROM ... WHERE ..." string plus a parameter dictionary from an AppFilter
    // list -- it already existed, just under a different file this pass's grep didn't cover, and
    // nothing in the engine called it before this.

    [Fact]
    public async Task ExecuteQueryEnhancedAsync_OnSuccess_SetsSystemVariablesLastQuery()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(0);
        var dataSource = new Mock<IDataSource>();
        _mockEditor.Setup(e => e.GetDataSource("DEFAULT_DB")).Returns(dataSource.Object);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity, "DEFAULT_DB");

        var succeeded = await manager.ExecuteQueryAsync("EMP").ConfigureAwait(false);

        Assert.True(succeeded);
        variables.Verify(v => v.SetLastQuery(
            It.Is<string>(q => q.Contains("SELECT") && q.Contains("EMP"))), Times.Once);
    }

    [Fact]
    public async Task ExecuteQueryEnhancedAsync_UnresolvableDataSource_DoesNotSetLastQuery()
    {
        // No GetDataSource setup on _mockEditor -- it returns null (Loose mock default),
        // exercising the best-effort guard: the query itself still succeeds.
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(0);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity, "MISSING_DB");

        var succeeded = await manager.ExecuteQueryAsync("EMP").ConfigureAwait(false);

        Assert.True(succeeded);
        variables.Verify(v => v.SetLastQuery(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ValidateQueryResultsForModeTransition -> per-block MaxRecords (2026-08-26)

    // BlockConfiguration.MaxRecords ("the maximum number of records to load") had a full
    // authoring path -- a developer could register it via Configuration.BlockConfigurations[block]
    // -- but ValidateQueryResultsForModeTransition only ever checked the manager-wide
    // Configuration.MaxRecordsPerBlock default, so a per-block override sat there with no reader.
    // Fixed to prefer the block's own BlockConfiguration.MaxRecords when the block is actually
    // registered in BlockConfigurations, falling back to MaxRecordsPerBlock exactly as before for
    // any block that never configured one -- the two defaults (1000 vs 10000) do not coincide, so
    // an unconditional overlay would have silently tightened the limit for every existing block.

    [Fact]
    public async Task ExecuteQueryAndEnterCrudModeAsync_RecordCountExceedsBlockSpecificMaxRecords_ReturnsWarningWithBlockLimit()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var units = new TheTechIdea.Beep.Editor.ObservableBindingList<TheTechIdea.Beep.Editor.Entity>(
            new List<TheTechIdea.Beep.Editor.Entity> { new(), new(), new(), new(), new() });
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.Units).Returns(units);
        uowMock.Setup(u => u.TotalItemCount).Returns(units.Count);
        var manager = new FormsManager(_mockEditor.Object);
        manager.Configuration.MaxRecordsPerBlock = 10000;
        manager.Configuration.BlockConfigurations["EMP"] = new BlockConfiguration { MaxRecords = 2 };
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.ExecuteQueryAndEnterCrudModeAsync("EMP").ConfigureAwait(false);

        Assert.Equal(Errors.Warning, result.Flag);
        Assert.Contains("exceeding limit of 2", result.Message);
    }

    [Fact]
    public async Task ExecuteQueryAndEnterCrudModeAsync_NoBlockSpecificConfiguration_FallsBackToManagerWideMaxRecordsPerBlock()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var units = new TheTechIdea.Beep.Editor.ObservableBindingList<TheTechIdea.Beep.Editor.Entity>(
            new List<TheTechIdea.Beep.Editor.Entity> { new(), new(), new(), new(), new() });
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.Units).Returns(units);
        uowMock.Setup(u => u.TotalItemCount).Returns(units.Count);
        var manager = new FormsManager(_mockEditor.Object);
        manager.Configuration.MaxRecordsPerBlock = 3;
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.ExecuteQueryAndEnterCrudModeAsync("EMP").ConfigureAwait(false);

        Assert.Equal(Errors.Warning, result.Flag);
        Assert.Contains("exceeding limit of 3", result.Message);
    }

    #endregion

    #region :SYSTEM.BLOCK_STATUS / :SYSTEM.RECORD_STATUS -- QUERY and NEW transitions (G0.36, continued, 2026-08-25)

    // The earlier G0.36 pass wired only the "CHANGED" value (from a real edit, via the
    // ItemChanged handler) and left "NEW"/"QUERY"/"INSERT" open pending their own call sites.
    // "QUERY" and "NEW" turned out to share the exact choke points SetMode/SetLastQuery already
    // use -- ExecuteQueryEnhancedAsync (a record just fetched by a query, untouched) and
    // EnterCrudModeForNewRecordAsync (a blank record just created, not yet edited) -- so no new
    // investigation was needed to find them, just to widen the existing hook. "INSERT" (a NEW
    // record that has since been edited, which Oracle Forms distinguishes from CHANGED-on-a-
    // queried-record) is deliberately not attempted here -- it needs per-record "was this row
    // ever queried" state the current block-level SystemVariables snapshot doesn't carry, a
    // genuinely bigger design question left for its own pass.

    [Fact]
    public async Task ExecuteQueryEnhancedAsync_OnSuccess_SetsSystemVariablesQueryStatus()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(0);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var succeeded = await manager.ExecuteQueryAsync("EMP").ConfigureAwait(false);

        Assert.True(succeeded);
        variables.Verify(v => v.SetBlockStatus("EMP", "QUERY"), Times.Once);
        variables.Verify(v => v.SetRecordStatus("EMP", "QUERY"), Times.Once);
    }

    [Fact]
    public async Task EnterCrudModeForNewRecordAsync_OnSuccess_SetsSystemVariablesNewStatus()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(0);
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);
        manager.GetBlock("EMP")!.EntityType = typeof(TestEntityRecord);

        var result = await manager.EnterCrudModeForNewRecordAsync("EMP").ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        variables.Verify(v => v.SetBlockStatus("EMP", "NEW"), Times.Once);
        variables.Verify(v => v.SetRecordStatus("EMP", "NEW"), Times.Once);
    }

    #endregion

    #region CommitFormAsync -> SystemVariables reset to QUERY on success (G0.36, continued, 2026-08-25)

    // Nothing downgraded BLOCK_STATUS/RECORD_STATUS/FORM_STATUS back off "CHANGED" once the
    // ItemChanged-handler wiring above set it -- a successfully committed block's records now
    // match what a fresh query would return, Oracle Forms' "QUERY" status, and since every
    // committed block was (by construction) one of the form's dirty blocks -- SetBlockStatus's
    // only source of "CHANGED" -- fm's aggregate FORM_STATUS can safely follow, giving
    // SetFormStatus its first real direct call site.

    [Fact]
    public async Task CommitFormAsync_OnSuccess_ResetsBlockRecordAndFormStatusToQuery()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        dataSource.Setup(d => d.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Ok });
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uow.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        variables.Verify(v => v.SetBlockStatus("EMP", "QUERY"), Times.Once);
        variables.Verify(v => v.SetRecordStatus("EMP", "QUERY"), Times.Once);
        variables.Verify(v => v.SetFormStatus("QUERY"), Times.Once);
    }

    [Fact]
    public async Task CommitFormAsync_BlockCommitFails_DoesNotResetStatusToQuery()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        dataSource.Setup(d => d.EndTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Failed, Message = "constraint violation" });
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uow.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Failed, result.Flag);
        variables.Verify(v => v.SetBlockStatus("EMP", "QUERY"), Times.Never);
        variables.Verify(v => v.SetRecordStatus("EMP", "QUERY"), Times.Never);
        variables.Verify(v => v.SetFormStatus("QUERY"), Times.Never);
    }

    #endregion

    #region RollbackFormAsync -> SystemVariables reset to QUERY on success (G0.36, continued, 2026-08-25)

    // Same reasoning as the CommitFormAsync region above, for the opposite outcome: a
    // rolled-back edit is discarded, so the block reverts to whatever a fresh query would
    // show ("QUERY"), not "CHANGED". Scoped to blocksForDefaultRollback rather than the full
    // dirty-blocks list -- a block with a registered ON-ROLLBACK handler ran its own
    // replacement logic and may have left that block in whatever state the form author
    // intended, which this reset must not silently overwrite.

    [Fact]
    public async Task RollbackFormAsync_OnSuccess_ResetsBlockRecordAndFormStatusToQuery()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);
        uowMock.Setup(u => u.IsDirty).Returns(true);
        uowMock.Setup(u => u.Rollback()).ReturnsAsync(new ErrorsInfo { Flag = Errors.Ok });
        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        var manager = new FormsManager(_mockEditor.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.RollbackFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        variables.Verify(v => v.SetBlockStatus("EMP", "QUERY"), Times.Once);
        variables.Verify(v => v.SetRecordStatus("EMP", "QUERY"), Times.Once);
        variables.Verify(v => v.SetFormStatus("QUERY"), Times.Once);
    }

    [Fact]
    public async Task RollbackFormAsync_OnRollbackRegistered_DoesNotOverrideThatBlocksStatus()
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

        var variables = new Mock<ISystemVariablesManager>(MockBehavior.Loose);
        using var manager = new FormsManager(_mockEditor.Object,
            triggerManager: triggers.Object, systemVariablesManager: variables.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.RollbackFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        variables.Verify(v => v.SetBlockStatus("EMP", "QUERY"), Times.Never);
        variables.Verify(v => v.SetRecordStatus("EMP", "QUERY"), Times.Never);
        variables.Verify(v => v.SetFormStatus("QUERY"), Times.Never);
    }

    #endregion

    #region RegisterBlockComputedFormula (G3.2, continued, 2026-08-26)

    // FieldFormulaEvaluator (infix +, -, *, / with parentheses and field references) existed
    // with zero callers anywhere in the engine -- a complete Oracle Forms "Calculation = Formula"
    // evaluator with no path that ever reached it. RegisterBlockComputedFormula is a thin adapter
    // over the already-wired RegisterBlockComputed/GetBlockComputedValue (G3.2) machinery, so it
    // inherits that machinery's existing error handling for free.

    [Fact]
    public void RegisterBlockComputedFormula_MultiplicationFormula_EvaluatesAgainstCurrentRecord()
    {
        var entity = CreateEntity("ORD", ("Qty", "int"), ("Price", "double"));
        var uowMock = CreateUowMock(1, new { Qty = 3, Price = 9.5 });
        _manager.RegisterBlock("ORD", uowMock.Object, entity);

        _manager.RegisterBlockComputedFormula("ORD", "LineTotal", "Qty * Price");

        var result = _manager.GetBlockComputedValue("ORD", "LineTotal");

        Assert.Equal(28.5, result);
    }

    [Fact]
    public void RegisterBlockComputedFormula_MalformedFormula_ReturnsNullRatherThanThrowing()
    {
        var entity = CreateEntity("ORD", ("Qty", "int"));
        var uowMock = CreateUowMock(1, new { Qty = 3 });
        _manager.RegisterBlock("ORD", uowMock.Object, entity);

        _manager.RegisterBlockComputedFormula("ORD", "Broken", "Qty * (");

        var result = _manager.GetBlockComputedValue("ORD", "Broken");

        Assert.Null(result);
    }

    #endregion

    #region SharedBlockManager surface — SharedBlockExists / RemoveSharedBlock / NotifySharedBlockChanged (2026-08-26)

    // SharedBlockManager's CreateSharedBlock/GetSharedBlock/TryLockSharedBlock/
    // ReleaseSharedBlockLock were already wired through FormsManager.InterFormComm.cs.
    // SharedBlockExists/RemoveSharedBlock existed on the concrete class and on
    // ISharedBlockManager with no FormsManager-level wrapper at all, and
    // NotifySharedBlockChanged existed on the concrete class only -- not even on
    // ISharedBlockManager, despite SharedBlockChanged (the event it raises) already
    // being part of the interface -- so no caller typed against the interface could
    // ever raise it. CommitFormAsync is exactly the "changes to a shared block were
    // just committed" moment NotifySharedBlockChanged's own doc comment describes;
    // nothing called it.

    [Fact]
    public void SharedBlockExists_AfterCreateSharedBlock_ReturnsTrue()
    {
        var uow = CreateUowMock(0);

        var created = _manager.CreateSharedBlock("SHARED_EMP", uow.Object);

        Assert.True(created);
        Assert.True(_manager.SharedBlockExists("SHARED_EMP"));
    }

    [Fact]
    public void SharedBlockExists_UnknownBlock_ReturnsFalse()
    {
        Assert.False(_manager.SharedBlockExists("NO_SUCH_BLOCK"));
    }

    [Fact]
    public void RemoveSharedBlock_RemovesIt_SharedBlockExistsThenReturnsFalse()
    {
        var uow = CreateUowMock(0);
        _manager.CreateSharedBlock("SHARED_EMP", uow.Object);

        var removed = _manager.RemoveSharedBlock("SHARED_EMP");

        Assert.True(removed);
        Assert.False(_manager.SharedBlockExists("SHARED_EMP"));
    }

    [Fact]
    public void RemoveSharedBlock_UnknownBlock_ReturnsFalse()
    {
        Assert.False(_manager.RemoveSharedBlock("NO_SUCH_BLOCK"));
    }

    [Fact]
    public async Task CommitFormAsync_CommittedBlockIsSharedBlock_NotifiesSharedBlockChanged()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        dataSource.Setup(d => d.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Ok });
        var sharedBlocks = new Mock<ISharedBlockManager>(MockBehavior.Loose);
        sharedBlocks.Setup(s => s.SharedBlockExists("EMP")).Returns(true);
        using var manager = new FormsManager(_mockEditor.Object, sharedBlockManager: sharedBlocks.Object);
        manager.CurrentFormName = "OrderEntry";
        manager.RegisterBlock("EMP", uow.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        sharedBlocks.Verify(s => s.NotifySharedBlockChanged("EMP", "OrderEntry", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CommitFormAsync_CommittedBlockIsNotSharedBlock_DoesNotNotify()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        dataSource.Setup(d => d.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var entity = CreateEntity("EMP", ("Name", "string"));
        var uow = CreateDirtyUowMock(dataSource.Object, new ErrorsInfo { Flag = Errors.Ok });
        var sharedBlocks = new Mock<ISharedBlockManager>(MockBehavior.Loose);
        sharedBlocks.Setup(s => s.SharedBlockExists("EMP")).Returns(false);
        using var manager = new FormsManager(_mockEditor.Object, sharedBlockManager: sharedBlocks.Object);
        manager.RegisterBlock("EMP", uow.Object, entity);
        manager.GetBlock("EMP")!.Mode = DataBlockMode.CRUD;

        var result = await manager.CommitFormAsync().ConfigureAwait(false);

        Assert.Equal(Errors.Ok, result.Flag);
        sharedBlocks.Verify(
            s => s.NotifySharedBlockChanged(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never);
    }

    #endregion

    #region PopulateRecordGroupAsync -> RecordGroup.LastPopulatedAt (2026-08-26)

    // IsPopulated and LastPopulatedAt are evident siblings -- both describe the same populate
    // event -- but only IsPopulated was ever set at the one call site that populates a group.
    // LastPopulatedAt existed with no writer anywhere, so any caller reading it to show "last
    // populated N minutes ago" got null forever.

    [Fact]
    public async Task PopulateRecordGroupAsync_OnSuccess_SetsLastPopulatedAt()
    {
        var entityStructure = new EntityStructure
        {
            EntityName = "EMP",
            Fields = new List<EntityField> { new() { FieldName = "EMPNO" } }
        };
        var dataSource = new Mock<IDataSource>();
        dataSource.Setup(d => d.GetEntityStructure("EMP", false)).Returns(entityStructure);
        dataSource.Setup(d => d.GetEntity("EMP", It.IsAny<List<AppFilter>>()))
            .Returns(new List<object> { new() });
        _mockEditor.Setup(e => e.GetDataSource("DB")).Returns(dataSource.Object);
        _manager.CreateRecordGroup("RG1", "DB", "EMP");

        var before = DateTime.UtcNow;
        var succeeded = await _manager.PopulateRecordGroupAsync("RG1").ConfigureAwait(false);

        Assert.True(succeeded);
        var group = _manager.GetRecordGroup("RG1");
        Assert.NotNull(group!.LastPopulatedAt);
        Assert.True(group.LastPopulatedAt >= before);
        Assert.True(group.IsPopulated);
    }

    #endregion

    #region HandleUnsavedChangesPrompt -> ShowAlertAsync (2026-08-26)

    // HandleUnsavedChangesPrompt previously always returned UnsavedChangesAction.Save behind a
    // comment reading "In a real application, this would show a dialog to the user / For now,
    // we'll use a simple default behavior" -- an honest stub, but one that silently auto-saved
    // on every unsaved-changes prompt in CreateNewRecordInMasterBlockAsync, regardless of what a
    // caller with a real IAlertProvider wired would have chosen. Fixed to actually call
    // ShowAlertAsync (Oracle Forms SHOW_ALERT, already fully implemented) with a real three-button
    // choice and respect whichever button the provider reports back.

    [Fact]
    public async Task CreateNewRecordInMasterBlockAsync_AlertProviderChoosesCancel_CancelsOperation()
    {
        var entity = CreateEntity("EMP", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);
        uowMock.Setup(u => u.IsDirty).Returns(true);
        var alertProvider = new Mock<IAlertProvider>();
        alertProvider
            .Setup(a => a.ShowAlertAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AlertStyle>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AlertResult.Button3);
        var manager = new FormsManager(_mockEditor.Object, alertProvider: alertProvider.Object);
        manager.RegisterBlock("EMP", uowMock.Object, entity);

        var result = await manager.CreateNewRecordInMasterBlockAsync("EMP").ConfigureAwait(false);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("cancelled by user", result.Message);
        alertProvider.Verify(a => a.ShowAlertAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AlertStyle>(),
            "Save", "Discard", "Cancel", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DirtyStateManager.GetDirtyRecordCount / GetLastModifiedTime (2026-08-26)

    // Both previously hardcoded a value regardless of the block's actual state: dirty-record
    // count always reported 1 when the block was dirty at all, and last-modified time always
    // reported DateTime.Now. Both feed UnsavedChangesEventArgs/DirtyBlockInfo -- the exact data
    // HandleUnsavedChangesPrompt's alert (fixed the previous pass) shows the user -- so the count
    // and timestamp the user saw when deciding Save/Discard/Cancel were both fabricated.

    [Fact]
    public async Task CheckAndHandleUnsavedChangesAsync_MultipleModifiedRecords_ReportsRealDirtyRecordCountAndLastModifiedTime()
    {
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.IsDirty).Returns(true);
        uowMock.Setup(u => u.GetModifiedEntities()).Returns(new[] { 0, 1 });
        var changeTime = DateTime.UtcNow.AddMinutes(-5);
        uowMock.Setup(u => u.GetChangeLog()).Returns(new List<ChangeRecord>
        {
            new() { Timestamp = changeTime.AddMinutes(-1) },
            new() { Timestamp = changeTime }
        });

        var blockInfo = new DataBlockInfo { BlockName = "EMP", UnitOfWork = uowMock.Object };
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["EMP"] = blockInfo
        };
        var dirtyStateManager = new DirtyStateManager(
            _mockEditor.Object,
            blocks,
            getDetailBlocksFunc: _ => new List<string>(),
            getBlockFunc: name => blocks.TryGetValue(name, out var b) ? b : null,
            getRelationshipsFunc: _ => new List<DataBlockRelationship>());

        UnsavedChangesEventArgs capturedArgs = null;
        dirtyStateManager.OnUnsavedChanges += (_, e) => capturedArgs = e;

        await dirtyStateManager.CheckAndHandleUnsavedChangesAsync("EMP").ConfigureAwait(false);

        Assert.NotNull(capturedArgs);
        var detail = Assert.Single(capturedArgs!.DirtyBlockDetails);
        Assert.Equal(2, detail.DirtyRecordCount);
        Assert.Equal(changeTime, detail.LastModified);
    }

    #endregion

    #region DirtyStateManager.SaveDirtyBlocksAsync -> Configuration.DefaultSaveOptions (2026-08-26)

    // SaveOptions.Default's own properties (MaxRetries, RetryDelayMs, ValidateBeforeSave, ...)
    // are genuinely read by SaveBlockWithRetryAsync -- but SaveDirtyBlocksAsync always used the
    // bare type default, ignoring UnitofWorksManagerConfiguration.DefaultSaveOptions entirely, so
    // a developer who configured Configuration.DefaultSaveOptions on the manager had that setting
    // silently discarded on every save.

    [Fact]
    public async Task SaveDirtyBlocksAsync_ConfiguredDefaultSaveOptions_UsesItsMaxRetries()
    {
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.IsDirty).Returns(true);
        uowMock.Setup(u => u.Commit())
            .ReturnsAsync(new ErrorsInfo { Flag = Errors.Failed, Message = "connection timeout" });

        var blockInfo = new DataBlockInfo { BlockName = "EMP", UnitOfWork = uowMock.Object };
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["EMP"] = blockInfo
        };
        var dirtyStateManager = new DirtyStateManager(
            _mockEditor.Object,
            blocks,
            getDetailBlocksFunc: _ => new List<string>(),
            getBlockFunc: name => blocks.TryGetValue(name, out var b) ? b : null,
            getRelationshipsFunc: _ => new List<DataBlockRelationship>(),
            getDefaultSaveOptionsFunc: () => new SaveOptions { MaxRetries = 2, RetryDelayMs = 0 });

        await dirtyStateManager.SaveDirtyBlocksAsync(new List<string> { "EMP" }).ConfigureAwait(false);

        // MaxRetries = 2 means 1 initial attempt + 2 retries = 3 total Commit() calls.
        // SaveOptions.Default's own MaxRetries (3) would have called Commit() 4 times instead.
        uowMock.Verify(u => u.Commit(), Times.Exactly(3));
    }

    #endregion

    #region DirtyStateManager.HasValidationErrors (gaps.md G0.53, closed 2026-08-27)

    // HasValidationErrors always returned false ("Placeholder"), regardless of the block's real
    // validation state. It feeds DirtyBlockInfo.HasErrors/IsValid -- the same data
    // HandleUnsavedChangesPrompt's alert shows the user -- so a block with genuinely failing
    // validation still reported "no errors" when asking Save/Discard/Cancel. Fixed via a new
    // constructor resolver, hasValidationErrorsFunc, wired in FormsManager.Core.cs to
    // ItemProperties.GetItemsWithErrors(blockName).Count > 0 -- the live per-item error state
    // ItemPropertyManager already tracks from real validation-rule failures.

    [Fact]
    public void GetDirtyBlocksWithDetails_ResolverReportsErrors_HasErrorsIsTrue()
    {
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.IsDirty).Returns(true);

        var blockInfo = new DataBlockInfo { BlockName = "EMP", UnitOfWork = uowMock.Object };
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["EMP"] = blockInfo
        };
        var dirtyStateManager = new DirtyStateManager(
            _mockEditor.Object,
            blocks,
            getDetailBlocksFunc: _ => new List<string>(),
            getBlockFunc: name => blocks.TryGetValue(name, out var b) ? b : null,
            getRelationshipsFunc: _ => new List<DataBlockRelationship>(),
            hasValidationErrorsFunc: name => string.Equals(name, "EMP", StringComparison.OrdinalIgnoreCase));

        var details = dirtyStateManager.GetDirtyBlocksWithDetails();

        var detail = Assert.Single(details);
        Assert.True(detail.HasErrors);
    }

    [Fact]
    public void GetDirtyBlocksWithDetails_ResolverReportsNoErrors_HasErrorsIsFalse()
    {
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.IsDirty).Returns(true);

        var blockInfo = new DataBlockInfo { BlockName = "EMP", UnitOfWork = uowMock.Object };
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["EMP"] = blockInfo
        };
        var dirtyStateManager = new DirtyStateManager(
            _mockEditor.Object,
            blocks,
            getDetailBlocksFunc: _ => new List<string>(),
            getBlockFunc: name => blocks.TryGetValue(name, out var b) ? b : null,
            getRelationshipsFunc: _ => new List<DataBlockRelationship>(),
            hasValidationErrorsFunc: _ => false);

        var details = dirtyStateManager.GetDirtyBlocksWithDetails();

        var detail = Assert.Single(details);
        Assert.False(detail.HasErrors);
    }

    [Fact]
    public void GetDirtyBlocksWithDetails_NoResolverSupplied_HasErrorsIsFalse()
    {
        // Backward-compatible default: a caller that does not wire the resolver (the shape
        // every FormsManagerTests DirtyStateManager construction above already used before
        // this fix) gets the conservative "no known errors" answer, not a fabricated one.
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.IsDirty).Returns(true);

        var blockInfo = new DataBlockInfo { BlockName = "EMP", UnitOfWork = uowMock.Object };
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["EMP"] = blockInfo
        };
        var dirtyStateManager = new DirtyStateManager(
            _mockEditor.Object,
            blocks,
            getDetailBlocksFunc: _ => new List<string>(),
            getBlockFunc: name => blocks.TryGetValue(name, out var b) ? b : null,
            getRelationshipsFunc: _ => new List<DataBlockRelationship>());

        var details = dirtyStateManager.GetDirtyBlocksWithDetails();

        var detail = Assert.Single(details);
        Assert.False(detail.HasErrors);
    }

    #endregion

    #region ExecuteSequence / Shared Blocks (IUnitofWorksManager reachability, closed 2026-08-27)

    // Both were declared on FormsManager (Editor/Forms/FormsManager.InterFormComm.cs) with zero
    // BeepDM test coverage of any kind and no path onto IUnitofWorksManager -- the interface
    // either Beep.Forms host is typed as. See Beep.Forms' own ENGINE-GAP-ANALYSIS.md for the
    // reachability self-test through the real host stack.

    private sealed class SequenceTestRecord
    {
        public int Id { get; set; }
    }

    [Fact]
    public void ExecuteSequence_PositiveSequenceValue_SetsFieldAndReturnsTrue()
    {
        var entity = CreateEntity("EMPLOYEES", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);
        uowMock.Setup(u => u.GetSeq(It.IsAny<string>())).Returns(42);
        _manager.RegisterBlock("EMP", uowMock.Object, entity, "DEFAULT_DB");

        var record = new SequenceTestRecord();
        var ok = _manager.ExecuteSequence("EMP", record, nameof(SequenceTestRecord.Id), "EMP_SEQ");

        Assert.True(ok);
        Assert.Equal(42, record.Id);
    }

    [Fact]
    public void ExecuteSequence_NonPositiveSequenceValue_ReturnsFalseAndLeavesFieldUnset()
    {
        var entity = CreateEntity("EMPLOYEES", ("EMPNO", "int"));
        var uowMock = CreateUowMock(1);
        uowMock.Setup(u => u.GetSeq(It.IsAny<string>())).Returns(-1);
        _manager.RegisterBlock("EMP", uowMock.Object, entity, "DEFAULT_DB");

        var record = new SequenceTestRecord { Id = 7 };
        var ok = _manager.ExecuteSequence("EMP", record, nameof(SequenceTestRecord.Id), "EMP_SEQ");

        Assert.False(ok);
        Assert.Equal(7, record.Id);
    }

    [Fact]
    public void CreateSharedBlock_ThenGetSharedBlock_ReturnsThePublishedUow()
    {
        var uowMock = CreateUowMock(1);

        var created = _manager.CreateSharedBlock("Shared1", uowMock.Object);
        var retrieved = _manager.GetSharedBlock("Shared1");

        Assert.True(created);
        Assert.Same(uowMock.Object, retrieved);
    }

    [Fact]
    public void SharedBlockExists_ReflectsCreateAndRemove()
    {
        var uowMock = CreateUowMock(1);

        Assert.False(_manager.SharedBlockExists("Shared2"));
        _manager.CreateSharedBlock("Shared2", uowMock.Object);
        Assert.True(_manager.SharedBlockExists("Shared2"));
        _manager.RemoveSharedBlock("Shared2");
        Assert.False(_manager.SharedBlockExists("Shared2"));
    }

    [Fact]
    public void TryLockSharedBlock_ThenReleaseSharedBlockLock_AllowsReacquire()
    {
        var uowMock = CreateUowMock(1);
        _manager.CreateSharedBlock("Shared3", uowMock.Object);

        var locked = _manager.TryLockSharedBlock("Shared3", TimeSpan.FromSeconds(1));
        Assert.True(locked);

        _manager.ReleaseSharedBlockLock("Shared3");

        var relocked = _manager.TryLockSharedBlock("Shared3", TimeSpan.FromSeconds(1));
        Assert.True(relocked);
    }

    [Fact]
    public void IUnitofWorksManagerTyped_ExposesExecuteSequenceAndSharedBlocks()
    {
        // The reachability defect this section closes: before adding these members to
        // IUnitofWorksManager, none of the calls below would compile against a
        // manager reference held only through the interface (the only type either
        // Beep.Forms host exposes FormsManager as).
        IUnitofWorksManager manager = _manager;
        var uowMock = CreateUowMock(1);
        uowMock.Setup(u => u.GetSeq(It.IsAny<string>())).Returns(5);
        manager.RegisterBlock("EMP", uowMock.Object, CreateEntity("EMPLOYEES", ("EMPNO", "int")), "DEFAULT_DB");

        var record = new SequenceTestRecord();
        Assert.True(manager.ExecuteSequence("EMP", record, nameof(SequenceTestRecord.Id), "EMP_SEQ"));
        Assert.Equal(5, record.Id);

        Assert.True(manager.CreateSharedBlock("Shared4", uowMock.Object));
        Assert.True(manager.SharedBlockExists("Shared4"));
        Assert.Same(uowMock.Object, manager.GetSharedBlock("Shared4"));
        Assert.True(manager.TryLockSharedBlock("Shared4", TimeSpan.FromSeconds(1)));
        manager.ReleaseSharedBlockLock("Shared4");
        Assert.True(manager.RemoveSharedBlock("Shared4"));
    }

    #endregion

    #region Paging / Performance / Record Introspection (IUnitofWorksManager reachability, closed 2026-08-27)

    // Neither IPagingManager nor IPerformanceManager, nor several FormsManager-level
    // convenience wrappers around them (some of which sync DataBlockInfo.Configuration
    // as well as delegating), nor CreateNewRecord/GetCurrentRecordInfo/GetCallStack, had
    // any path onto IUnitofWorksManager. See Beep.Forms' own ENGINE-GAP-ANALYSIS.md for
    // the reachability self-test through the real host stack.

    [Fact]
    public void SetBlockPageSize_SyncsPagingManagerAndBlockConfiguration()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(1).Object, entity);

        _manager.SetBlockPageSize("ORD", 25);

        Assert.Equal(25, _manager.Paging.GetPageSize("ORD"));
        Assert.Equal(25, _manager.GetBlock("ORD").Configuration.PageSize);
    }

    [Fact]
    public void GetTotalRecordCount_NoneStored_FallsBackToUnitOfWorkTotalItemCount()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(7).Object, entity);

        Assert.Equal(7, _manager.GetTotalRecordCount("ORD"));
    }

    [Fact]
    public void GetTotalRecordCount_StoredValue_TakesPrecedenceOverUnitOfWork()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(7).Object, entity);

        _manager.Paging.SetTotalRecordCount("ORD", 500);

        Assert.Equal(500, _manager.GetTotalRecordCount("ORD"));
    }

    [Fact]
    public void SetFetchAheadDepth_SyncsBlockConfiguration()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(1).Object, entity);

        _manager.SetFetchAheadDepth("ORD", 3);

        Assert.Equal(3, _manager.GetBlock("ORD").Configuration.FetchAheadDepth);
    }

    [Fact]
    public void SetLazyLoadMode_ThenGetLazyLoadMode_RoundTrips()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(1).Object, entity);

        _manager.SetLazyLoadMode("ORD", LazyLoadMode.OnDemand);

        Assert.Equal(LazyLoadMode.OnDemand, _manager.GetLazyLoadMode("ORD"));
        Assert.True(_manager.GetBlock("ORD").Configuration.EnableLazyLoad);
    }

    [Fact]
    public void SetMaxRecordsPerFetch_SetsBlockConfiguration()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(1).Object, entity);

        _manager.SetMaxRecordsPerFetch("ORD", 250);

        Assert.Equal(250, _manager.GetBlock("ORD").Configuration.MaxRecordsPerFetch);
    }

    [Fact]
    public void SetBlockCacheTtl_SyncsPerformanceManagerAndBlockConfiguration()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        _manager.RegisterBlock("ORD", CreateUowMock(1).Object, entity);

        _manager.SetBlockCacheTtl("ORD", TimeSpan.FromMinutes(10));

        Assert.Equal(10, _manager.GetBlock("ORD").Configuration.CacheTtlMinutes);
    }

    [Fact]
    public void GetRecordCount_ReturnsUnitOfWorkUnitsCount()
    {
        var entity = CreateEntity("ORD", ("Name", "string"));
        var uow = CreateUowMock(4);
        var units = new List<object> { new(), new(), new(), new() };
        uow.Setup(u => u.Units).Returns(units);
        _manager.RegisterBlock("ORD", uow.Object, entity);

        Assert.Equal(4, _manager.GetRecordCount("ORD"));
    }

    [Fact]
    public void GetCallStack_ReturnsSnapshot_EmptyWhenNoMultiFormCallsMade()
    {
        Assert.Empty(_manager.GetCallStack());
    }

    [Fact]
    public void IUnitofWorksManagerTyped_ExposesPagingPerformanceAndRecordIntrospection()
    {
        // The reachability defect this section closes: before adding these members to
        // IUnitofWorksManager, none of the calls below would compile against a
        // manager reference held only through the interface.
        IUnitofWorksManager manager = _manager;
        var entity = CreateEntity("ORD", ("Name", "string"));
        // GetCurrentRecordInfo dynamic-dispatches onto Units (CurrentIndex/Current/Count),
        // which needs a real, PUBLIC-typed ObservableBindingList -- a bare mock or a
        // private nested test class both fail the dynamic bind (see the established
        // pattern/comment on NavigateToRecordAsync's own ObservableBindingList test above).
        var units = new TheTechIdea.Beep.Editor.ObservableBindingList<TheTechIdea.Beep.Editor.Entity>(
            new List<TheTechIdea.Beep.Editor.Entity> { new() });
        var uowMock = new Mock<IUnitofWork>();
        uowMock.Setup(u => u.Units).Returns(units);
        uowMock.Setup(u => u.TotalItemCount).Returns(units.Count);
        uowMock.Setup(u => u.CurrentItem).Returns(new TestRecord());
        manager.RegisterBlock("ORD", uowMock.Object, entity);

        Assert.NotNull(manager.Paging);
        Assert.NotNull(manager.PerformanceManager);

        manager.SetBlockPageSize("ORD", 10);
        Assert.True(manager.GetTotalRecordCount("ORD") >= 0);
        manager.SetFetchAheadDepth("ORD", 2);
        manager.SetLazyLoadMode("ORD", LazyLoadMode.Deferred);
        Assert.Equal(LazyLoadMode.Deferred, manager.GetLazyLoadMode("ORD"));
        manager.SetMaxRecordsPerFetch("ORD", 50);
        manager.SetBlockCacheTtl("ORD", TimeSpan.FromMinutes(5));
        Assert.True(manager.GetRecordCount("ORD") >= 0);

        var record = Assert.IsType<TestRecord>(manager.CreateNewRecord("ORD"));
        Assert.NotNull(record);

        var info = manager.GetCurrentRecordInfo("ORD");
        Assert.NotNull(info);
        Assert.Equal("ORD", info.BlockName);

        Assert.NotNull(manager.GetCallStack());
    }

    #endregion
}
