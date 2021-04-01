using System;
using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.Logger;
using TheTechIdea.Util;
namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IRDBMSHelper
    {
        event EventHandler<PassedArgs> ReportProgress;
        PassedArgs Passedargs { get; set; }
        IErrorsInfo ErrorObject { get; set; } 
     
        int id { get; set; }
        IDMLogger Log { get; set; }
        List<EntityStructure> Entities { get; set; }
        LScriptTracker trackingHeader { get; set; }
        List<LScript> script { get; set; }
        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo RunScript(IDataSource destds,List<LScript> script);
        List<LScript> GenerateCreatEntityScript(IDataSource destds, List<EntityStructure> entities);
        IErrorsInfo CopyEntityStruct(IDataSource ds1, string sourceds, IDataSource destds, string tablenameDest);
        string CreateEntityScript(IDataSource ds, string tablename);
      
        string CreateEntity(IDataSource ds, EntityStructure t1);
      
        IErrorsInfo CopyDataSource(IDataSource sourceds, IDataSource destds,bool CopyData);
        IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntities(IDataSource sourceds, IDataSource destds, List<string> entities, bool CopyData = false);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, bool CreateMissingEntity = true);
      


    }
}