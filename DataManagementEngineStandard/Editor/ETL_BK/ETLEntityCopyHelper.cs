using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Editor.Mapping.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.ETL
{
    /// <summary>
    /// Handles copying entity structure and data between data sources.
    /// </summary>
    public class ETLEntityCopyHelper
    {
        private readonly IDMEEditor _dme;
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLEntityCopyHelper"/> class.
        /// </summary>
        /// <param name="dme">The DME editor instance.</param>
        public ETLEntityCopyHelper(IDMEEditor dme) => _dme = dme ?? throw new ArgumentNullException(nameof(dme));

        /// <summary>
        /// Copies the structure of an entity from a source data source to a destination data source.
        /// </summary>
        public IErrorsInfo CopyEntityStructure(IDataSource source, IDataSource dest, string srcEntity, string destEntity, bool createIfMissing, IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var entity = source.GetEntityStructure(srcEntity, true);
                if (entity == null)
                {
                    _dme.AddLogMessage("Fail", $"Source entity {srcEntity} not found", DateTime.Now, 0, null, Errors.Failed);
                    return _dme.ErrorObject;
                }
                if (dest.Category == DatasourceCategory.RDBMS)
                    (dest as IRDBSource)?.DisableFKConstraints(entity);

                var working = (EntityStructure)entity.Clone();
                working.EntityName = destEntity;
                working.DatasourceEntityName = destEntity;
                working.OriginalEntityName = destEntity;

                if (dest.CreateEntityAs(working))
                {
                    progress?.Report(new PassedArgs { Messege = $"Created entity {destEntity}" });
                }
                else
                {
                    _dme.AddLogMessage("Fail", $"Could not create entity {destEntity}", DateTime.Now, 0, null, Errors.Failed);
                }

                if (dest.Category == DatasourceCategory.RDBMS)
                    (dest as IRDBSource)?.EnableFKConstraints(working);
            }
            catch (Exception ex)
            {
                _dme.AddLogMessage("Fail", $"Error creating entity {srcEntity}->{destEntity} {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return _dme.ErrorObject;
        }

        /// <summary>
        /// Copies data rows from a source entity to a destination entity.
        /// Applies DefaultsManager for missing values after optional mapping.
        /// </summary>
        public IErrorsInfo CopyEntityData(IDataSource source, IDataSource dest, string srcEntity, string destEntity, IProgress<PassedArgs> progress, CancellationToken token, EntityDataMap_DTL map = null)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var srcStruct = source.GetEntityStructure(srcEntity, true);
                var destStruct = dest.GetEntityStructure(destEntity, true);
                if (srcStruct == null || destStruct == null)
                {
                    _dme.AddLogMessage("Fail", $"Source or destination entity missing {srcEntity}->{destEntity}", DateTime.Now, 0, null, Errors.Failed);
                    return _dme.ErrorObject;
                }

                if (dest.Category == DatasourceCategory.RDBMS)
                    (dest as IRDBSource)?.DisableFKConstraints(destStruct);

                var data = source.GetEntity(srcStruct.EntityName, null);
                if (data == null)
                {
                    progress?.Report(new PassedArgs { Messege = $"No data for {srcEntity}" });
                    return _dme.ErrorObject;
                }

                var list = NormalizeToList(data, srcStruct);
                int count = 0;
                foreach (var row in list)
                {
                    token.ThrowIfCancellationRequested();

                    object payload = row;
                    // Optional mapping
                    if (map != null)
                    {
                        payload = MappingManager.MapObjectToAnother(_dme, destEntity, map, row);
                    }

                    // Apply Defaults after mapping (or directly on row if no mapping)
                    MappingDefaultsHelper.ApplyDefaultsToObject(_dme, dest.DatasourceName, destEntity, payload, destStruct.Fields);

                    dest.InsertEntity(destEntity, payload);
                    count++;
                    if (count % 100 == 0)
                        progress?.Report(new PassedArgs { Messege = $"Copied {count} rows for {destEntity}", ParameterInt1 = count });
                }
                progress?.Report(new PassedArgs { Messege = $"Finished copying {count} rows for {destEntity}", ParameterInt1 = count });

                if (dest.Category == DatasourceCategory.RDBMS)
                    (dest as IRDBSource)?.EnableFKConstraints(destStruct);
            }
            catch (Exception ex)
            {
                _dme.AddLogMessage("Fail", $"Error copying data {srcEntity}->{destEntity} {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return _dme.ErrorObject;
        }

        /// <summary>
        /// Normalizes supported data container types into a list of objects representing entity rows.
        /// </summary>
        private List<object> NormalizeToList(object data, EntityStructure entity)
        {
            var list = new List<object>();
            if (data == null) return list;
            var typeName = data.GetType().FullName ?? string.Empty;

            if (typeName.Contains("DataTable", StringComparison.InvariantCultureIgnoreCase))
            {
                if (data is DataTable dt)
                {
                    DMTypeBuilder.CreateNewObject(_dme, null, entity.EntityName, entity.Fields);
                    list = _dme.Utilfunction.GetListByDataTable(dt, DMTypeBuilder.MyType, entity);
                }
                return list;
            }

            if (data is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    list.Add(item);
                }
                return list;
            }

            // Single object
            list.Add(data);
            return list;
        }
    }
}
