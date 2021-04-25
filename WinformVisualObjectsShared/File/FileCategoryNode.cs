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
    public class FileCategoryNode : IBranch, ITreeView
    {
        public FileCategoryNode()
        {

        }
        public FileCategoryNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
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
        public string Name { get; set; }
        public string BranchText { get; set; } = "Files";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Category;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "Category.ico";
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
                    CategoryFolder x = DMEEditor.ConfigEditor.CategoryFolders.Where(y => y.FolderName == BranchText && y.RootName == "FILE").FirstOrDefault();

                    if (x.items.Contains(f.FileName) == false)
                    {
                        x.items.Add(f.FileName);
                    }
                   
                    CreateFileNode( f.FileName);
                }
                DMEEditor.ConfigEditor.SaveCategoryFoldersValues();
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
        [BranchDelegate(Caption = "Remove Category(files will not removed)", Hidden = false)]
        public IErrorsInfo Remove()
        {

            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("Remove", "Area you Sure  you want to remove Category???") == System.Windows.Forms.DialogResult.Yes)
                {

                     
                        TreeEditor.RemoveEntityFromCategory("FILE", TreeEditor.GetBranch(ParentBranchID).BranchText, BranchText);
                        TreeEditor.RemoveBranch(this);
                       
                        DMEEditor.ConfigEditor.SaveCategoryFoldersValues();
                   
                  
                }
                DMEEditor.AddLogMessage("Success", "Remove Category", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove Category";
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


                if (BranchType == EnumBranchType.Category)
                {
                 
                    foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "FILE" && x.FolderName == BranchText))
                    {
                        foreach (string item in i.items)
                        {
                            CreateFileNode(item);
                        }



                    }
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
        private IBranch CreateFileNode( string FileName)
        {
            FileEntityNode viewbr = null;
            try
            {
                string ext = Path.GetExtension(FileName).Remove(0, 1);
                IconImageName = ext + ".ico";
                viewbr = new FileEntityNode(TreeEditor, DMEEditor, this, FileName, TreeEditor.SeqID, EnumBranchType.DataPoint, IconImageName, FileName);
                viewbr.DataSource = DataSource;
               // viewbr.DataSourceName = DataSourceName;
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
        public virtual List<ConnectionProperties> LoadFiles()
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
                {
                    Title = "Browse Files",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    DefaultExt = "*",
                    Filter = DMEEditor.ConfigEditor.CreateFileExtensionString(), // "txt files (*.txt)|*.txt|csv files (*.csv)|*.csv|xls files (*.xls)|*.xls|All files (*.*)|*.*",
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
                            Ext = Path.GetExtension(file).Replace(".", "").ToLower(),
                            ConnectionName = Path.GetFileName(file)


                        };
                        string ext = Path.GetExtension(file).Replace(".", "").ToLower();
                        List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
                        ConnectionDriversConfig c = clss.Where(o => o.extensionstoHandle.Contains(ext)).FirstOrDefault();
                        f.DriverName = c.PackageName;
                        f.DriverVersion = c.version;
                        f.Category = c.DatasourceCategory;

                        switch (f.Ext.ToLower())
                        {
                            case "txt":
                                f.DatabaseType = DataSourceType.Text;
                                break;
                            case "csv":
                                f.DatabaseType = DataSourceType.CSV;
                                break;
                            case "xml":
                                f.DatabaseType = DataSourceType.xml;

                                break;
                            case "json":
                                f.DatabaseType = DataSourceType.Json;
                                break;
                            case "xls":
                            case "xlsx":
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
