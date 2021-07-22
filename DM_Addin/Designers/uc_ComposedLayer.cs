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
using TheTechIdea.Beep;
using TheTechIdea.Beep.CompositeLayer;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_ComposedLayer : UserControl,IDM_Addin
    {
        public uc_ComposedLayer()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string AddinName { get; set; } = "Composed Layer Creator";
        public string Description { get; set; } = "Composed Layer Creator";
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
        public IPassedArgs Passedarg { get ; set ; }

       // public event EventHandler<PassedArgs> OnObjectSelected;
       // private IDMDataView MyDataView;
        public IVisUtil Visutil { get; set; }
        DataViewDataSource ds;
        IBranch RootCompositeLayerBranch;
        CompositeLayer Layer;
        WaitFormFunc waitForm;
        IBranch branch;
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
          
            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            RootCompositeLayerBranch = (IBranch)e.Objects.Where(c => c.Name == "RootCompositeLayerBranch").FirstOrDefault().obj;
          
            if (branch.DataSourceName != null)
            {
                ds = (DataViewDataSource)DMEEditor.GetDataSource(branch.DataSourceName);
                ds.Openconnection();
            }
           
            dataViewDataSourceNameComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.DataConnections.Where(x=>x.Category==DatasourceCategory.VIEWS).ToList())
            {
                dataViewDataSourceNameComboBox.Items.Add(item.ConnectionName);
            }
            localDBDriverComboBox.Items.Clear();
            localDBDriverVersionComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.DataDriversClasses.Where(x => x.CreateLocal == true && x.classHandler != null))
            {
                localDBDriverComboBox.Items.Add(item.classHandler);
                localDBDriverVersionComboBox.Items.Add(item.version);
            }
           
          
            localDBDriverComboBox.SelectedValueChanged += LocalDBDriverComboBox_SelectedValueChanged;
            this.FolderLocationbutton.Click += FolderLocationbutton_Click;
            Createbutton.Click += Createbutton_Click;
            compositeQueryLayersBindingSource.DataSource = DMEEditor.ConfigEditor.CompositeQueryLayers;
            compositeQueryLayersBindingSource.AddingNew += CompositeQueryLayersBindingSource_AddingNew;
            compositeQueryLayersBindingSource.AddNew();
            this.layerNameTextBox.TextChanged += LayerNameTextBox_TextChanged;
        //    localDBDriverComboBox.SelectedIndex = 1;


        }

        private void LayerNameTextBox_TextChanged(object sender, EventArgs e)
        {
          //if(DMEEditor.DataConnectionNameExist(layerNameTextBox.Text))
          //  {
          //      MessageBox.Show("Error, Already there is a Data connection the Same name")
          //  }
        }

        private void FolderLocationbutton_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = DMEEditor.ConfigEditor.Config.Folders.Where(i => i.FolderFilesType == FolderFileTypes.DataFiles).FirstOrDefault().FolderPath;
           // saveFileDialog1.Filter = "json files (*.json)|*.txt|All files (*.*)|*.*";
       //     saveFileDialog1.CheckFileExists = true;
            saveFileDialog1.CheckPathExists = true;
          //  saveFileDialog1.DefaultExt = "json";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.FolderSavelocationlabel.Text =Path.GetDirectoryName(saveFileDialog1.FileName);
                this.FileNametextBox.Text= Path.GetFileName(saveFileDialog1.FileName);
            }
        }

        private void CompositeQueryLayersBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            CompositeLayer Layer1 = new CompositeLayer();
            Layer1.ID = Guid.NewGuid().ToString();
            if (ds != null)
            {
                Layer1.DataViewDataSourceName = ds.DatasourceName;
            }
        
            Layer1.DateCreated = DateTime.Now;
            Layer1.DateUpdated = DateTime.Now;
          
            e.NewObject = Layer1;

        }

        private void Createbutton_Click(object sender, EventArgs e)
        {
             try

            {
                

                if (string.IsNullOrEmpty(this.layerNameTextBox.Text)|| string.IsNullOrEmpty(this.localDBDriverVersionComboBox.Text) || string.IsNullOrEmpty(this.layerNameTextBox.Text) )
                {
                    MessageBox.Show("Error, Please Fill all missing Fields");
                    throw new InvalidOperationException("Error, Please Fill all missing Fields");
                }
                if (DMEEditor.ConfigEditor.DataConnectionExist(layerNameTextBox.Text))
                {
                    
                    MessageBox.Show("Error, Already there is a Data connection the Same name");
                    throw new InvalidOperationException("Error, Already there is a Data connection the Same name");
                }
               
                ConnectionProperties cn = new ConnectionProperties();
                ConnectionDriversConfig package = DMEEditor.ConfigEditor.DataDriversClasses.Where(x => x.classHandler == localDBDriverComboBox.Text).OrderByDescending(o => o.version).FirstOrDefault();
                Layer =(CompositeLayer) compositeQueryLayersBindingSource.Current;
                cn.CompositeLayer = true;
                cn.ConnectionName = layerNameTextBox.Text; 
                cn.CompositeLayerName = layerNameTextBox.Text;
                cn.ConnectionName = layerNameTextBox.Text;
                cn.DatabaseType = package.DatasourceType;
                cn.Category = package.DatasourceCategory;
                if ( !string.IsNullOrEmpty(FileNametextBox.Text) || !string.IsNullOrEmpty(FolderSavelocationlabel.Text))
                {
                    cn.FilePath = FolderSavelocationlabel.Text;
                    cn.FileName = FileNametextBox.Text;
                }
                else
                {
                    cn.FilePath = DMEEditor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.DataFiles).FirstOrDefault().FolderPath ;
                    cn.FileName = layerNameTextBox.Text;
                }
                if (cn.FilePath.Contains(DMEEditor.ConfigEditor.ExePath))
                {
                    cn.FilePath.Replace(DMEEditor.ConfigEditor.ExePath, ".");
                }
                cn.DriverName = package.PackageName;
                cn.DriverVersion = package.version;
                cn.ID = DMEEditor.ConfigEditor.DataConnections.Max(p => p.ID) + 1;
                cn.Category = DatasourceCategory.RDBMS;
                Layer.DataSourceName= this.layerNameTextBox.Text;
                Layer.DataViewDataSourceName = this.dataViewDataSourceNameComboBox.Text;
                Layer.Entities = new List<EntityStructure>();
                compositeQueryLayersBindingSource.EndEdit();
                DMEEditor.ConfigEditor.RemoveDataSourceEntitiesValues(Layer.DataSourceName);
                ILocalDB db = (ILocalDB)DMEEditor.CreateLocalDataSourceConnection(cn,cn.ConnectionName,package.classHandler);
                db.CreateDB();
             
                DMEEditor.ConfigEditor.AddDataConnection(cn);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
              //  DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                //--------------------
                try
                {
                  //  waitForm = new WaitFormFunc();
                  //  waitForm.Show(this.ParentForm);
                    CompositeLayerDataSource compositeLayerDataSource = new CompositeLayerDataSource(cn.ConnectionName, DMEEditor.Logger, DMEEditor, cn.DatabaseType, DMEEditor.ErrorObject);
                    ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(cn);
                    compositeLayerDataSource.Dataconnection.ConnectionProp = cn;
                    compositeLayerDataSource.Dataconnection.DataSourceDriver = driversConfig;
                    compositeLayerDataSource.LocalDB = db;
                    // compositeLayerDataSource.Dataconnection.OpenConnection();
                    DMEEditor.OpenDataSource(cn.ConnectionName);
                    compositeLayerDataSource.Openconnection();
                    //   Visutil.treeEditor.ShowWaiting();
                    //   Visutil.treeEditor.ChangeWaitingCaption($"Getting  Composed Layer Entities Total:{compositeLayerDataSource.Entities.Count}");
                    compositeLayerDataSource.GetAllEntitiesFromDataView();
                //    Visutil.treeEditor.HideWaiting();
                    DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                    RootCompositeLayerBranch.CreateChildNodes();
                 //   waitForm.Close();
                }
                catch (Exception ex1)
                {

                    string errmsg = $"Error Creating Composite Layer for view";
                 //   waitForm.Close();
                    DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex1.Message}", DateTime.Now, 0, null, Errors.Failed);
                }
              
                MessageBox.Show($"Creating Composite Layer for view {branch.BranchText}");
                DMEEditor.AddLogMessage("Success", $"Creating Composite Layer for view {branch.BranchText}", DateTime.Now, 0, null, Errors.Ok);
                this.ParentForm.Close();
            }
            catch (Exception ex)
            {
                string errmsg = $"Error Creating Composite Layer for view {branch.BranchText}";
              
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                MessageBox.Show(errmsg);
            }
            
        }

        private void LocalDBDriverComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            string pkname = localDBDriverComboBox.Text;
            localDBDriverVersionComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.classHandler == pkname))
            {
                localDBDriverVersionComboBox.Items.Add(item.version);
            }
        }
    }
}
