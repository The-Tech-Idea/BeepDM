using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using Xunit;

namespace Assembly_helpers.IntegrationTests;

public class FormsManagerExportImportIntegrationTests
{
    [Theory]
    [InlineData("json")]
    [InlineData("csv")]
    public async Task ExportThenImport_RoundTripsBlockData_ForSupportedFormats(string format)
    {
        var manager = CreateFormsManager();
        var sourceRecords = new ObservableBindingList<ProductRecord>(new[]
        {
            new ProductRecord { Id = 1, Name = "Widget", Price = 12.50m },
            new ProductRecord { Id = 2, Name = "Gadget", Price = 8.75m }
        });
        sourceRecords.CurrentIndex = 0;

        var targetRecords = new ObservableBindingList<ProductRecord>();

        var sourceUnitOfWork = CreateTransferUnitOfWork(sourceRecords);
        ConfigureExport(sourceUnitOfWork, sourceRecords);

        var targetUnitOfWork = CreateTransferUnitOfWork(targetRecords);
        ConfigureImport(targetUnitOfWork, targetRecords);

        manager.RegisterBlock("SourceProducts", sourceUnitOfWork.Object, CreateEntityStructure("SourceProducts", "Id", "Name", "Price"));
        manager.RegisterBlock("ImportedProducts", targetUnitOfWork.Object, CreateEntityStructure("ImportedProducts", "Id", "Name", "Price"));

        using var stream = new MemoryStream();
        int importedCount;

        if (string.Equals(format, "json", System.StringComparison.OrdinalIgnoreCase))
        {
            await manager.ExportBlockToJsonAsync("SourceProducts", stream);
            stream.Position = 0;
            importedCount = await manager.ImportBlockFromJsonAsync("ImportedProducts", stream);
        }
        else
        {
            await manager.ExportBlockToCsvAsync("SourceProducts", stream);
            stream.Position = 0;
            importedCount = await manager.ImportBlockFromCsvAsync("ImportedProducts", stream);
        }

        importedCount.Should().Be(2);
        targetRecords.Should().HaveCount(2);
        targetRecords.Select(record => (record.Id, record.Name, record.Price))
            .Should().Equal(sourceRecords.Select(record => (record.Id, record.Name, record.Price)));
    }

    private static FormsManager CreateFormsManager()
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration());

        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager.Setup(manager => manager.GetDirtyBlocks()).Returns(new List<string>());
        dirtyStateManager.Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>())).ReturnsAsync(true);

        return new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);
    }

    private static Mock<IUnitofWork> CreateTransferUnitOfWork(ObservableBindingList<ProductRecord> records)
    {
        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => records);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => records.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => records.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(ProductRecord));
        unitOfWork.Setup(work => work.Clear()).Callback(() => records.Clear());
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => records.MoveTo(index));
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => records.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => records.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => records.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => records.MoveLast());
        return unitOfWork;
    }

    private static void ConfigureExport(Mock<IUnitofWork> unitOfWork, ObservableBindingList<ProductRecord> records)
    {
        unitOfWork.As<IExportable>()
            .Setup(exportable => exportable.ToDataTable())
            .Returns(() => records.ToDataTable("Products"));

        unitOfWork.As<IExportable>()
            .Setup(exportable => exportable.ToJsonAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>(async (stream, ct) =>
            {
                await JsonSerializer.SerializeAsync(stream, records.ToList(), cancellationToken: ct);
                await stream.FlushAsync(ct);
            });

        unitOfWork.As<IExportable>()
            .Setup(exportable => exportable.ToCsvAsync(It.IsAny<Stream>(), It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, char, CancellationToken>(async (stream, delimiter, ct) =>
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
                await writer.WriteLineAsync($"Id{delimiter}Name{delimiter}Price");
                foreach (var record in records)
                {
                    await writer.WriteLineAsync($"{record.Id}{delimiter}{record.Name}{delimiter}{record.Price.ToString(CultureInfo.InvariantCulture)}");
                }

                await writer.FlushAsync(ct);
            });
    }

    private static void ConfigureImport(Mock<IUnitofWork> unitOfWork, ObservableBindingList<ProductRecord> records)
    {
        unitOfWork.As<IImportable>()
            .Setup(importable => importable.LoadFromJsonAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, bool, CancellationToken>(async (stream, clearFirst, ct) =>
            {
                if (clearFirst)
                {
                    records.Clear();
                }

                var imported = await JsonSerializer.DeserializeAsync<List<ProductRecord>>(stream, cancellationToken: ct) ?? new List<ProductRecord>();
                foreach (var record in imported)
                {
                    records.Add(record);
                }

                if (records.Count > 0)
                {
                    records.CurrentIndex = 0;
                }

                return imported.Count;
            });

        unitOfWork.As<IImportable>()
            .Setup(importable => importable.LoadFromCsvAsync(It.IsAny<Stream>(), It.IsAny<char>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, char, bool, bool, CancellationToken>(async (stream, delimiter, clearFirst, hasHeaderRow, ct) =>
            {
                if (clearFirst)
                {
                    records.Clear();
                }

                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var content = await reader.ReadToEndAsync(ct);
                var lines = content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                var startIndex = hasHeaderRow ? 1 : 0;
                var importedCount = 0;

                for (var lineIndex = startIndex; lineIndex < lines.Length; lineIndex++)
                {
                    var columns = lines[lineIndex].Split(delimiter);
                    records.Add(new ProductRecord
                    {
                        Id = int.Parse(columns[0], CultureInfo.InvariantCulture),
                        Name = columns[1],
                        Price = decimal.Parse(columns[2], CultureInfo.InvariantCulture)
                    });
                    importedCount++;
                }

                if (records.Count > 0)
                {
                    records.CurrentIndex = 0;
                }

                return importedCount;
            });
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

    public class ProductRecord : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private decimal _price;

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

        public decimal Price
        {
            get => _price;
            set => SetField(ref _price, value);
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