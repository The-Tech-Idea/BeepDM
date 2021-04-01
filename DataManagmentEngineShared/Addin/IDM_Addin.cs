using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using System;
using System.Data;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine;

namespace TheTechIdea
{
    public interface IDM_Addin
    {
        string ParentName { get; set; }
        string ObjectName { get; set; }
        string AddinName { get; set; }
        string Description { get; set; }
         Boolean DefaultCreate { get; set; }
         string DllPath { get; set; }
        string DllName { get; set; }
        string NameSpace { get; set; }
        DataSet Dset { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        IDMEEditor DME_editor { get; set; }
        TableData Tabledata { get; set; }
        string TableName { get; set; }
        PassedArgs Args { get; set; }
        event EventHandler<PassedArgs> OnObjectSelected;

        void RaiseObjectSelected();
        void Run(string param1);
        void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil,string[] args, PassedArgs e, IErrorsInfo per );
    }
}