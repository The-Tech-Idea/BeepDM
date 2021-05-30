using System;
using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Logger;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Tools;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine
{
    public interface IDMEEditor
    {
        List<IDataSource> DataSources { get; set; }
     //   IRDBMSHelper RDBMSHelper { get; set; }
        IETL ETL { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        IDataTypesHelper typesHelper { get; set; }
        IUtil Utilfunction { get; set; }
        IConfigEditor ConfigEditor { get; set; }
        IWorkFlowEditor WorkFlowEditor { get; set; }
        IClassCreator classCreator { get; set; }
        IAssemblyHandler assemblyHandler { get; set; }
        BindingList<ILogAndError> Loganderrors { get; set; }
        PassedArgs Passedarguments { get; set; }
        LScriptHeader Script { get; set; }
        IDataSource GetDataSource(string pdatasourcename);
        IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename);
        IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename, string ClassDBHandlerName);
        bool RemoveDataDource(string pdatasourcename);
        bool CheckDataSourceExist(string pdatasourcename);
        bool OpenDataSource(string pdatasourcename);
        bool CloseDataSource(string pdatasourcename);
        AssemblyClassDefinition GetDataSourceClass(string DatasourceName);
        void AddLogMessage(string pLogType, string pLogMessage, DateTime pLogData, int pRecordID, string pMiscData, Errors pFlag);
        void AddLogMessage(string pLogMessage);
        event EventHandler<PassedArgs> PassEvent;
        void RaiseEvent(object sender,PassedArgs args);


    }
}