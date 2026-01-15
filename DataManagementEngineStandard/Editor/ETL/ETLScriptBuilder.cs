using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.ETL
{
    /// <summary>
    /// Helper responsible for building ETL scripts (Create / Copy / Alter etc.)
    /// Keeps ETLEditor thin and testable.
    /// </summary>
    public class ETLScriptBuilder
    {
        private readonly IDMEEditor _dme;
        public ETLScriptBuilder(IDMEEditor dme)
        {
            _dme = dme ?? throw new ArgumentNullException(nameof(dme));
        }

        public List<ETLScriptDet> BuildCreateEntityScripts(IDataSource src, IDataSource dest, IEnumerable<EntityStructure> entities, bool copyData, IProgress<PassedArgs> progress, CancellationToken token)
        {
            var list = new List<ETLScriptDet>();
            if (src == null || dest == null) return list;
            int i = 0;
            foreach (var e in entities)
            {
                token.ThrowIfCancellationRequested();
                var script = new ETLScriptDet
                {
                    Id = i++,
                    SourceDataSourceName = src.DatasourceName,
                    SourceEntityName = e.EntityName,
                    SourceDataSourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName) ? e.EntityName : e.DatasourceEntityName,
                    DestinationDataSourceName = dest.DatasourceName,
                    DestinationEntityName = e.EntityName,
                    DestinationDataSourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName) ? e.EntityName : e.DatasourceEntityName,
                    SourceEntity = e,
                    ScriptType = DDLScriptType.CreateEntity,
                    CopyData = copyData,
                    Active = true,
                    Mapping = new EntityDataMap_DTL(),
                    Tracking = new List<SyncErrorsandTracking>()
                };
                list.Add(script);
                progress?.Report(new PassedArgs { Messege = $"Prepared create script for {e.EntityName}" });
            }
            return list;
        }

        public List<ETLScriptDet> BuildCopyDataScripts(IDataSource src, IDataSource dest, IEnumerable<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token)
        {
            var list = new List<ETLScriptDet>();
            if (src == null || dest == null) return list;
            int i = 0;
            foreach (var e in entities)
            {
                token.ThrowIfCancellationRequested();
                var script = new ETLScriptDet
                {
                    Id = i++,
                    SourceDataSourceName = src.DatasourceName,
                    SourceEntityName = e.EntityName,
                    SourceDataSourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName) ? e.EntityName : e.DatasourceEntityName,
                    DestinationDataSourceName = dest.DatasourceName,
                    DestinationEntityName = e.EntityName,
                    DestinationDataSourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName) ? e.EntityName : e.DatasourceEntityName,
                    SourceEntity = e,
                    ScriptType = DDLScriptType.CopyData,
                    Active = true,
                    Mapping = new EntityDataMap_DTL(),
                    Tracking = new List<SyncErrorsandTracking>()
                };
                list.Add(script);
                progress?.Report(new PassedArgs { Messege = $"Prepared copy script for {e.EntityName}" });
            }
            return list;
        }
    }
}
