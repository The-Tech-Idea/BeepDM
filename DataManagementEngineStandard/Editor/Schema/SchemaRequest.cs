using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Input contract for <see cref="ISchemaManager"/>.
    /// Datasource-agnostic. Any pair of <see cref="IDataSource"/> can be source or target.
    /// </summary>
    public sealed class SchemaRequest
    {
        public string RequestId { get; init; } = Guid.NewGuid().ToString("N");

        // Source side
        public string SourceDataSourceName { get; init; } = string.Empty;
        public string SourceEntityName     { get; init; } = string.Empty;

        // Destination side
        public string DestinationDataSourceName { get; init; } = string.Empty;
        public string DestinationEntityName     { get; init; } = string.Empty;

        // Behaviour
        public bool AddMissingColumns { get; init; }
        public bool CreateDestinationIfNotExists { get; init; }
        public object? Mapping { get; init; }
        public IReadOnlyList<object>? SourceFilters { get; init; }

        // Optional pre-resolved handles
        public IDataSource? SourceData           { get; init; }
        public IDataSource? DestinationData      { get; init; }
        public EntityStructure? SourceEntityStructure    { get; init; }
        public EntityStructure? DestinationEntityStructure { get; init; }
    }

    /// <summary>Output of a preflight run.</summary>
    public sealed class SchemaPreflightResult
    {
        public IErrorsInfo Status { get; init; } = new ErrorsInfo { Flag = Errors.Ok, Message = "Preflight not run." };
        public bool SourceResolved       { get; init; }
        public bool SourceConnected      { get; init; }
        public bool DestinationResolved  { get; init; }
        public bool DestinationConnected { get; init; }
        public bool SourceStructureLoaded     { get; init; }
        public bool DestinationStructureLoaded { get; init; }
        public bool DestinationExisted    { get; init; }
        public IReadOnlyList<string> MissingDestinationFields { get; init; } = new List<string>();
        public SchemaSnapshot? DestinationSnapshot { get; init; }
    }

    /// <summary>Output of <see cref="ISchemaManager.BuildSyncDraftAsync"/>.</summary>
    public sealed class SchemaDraftResult
    {
        public DataSyncSchema? Draft { get; init; }
        public IErrorsInfo Status { get; init; } = new ErrorsInfo { Flag = Errors.Ok, Message = "Draft not built." };
    }

    /// <summary>Output of <see cref="ISchemaManager.CreateEntityAsync"/>.</summary>
    public sealed class SchemaEntityResult
    {
        public IErrorsInfo Status { get; init; } = new ErrorsInfo { Flag = Errors.Ok, Message = "Entity not created." };
        public bool Created { get; init; }
    }
}
