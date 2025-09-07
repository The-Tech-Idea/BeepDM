using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.Helpers.ETL
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
        /// <param name="source">Source data source.</param>
        /// <param name="dest">Destination data source.</param>
        /// <param name="srcEntity">Source entity name.</param>
        /// <param name="destEntity">Destination entity name.</param>
        /// <param name="createIfMissing">If true and entity missing it will be created (currently always creates to match legacy behavior).</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Error object reflecting operation result.</returns>
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
    /// </summary>
    /// <param name="source">Source data source.</param>
    /// <param name="dest">Destination data source.</param>
    /// <param name="srcEntity">Source entity name.</param>
    /// <param name="destEntity">Destination entity name.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="token">Cancellation token.</param>
    /// <param name="map">Optional mapping (currently unused; placeholder for future field mapping).</param>
    /// <returns>Error object reflecting operation result.</returns>
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

                List<object> list = NormalizeToList(data, srcStruct);
                int count = 0;
                foreach (var row in list)
                {
                    token.ThrowIfCancellationRequested();
                    dest.InsertEntity(destEntity, row);
                    count++;
                    if (count % 100 == 0)
                        progress?.Report(new PassedArgs { Messege = $"Copied {count} rows for {destEntity}" });
                }
                progress?.Report(new PassedArgs { Messege = $"Finished copying {count} rows for {destEntity}" });

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
        /// Executes a copy entity script (wrapper used by ETLEditor legacy RunCopyEntityScript).
        /// </summary>
        /// <param name="sc">Script detail (currently only used for metadata/logging externally).</param>
        /// <param name="sourceds">Source datasource.</param>
        /// <param name="destds">Destination datasource.</param>
        /// <param name="srcentity">Source entity name.</param>
        /// <param name="destentity">Destination entity name.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="createMissingEntity">Whether to create entity if missing (not implemented here; legacy always assumes exist).</param>
        /// <param name="map_DTL">Optional mapping.</param>
        /// <returns>Error object after operation.</returns>
        public IErrorsInfo RunCopyEntityScript(ETLScriptDet sc, IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool createMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            return CopyEntityData(sourceds, destds, srcentity, destentity, progress, token, map_DTL);
        }

    /// <summary>
    /// Normalizes supported data container types into a list of objects representing entity rows.
    /// </summary>
    /// <param name="data">The raw data returned by IDataSource.GetEntity (DataTable, List, IEnumerable, etc.).</param>
    /// <param name="entity">Entity structure metadata.</param>
    /// <returns>List of row objects.</returns>
    private List<object> NormalizeToList(object data, EntityStructure entity)
        {
            var list = new List<object>();
            if (data == null) return list;
            var typeName = data.GetType().FullName;
            if (typeName.Contains("DataTable"))
            {
                var dt = data as DataTable;
                if (dt != null)
                {
                    DMTypeBuilder.CreateNewObject(_dme, null, entity.EntityName, entity.Fields);
                    list = _dme.Utilfunction.GetListByDataTable(dt, DMTypeBuilder.MyType, entity);
                }
            }
            else if (typeName.Contains("ObservableBindingList") || typeName.Contains("List") || typeName.Contains("IEnumerable"))
            {
                list = ((System.Collections.IEnumerable)data).Cast<object>().ToList();
            }
            return list;
        }
    }
}
