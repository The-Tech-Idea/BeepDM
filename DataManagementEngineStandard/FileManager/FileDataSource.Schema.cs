using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Readers;
using TheTechIdea.Beep.FileManager.Schema;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Schema inference partial — GetEntityStructure, CreateEntityAs, CreateEntities.
    /// Delegates header reading and type inference to <see cref="_reader"/>.
    /// Phase 3: uses confidence-scored <see cref="TypeInferenceHelper.InferWithStats"/>
    /// for per-column type and quality statistics.
    /// </summary>
    public partial class FileDataSource
    {
        // ── Schema API ────────────────────────────────────────────────────────

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (!refresh)
            {
                EntityStructure cached = Entities.FirstOrDefault(e =>
                    string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                if (cached != null) return cached;
            }

            return InferEntityStructure(EntityName);
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (fnd == null) return null;
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                return false;

            string path    = ResolveEntityFilePath(entity.EntityName);
            var    headers = entity.Fields
                .Select(f => f.Originalfieldname ?? f.FieldName)
                .ToList();

            if (!File.Exists(path))
                _reader.CreateFile(path, headers);

            RegisterEntity(entity);
            return true;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities != null)
                foreach (EntityStructure e in entities)
                    CreateEntityAs(e);
            return ErrorObject;
        }

        // ── Enterprise schema versioning ─────────────────────────────────────

        internal FileSchemaVersion BuildSchemaVersion(EntityStructure entity, string checksum)
        {
            var columns = entity.Fields.Select(f => new FileColumnSchema
            {
                OrdinalPosition  = f.FieldIndex,
                SourceColumnName = f.Originalfieldname ?? f.FieldName,
                TargetFieldName  = f.FieldName,
                InferredType     = f.Fieldtype,
                IsNullable       = f.AllowDBNull,
                MaxLength        = f.Size1
            }).ToList();

            return new FileSchemaVersion
            {
                FileChecksum = checksum,
                Delimiter    = _reader?.GetDefaultExtension() ?? string.Empty,
                Columns      = columns,
                RegisteredBy = "FileDataSource"
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Phase 3: confidence-scored inference using a single streaming pass.
        /// Gathers per-column samples then calls <see cref="TypeInferenceHelper.InferWithStats"/>.
        /// </summary>
        private EntityStructure InferEntityStructure(string entityName)
        {
            string filePath = ResolveEntityFilePath(entityName);
            if (!File.Exists(filePath)) return null;

            string[] headers = _reader.ReadHeaders(filePath);
            if (headers.Length == 0) return null;

            string baseName = Path.GetFileNameWithoutExtension(filePath);
            var entity = new EntityStructure
            {
                EntityName           = baseName,
                DatasourceEntityName = baseName,
                OriginalEntityName   = baseName,
                Caption              = baseName,
                DatabaseType         = DatasourceType,
                Viewtype             = ViewType.File,
                DataSourceID         = DatasourceName,
                Fields               = new List<EntityField>()
            };

            // Build initial field list preserving ordinal and original names
            for (int i = 0; i < headers.Length; i++)
            {
                string normalized = NormalizeFieldName(headers[i]);
                entity.Fields.Add(new EntityField
                {
                    FieldIndex        = i,
                    FieldName         = normalized,
                    Originalfieldname = headers[i],
                    Fieldtype         = typeof(string).FullName,
                    EntityName        = entity.EntityName,
                    AllowDBNull       = true,
                    IsKey             = i == 0
                });
            }

            // Per-column sample buckets (up to 200 rows)
            const int SampleLimit = 200;
            var columnSamples = new List<string>[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                columnSamples[i] = new List<string>(SampleLimit);

            int rowsScanned = 0;
            foreach (string[] row in _reader.ReadRows(filePath))
            {
                for (int i = 0; i < headers.Length && i < row.Length; i++)
                    columnSamples[i].Add(row[i]);
                if (++rowsScanned >= SampleLimit) break;
            }

            // Apply confidence-scored inference per column
            for (int i = 0; i < entity.Fields.Count; i++)
            {
                FieldInferenceResult result = TypeInferenceHelper.InferWithStats(columnSamples[i]);
                entity.Fields[i].Fieldtype   = result.InferredType;
                entity.Fields[i].AllowDBNull = result.NullCount > 0 || result.NullRate > 0;
                // Store uniqueness ratio in Size2, null rate * 100 in Fieldsize (best-effort metadata)
                entity.Fields[i].Size2       = (int)Math.Round(result.UniquenessRatio * 100);
            }

            RegisterEntity(entity);
            return entity;
        }

        private void RegisterEntity(EntityStructure entity)
        {
            Entities.RemoveAll(e =>
                string.Equals(e.EntityName, entity.EntityName, StringComparison.OrdinalIgnoreCase));
            Entities.Add(entity);

            if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                EntitiesNames.Add(entity.EntityName);
        }
    }
}
