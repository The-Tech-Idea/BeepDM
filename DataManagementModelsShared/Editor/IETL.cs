using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public interface IETL
    {
        event EventHandler<PassedArgs> PassEvent;
        IDMEEditor DMEEditor { get; set; }
        List<EntityStructure> Entities { get; set; }
        List<string> EntitiesNames { get; set; }
        PassedArgs Passedargs { get; set; }
        SyncDataSource script { get; set; }
        int ScriptCount { get; set; }
        int CurrentScriptRecord { get; set; }
       // LScriptTracking Tracker { get; set; }
        void CreateScriptHeader( IDataSource Srcds, IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string srcentity,  string destentity, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true);
        IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken toke, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity,string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<SyncEntity> scripts, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true);
        List<SyncEntity> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token);
        List<SyncEntity> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> RunScriptAsync(IProgress<PassedArgs> progress, CancellationToken token);
      
    }
}
