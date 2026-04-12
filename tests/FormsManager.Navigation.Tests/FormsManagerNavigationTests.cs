using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace FormsManagerNavigation.Tests;

public class FormsManagerNavigationTests
{
    [Fact]
    public async Task NextRecordAsync_RecordsPreviousIndexInNavigationHistory()
    {
        var manager = CreateFormsManager();
        var (unitOfWork, _) = CreateUnitOfWork(
            new TestRecord { Id = 1, Name = "Alpha" },
            new TestRecord { Id = 2, Name = "Beta" },
            new TestRecord { Id = 3, Name = "Gamma" });

        manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));

        var success = await manager.NextRecordAsync("Customers");

        success.Should().BeTrue();
        manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(1);
        manager.CanNavigateBack("Customers").Should().BeTrue();
        manager.GetNavigationHistory("Customers").Select(entry => entry.RecordIndex).Should().Equal(0);
    }

    [Fact]
    public async Task NavigateBackAndForwardAsync_PreserveHistoryStacks()
    {
        var manager = CreateFormsManager();
        var (unitOfWork, _) = CreateUnitOfWork(
            new TestRecord { Id = 1, Name = "Alpha" },
            new TestRecord { Id = 2, Name = "Beta" },
            new TestRecord { Id = 3, Name = "Gamma" });

        manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));
        await manager.NextRecordAsync("Customers");
        await manager.NextRecordAsync("Customers");

        var backSuccess = await manager.NavigateBackAsync("Customers");

        backSuccess.Should().BeTrue();
        manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(1);
        manager.CanNavigateForward("Customers").Should().BeTrue();

        var forwardSuccess = await manager.NavigateForwardAsync("Customers");

        forwardSuccess.Should().BeTrue();
        manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(2);
        manager.GetNavigationHistory("Customers").Select(entry => entry.RecordIndex).Should().Equal(0, 1);
    }

    [Fact]
    public async Task NavigateToRecordAsync_RecordsPreviousIndexInNavigationHistory()
    {
        var manager = CreateFormsManager();
        var (unitOfWork, _) = CreateUnitOfWork(
            new TestRecord { Id = 1, Name = "Alpha" },
            new TestRecord { Id = 2, Name = "Beta" },
            new TestRecord { Id = 3, Name = "Gamma" });

        manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));

        var success = await manager.NavigateToRecordAsync("Customers", 2);

        success.Should().BeTrue();
        manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(2);
        manager.CanNavigateBack("Customers").Should().BeTrue();
        manager.GetNavigationHistory("Customers").Select(entry => entry.RecordIndex).Should().Equal(0);
    }

    [Fact]
    public async Task NavigateToRecordAsync_HonorsValidateBeforeNavigation()
    {
        var manager = CreateFormsManager(validateBeforeNavigation: true);
        var (unitOfWork, _) = CreateUnitOfWork(
            new TestRecord { Id = 1, Name = string.Empty },
            new TestRecord { Id = 2, Name = "Beta" });

        manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));
        manager.Validation.RegisterRule(new ValidationRule
        {
            RuleName = "CustomerNameRequired",
            BlockName = "Customers",
            ItemName = "Name",
            ValidationType = ValidationType.Required,
            Timing = ValidationTiming.Manual
        });

        var success = await manager.NavigateToRecordAsync("Customers", 1);

        success.Should().BeFalse();
        manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(0);
        manager.CanNavigateBack("Customers").Should().BeFalse();
    }

    private static FormsManager CreateFormsManager(bool validateBeforeNavigation = false)
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration
        {
            Navigation = new NavigationConfiguration
            {
                ValidateBeforeNavigation = validateBeforeNavigation
            }
        });

        return new FormsManager(editor.Object, configurationManager: configurationManager.Object);
    }

    private static (Mock<IUnitofWork> UnitOfWork, ObservableBindingList<TestRecord> Units) CreateUnitOfWork(params TestRecord[] records)
    {
        var units = new ObservableBindingList<TestRecord>(records);
        if (units.Count > 0)
        {
            units.CurrentIndex = 0;
        }

        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(TestRecord));
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => units.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => units.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => units.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => units.MoveLast());
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => units.MoveTo(index));

        return (unitOfWork, units);
    }

    private static EntityStructure CreateEntityStructure(string entityName)
    {
        return new EntityStructure
        {
            EntityName = entityName,
            DatasourceEntityName = entityName,
            Fields = new List<EntityField>()
        };
    }

    public class TestRecord : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
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