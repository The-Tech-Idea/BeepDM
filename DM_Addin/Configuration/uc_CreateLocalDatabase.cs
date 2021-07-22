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
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Configuration
{
    public partial class uc_CreateLocalDatabase : UserControl,IDM_Addin
    {
        public uc_CreateLocalDatabase()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string AddinName { get; set; } = "Create Local Database";
        public string ObjectType { get; set; } = "UserControl";
        public string Description { get; set; } = "Create Local Database";
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
        public IPassedArgs Passedarg { get ; set ; }
        public IVisUtil Visutil { get; set; }
       // IBranch RootAppBranch;
        IBranch branch;
       // public event EventHandler<PassedArgs> OnObjectSelected;

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
            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }

          
         
            //foreach (var item in Enum.GetValues(typeof(DataSourceType)))
            //{
            //    data.Items.Add(item);
            //}
            //foreach (var item in Enum.GetValues(typeof(DatasourceCategory)))
            //{
            //    CategorycomboBox.Items.Add(item);
            //  //  var it = DatasourceCategorycomboBox.Items.Add(item);

            //}
            foreach (ConnectionDriversConfig cls in DMEEditor.ConfigEditor.DataDriversClasses.Where(x=>x.CreateLocal==true))
            {
                this.EmbeddedDatabaseTypecomboBox.Items.Add(cls.classHandler);
            }
            foreach (StorageFolders p in DMEEditor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.DataFiles || x.FolderFilesType == FolderFileTypes.ProjectData))
            {
                try
                {
                    this.InstallFoldercomboBox.Items.Add(p.FolderPath);

                }
                catch (FileLoadException ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Ex = ex;
                    Logger.WriteLog($"Error Loading icons ({ex.Message})");
                }
            }
            this.CreateDBbutton.Click += CreateDBbutton_Click;
        }
        private ConnectionProperties CreateConn()
        {
            try
            {

                ConnectionProperties dataConnection = new ConnectionProperties();
                ConnectionDriversConfig package = DMEEditor.ConfigEditor.DataDriversClasses.Where(x => x.classHandler == EmbeddedDatabaseTypecomboBox.Text).OrderByDescending(o=>o.version).FirstOrDefault();


                dataConnection.Category = package.DatasourceCategory;//(DatasourceCategory)(int) Enum.Parse(typeof( DatasourceCategory),CategorycomboBox.Text);
                dataConnection.DatabaseType = package.DatasourceType; //(DataSourceType)(int)Enum.Parse(typeof(DataSourceType), DatabaseTypecomboBox.Text);
                dataConnection.ConnectionName = databaseTextBox.Text;
                dataConnection.DriverName = package.PackageName;
                dataConnection.DriverVersion = package.version;
                dataConnection.ID = DMEEditor.ConfigEditor.DataConnections.Max(y => y.ID) + 1;
                dataConnection.FilePath = InstallFoldercomboBox.Text;
                dataConnection.FileName = databaseTextBox.Text;
                dataConnection.ConnectionString =  package.ConnectionString; //Path.Combine(dataConnection.FilePath, dataConnection.FileName);
                if (dataConnection.FilePath.Contains(DMEEditor.ConfigEditor.ExePath))
                {
                    dataConnection.FilePath.Replace(DMEEditor.ConfigEditor.ExePath, ".");
                }
                //  dataConnection.Host = "localhost";
                dataConnection.UserID = "";
                dataConnection.Password = passwordTextBox.Text;
             //   dataConnection.Database = Path.Combine(dataConnection.FilePath, dataConnection.FileName);
                return dataConnection;
            }
            catch (Exception )
            {

                return null;
            }
        }
        private void CreateDBbutton_Click(object sender, EventArgs e)
        {
            try

            {
               
              if (!DMEEditor.ConfigEditor.DataConnectionExist(databaseTextBox.Text))
                {
                    ConnectionProperties cn = CreateConn();
                    IDataSource ds = DMEEditor.CreateLocalDataSourceConnection(cn, databaseTextBox.Text, EmbeddedDatabaseTypecomboBox.Text);
                    
                    if (ds != null)
                    {
                        ILocalDB dB = (ILocalDB)ds;
                        bool ok= dB.CreateDB();
                        if (ok)
                        {
                            //ds.ConnectionStatus = ds.Dataconnection.OpenConnection();
                            DMEEditor.OpenDataSource(cn.ConnectionName);
                        }
                        DMEEditor.ConfigEditor.AddDataConnection(cn);
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                        if(Passedarg.ObjectType=="COMPOSITEDB")
                        {

                        }
                        branch.CreateChildNodes();
                        MessageBox.Show("Local/Embedded Database Created successfully", "Beep");
                    }
                    else
                    {
                        MessageBox.Show("Coudl not Create Local/Embedded Database ", "Beep");
                    }
                }
                else
                {
                    MessageBox.Show("Database Already Exist by this name please try another name ", "Beep");
                }
               


               
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error creating Database";
                MessageBox.Show(errmsg, "Beep");
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
        }


      
    }
}
