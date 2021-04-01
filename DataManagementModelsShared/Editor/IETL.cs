using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public interface IETL
    {
        IDMEEditor DMEEditor { get; set; }
        List<EntityStructure> Entities { get; set; }
        List<string> EntitiesNames { get; set; }
        PassedArgs Passedargs { get; set; }
        LScriptHeader script { get; set; }
        LScriptTrackHeader trackingHeader { get; set; }
        IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string entity, bool CreateMissingEntity = true);
        IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string entity, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<LScript> scripts, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, LScript scripts, bool CreateMissingEntity = true);
        List<LScript> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities);
        List<LScript> GetCreateEntityScript(IDataSource ds, List<string> entities);
        IErrorsInfo RunScript();
      
    }
}
