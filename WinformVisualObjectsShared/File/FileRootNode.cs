using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class FileRootNode  : IBranch, ITreeView,IOrder,IBranchRootCategory
    {
        public FileRootNode()
        {

        }
        public FileRootNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename, string ConnectionName)
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
                BranchID = pID;
            }
        }

        #region "Properties"
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public int Order { get; set; } = 4;
        public string Name { get; set; }
        public string BranchText { get; set; } = "Files";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Root;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "file.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "FILE";
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
        #endregion "Properties"
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                CreateNodes();

                DMEEditor.AddLogMessage("Success", "Added Child Nodes", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Child Nodes";
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

                //   DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
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
        [BranchDelegate(Caption = "Add File(s)", Hidden = false)]
        public IErrorsInfo AddFile()
        {

            try
            {
                List<ConnectionProperties> files = new List<ConnectionProperties>();
                files = LoadFiles();
                foreach (ConnectionProperties f in files)
                {
                    DMEEditor.ConfigEditor.AddDataConnection(f);
                    DMEEditor.GetDataSource(f.FileName);
                    CreateFileNode(f.ID,f.FileName,f.ConnectionName);
                }
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
        public IErrorsInfo CreateNodes()
        {

            try
            {

               // Visutil.GetImageIndex(ParentTree, MainNode, "file.ico");
                foreach (ConnectionProperties i in DMEEditor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.FILE))
                {
                    if (TreeEditor.CheckifBranchExistinCategory(i.ConnectionName, "FILE") == null)
                    {
                        CreateFileNode(i.ID,i.FileName,i.ConnectionName);
                        i.Drawn = true;
                    }



                }
                foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "FILE"))
                {

                    CreateCategoryNode(i);


                }
                DMEEditor.AddLogMessage("Success", "Created child Nodes", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create child Nodes";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        private IBranch CreateFileNode(int id, string FileName, string Connectionname)
        {
            FileEntityNode viewbr = null;
            try
            {
                string ext = Path.GetExtension(Connectionname).Remove(0, 1);
                IconImageName = ext + ".ico";
                viewbr = new FileEntityNode(TreeEditor, DMEEditor, this, FileName, TreeEditor.SeqID, EnumBranchType.DataPoint, IconImageName, Connectionname);
                viewbr.DataSource = DataSource;
             //   viewbr.DataSourceName = DataSourceName;
                TreeEditor.AddBranch(this, viewbr);
                 ChildBranchs.Add(viewbr);
                viewbr.CreateChildNodes();

                DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                viewbr = null;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };

            return viewbr;
        }
        public IErrorsInfo CreateCategoryNode(CategoryFolder p)
        {
            try
            {
                FileCategoryNode categoryBranch = new FileCategoryNode(TreeEditor, DMEEditor, this, p.FolderName, TreeEditor.SeqID, EnumBranchType.Category, "category.ico");
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
        public virtual List<ConnectionProperties> LoadFiles()
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
                {

                    Title = "Browse Text Files",

                    CheckFileExists = true,
                    CheckPathExists = true,

                    DefaultExt = "txt",
                    Filter = "txt files (*.txt)|*.txt|csv files (*.csv)|*.csv|xls files (*.xls)|*.xls|json files (*.json)|*.json|All files (*.*)|*.*",
                    FilterIndex = 2,
                    RestoreDirectory = true

                    //ReadOnlyChecked = true,
                    //ShowReadOnly = true
                };
                openFileDialog1.InitialDirectory = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataFiles).FirstOrDefault().FolderPath;
                openFileDialog1.Multiselect = true;
                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK) // Test result.
                {
                    foreach (String file in openFileDialog1.FileNames)
                    {

                        ConnectionProperties f = new ConnectionProperties
                        {
                            FileName = Path.GetFileName(file),
                            FilePath = Path.GetDirectoryName(file),
                            Ext = Path.GetExtension(file),
                            ConnectionName = Path.GetFileName(file),
                            DriverVersion = "1",
                            DriverName = "FileReader",




                            //Fields = new List<EntityField>()
                        };
                        switch (f.Ext.ToLower())
                        {
                            case ".txt":
                                f.DatabaseType = DataSourceType.Text;
                                break;
                            case ".csv":
                                f.DatabaseType = DataSourceType.CSV;
                                break;
                            case ".xml":
                                f.DatabaseType = DataSourceType.xml;

                                break;
                            case ".json":
                                f.DatabaseType = DataSourceType.Json;
                                break;
                            case ".xls":
                            case ".xlsx":
                                f.DatabaseType = DataSourceType.Xls;
                                break;
                            default:
                                f.DatabaseType = DataSourceType.Text;
                                break;
                        }
                        f.Category = DatasourceCategory.FILE;


                     
                        retval.Add(f);
                    }

                   

                };
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };

        }
        #endregion"Other Methods"
    }
}
