using System.Collections.Generic;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace Assembly_helpers.IntegrationTests
{
    public class MasterDetailKeyResolverTests
    {
        private readonly MasterDetailKeyResolver _resolver = new();

        [Fact]
        public void Resolve_UsesExplicitMapping_WhenProvided()
        {
            var masterBlock = CreateBlock("Orders", CreateEntity("Orders", primaryKeyString: "OrderId", fieldNames: new[] { "OrderId" }));
            var detailBlock = CreateBlock("OrderLines", CreateEntity("OrderLines", fieldNames: new[] { "OrderId" }));

            var resolution = _resolver.Resolve(masterBlock, detailBlock, "OrderId", "OrderId");

            resolution.IsResolved.Should().BeTrue();
            resolution.Source.Should().Be(MasterDetailKeyResolutionSource.ExplicitConfiguration);
            resolution.MasterKeyField.Should().Be("OrderId");
            resolution.DetailForeignKeyField.Should().Be("OrderId");
        }

        [Fact]
        public void Resolve_FiltersEntityRelations_ByMasterEntity()
        {
            var masterBlock = CreateBlock("Orders", CreateEntity("Orders", primaryKeyString: "OrderId", fieldNames: new[] { "OrderId" }));
            var detailEntity = CreateEntity(
                "OrderLines",
                relations: new List<RelationShipKeys>
                {
                    new RelationShipKeys
                    {
                        RalationName = "FK_OrderLine_Order",
                        RelatedEntityID = "Orders",
                        RelatedEntityColumnID = "OrderId",
                        EntityColumnID = "OrderId"
                    },
                    new RelationShipKeys
                    {
                        RalationName = "FK_OrderLine_Product",
                        RelatedEntityID = "Products",
                        RelatedEntityColumnID = "ProductId",
                        EntityColumnID = "ProductId"
                    }
                },
                fieldNames: new[] { "OrderId", "ProductId" });
            var detailBlock = CreateBlock("OrderLines", detailEntity);

            var resolution = _resolver.Resolve(masterBlock, detailBlock);

            resolution.IsResolved.Should().BeTrue();
            resolution.Source.Should().Be(MasterDetailKeyResolutionSource.EntityRelations);
            resolution.MasterKeyField.Should().Be("OrderId");
            resolution.DetailForeignKeyField.Should().Be("OrderId");
        }

        [Fact]
        public void Resolve_UsesDataSourceForeignKeys_WhenEntityRelationsAreMissing()
        {
            var masterBlock = CreateBlock("Orders", CreateEntity("Orders", primaryKeyString: "OrderId", fieldNames: new[] { "OrderId" }));
            var detailBlock = CreateBlock(
                "OrderLines",
                CreateEntity("OrderLines", fieldNames: new[] { "OrderId" }),
                dataSourceRelations: new List<RelationShipKeys>
                {
                    new RelationShipKeys
                    {
                        RalationName = "FK_OrderLine_Order",
                        RelatedEntityID = "Orders",
                        RelatedEntityColumnID = "OrderId",
                        EntityColumnID = "OrderId"
                    }
                });

            var resolution = _resolver.Resolve(masterBlock, detailBlock);

            resolution.IsResolved.Should().BeTrue();
            resolution.Source.Should().Be(MasterDetailKeyResolutionSource.DataSourceForeignKeys);
            resolution.MasterKeyField.Should().Be("OrderId");
            resolution.DetailForeignKeyField.Should().Be("OrderId");
        }

        [Fact]
        public void Resolve_FallsBackToMatchingPrimaryKeyNames_WhenMetadataIsMissing()
        {
            var masterBlock = CreateBlock(
                "Users",
                CreateEntity(
                    "Users",
                    primaryKeyString: "TenantId, UserId",
                    fieldNames: new[] { "TenantId", "UserId", "DisplayName" }));
            var detailBlock = CreateBlock(
                "UserPreferences",
                CreateEntity(
                    "UserPreferences",
                    fieldNames: new[] { "TenantId", "UserId", "Theme" }));

            var resolution = _resolver.Resolve(masterBlock, detailBlock);

            resolution.IsResolved.Should().BeTrue();
            resolution.Source.Should().Be(MasterDetailKeyResolutionSource.MatchingPrimaryKeyNames);
            resolution.MasterKeyField.Should().Be("TenantId, UserId");
            resolution.DetailForeignKeyField.Should().Be("TenantId, UserId");
        }

        [Fact]
        public void Resolve_Fails_WhenMultipleNamedRelationsMatchTheSameMaster()
        {
            var masterBlock = CreateBlock("Users", CreateEntity("Users", primaryKeyString: "UserId", fieldNames: new[] { "UserId" }));
            var detailBlock = CreateBlock(
                "Documents",
                CreateEntity(
                    "Documents",
                    relations: new List<RelationShipKeys>
                    {
                        new RelationShipKeys
                        {
                            RalationName = "FK_Documents_CreatedBy",
                            RelatedEntityID = "Users",
                            RelatedEntityColumnID = "UserId",
                            EntityColumnID = "CreatedByUserId"
                        },
                        new RelationShipKeys
                        {
                            RalationName = "FK_Documents_ApprovedBy",
                            RelatedEntityID = "Users",
                            RelatedEntityColumnID = "UserId",
                            EntityColumnID = "ApprovedByUserId"
                        }
                    },
                    fieldNames: new[] { "CreatedByUserId", "ApprovedByUserId" }));

            var resolution = _resolver.Resolve(masterBlock, detailBlock);

            resolution.IsResolved.Should().BeFalse();
            resolution.ErrorMessage.Should().Contain("Multiple candidate relationships");
        }

        private static DataBlockInfo CreateBlock(
            string blockName,
            EntityStructure entityStructure,
            IEnumerable<RelationShipKeys> dataSourceRelations = null)
        {
            var unitOfWork = new Mock<IUnitofWork>();

            if (dataSourceRelations != null)
            {
                var dataSource = new Mock<IDataSource>();
                dataSource
                    .Setup(source => source.GetEntityforeignkeys(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(new List<RelationShipKeys>(dataSourceRelations));
                unitOfWork.SetupProperty(work => work.DataSource, dataSource.Object);
            }

            return new DataBlockInfo
            {
                BlockName = blockName,
                EntityStructure = entityStructure,
                UnitOfWork = unitOfWork.Object
            };
        }

        private static EntityStructure CreateEntity(
            string entityName,
            string primaryKeyString = null,
            List<RelationShipKeys> relations = null,
            string[] fieldNames = null)
        {
            var entity = new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                PrimaryKeyString = primaryKeyString,
                Relations = relations ?? new List<RelationShipKeys>(),
                Fields = new List<EntityField>()
            };

            if (fieldNames != null)
            {
                foreach (var fieldName in fieldNames)
                {
                    entity.Fields.Add(new EntityField { FieldName = fieldName });
                }
            }

            if (!string.IsNullOrWhiteSpace(primaryKeyString))
            {
                entity.PrimaryKeys = new List<EntityField>();
                foreach (var primaryKey in primaryKeyString.Split(','))
                {
                    entity.PrimaryKeys.Add(new EntityField { FieldName = primaryKey.Trim() });
                }
            }

            return entity;
        }
    }
}