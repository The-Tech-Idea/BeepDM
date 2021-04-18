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
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.Configuration
{
    public partial class uc_webapiQueryParameters : UserControl,IDM_Addin
    {
        public uc_webapiQueryParameters()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "WebApi Queries";
        public string Description { get; set; } = "WebApi Queries";
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
        public PassedArgs Passedarg { get ; set ; }
        IDataSource ds;
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
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            this.entitiesBindingNavigatorSaveItem.Click += EntitiesBindingNavigatorSaveItem_Click;
            EntityName = e.DatasourceName;
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            this.entitiesBindingSource.AddingNew += EntitiesBindingSource_AddingNew;
            this.entitiesBindingSource.DataSource = ds.Entities;
        }

        private void EntitiesBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
         
            EntityStructure ent = new EntityStructure();
           
            e.NewObject = ent;
        }

        private void EntitiesBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try

            {
                ds.Entities = (List<EntityStructure>)this.entitiesBindingSource.List;
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ds.DatasourceName, Entities = ds.Entities });
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                MessageBox.Show("Saved Successfully");
            }
            catch (Exception ex)
            {
                string errmsg = "Error in saving queries";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                MessageBox.Show($"{errmsg}:{ex.Message}");
            }
        }
    }
}
