using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using Xunit;

namespace FormsManagerFormOperations.Tests;

public class FormsManagerFormOperationsTests
{
    [Fact]
    public async Task OpenFormAsync_SetsCurrentFormName()
    {
        var manager = CreateFormsManager().Manager;

        var success = await manager.OpenFormAsync("OrderEntry");

        success.Should().BeTrue();
        manager.CurrentFormName.Should().Be("OrderEntry");
    }

    [Fact]
    public async Task CloseFormAsync_WhenFormIsClean_ClearsCurrentFormName()
    {
        var manager = CreateFormsManager().Manager;
        await manager.OpenFormAsync("OrderEntry");

        var success = await manager.CloseFormAsync();

        success.Should().BeTrue();
        manager.CurrentFormName.Should().BeNull();
    }

    [Fact]
    public async Task CloseFormAsync_WhenUnsavedChangesHandlingFails_KeepsFormOpen()
    {
        var context = CreateFormsManager();
        context.DirtyStateManager
            .Setup(manager => manager.GetDirtyBlocks())
            .Returns(new List<string> { "Customers" });
        context.DirtyStateManager
            .Setup(manager => manager.CheckAndHandleUnsavedChangesAsync("OrderEntry"))
            .ReturnsAsync(false);

        var manager = context.Manager;
        await manager.OpenFormAsync("OrderEntry");
        manager.RegisterBlock("Customers", CreateUnitOfWork(isDirty: true).Object, CreateEntityStructure("Customers"));

        var success = await manager.CloseFormAsync();

        success.Should().BeFalse();
        manager.CurrentFormName.Should().Be("OrderEntry");
        context.DirtyStateManager.Verify(state => state.CheckAndHandleUnsavedChangesAsync("OrderEntry"), Times.Once);
    }

    [Fact]
    public async Task CommitFormAsync_WhenNoDirtyBlocks_ReturnsNoChangesMessage()
    {
        var context = CreateFormsManager(configuration => configuration.ValidateBeforeCommit = false);
        var manager = context.Manager;

        var result = await manager.CommitFormAsync();

        result.Message.Should().Be("No changes to commit");
        result.Flag.Should().Be(Errors.Ok);
        context.DirtyStateManager.Verify(state => state.SaveDirtyBlocksAsync(It.IsAny<List<string>>()), Times.Never);
    }

    private static (FormsManager Manager, Mock<IDirtyStateManager> DirtyStateManager) CreateFormsManager(
        System.Action<UnitofWorksManagerConfiguration>? configure = null)
    {
        var editor = new Mock<IDMEEditor>();
        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager
            .Setup(manager => manager.GetDirtyBlocks())
            .Returns(new List<string>());
        var configurationManager = new Mock<IConfigurationManager>();
        var configuration = new UnitofWorksManagerConfiguration();
        configure?.Invoke(configuration);
        configurationManager.SetupProperty(manager => manager.Configuration, configuration);

        var manager = new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);

        return (manager, dirtyStateManager);
    }

    private static Mock<IUnitofWork> CreateUnitOfWork(bool isDirty)
    {
        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.IsDirty).Returns(isDirty);
        return unitOfWork;
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
}