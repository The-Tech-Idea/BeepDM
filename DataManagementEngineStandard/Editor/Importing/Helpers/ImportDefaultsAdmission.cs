using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    internal sealed class ImportDefaultsAdmission
    {
        private readonly IDMEEditor _editor;
        private readonly string _sourceDataSource, _sourceEntity, _destDataSource, _destEntity;
        private readonly bool _apply;
        private readonly List<DefaultValue> _defaults;
        internal RequiredResolverRegistry Registry { get; }

        private ImportDefaultsAdmission(IDMEEditor editor, DataImportConfiguration config, CancellationToken token,
            RequiredResolverRegistry registry)
        {
            _editor = editor;
            _sourceDataSource = config.SourceDataSourceName;
            _sourceEntity = config.SourceEntityName;
            _destDataSource = config.DestDataSourceName;
            _destEntity = config.DestEntityName;
            _apply = config.ApplyDefaults;
            _defaults = !_apply ? new List<DefaultValue>() : config.DefaultValues?.Any() == true
                ? DefaultValueHelper.CaptureRequired(config.DefaultValues)
                : new DefaultValueHelper(editor).GetDefaultsRequired(_destDataSource);
            if (registry != null && !registry.IsOwnedBy(editor)) throw new DefaultCatalogReadException();
            Registry = registry ?? (_defaults.Any(value => !string.IsNullOrWhiteSpace(value.Rule))
                ? DefaultsManager.CaptureRequiredResolverRegistry(editor, token) : null);
        }

        internal static ImportDefaultsAdmission Capture(IDMEEditor editor, DataImportConfiguration config, CancellationToken token,
            RequiredResolverRegistry registry = null)
        {
            token.ThrowIfCancellationRequested();
            if (editor == null || config == null) throw new DefaultCatalogReadException();
            var captured = new ImportDefaultsAdmission(editor, config, token, registry);
            token.ThrowIfCancellationRequested();
            return captured;
        }

        internal IDisposable EnterResolutionScope() => new RequiredResolverContext(Registry, _destDataSource, _defaults);

        internal DataImportConfiguration Bind(IDMEEditor editor, DataImportConfiguration config)
        {
            if (!ReferenceEquals(editor, _editor) || config == null ||
                !string.Equals(_sourceDataSource, config.SourceDataSourceName, StringComparison.Ordinal) ||
                !string.Equals(_sourceEntity, config.SourceEntityName, StringComparison.Ordinal) ||
                !string.Equals(_destDataSource, config.DestDataSourceName, StringComparison.Ordinal) ||
                !string.Equals(_destEntity, config.DestEntityName, StringComparison.Ordinal))
                throw new DefaultCatalogReadException();

            // Top-level execution settings and list containers are private. Metadata, provider
            // handles and plugin objects still require host coordination; this is not full intent capture.
            return new DataImportConfiguration
            {
                ImportRunId = config.ImportRunId,
                SourceEntityName = _sourceEntity, DestEntityName = _destEntity,
                SourceDataSourceName = _sourceDataSource, DestDataSourceName = _destDataSource,
                SourceEntityStructure = config.SourceEntityStructure, DestEntityStructure = config.DestEntityStructure,
                SourceData = config.SourceData, DestData = config.DestData, Mapping = config.Mapping,
                RequireBoundMappingMetadata = config.RequireBoundMappingMetadata,
                SourceFilters = config.SourceFilters?.ToList(), SelectedFields = config.SelectedFields?.ToList(),
                ApplyDefaults = _apply, DefaultValues = DefaultValueHelper.CaptureRequired(_defaults),
                CustomTransformation = config.CustomTransformation, BatchSize = config.BatchSize,
                CreateDestinationIfNotExists = config.CreateDestinationIfNotExists, SkipBlanks = config.SkipBlanks,
                RunMigrationPreflight = config.RunMigrationPreflight, AddMissingColumns = config.AddMissingColumns,
                CreateSyncProfileDraft = config.CreateSyncProfileDraft, OnBatchError = config.OnBatchError,
                MaxRetries = config.MaxRetries, SyncMode = config.SyncMode,
                WatermarkColumn = config.WatermarkColumn, LastWatermarkValue = config.LastWatermarkValue,
                UpsertKeyColumns = config.UpsertKeyColumns?.ToList(), QualityRules = config.QualityRules?.ToList(),
                QualityFailureMode = config.QualityFailureMode, QualityRuleTimeoutMs = config.QualityRuleTimeoutMs,
                RecordAdmission = config.RecordAdmission, DriftPolicy = config.DriftPolicy,
                ErrorStore = config.ErrorStore, RunHistoryStore = config.RunHistoryStore,
                Staging = config.Staging == null ? null : new StagingOptions
                {
                    Enabled = config.Staging.Enabled, StagingEntitySuffix = config.Staging.StagingEntitySuffix,
                    DropStagingAfterNormalize = config.Staging.DropStagingAfterNormalize,
                    SkipNormalization = config.Staging.SkipNormalization
                }
            };
        }
    }
}
