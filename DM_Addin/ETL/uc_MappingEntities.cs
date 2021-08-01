using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_MappingEntities : UserControl, IDM_Addin

    {
        public uc_MappingEntities()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get; set; } = "Create Mapping Between Entities";
        public string Description { get; set; } = "Create Mapping Between Entities";
        public bool DefaultCreate { get; set; } = false;
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public IPassedArgs Passedarg { get; set; }
        public IUtil util { get; set; }
        public IDataSource ds1 { get; set; }
        public IDataSource ds2 { get; set; }

       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void RaiseObjectSelected()
        {
           
        }

        public void Run(IPassedArgs pPassedarg)
        {
           
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg=  e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            util = putil;
          //  mappingsBindingSource.DataSource = DMEEditor.WorkFlowEditor.Mappings;
            foreach (ConnectionProperties i in DMEEditor.ConfigEditor.DataConnections)
            {
                entity1DataSourceComboBox.Items.Add(i.ConnectionName);
                entity2DataSourceComboBox.Items.Add(i.ConnectionName);

            }

            if (args[1] == null)
            {

                mappingsBindingSource.DataSource = DMEEditor.ConfigEditor.Mappings;
                mappingsBindingSource.AddNew();
            }
            else
            {


              mappingsBindingSource.DataSource = DMEEditor.ConfigEditor.Mappings.Where(c => c.MappingName == args[1]).FirstOrDefault();
                CreateDataSources();
                CreateEntities();
                GetFieldTypes();
                CreateFieldMappingCombo();
                CreateFieldMappingCombo();

            }
            mappingsBindingSource.AddingNew += MappingsBindingSource_AddingNew;
            fldMappingDataGridView.DataError += FldMappingDataGridView_DataError;
            fldMappingBindingSource.AddingNew += FldMappingBindingSource_AddingNew;            
            this.SyncMappingbutton.Click += SyncMappingbutton_Click;
            this.CreateFileMappingbutton.DragEnter += CreateFileMappingbutton_DragEnter;
            this.CreateFileMappingbutton.DragOver += CreateFileMappingbutton_DragOver;
            this.CreateFileMappingbutton.DragDrop += CreateFileMappingbutton_DragDrop;
            //this.listBox1.DragDrop += ListBox1_DragDrop;
            //this.listBox1.DragEnter += ListBox1_DragEnter;
            //this.listBox1.DragOver += ListBox1_DragOver;

        }

        private void CreateFileMappingbutton_DragDrop(object sender, DragEventArgs e)
        {
            string dsnamewithentiy = (string)e.Data.GetData(DataFormats.Text);

            //  ITreeBranch br = (ITreeBranch)e.Data.GetData(typeof(ITreeBranch));

            if (dsnamewithentiy != null)
            {
                string[] splitstring = dsnamewithentiy.Split(',');
                string dsname = splitstring[0];
                string entityname = splitstring[1];
                // entityname = br.BranchText;
                // dsname = br.ParentNode.Text;

                mappingsBindingSource.AddNew();


                this.mappingNameTextBox.Text = dsname + "_" + entityname + "_";
                entity1DataSourceComboBox.SelectedText = dsname;
                entityName1ComboBox.SelectedText = entityname;

                ds1 = DMEEditor.GetDataSource(dsname);
                if (ds1 != null)
                {
                    ds1.GetEntitesList();
                    entityName1ComboBox.Items.Clear();
                    foreach (string s in ds1.EntitiesNames)
                    {
                        entityName1ComboBox.Items.Add(s);


                    }
                }
                mappingsBindingSource.MovePrevious();
                mappingsBindingSource.MoveNext();
             //   mappingsBindingSource.CurrentChanged += MappingsBindingSource_CurrentChanged;

            }


        }
        private void CreateFileMappingbutton_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void CreateFileMappingbutton_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ListBox1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ListBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
         
        }

        private void ListBox1_DragDrop(object sender, DragEventArgs e)
        {

            string dsnamewithentiy = (string)e.Data.GetData(DataFormats.Text);
             
          //  ITreeBranch br = (ITreeBranch)e.Data.GetData(typeof(ITreeBranch));
           
            if(dsnamewithentiy!=null)
            {
                string[] splitstring = dsnamewithentiy.Split(',');
                string dsname = splitstring[0];
                string entityname = splitstring[1];
                // entityname = br.BranchText;
                // dsname = br.ParentNode.Text;

                mappingsBindingSource.AddNew();
               
              
                this.mappingNameTextBox.Text = dsname + "_" + entityname + "_";
                entity1DataSourceComboBox.SelectedText = dsname;
                entityName1ComboBox.SelectedText = entityname;

                ds1 = DMEEditor.GetDataSource(dsname);
                if (ds1 != null)
                {
                    ds1.GetEntitesList();
                    entityName1ComboBox.Items.Clear();
                    foreach (string s in ds1.EntitiesNames)
                    {
                        entityName1ComboBox.Items.Add(s);


                    }
                }

            }
        }

        private void FldMappingBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            Mapping_rep_fields x = new Mapping_rep_fields();
            Mapping_rep y = (Mapping_rep)mappingsBindingSource.Current;
           
            x.EntityName1 = y.EntityName1;
            x.EntityName2 = y.EntityName2;
            e.NewObject = x;
        }

        private void SyncMappingbutton_Click(object sender, EventArgs e)
        {
            if (mappingsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
                if (x.EntityName1 != null && x.EntityName2 != null && x.Entity1DataSource != null && x.Entity2DataSource != null)
                {
                    SyncFields();

                }

            }
        }
        private void SyncFields()
        {
            fldMappingBindingSource.Clear();

            Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
            foreach (EntityField s in x.Entity1Fields)
            {
                    if (FieldExist(s.fieldname,x.Entity2Fields))
                {
                    fldMappingBindingSource.AddNew();
                    Mapping_rep_fields T = (Mapping_rep_fields)fldMappingBindingSource.Current;
                    T.FieldName1 = s.fieldname;
                    T.FieldName2 = s.fieldname;
                    T.FieldType1 = s.fieldtype;
                    T.FieldType2 = s.fieldtype;
                    

                }
            }
            
        }
        private bool FieldExist(string pname,List<EntityField> ls)
        {
            return ls.Where(x => x.fieldname == pname).Count()>=0;
        }
        private void FldMappingDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            //// commited, display an error message.
            if (e.Exception != null &&
                e.Context == DataGridViewDataErrorContexts.Formatting)
            {

            }
        }
        private void MappingsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            Mapping_rep x = new Mapping_rep();
          
            e.NewObject = x;

        }
        private void CreateMappingbutton_Click(object sender, EventArgs e)
        {
            if (mappingsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
                if (x.EntityName1!=null && x.EntityName2 !=null && x.Entity1DataSource!=null && x.Entity2DataSource != null)
                {
                    GetFieldTypes();
                    CreateFieldMappingCombo();

                }
               
            }

           
        }
        private void CreateDataSources()
        {
            Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
            ds1 = DMEEditor.GetDataSource(x.Entity1DataSource);

            ds2 = DMEEditor.GetDataSource(x.Entity2DataSource);
        }
        private void CreateEntities()
        {
            if (ds1 != null)
            {
                ds1.GetEntitesList();
                entityName1ComboBox.Items.Clear();
                foreach (string s in ds1.EntitiesNames)
                {
                    entityName1ComboBox.Items.Add(s);


                }
            }
            if (ds2 != null)
            {
                ds2.GetEntitesList();

                entityName2ComboBox.Items.Clear();

                foreach (string s in ds2.EntitiesNames)
                {
                    entityName2ComboBox.Items.Add(s);


                }
            }

              

        }
        private void CreateFieldMappingCombo()
        {
            FieldName1Comobobox.Items.Clear();
            FieldName2Comobobox.Items.Clear();
            if (mappingsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
                
                foreach (EntityField s in x.Entity1Fields)
                {
                    FieldName1Comobobox.Items.Add(s.fieldname);
                }
                foreach (EntityField s in x.Entity2Fields)
                {
                    FieldName2Comobobox.Items.Add(s.fieldname);
                }

            }
        }
        private void GetFieldTypes()
        {
            if (mappingsBindingSource.Count > 0)
            {
                Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
                if (ds1 != null)
                {
                    x.Entity1Fields = ds1.GetEntityStructure(x.EntityName1,false).Fields;
                }
                   
                if (ds2 != null)
                {
                    x.Entity2Fields = ds2.GetEntityStructure(x.EntityName2,false).Fields;
                }
                   

            }
            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
        }
        private void GetEntitesbutton_Click(object sender, EventArgs e)
        {
            CreateDataSources();
            CreateEntities();

        }
        private void Savebutton_Click(object sender, EventArgs e)
        {
            try
            {
                mappingsBindingSource.EndEdit();
                Mapping_rep x = (Mapping_rep)mappingsBindingSource.Current;
                if (x.MappingName != null && x.EntityName1 != null && x.EntityName2 != null && x.Entity1DataSource != null && x.Entity2DataSource != null)
                {
                    CleanMapping();
                   // DMEEditor.ConfigEditor.Mappings = DMEEditor.WorkFlowEditor.Mappings;
                    DMEEditor.ConfigEditor.SaveMappingValues();
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
        private void CleanMapping()
        {
            foreach(Mapping_rep x in DMEEditor.ConfigEditor.Mappings)
            {
                
                if (x.FldMapping.Count == 0 || x.MappingName == null || x.Entity2DataSource == null || x.Entity1DataSource == null ||x.EntityName1==null||x.EntityName2==null)
                {
                    mappingsBindingSource.RemoveCurrent();
                }
            }
        }
    }
}
