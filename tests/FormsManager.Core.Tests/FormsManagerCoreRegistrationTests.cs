using System;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using Xunit;
using FormsManagerClass = TheTechIdea.Beep.Editor.UOWManager.FormsManager;

namespace FormsManagerCore.Tests;

public class FormsManagerCoreRegistrationTests
{
    [Fact]
    public void RegisterBlock_GenericOverload_StoresClrEntityType()
    {
        var manager = CreateManager();
        var unitOfWork = CreateUnitOfWork();

        manager.RegisterBlock<CustomerRecord>("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));

        var block = manager.GetBlock<CustomerRecord>("Customers");

        block.Should().NotBeNull();
        block!.EntityType.Should().Be(typeof(CustomerRecord));
    }

    [Fact]
    public void RegisterBlock_NonGeneric_InfersClrEntityTypeFromUnitOfWork()
    {
        var manager = CreateManager();
        var unitOfWork = CreateUnitOfWork(typeof(CustomerRecord));

        manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));

        var block = manager.GetBlock("Customers");

        block.Should().NotBeNull();
        block!.EntityType.Should().Be(typeof(CustomerRecord));
    }

    [Fact]
    public void RegisterBlock_NonGeneric_UsesEntityStructureFromUnitOfWork_WhenNotPassedExplicitly()
    {
        var manager = CreateManager();
        var structure = CreateEntityStructure("Customers");
        var unitOfWork = CreateUnitOfWork(typeof(CustomerRecord), structure);

        manager.RegisterBlock("Customers", unitOfWork.Object);

        var block = manager.GetBlock("Customers");

        block.Should().NotBeNull();
        block!.EntityStructure.Should().BeSameAs(structure);
        block.EntityStructure.EntityName.Should().Be("Customers");
        block.EntityStructure.Fields.Should().NotBeNull();
    }

    [Fact]
    public void CreateNewRecord_UsesResolvedClrType_AndDoesNotFallBackToDynamicObjects()
    {
        var manager = CreateManager();
        var unitOfWork = CreateUnitOfWork(typeof(CustomerRecord));

        manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));

        var record = manager.CreateNewRecord("Customers");

        record.Should().BeOfType<CustomerRecord>();
        var typedRecord = (CustomerRecord)record!;
        typedRecord.CreatedBy.Should().Be(Environment.UserName);
        typedRecord.ModifiedBy.Should().Be(Environment.UserName);
        typedRecord.CreatedDate.Should().NotBe(default);
        typedRecord.ModifiedDate.Should().NotBe(default);
    }

    [Fact]
    public void CreateNewRecord_ReturnsNull_WhenClrTypeCannotBeResolved()
    {
        var manager = CreateManager();
        var unitOfWork = CreateUnitOfWork();

        manager.RegisterBlock("Adhoc", unitOfWork.Object, CreateEntityStructure("Missing.Type.Name"));

        var record = manager.CreateNewRecord("Adhoc");

        record.Should().BeNull();
        manager.Status.Should().ContainEquivalentOf("no CLR entity type");
    }

    private static FormsManagerClass CreateManager()
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(p => p.Configuration, new UnitofWorksManagerConfiguration());

        return new FormsManagerClass(editor.Object, configurationManager: configurationManager.Object);
    }

    private static Mock<IUnitofWork> CreateUnitOfWork(Type? entityType = null, EntityStructure? entityStructure = null)
    {
        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.EntityType).Returns(entityType);
        unitOfWork.SetupGet(work => work.EntityStructure).Returns(entityStructure);
        return unitOfWork;
    }

    private static EntityStructure CreateEntityStructure(string entityName)
    {
        return new EntityStructure
        {
            EntityName = entityName,
            DatasourceEntityName = entityName,
            Fields = new System.Collections.Generic.List<EntityField>()
        };
    }

    private sealed class CustomerRecord
    {
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? Name { get; set; }
    }
}