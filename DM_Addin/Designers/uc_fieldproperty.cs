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
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Configuration
{
    public partial class uc_fieldproperty : UserControl, IDM_Addin
    {
        public uc_fieldproperty()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Data Source Field Properties";
        public string Description { get; set; } = "Data Source Field Properties";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = false;
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
        public IVisUtil Visutil { get; set; }
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }
        string dsname;
        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Args = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            DMEEditor.ConfigEditor.LoadAppFieldPropertiesValues(e.DatasourceName);
            this.appfieldPropertiesBindingSource.AddingNew += AppfieldPropertiesBindingSource_AddingNew;
            dsname = e.DatasourceName;
            EntityName = e.CurrentEntity;
           this.appfieldPropertiesBindingSource.DataSource = DMEEditor.ConfigEditor.AppfieldProperties;

            if (appfieldPropertiesBindingSource.Count == 0 && appfieldPropertiesBindingSource.Current == null)
            {
                this.appfieldPropertiesBindingSource.AddNew();
            }
            else
            {

                FindDSRecord(dsname);
            }
            FillEntities();
            if (e.CurrentEntity != null)
            {
                FindEntityRecord(e.CurrentEntity);
            }
            foreach (var item in Enum.GetValues(typeof(DisplayFieldTypes)))
            {
                this.fieldTypesComboBox.Items.Add(item);
            }
           
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
           
            this.Savebutton.Click += FieldPropertiesBindingNavigatorSaveItem_Click;
            
          
        }

        private void AppfieldPropertiesBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            DataSourceFieldProperties x = new DataSourceFieldProperties();
            x.DatasourceName = dsname;
            e.NewObject = x;
        }
        private void FillEntities()
        {
            DataSourceFieldProperties w = (DataSourceFieldProperties)appfieldPropertiesBindingSource.Current;
           
                if (!w.enitities.Any(s => s.entity == EntityName))
                {
                    IDataSource ds = DMEEditor.GetDataSource(dsname);
                    EntityStructure = ds.GetEntityStructure(EntityName, true);
                    DataSourceEntityProperties y = new DataSourceEntityProperties();
                    y.entity = EntityStructure.EntityName;
                    foreach (EntityField item in EntityStructure.Fields)
                    {
                        AppField x = new AppField();
                        x.datasourcename = dsname;
                        x.entityname = EntityName;
                        x.fieldname = item.fieldname;
                        y.properties.Add(x);

                    }
                    this.enititiesBindingSource.Add(y);
                }
           
            
        }
        private bool FindDSRecord(string name)
        {
            appfieldPropertiesBindingSource.MoveFirst();
            bool found = false;
            while (!found)
            {
                DataSourceFieldProperties w = (DataSourceFieldProperties)appfieldPropertiesBindingSource.Current;
                if (w.DatasourceName == name || (appfieldPropertiesBindingSource.Position == appfieldPropertiesBindingSource.Count - 1))
                {
                    found = true;
                }
                else
                {
                    appfieldPropertiesBindingSource.MoveNext();
                }
            }
            return found;
          //  workFlowsBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)];
        }
        private bool FindEntityRecord(string name)
        {
            enititiesBindingSource.MoveFirst();
            bool found = false;
            while (!found)
            {
                DataSourceEntityProperties w = (DataSourceEntityProperties)enititiesBindingSource.Current;
                if (w.entity == name || (enititiesBindingSource.Position == enititiesBindingSource.Count - 1))
                {
                    found = true;
                }
                else
                {
                    enititiesBindingSource.MoveNext();
                }
            }
            return found;
           // workFlowsBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)];
        }
        private void FieldPropertiesBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try

                
            {
                DMEEditor.ConfigEditor.AppfieldProperties[DMEEditor.ConfigEditor.AppfieldProperties.FindIndex(x => x.DatasourceName == dsname)]=(DataSourceFieldProperties) this.appfieldPropertiesBindingSource.Current;
                DMEEditor.ConfigEditor.SaveAppFieldPropertiesValues();
                MessageBox.Show("Saved Successfully");

            }
            catch (Exception ex)
            {
                string errmsg = "Error in saving defaults";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                MessageBox.Show($"{errmsg}:{ex.Message}");
            }
        }
    }
}
