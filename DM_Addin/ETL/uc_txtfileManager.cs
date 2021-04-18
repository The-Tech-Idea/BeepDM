using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.FileManager;
using TheTechIdea;

namespace TheTechIdea.ETL
{
    public partial class uc_txtfileManager : UserControl, IDM_Addin
    {
        public uc_txtfileManager()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; } 
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get  ; set  ; } = "Text/CSV File Manager";
        public string Description { get  ; set  ; } = "Text/CSV File Manager";
        public bool DefaultCreate { get  ; set  ; } = true;
        public string DllPath { get  ; set  ; }
        public string DllName { get  ; set  ; }
        public string NameSpace { get  ; set  ; }
        public IDataSource FileDs { get  ; set  ; }
       
        public DataSet Dset { get  ; set  ; }
        public IErrorsInfo ErrorObject  { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public IDMEEditor DMEEditor { get  ; set  ; }
        public EntityStructure EntityStructure { get  ; set  ; }
        public string EntityName { get  ; set  ; }
        public PassedArgs Passedarg { get  ; set  ; }
        public IVisUtil Visutil { get  ; set  ; }

        public string entityname { get; set; }
        public string sheetname { get; set; }
        EntityStructure EntityData { get; set; } = new EntityStructure();
        private BindingSource bs { get; set; } = new BindingSource();
       // public event EventHandler<PassedArgs> OnObjectSelected;

      

        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Passedarg=  e;
            Logger = plogger;
           ErrorObject  = per;
            DMEEditor = pbl;
            FileDs = (IDataSource)e.DataSource;
            fieldsDataGridView.DataError += FieldsDataGridView_DataError;
            FileDs.GetEntitesList();
            //if (e.EventType == "FILESHEETSELECTED")

            //{
                
            //    entityname = e.ObjectName;
              
            //}
            //else
            //{
            //    if (FileDs.Entities.Count() == 1)
            //    {
            //        entityname = e.ObjectName;
            //    }
              

            //}
            entityname = e.CurrentEntity;

            EntityData = FileDs.Entities.Where(c => c.EntityName == entityname).FirstOrDefault();
           if (EntityData == null)
            {
                EntityData=FileDs.GetEntityStructure(entityname,false);
                FileDs.Entities.Add(EntityData);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
            }
            fieldtype.DataSource = pbl.typesHelper.GetNetDataTypes2();
            entitiesBindingSource.DataSource = EntityData;
            SampledataGridView.RowPostPaint += SampledataGridView_RowPostPaint;
            LoadSampleDatabutton.Click += LoadSampleDatabutton_Click;
            SaveConfigbutton.Click += SaveConfigbutton_Click;
            
           

        }

        private void SampledataGridView_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            var rowIdx = (e.RowIndex + 1).ToString();

            var centerFormat = new StringFormat()
            {
                // right alignment might actually make more sense for numbers
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
        }

        private void LoadSampleDatabutton_Click(object sender, EventArgs e)
        {
            LoadSampleData();
        }

        private void SaveConfigbutton_Click(object sender, EventArgs e)
        {
            EntityStructure x = FileDs.Entities.Where(c => c.EntityName == entityname).FirstOrDefault();
            if (x == null)
            {
                FileDs.Entities.Add(EntityData);
            }else
            {
                FileDs.Entities.Where(c => c.EntityName == entityname).FirstOrDefault().Fields = EntityData.Fields;
            }
           

            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
        }

        private void FieldsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            //// commited, display an error message.
            if (e.Exception != null &&
                e.Context == DataGridViewDataErrorContexts.Formatting)
            {

            }
        }
        private void LoadSampleData()
        {
            Logger.WriteLog($"Start Load Table in Form");
            ErrorObject.Flag = Errors.Ok;
            try
            {
                //FileDs.HeaderExist = true;
                //FileDs.fromline = 0;
                //FileDs.toline = 100;
                // FileDs.SourceEntityData =
                bs.DataSource = (DataTable)FileDs.GetEntity(entityname, null);  //FileDs.SourceEntityData;
                bs.ResetBindings(true);
                SampledataGridView.DataSource = null;
                SampledataGridView.AutoGenerateColumns = true;
                SampledataGridView.Columns.Clear();
                Logger.WriteLog($"Reset Grid Columns");
                SampledataGridView.DataSource = bs;
                bs.ResumeBinding();
                SampledataGridView.Refresh();
                Logger.WriteLog($"Reset Datasource");


                // bindingSource1.ResetBindings(true);
                //DataGridViewEdit.Refresh();

                // ShowViewonTree(MyDataView);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in Loading Table in Grid ({ex.Message}) ");

            }
        }

    }
}
