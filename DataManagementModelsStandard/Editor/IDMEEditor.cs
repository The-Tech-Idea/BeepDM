﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    public interface IDMEEditor: IDisposable
    {

       

        List<IDataSource> DataSources { get; set; }
        IProgress<PassedArgs> progress { get; set; }
        bool ContainerMode { get; set; }
        string ContainerName { get; set; }
        string EntityName { get; set; }
        string DataSourceName { get; set; }
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
        IPassedArgs Passedarguments { get; set; }

        IDataSource GetDataSource(string pdatasourcename);
        IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename);
        IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename, string ClassDBHandlerName);
        bool RemoveDataDource(string pdatasourcename);
        bool CheckDataSourceExist(string pdatasourcename);
        ConnectionState OpenDataSource(string pdatasourcename);
        bool CloseDataSource(string pdatasourcename);
        AssemblyClassDefinition GetDataSourceClass(string DatasourceName);
        void AddLogMessage(string pLogType, string pLogMessage, DateTime pLogData, int pRecordID, string pMiscData, Errors pFlag);
        void AddLogMessage(string pLogMessage);
       
        void RaiseEvent(object sender,PassedArgs args);
        IErrorsInfo AskQuestion(IPassedArgs args);
        object GetData(IDataSource ds, EntityStructure entity);
        List<DefaultValue> Getdefaults(string DatasourceName);
        IErrorsInfo Savedefaults(List<DefaultValue> defaults, string DatasourceName);

        bool RemoveDataDourceUsingGuidID(string guidID);
        bool CheckDataSourceExistUsingGuidID(string guidID);
        ConnectionState OpenDataSourceUsingGuidID(string guidID);
        bool CloseDataSourceUsingGuidID(string guidID);
    }
}