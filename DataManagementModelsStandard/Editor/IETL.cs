using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public interface IETL:IDisposable
    {
        event EventHandler<PassedArgs> PassEvent;
        IDMEEditor DMEEditor { get; set; }
        //List<EntityStructure> Entities { get; set; }
      //  List<string> EntitiesNames { get; set; }
        PassedArgs Passedargs { get; set; }
        ETLScriptHDR Script { get; set; }
        int ScriptCount { get; set; }
        decimal StopErrorCount { get; set; }
        int CurrentScriptRecord { get; set; }
        IRulesEditor RulesEditor { get; set; }
        List<LoadDataLogResult> LoadDataLogs { get; set; }
        // LScriptTracking Tracker { get; set; }
        void CreateScriptHeader( IDataSource Srcds, IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string srcentity,  string destentity, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true);
        IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null);
        IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity,string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<ETLScriptDet> scripts, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null);
        List<ETLScriptDet> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false);
        List<ETLScriptDet> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false);
        Task<IErrorsInfo> RunCreateScript(IProgress<PassedArgs> progress, CancellationToken token);
        List<ETLScriptDet> GetCopyDataEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo CreateImportScript(EntityDataMap mapping, EntityDataMap_DTL SelectedMapping);
        Task<IErrorsInfo> RunImportScript(IProgress<PassedArgs> progress, CancellationToken token);


    }
}
