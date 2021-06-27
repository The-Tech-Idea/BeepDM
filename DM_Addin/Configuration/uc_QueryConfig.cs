using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Util;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.Configuration
{
    public partial class uc_QueryConfig : UserControl, IDM_Addin, IAddinVisSchema
    {
        public uc_QueryConfig()
        {
            InitializeComponent();
        }
        public string AddinName { get; set; } = "Config. Query Editor";
        public string Description { get; set; } = "Config Query Editor";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string DllName { get; set; }
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = true;
        public IDataSource DestConnection { get; set; }
        public IDMLogger Logger { get; set; }
        public IDataSource SourceConnection { get; set; }
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 5;
        public int ID { get; set; } = 5;
        public string BranchText { get; set; } = "Query Setup";
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
        public int BranchID { get; set; } = 5;
        public string IconImageName { get; set; } = "query.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "ADDIN";
        #endregion "IAddinVisSchema"
        public string EntityName { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public DataSet Dset { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public PassedArgs Passedarg { get; set; }
       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            //SourceConnection = pdataSource;
            Logger = plogger;
            DMEEditor = pDMEEditor;
            ErrorObject = per;
            //Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            foreach (var item in Enum.GetValues(typeof(DataSourceType)))
            {
                DatabasetypeComboBox.Items.Add(item);
            }
            foreach (var item in Enum.GetValues(typeof(Sqlcommandtype)))
            {
                SQLTypeComboBox.Items.Add(item);
            }
            queryListBindingSource.DataSource = DMEEditor.ConfigEditor.QueryList;
            this.queryListBindingNavigatorSaveItem.Click += QueryListBindingNavigatorSaveItem_Click1;

        }

        private void QueryListBindingNavigatorSaveItem_Click1(object sender, EventArgs e)
        {
            try

            {
                DMEEditor.ConfigEditor.SaveQueryFile();


                MessageBox.Show("Success Saving Query changes");
                DMEEditor.AddLogMessage("Success", $"Saving Query changes", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Saving Query changes";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }

        }



    }

    
}
