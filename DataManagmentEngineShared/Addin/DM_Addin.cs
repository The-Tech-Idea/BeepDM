using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine;

namespace TheTechIdea
{
    public class DM_Addin : IDM_Addin
    {
        public string AddinName { get; set; }
        public string ObjectName { get; set; }
        public string Description { get; set; }
          public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = true;
        public IDMLogger Logger { get; set; }
    
        public string TableName { get; set; }
        public TableData Tabledata { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public PassedArgs Args { get; set; }
        public IDMEEditor DME_editor { get; set; }
        public event EventHandler<PassedArgs> OnObjectSelected;
        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil,  string[] args, PassedArgs e, IErrorsInfo per)
        {


        }
        public void RaiseObjectSelected()
        {
            var args = new PassedArgs
            {
                Addin = this,
                AddinName = this.AddinName,
                AddinType = "",
                CurrentTable = "",
                RDBSource = null
            };
            OnObjectSelected(this, args);
        }
        public DM_Addin()
        {

        }
        public void Run(string param1)
        {


        }
    }
}
