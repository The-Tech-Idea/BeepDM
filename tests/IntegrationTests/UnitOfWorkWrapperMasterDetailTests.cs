using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Report;
using Xunit;

namespace Assembly_helpers.IntegrationTests
{
    public class UnitOfWorkWrapperMasterDetailTests
    {
        [Fact]
        public void RegisterDetail_ShouldEstablishBidirectionalMasterDetailMetadata()
        {
            var masterBacking = new FakeWrappedUnitOfWork<MasterRecord>();
            var detailBacking = new FakeWrappedUnitOfWork<DetailRecord>();
            var masterWrapper = new UnitOfWorkWrapper(masterBacking);
            var detailWrapper = new UnitOfWorkWrapper(detailBacking);

            masterWrapper.RegisterDetail(detailWrapper, "Id", "ParentId");

            masterWrapper.DetailUnitOfWorks.Should().ContainSingle().Which.Should().BeSameAs(detailWrapper);
            detailWrapper.MasterUnitOfWork.Should().BeSameAs(masterWrapper);
            detailWrapper.MasterKeyField.Should().Be("Id");
            detailWrapper.ForeignKeyField.Should().Be("ParentId");
        }

        [Fact]
        public async Task SynchronizeDetailsAsync_ShouldQueryDetailUsingMasterKeyFilter()
        {
            var masterBacking = new FakeWrappedUnitOfWork<MasterRecord>
            {
                CurrentItem = new MasterRecord { Id = 42 }
            };
            var detailBacking = new FakeWrappedUnitOfWork<DetailRecord>();
            var masterWrapper = new UnitOfWorkWrapper(masterBacking);
            var detailWrapper = new UnitOfWorkWrapper(detailBacking);

            masterWrapper.RegisterDetail(detailWrapper, "Id", "ParentId");

            await masterWrapper.SynchronizeDetailsAsync();

            detailBacking.GetCallCount.Should().Be(1);
            detailBacking.ClearCallCount.Should().Be(0);
            detailBacking.LastFilters.Should().ContainSingle();
            detailBacking.LastFilters[0].FieldName.Should().Be("ParentId");
            detailBacking.LastFilters[0].Operator.Should().Be("=");
            detailBacking.LastFilters[0].FilterValue.Should().Be("42");
        }

        [Fact]
        public async Task SynchronizeDetailsAsync_ShouldClearDetailWhenMasterRecordIsMissing()
        {
            var masterBacking = new FakeWrappedUnitOfWork<MasterRecord>();
            var detailBacking = new FakeWrappedUnitOfWork<DetailRecord>();
            var masterWrapper = new UnitOfWorkWrapper(masterBacking);
            var detailWrapper = new UnitOfWorkWrapper(detailBacking);

            masterWrapper.RegisterDetail(detailWrapper, "Id", "ParentId");

            await masterWrapper.SynchronizeDetailsAsync();

            detailBacking.ClearCallCount.Should().Be(1);
            detailBacking.GetCallCount.Should().Be(0);
        }

        [Fact]
        public void ApplyMasterValueToCurrentItem_ShouldCopyMasterKeyIntoDetailForeignKey()
        {
            var masterBacking = new FakeWrappedUnitOfWork<MasterRecord>
            {
                CurrentItem = new MasterRecord { Id = 77 }
            };
            var detailBacking = new FakeWrappedUnitOfWork<DetailRecord>
            {
                CurrentItem = new DetailRecord { ParentId = 0 }
            };
            var masterWrapper = new UnitOfWorkWrapper(masterBacking);
            var detailWrapper = new UnitOfWorkWrapper(detailBacking);

            masterWrapper.RegisterDetail(detailWrapper, "Id", "ParentId");

            var applied = detailWrapper.ApplyMasterValueToCurrentItem();

            applied.Should().BeTrue();
            detailBacking.CurrentItem.ParentId.Should().Be(77);
        }

        public sealed class FakeWrappedUnitOfWork<TEntity>
            where TEntity : Entity
        {
            public TEntity CurrentItem { get; set; }

            public int GetCallCount { get; private set; }

            public int ClearCallCount { get; private set; }

            public List<AppFilter> LastFilters { get; private set; }

            public Task<List<TEntity>> Get(List<AppFilter> filters)
            {
                GetCallCount++;
                LastFilters = filters?.ToList() ?? new List<AppFilter>();
                return Task.FromResult(new List<TEntity>());
            }

            public void Clear()
            {
                ClearCallCount++;
            }
        }

        public sealed class MasterRecord : Entity
        {
            public int Id { get; set; }
        }

        public sealed class DetailRecord : Entity
        {
            public int ParentId { get; set; }
        }
    }

}