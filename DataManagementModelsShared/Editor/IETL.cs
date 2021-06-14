﻿using System;
using System.Collections.Generic;
using System.Text;
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
        LScriptHeader script { get; set; }
        LScriptTrackHeader trackingHeader { get; set; }
        void CreateScriptHeader(IProgress<int> progress, IDataSource Srcds);
        IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<int> progress, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string srcentity,  string destentity, IProgress<int> progress, bool CreateMissingEntity = true);
        IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, IProgress<int> progress, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<int> progress, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity,string destentity, IProgress<int> progress, bool CreateMissingEntity = true);
        IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<LScript> scripts, IProgress<int> progress, bool CreateMissingEntity = true);
        List<LScript> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<int> progress);
        List<LScript> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<int> progress);
        Task<IErrorsInfo> RunScriptAsync(IProgress<int> progress);
      
    }
}
