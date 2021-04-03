using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Configuration
{
    public partial class uc_webapiHeaders : UserControl,IDM_Addin

    {
        public uc_webapiHeaders()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "WebApi Headers";
        public string Description { get; set; } = "WebApi Headers";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Args { get ; set ; }

       // public event EventHandler<PassedArgs> OnObjectSelected;
        public IVisUtil Visutil { get; set; }
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Args = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            EntityName = e.DatasourceName;
            this.headersBindingNavigatorSaveItem.Click += HeadersBindingNavigatorSaveItem_Click;
            this.headersBindingSource.AddingNew += HeadersBindingSource_AddingNew;
            this.headersBindingSource.DataSource = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(x => x.ConnectionName == EntityName)].Headers;

        }

        private void HeadersBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
           
            WebApiHeader apiHeader = new WebApiHeader();
           
            e.NewObject = apiHeader;
        }

        private void HeadersBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try

            {
                DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(x => x.ConnectionName == EntityName)].Headers = (List<WebApiHeader>)this.headersBindingSource.List;
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                MessageBox.Show("Saved Successfully");

            }
            catch (Exception ex)
            {
                string errmsg = "Error in saving headesrs";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                MessageBox.Show($"{errmsg}:{ex.Message}");
            }
        }
    }
}
