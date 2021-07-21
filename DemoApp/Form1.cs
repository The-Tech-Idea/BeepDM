using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace DemoApp
{
    public partial class Form1 : Form,IDM_Addin
    {
        public Form1()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; } = "Main Form";
        public string ObjectType { get; set; } = "Form";
        public string AddinName { get; set; } = "Main Form";
        public string Description { get ; set ; } = "Main Form";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public IPassedArgs Passedarg { get ; set ; }


        // Added Property for Visualization
        public IVisUtil Visutil { get; set; }
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {

            //------------------ 
            // every Addin is Setup though VisUtil Class
            // It will allow engine to setup required parameters
            // every addin will be passed these 6 paramaters
            // all of them are setup though the VisUtil Class

            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            //---------------
            Datasourcesbutton.Click += Datasourcesbutton_Click;
        }

        private void Datasourcesbutton_Click(object sender, EventArgs e)
        {
            string[] args = new string[] {""};
            Visutil.ShowUserControlPopUp("uc_DataConnection", DMEEditor, args, Passedarg);
        }
        private IDMDataView CreateView()
        {
            IDMDataView DataView = null;
            try
            {
                string viewname = null;
                string fullname = null;
                if (Visutil.controlEditor.InputBox("Create View", "Please Enter Name of View (Name Should not exist already in Views)", ref viewname) == System.Windows.Forms.DialogResult.OK)
                {
                    if ((viewname != null) && DMEEditor.ConfigEditor.DataConnectionExist(viewname + ".json") == false)
                    {


                        fullname = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath, viewname + ".json");
                        ConnectionProperties f = new ConnectionProperties
                        {

                            FileName = Path.GetFileName(fullname),
                            FilePath = Path.GetDirectoryName(fullname),
                            Ext = Path.GetExtension(fullname),
                            ConnectionName = Path.GetFileName(fullname)
                        };

                        f.Category = DatasourceCategory.VIEWS;
                        f.DriverVersion = "1";
                        f.DriverName = "DataViewReader";

                        DMEEditor.ConfigEditor.DataConnections.Add(f);
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                        DataViewDataSource ds = (DataViewDataSource)DMEEditor.GetDataSource(f.ConnectionName);

                        DataView = ds.DataView;


                       
                        DMEEditor.AddLogMessage("Success", "Added View", DateTime.Now, 0, null, Errors.Ok);

                    }
                    else
                    {
                        MessageBox.Show("Please Select Other Name, Data Connection by this name Exist");
                    }

                }
                else
                {
                    Visutil.controlEditor.MsgBox("DM Engine", "Please Try another name . DataSource Exist");
                }
             
            }
            catch (Exception ex)
            {

                string mes = "Could not Added View ";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
               
            };
            return DataView;
        }
        private IDMDataView AddViewFile()
        {
            IDMDataView DataView = null;
            try
            {
                string viewname = null;
                string fullname = null;
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.InitialDirectory = DMEEditor.ConfigEditor.Config.Folders.Where(i => i.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath;
                openFileDialog1.Filter = "json files (*.json)|*.txt|All files (*.*)|*.*";

                openFileDialog1.DefaultExt = "json";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    viewname = Path.GetFileName(openFileDialog1.FileName);
                    if ((viewname != null) && DMEEditor.ConfigEditor.DataConnectionExist(viewname + ".json") == false)
                    {


                        fullname = openFileDialog1.FileName;  //Path.Combine(Path.GetDirectoryName(openFileDialog1.FileName), Path.GetFileName(openFileDialog1.FileName));
                        ConnectionProperties f = new ConnectionProperties
                        {

                            FileName = Path.GetFileName(fullname),
                            FilePath = Path.GetDirectoryName(fullname),
                            Ext = Path.GetExtension(fullname),
                            ConnectionName = Path.GetFileName(fullname)
                        };

                        f.Category = DatasourceCategory.VIEWS;
                        f.DriverVersion = "1";
                        f.DriverName = "DataViewReader";

                        DMEEditor.ConfigEditor.DataConnections.Add(f);
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                        DataViewDataSource ds = (DataViewDataSource)DMEEditor.GetDataSource(f.ConnectionName);

                        DataView = ds.DataView;


                        


                    }
                    DMEEditor.AddLogMessage("Success", "Added View", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    Visutil.controlEditor.MsgBox("DM Engine", "Please Try another name . DataSource Exist");
                }
              
            }
            catch (Exception ex)
            {
                string mes = "Could not Added View ";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DataView;
        }
        private IDMDataView CreateViewUsingTable( EntityStructure EntitySource)
        {
            IDMDataView DataView = null;
            IDataSource DataSource = null;
            IDataSource EntityDataSource = null;
            try
            {
                string viewname = null;
                string fullname = null;
                if (Visutil.controlEditor.InputBox("Create View", "Please Enter Name of View (Name Should not exist already in Views)", ref viewname) == System.Windows.Forms.DialogResult.OK)
                {
                    if ((viewname != null) && DMEEditor.ConfigEditor.DataConnectionExist(viewname + ".json") == false)
                    {


                        fullname = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath, viewname + ".json");
                        ConnectionProperties f = new ConnectionProperties
                        {

                            FileName = Path.GetFileName(fullname),
                            FilePath = Path.GetDirectoryName(fullname),
                            Ext = Path.GetExtension(fullname),
                            ConnectionName = Path.GetFileName(fullname)
                        };

                        f.Category = DatasourceCategory.VIEWS;
                        f.DriverVersion = "1";
                        f.DriverName = "DataViewReader";

                        DMEEditor.ConfigEditor.DataConnections.Add(f);
                        DataViewDataSource ds = (DataViewDataSource)DMEEditor.GetDataSource(f.ConnectionName);
                        EntityDataSource = DMEEditor.GetDataSource(EntitySource.DataSourceID);
                        if (EntitySource != null)
                        {

                            int x = ds.AddEntitytoDataView( EntityDataSource, EntitySource.EntityName, EntityDataSource.Dataconnection.ConnectionProp.SchemaName, null);

                        }
                        ds.WriteDataViewFile(fullname);
                        DataSource = DMEEditor.GetDataSource(f.ConnectionName);
                        DataView = ds.DataView;
                        DataView.EntityDataSourceID = EntityDataSource.DatasourceName;
  
                    }
                    DMEEditor.AddLogMessage("Success", "Added View", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    Visutil.controlEditor.MsgBox("DM Engine", "Please Try another name . DataSource Exist");
                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Added View ";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DataView;
        }
        private void CreateLocalDB()
        {

        }
        private void CreateDataViewFromLocalDB()
        {

        }
        private void AddEntityFromOtherLocalDB()
        {

        }

    }
}
