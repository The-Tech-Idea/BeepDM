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
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;
using static System.Windows.Forms.ListBox;

namespace TheTechIdea.ETL
{
    public partial class uc_MappingSchema : UserControl, IDM_Addin
    {
        public uc_MappingSchema()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Mapping Schema Editor";
        public string Description { get; set; } = "Mapping Schema Editor";
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
        IBranch branch;
        public IDataSource ds1 { get; set; }
        public IDataSource ds2 { get; set; }
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
            this.mappingSchemaBindingSource.AddingNew += MappingSchemaBindingSource_AddingNew;
            this.mapsBindingSource.AddingNew += MapsBindingSource_AddingNew;
            this.fldMappingBindingSource.AddingNew += FldMappingBindingSource_AddingNew;
            this.dataConnectionsBindingSource.DataSource = DMEEditor.ConfigEditor.DataConnections;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            foreach (var item in Enum.GetValues(typeof(DatasourceCategory)))
            {
                SrcFiltercomboBox.Items.Add(item);
                DestTypeFiltercomboBox.Items.Add(item);
              
            }
                if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            foreach (ConnectionProperties i in DMEEditor.ConfigEditor.DataConnections)
            {
                entity1DataSourceComboBox.Items.Add(i.ConnectionName);
                entity2DataSourceComboBox.Items.Add(i.ConnectionName);
            }
            if (string.IsNullOrEmpty(e.CurrentEntity))
            {
                this.mappingSchemaBindingSource.DataSource = DMEEditor.ConfigEditor.MappingSchema;
                this.mappingSchemaBindingSource.AddNew();
            }
            else
            {
                this.schemaNameTextBox.Enabled = false;
                this.mappingSchemaBindingSource.DataSource = DMEEditor.ConfigEditor.MappingSchema[DMEEditor.ConfigEditor.MappingSchema.FindIndex(i => i.SchemaName == e.CurrentEntity)];
            }
            this.entity1DataSourceComboBox.SelectedValueChanged += Entity1DataSourceComboBox_SelectedValueChanged;
            this.entity2DataSourceComboBox.SelectedValueChanged += Entity2DataSourceComboBox_SelectedValueChanged;
            this.entityName1ComboBox.SelectedValueChanged += EntityName1ComboBox_SelectedValueChanged;
            this.entityName2ComboBox.SelectedValueChanged += EntityName2ComboBox_SelectedValueChanged;

            this.mappingSchemaBindingNavigatorSaveItem.Click += MappingSchemaBindingNavigatorSaveItem_Click;
          
            this.fldMappingDataGridView.DataError += FldMappingDataGridView_DataError;
            this.mappingsDataGridView.DataError += MappingsDataGridView_DataError;
        

            this.SrcDataSourcelistBox1.MouseDown += ListBox1_MouseDown;
            this.DestDatasourelistBox.MouseDown += DestDatasourelistBox_MouseDown;
            this.mappingsDataGridView.DragEnter += MappingsDataGridView_DragEnter;
            this.mappingsDataGridView.DragDrop += MappingsDataGridView_DragDrop;

            this.SrcFiltercomboBox.SelectedValueChanged += SrcFiltercomboBox_SelectedValueChanged;
            this.DestTypeFiltercomboBox.SelectedValueChanged += DestTypeFiltercomboBox_SelectedValueChanged;

            this.CreateMapButton.Click += CreateMapButton_Click;
         
            
        }

        private void CreateMapButton_Click(object sender, EventArgs e)
        {
            FillData();
        }

        private void DestTypeFiltercomboBox_SelectedValueChanged(object sender, EventArgs e)
        {

            DestDatasourelistBox.DataSource = DMEEditor.ConfigEditor.DataConnections.Where(x => x.Category.ToString() == DestTypeFiltercomboBox.Text).Select(k => k.ConnectionName).ToList();
            
        }

        private void SrcFiltercomboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            SrcDataSourcelistBox1.DataSource = DMEEditor.ConfigEditor.DataConnections.Where(x => x.Category.ToString() == SrcFiltercomboBox.Text).Select(k=>k.ConnectionName).ToList();
        }

        private void MappingSchemaBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try
            {
                fldMappingBindingSource.EndEdit();
                mappingSchemaBindingSource.EndEdit();
                mapsBindingSource.EndEdit();

                if (!string.IsNullOrWhiteSpace(schemaNameTextBox.Text))
                {
                    DMEEditor.ConfigEditor.SaveMappingSchemaValue();
                    branch.CreateChildNodes();
                }
                else
                {
                    MessageBox.Show("Please Complete all data fields before save");
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
            }
        }

        #region "Drag and Drop"
        private void DestDatasourelistBox_MouseDown(object sender, MouseEventArgs e)
        {
            DestDatasourelistBox.DoDragDrop("DEST", DragDropEffects.Copy);
        }
        private void MappingsDataGridView_DragDrop(object sender, DragEventArgs e)
        {
            Mapping_rep x = null ;
            DataGridViewRow dragRow = null;
            if (e.Data.GetData("System.String").ToString() == "SOURCE")
            {
                if (SrcDataSourcelistBox1.SelectedItems.Count > 1)
                {
                    foreach (string item in SrcDataSourcelistBox1.SelectedItems)
                    {
                         x = new Mapping_rep();
                        x.Entity1DataSource = item;
                        x.MappingName = $"From_{item}";
                        this.mapsBindingSource.Add(x);
                    }
                }
                else
                {
                    if (SrcDataSourcelistBox1.SelectedItems.Count == 1)
                    {
                        if (this.mapsBindingSource.Count == 0)
                        {
                            this.mapsBindingSource.AddNew();
                        }
                        string o = (string)SrcDataSourcelistBox1.SelectedItem;
                         x = (Mapping_rep)this.mapsBindingSource.Current;
                        x.Entity1DataSource = o;
                        x.MappingName = $"From_{ o}";
                    }

                }
            }else
            {
                if (DestDatasourelistBox.SelectedItems.Count == 1)
                {
                    if (this.mapsBindingSource.Count == 0)
                    {
                        this.mapsBindingSource.AddNew();
                    }
                    
                        Point p = mappingsDataGridView.PointToClient(new Point(e.X, e.Y));
                        int dragIndex = mappingsDataGridView.HitTest(p.X, p.Y).RowIndex;
                        // Determine if dragindex is valid row index       
                        if (dragIndex > -1)
                        {
                            
                             dragRow = (DataGridViewRow)mappingsDataGridView.Rows[dragIndex];
                                
                          
                        }
                        else
                        {
                        } // Do any message here if selected in column header and blank space. 


                    string o = (string)DestDatasourelistBox.SelectedItem;
                  
                    x = (Mapping_rep)dragRow.DataBoundItem;
                    x.Entity2DataSource = o;
                    x.MappingName = x.MappingName+$"_TO_{ o}";
                }
            }
           
            this.mappingsDataGridView.Refresh();
        }
        private void MappingsDataGridView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        private void ListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            SrcDataSourcelistBox1.DoDragDrop("SOURCE", DragDropEffects.Copy);
        }
        private void MappingsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion
        #region "Add new Events"
        private void FldMappingBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
         
            Mapping_rep_fields u = new Mapping_rep_fields();
           
            e.NewObject = u;
        }
        private void MapsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
         
            Mapping_rep y = new Mapping_rep();
          
            e.NewObject = y;

        }
        private void MappingSchemaBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            Map_Schema x = new Map_Schema();
            e.NewObject = x;

        }
        #endregion
        #region "Grid Events"
        private void FldMappingDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion
        #region "Selection Value changed"
        private void Entity2DataSourceComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (entity2DataSourceComboBox.SelectedItem == null)
            {

            }
            else
            {
                ds2 = DMEEditor.GetDataSource(entity2DataSourceComboBox.SelectedItem.ToString());
                if (ds2 != null)
                {
                    ds2.GetEntitesList();

                    entityName2ComboBox.Items.Clear();

                    foreach (string s in ds2.EntitiesNames)
                    {
                        entityName2ComboBox.Items.Add(s);


                    }
                }
                else
                {
                    MessageBox.Show($"Error Could not Open Datasource {entity2DataSourceComboBox.SelectedValue.ToString()} : {DMEEditor.ErrorObject.Message}");
                }
            }

        }
        private void Entity1DataSourceComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (entity1DataSourceComboBox.SelectedItem == null)
            {

            }
            else
            {
                ds1 = DMEEditor.GetDataSource(entity1DataSourceComboBox.Text);
                if (ds1 != null)
                {
                    ds1.GetEntitesList();
                    entityName1ComboBox.Items.Clear();
                    foreach (string s in ds1.EntitiesNames)
                    {
                        entityName1ComboBox.Items.Add(s);


                    }
                }
                else
                {
                    MessageBox.Show($"Error Could not Open Datasource {entity1DataSourceComboBox.SelectedItem.ToString()} : {DMEEditor.ErrorObject.Message}");
                }

            }

        }
        private void EntityName2ComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (mapsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mapsBindingSource.Current;
                if ( !string.IsNullOrEmpty(x.EntityName2))
                {
                   
                    if (ds2 != null)
                    {
                        if (ds2.Entities.Count == 0 || !ds2.Entities.Any(i => i.EntityName == x.EntityName2))
                        {
                            x.Entity2Fields = ds2.GetEntityStructure(x.EntityName2, false).Fields;
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ds2.DatasourceName, Entities = ds2.Entities });
                        }
                        else
                            x.Entity2Fields = ds2.Entities.Where(i => i.EntityName == x.EntityName2).FirstOrDefault().Fields;

                    }
                    
                    fieldName2ComboBox.Items.Clear();
                   
                    foreach (EntityField s in x.Entity2Fields)
                    {
                        fieldName2ComboBox.Items.Add(s.fieldname);
                    }


                }

            }

        }
        private void EntityName1ComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (mapsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mapsBindingSource.Current;
                if (!string.IsNullOrEmpty(x.EntityName1) )
                {
                    if (ds1 != null)
                    {
                        if (ds1.Entities.Count == 0 || !ds1.Entities.Any(i => i.EntityName == x.EntityName1))
                        {
                            x.Entity1Fields = ds1.GetEntityStructure(x.EntityName1, false).Fields;
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ds1.DatasourceName, Entities = ds1.Entities });
                        }
                        else
                            x.Entity1Fields = ds1.Entities.Where(i => i.EntityName == x.EntityName1).FirstOrDefault().Fields;

                    }

                   
                    fieldName1ComboBox.Items.Clear();
                  
                    foreach (EntityField s in x.Entity1Fields)
                    {
                        fieldName1ComboBox.Items.Add(s.fieldname);
                    }
                  
                }

            }

        }
        private void CreateFieldMappingCombo()
        {
            if (mapsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mapsBindingSource.Current;
                if (!string.IsNullOrEmpty(x.EntityName1) && !string.IsNullOrEmpty(x.EntityName2))
                {
                    if (ds1 != null)
                    {
                        if (ds1.Entities.Count == 0 || !ds1.Entities.Any(i => i.EntityName == x.EntityName1))
                        {
                            x.Entity1Fields = ds1.GetEntityStructure(x.EntityName1, false).Fields;
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ds1.DatasourceName, Entities = ds1.Entities });
                        }
                        else
                            x.Entity1Fields = ds1.Entities.Where(i => i.EntityName == x.EntityName1).FirstOrDefault().Fields;
                       
                    }

                    if (ds2 != null)
                    {
                        if (ds2.Entities.Count == 0 || !ds2.Entities.Any(i => i.EntityName == x.EntityName2))
                        {
                            x.Entity2Fields = ds2.GetEntityStructure(x.EntityName2, false).Fields;
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ds2.DatasourceName, Entities = ds2.Entities });
                        }
                        else
                            x.Entity2Fields = ds2.Entities.Where(i => i.EntityName == x.EntityName2).FirstOrDefault().Fields;

                    }
                    fieldName1ComboBox.Items.Clear();
                    fieldName2ComboBox.Items.Clear();
                    foreach (EntityField s in x.Entity1Fields)
                    {
                        fieldName1ComboBox.Items.Add(s.fieldname);
                    }
                    foreach (EntityField s in x.Entity2Fields)
                    {
                        fieldName2ComboBox.Items.Add(s.fieldname);
                    }


                }

            }



        }
        #endregion
        #region "Auto Fill Data"
        private void FillData()
        {
            Mapping_rep m = (Mapping_rep)mapsBindingSource.Current;
            if (m != null)
            {
                if (this.fldMappingBindingSource.Count > 0)
                {
                   if( MessageBox.Show("Would you like to overwrite the existing mapping", "Mapping", MessageBoxButtons.OKCancel)==DialogResult.OK)
                    {
                        AutoFillFields();
                    }
                }else
                {
                    AutoFillFields();
                }
            }
        }
        private void AutoFillDestDatasource()
        {
            if (this.mapsBindingSource.Count > 0)
            {
                if (this.DestDatasourelistBox.SelectedItem != null)
                {
                    ConnectionProperties o = (ConnectionProperties)DestDatasourelistBox.SelectedItem;
                    mapsBindingSource.MoveFirst();
                    for (int i = 0; i < mapsBindingSource.Count; i++)
                    {
                        Mapping_rep m = (Mapping_rep)mapsBindingSource.Current;
                        m.Entity2DataSource = o.ConnectionName;
                        
                            mapsBindingSource.MoveNext();

                    }
                }
            }
        }
     
        private void AutoFillFields()
        {
            Mapping_rep x = (Mapping_rep)mapsBindingSource.Current;
            //IDataSource ds = DMEEditor.GetDataSource(x.Entity2DataSource);

            List<DefaultValue> defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == x.Entity2DataSource)].DatasourceDefaults;
            fldMappingBindingSource.Clear();
            foreach (EntityField s in x.Entity2Fields.OrderBy(p=>p.FieldIndex))
            {
                fldMappingBindingSource.AddNew();
                Mapping_rep_fields T = T = (Mapping_rep_fields)fldMappingBindingSource.Current;
                if (FieldExist(s.fieldname, x.Entity1Fields))
                {
                    T.FieldName1 = s.fieldname;
                    T.FieldName2 = s.fieldname;
                    T.FieldType1 = s.fieldtype;
                    T.FieldType2 = s.fieldtype;
                }
                else
                {
                    T.FieldName2 = s.fieldname;
                    T.FieldType2 = s.fieldtype;
                }
                if (defaults != null)
                {
                    if (defaults.Any(u => u.propertyName.ToLower() == s.fieldname.ToLower() && u.propertyType != DefaultValueType.DisplayLookup))
                    {
                        string df = defaults.Where(u => u.propertyName.ToLower() == s.fieldname.ToLower() && u.propertyType!=DefaultValueType.DisplayLookup).FirstOrDefault().propertyName;
                        T.Rules = $":Default.{df}";
                    }
                }
                

            }
        }
       
        private bool FieldExist(string pname, List<EntityField> ls)
        {
            return ls.Any(x => x.fieldname.ToLower() == pname.ToLower());
        }
        #endregion


    }
}
