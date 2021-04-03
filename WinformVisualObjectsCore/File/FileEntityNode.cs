using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.FileManager;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class FileEntityNode : IBranch, ITreeView
    {

        public FileEntityNode()
        {


        }
        public FileEntityNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename, string ConnectionName)
        {



            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            DataSourceName = pBranchText;
            string ext = Path.GetExtension(BranchText).Remove(0, 1);
            IconImageName = ext + ".ico";
            //  IconImageName = pimagename;

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
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
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
        [BranchDelegate(Caption = "Show", Hidden = false)]
        public IErrorsInfo Show()
        {

            try
            {


                DMEEditor.AddLogMessage("Success", "Show File", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Show File";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        //[BranchDelegate(Caption = "Remove", Hidden = false)]
        //public IErrorsInfo Remove()
        //{

        //    try
        //    {


        //        DMEEditor.AddLogMessage("Success", "Remove File", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Remove File";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;
        //}
        [BranchDelegate(Caption = "Remove")]
        public IErrorsInfo Remove()
        {

            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("Remove", "Area you Sure ? you want to remove File???") == System.Windows.Forms.DialogResult.Yes)
                {

                    try
                    {
                       // DMEEditor.viewEditor.Views.Remove(DMEEditor.viewEditor.Views.Where(x => x.ViewName == DataView.ViewName).FirstOrDefault());
                        DMEEditor.ConfigEditor.RemoveDataConnection(BranchText);
                        DMEEditor.RemoveDataDource(BranchText);
                        TreeEditor.RemoveBranch(this);
                        TreeEditor.RemoveEntityFromCategory("FILE",TreeEditor.GetBranch(ParentBranchID).BranchText, BranchText);
                        DMEEditor.ConfigEditor.SaveCategoryFoldersValues();
                        DMEEditor.AddLogMessage("Success", "Removed View from Views List", DateTime.Now, 0, null, Errors.Ok);
                    }
                    catch (Exception ex)
                    {
                        string mes = "Could not Remove View from Views List";
                        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                    };

                
                    TreeEditor.RemoveBranch(this);
                }


                DMEEditor.AddLogMessage("Success", "Remove View", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove View";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
        public IErrorsInfo CreateNodes()
        {
          //  TxtXlsCSVFileSource fs = null; ;
            try
            {
                int i = 0;
                DataSource = (IDataSource)DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {
                    //fs = (TxtXlsCSVFileSource)DataSource;
                    DataSource.GetEntitesList();
                }

                if (DataSource != null)
                {

                    if (DataSource.EntitiesNames.Count > 0)
                    {
                        foreach (string n in DataSource.EntitiesNames)
                        {
                            CreateFileItemSheetsNode(i, n);
                            i += 1;
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
        private IErrorsInfo CreateFileItemSheetsNode(int id, string Sheetname)
        {

            try
            {
                FileEntitySheetNode fileitemsheet = new FileEntitySheetNode(TreeEditor, DMEEditor, this, Sheetname, TreeEditor.SeqID, EnumBranchType.DataPoint, IconImageName, BranchText);
                fileitemsheet.DataSource = DataSource;
                ChildBranchs.Add(fileitemsheet);
                TreeEditor.AddBranch(this,fileitemsheet);

                DMEEditor.AddLogMessage("Success", "Added sheet", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add sheet";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
          
           
        }
        #endregion"Other Methods"

    }
}
