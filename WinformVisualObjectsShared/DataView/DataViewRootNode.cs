using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class DataViewRootNode  : IBranch, ITreeView,IOrder,IBranchRootCategory
    {
        public DataViewRootNode()
        {

        }
        public DataViewRootNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            IconImageName = pimagename;
            if (pID != 0)
            {
                ID = pID;
                BranchID = ID;
            }
            //.GetImageIndex(ParentTree, MainNode, "dataview.ico");
        }
        #region "Properties"
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public int Order { get; set; } = 0;
        public string Name { get; set; }
        public string BranchText { get; set; } = "DataView";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; }= EnumBranchType.Root;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "dataview.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "VIEW";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public object TreeStrucure { get; set; }
        public IVisUtil Visutil { get; set; }
        public int MiscID { get; set; }

       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;

        public IDMDataView DataView { get; set; }
        public int DataViewID { get; set; }

        #endregion "Properties"
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                TreeEditor.RemoveChildBranchs(this);
                foreach (ConnectionProperties i in DMEEditor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.VIEWS))
                {

                    if (TreeEditor.CheckifBranchExistinCategory(i.ConnectionName, "VIEW") == null)
                    {
                        // ObjectDataSourcetemp = i.FileName;

                        CreateViewNode(i.ID,i.FileName, i.ConnectionName);

                        i.Drawn = true;
                    }
                }
                foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "VIEW"))
                {

                    CreateCategoryNode(i);


                }

                //  DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Views";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo ExecuteBranchAction(string ActionName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo MenuItemClicked(string ActionNam)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RemoveChildNodes()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
        {
            try
            {
                TreeEditor = pTreeEditor;
                DMEEditor = pDMEEditor;
                ParentBranchID = pParentNode.ID;
                BranchText = pBranchText;
                BranchType = pBranchType;
                IconImageName = pimagename;
                if (pID != 0)
                {
                    ID = pID;
                }

            //    DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Set Config";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion "Interface Methods"
        #region "Exposed Interface"
        [BranchDelegate(Caption = "Add Category")]
        public IErrorsInfo AddCategory()
        {

            try
            {
                TreeEditor.AddCategory(this);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Create View")]
        public IErrorsInfo CreateView()
        {

            try
            {
                string viewname = null;
                string fullname = null;
                if (Visutil.controlEditor.InputBox("Create View", "Please Enter Name of View (Name Should not exist already in Views)", ref viewname) == System.Windows.Forms.DialogResult.OK)
                {
                    if ((viewname != null) && DMEEditor.ConfigEditor.DataConnectionExist(viewname+".json")==false )
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


                        CreateViewNode(DataView.ViewID, DataView.ViewName, f.ConnectionName);
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
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Add View File")]
        public IErrorsInfo AddViewFile()
        {

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
                    if ((viewname != null) && DMEEditor.ConfigEditor.DataConnectionExist(viewname+".json") == false)
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


                        CreateViewNode(DataView.ViewID, DataView.ViewName, f.ConnectionName);


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
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Create View using Table",Hidden =true)]
        public IErrorsInfo CreateView(IBranch EntitySource)
        {

            try
            {
                string viewname = null;
                string fullname = null;
                if (Visutil.controlEditor.InputBox("Create View", "Please Enter Name of View (Name Should not exist already in Views)", ref viewname) == System.Windows.Forms.DialogResult.OK)
                {
                    if ((viewname != null) && DMEEditor.ConfigEditor.DataConnectionExist(viewname+".json") == false)
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
                       
                        if (EntitySource != null)
                        {
                           
                            int x = ds.AddEntitytoDataView(EntitySource.DataSource, EntitySource.BranchText, EntitySource.DataSource.Dataconnection.ConnectionProp.SchemaName, null);
                       
                        }
                        ds.WriteDataViewFile(fullname);
                        DataSource = DMEEditor.GetDataSource(f.ConnectionName);
                        DataView = ds.DataView;
                        DataView.EntityDataSourceID = EntitySource.DataSource.DatasourceName;
                        IBranch br = CreateViewNode(DataView.ViewID, DataView.ViewName, f.ConnectionName);


                        if (EntitySource != null)
                        {
                           
                          //  int x = DMEEditor.viewEditor.AddEntitytoDataView(EntitySource.DataSource, EntitySource.BranchText, EntitySource.DataSource.Dataconnection.ConnectionProp.SchemaName, null, DataView.id);
                            br.CreateChildNodes();
                        }
                      
                        // IBranch br = TreeEditor.GetBranch(DataView.ViewName);



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
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
        private IBranch CreateViewNode(int id, string ViewName,string Connectionname)
        {
            DataViewNode viewbr = null;
            try
            {

                viewbr = new DataViewNode(TreeEditor, DMEEditor, this, ViewName, TreeEditor.SeqID, EnumBranchType.DataPoint, "dataview.ico", Connectionname);
            
                viewbr.DataSource = DataSource;
                viewbr.DataSourceName = Connectionname;
                viewbr.ID = id;

                TreeEditor.AddBranch(this, viewbr);
              
                ChildBranchs.Add(viewbr);

                DMEEditor.AddLogMessage("Success", "Added DataView Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add DataView Connection";
                viewbr = null;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };

            return viewbr;
        }
        public IErrorsInfo CreateCategoryNode(CategoryFolder p)
    {
        try
        {
                DataViewCategoryNode categoryBranch = new DataViewCategoryNode(TreeEditor, DMEEditor,this, p.FolderName, TreeEditor.SeqID, EnumBranchType.Category, "category.ico");
                TreeEditor.AddBranch(this, categoryBranch);
                ChildBranchs.Add(categoryBranch);
                categoryBranch.CreateChildNodes();

         
        }
        catch (Exception ex)
        {
                DMEEditor.Logger.WriteLog($"Error Creating Category  View Node ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
        }
        return DMEEditor.ErrorObject;

    }
        #endregion

}
}
