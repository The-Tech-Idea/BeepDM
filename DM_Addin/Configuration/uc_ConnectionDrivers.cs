using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using System.IO;

using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Configuration
{
    public partial class uc_ConnectionDrivers : UserControl,IDM_Addin, IAddinVisSchema
    {
        public uc_ConnectionDrivers()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string AddinName { get; set; } = "Connection Drivers";
        public string ObjectType { get; set; } = "UserControl";
        public string Description { get ; set ; } = "Data Sources Connection Drivers Setup Screen";
        public bool DefaultCreate { get; set; } = true;
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
       // public event EventHandler<PassedArgs> OnObjectSelected;
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 3;
        public int ID { get; set; } = 3;
        public string BranchText { get; set; } = "Connection Drivers";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; } = 3;
        public string IconImageName { get; set; } = "connectiondrivers.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "Data Sources Connection Drivers Setup Screen";
        public string BranchClass { get; set; } = "ADDIN";
        #endregion "IAddinVisSchema"
        public void RaiseObjectSelected()
        {
           
        }

        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Args = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
           
            List<Icon> icons=new List<Icon>();

            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            foreach(AssemblyClassDefinition cls in DMEEditor.ConfigEditor.DataSources)
            {
                this.classHandlerComboBox.Items.Add(cls.className);
            }
            foreach (var item in Enum.GetValues(typeof(DatasourceCategory)))
            {
                DatasourceCategoryComboBox.Items.Add(item);
                //  var it = DatasourceCategorycomboBox.Items.Add(item);

            }
            foreach (var item in Enum.GetValues(typeof(DataSourceType)))
            {
                DatasourceTypeComboBox.Items.Add(item);
                //  var it = DatasourceCategorycomboBox.Items.Add(item);

            }
            foreach (string filename_w_path in Directory.GetFiles(DMEEditor.ConfigEditor.Config.Folders.Where(x=>x.FolderFilesType==FolderFileTypes.GFX).FirstOrDefault().FolderPath, "*.ico", SearchOption.AllDirectories))
            {
                try
                {
                    string filename = Path.GetFileName(filename_w_path);
                  
                    this.iconname.Items.Add(filename);
                    Icon ic = new Icon(filename_w_path);
                    icons.Add(ic);

                }
                catch (FileLoadException ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Ex = ex;
                    Logger.WriteLog($"Error Loading icons ({ex.Message})");
                }
            }
            connectiondriversConfigBindingNavigator.BindingSource = connectiondriversConfigBindingSource;
            connectiondriversConfigBindingSource.DataSource = DMEEditor.ConfigEditor.DataDrivers;
            
           connectiondriversConfigBindingNavigatorSaveItem.Click += ConnectiondriversConfigBindingNavigatorSaveItem_Click;
            this.connectiondriversConfigDataGridView.DataError += ConnectiondriversConfigDataGridView_DataError;
        }

        private void ConnectiondriversConfigDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }
        private bool updateDriversemptycopy()
        {
            try
            {
                foreach (ConnectionDriversConfig item in DMEEditor.ConfigEditor.DataDrivers.Where(c=>c.DbConnectionType!=null).ToList())
                {

                    foreach (ConnectionDriversConfig cfg in DMEEditor.ConfigEditor.DataDrivers.Where(x => x.PackageName == item.PackageName && x.version == item.version && x.classHandler != item.classHandler  && x.DbConnectionType == null ))
                    {
                        cfg.DbConnectionType = item.DbConnectionType;
                        cfg.DbTransactionType = item.DbTransactionType;
                        cfg.AdapterType = item.AdapterType;
                        cfg.parameter1 = item.parameter1;
                        cfg.parameter2 = item.parameter2;
                        cfg.parameter3 = item.parameter3;

                    }
                  
                
                }
                return true;
            }
            catch (Exception )
            {

                return false;
            }
            
                
               
        }
        private void ConnectiondriversConfigBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            SaveData();

        }
        private void SaveData()
        {
            try

            {
                updateDriversemptycopy();
                
                connectiondriversConfigBindingSource.MoveFirst();
                connectiondriversConfigBindingSource.EndEdit();

                DMEEditor.ConfigEditor.SaveConnectionDriversConfigValues();


                MessageBox.Show("Changes Saved Successfuly", "DB Engine");
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error Saving DataSource Drivers Path";
                MessageBox.Show(errmsg, "DB Engine");
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
        }
    }
}
