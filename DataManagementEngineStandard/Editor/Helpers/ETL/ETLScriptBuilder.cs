using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Helpers.ETL
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
                    ID = i++,
                    sourcedatasourcename = src.DatasourceName,
                    sourceentityname = e.EntityName,
                    sourceDatasourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName)? e.EntityName: e.DatasourceEntityName,
                    destinationdatasourcename = dest.DatasourceName,
                    destinationentityname = e.EntityName,
                    destinationDatasourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName)? e.EntityName: e.DatasourceEntityName,
                    SourceEntity = e,
                    scriptType = DDLScriptType.CreateEntity,
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
                    ID = i++,
                    sourcedatasourcename = src.DatasourceName,
                    sourceentityname = e.EntityName,
                    sourceDatasourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName)? e.EntityName: e.DatasourceEntityName,
                    destinationdatasourcename = dest.DatasourceName,
                    destinationentityname = e.EntityName,
                    destinationDatasourceEntityName = string.IsNullOrEmpty(e.DatasourceEntityName)? e.EntityName: e.DatasourceEntityName,
                    SourceEntity = e,
                    scriptType = DDLScriptType.CopyData,
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
