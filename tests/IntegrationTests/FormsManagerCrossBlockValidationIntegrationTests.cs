using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace Assembly_helpers.IntegrationTests;

public class FormsManagerCrossBlockValidationIntegrationTests
{
    [Fact]
    public async Task CommitFormAsync_WhenCrossBlockRuleFails_DoesNotPersistDirtyBlocks()
    {
        var dirtyBlocks = new List<string> { "Orders", "OrderLines" };
        var saveCalls = 0;
        var isDirty = true;
        var manager = CreateFormsManager(
            dirtyBlocks,
            () => isDirty,
            value => isDirty = value,
            () => saveCalls++);

        var orderUnitOfWork = CreateCurrentRecordUnitOfWork(new OrderHeader { OrderId = 10, Total = 100m }, () => isDirty);
        var detailUnitOfWork = CreateCurrentRecordUnitOfWork(new OrderLineSummary { OrderId = 10, Total = 125m }, () => isDirty);

        manager.RegisterBlock("Orders", orderUnitOfWork.Object, CreateEntityStructure("Orders", "OrderId", "Total"));
        manager.RegisterBlock("OrderLines", detailUnitOfWork.Object, CreateEntityStructure("OrderLines", "OrderId", "Total"));
        await manager.OpenFormAsync("OrderForm");

        manager.RegisterCrossBlockRule(new CrossBlockValidationRule
        {
            RuleName = "DetailTotalMustNotExceedMaster",
            BlockA = "Orders",
            BlockB = "OrderLines",
            Validator = (master, detail) =>
            {
                var order = (OrderHeader)master.CurrentItem;
                var lines = (OrderLineSummary)detail.CurrentItem;
                return lines.Total > order.Total
                    ? "Detail total exceeds master total."
                    : null;
            }
        });

        var result = await manager.CommitFormAsync();

        result.Flag.Should().Be(Errors.Failed);
        result.Message.Should().Contain("Cross-block validation failed");
        result.Message.Should().Contain("Detail total exceeds master total");
        saveCalls.Should().Be(0);
        manager.GetDirtyBlocks().Should().Equal("Orders", "OrderLines");
        manager.IsDirty.Should().BeTrue();
    }

    private static FormsManager CreateFormsManager(
        List<string> dirtyBlocks,
        System.Func<bool> getIsDirty,
        System.Action<bool> setIsDirty,
        System.Action onSave)
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration
        {
            ValidateBeforeCommit = false
        });

        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager.Setup(manager => manager.GetDirtyBlocks()).Returns(() => new List<string>(dirtyBlocks));
        dirtyStateManager.Setup(manager => manager.SaveDirtyBlocksAsync(It.IsAny<List<string>>())).ReturnsAsync(() =>
        {
            onSave();
            dirtyBlocks.Clear();
            setIsDirty(false);
            return true;
        });
        dirtyStateManager.Setup(manager => manager.RollbackDirtyBlocksAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
        dirtyStateManager.Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>())).ReturnsAsync(true);

        return new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);
    }

    private static Mock<IUnitofWork> CreateCurrentRecordUnitOfWork<T>(T currentRecord, System.Func<bool> isDirtyProvider)
        where T : class, INotifyPropertyChanged, new()
    {
        var units = new ObservableBindingList<T>(new List<T> { currentRecord });
        units.CurrentIndex = 0;

        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(() => isDirtyProvider());
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(T));
        unitOfWork.Setup(work => work.Clear()).Callback(() => units.Clear());
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => units.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => units.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => units.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => units.MoveLast());
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => units.MoveTo(index));

        return unitOfWork;
    }

    private static EntityStructure CreateEntityStructure(string entityName, params string[] fieldNames)
    {
        var entity = new EntityStructure
        {
            EntityName = entityName,
            DatasourceEntityName = entityName,
            Fields = new List<EntityField>()
        };

        foreach (var fieldName in fieldNames)
        {
            entity.Fields.Add(new EntityField { FieldName = fieldName });
        }

        return entity;
    }

    public class OrderHeader : INotifyPropertyChanged
    {
        private int _orderId;
        private decimal _total;

        public int OrderId
        {
            get => _orderId;
            set => SetField(ref _orderId, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetField(ref _total, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class OrderLineSummary : INotifyPropertyChanged
    {
        private int _orderId;
        private decimal _total;

        public int OrderId
        {
            get => _orderId;
            set => SetField(ref _orderId, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetField(ref _total, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}