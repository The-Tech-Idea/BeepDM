using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;
using static TheTechIdea.DataManagment_Engine.Util;

namespace AI
{
    public class AIRootNode : IBranch, ITreeView, IOrder
    {
        public AIRootNode()
        {
            BranchText = "AI";
            BranchClass = "AI";
            IconImageName = "ai.ico";
            BranchType = EnumBranchType.Root;
        }
       
        public int Order { get; set; } = 11;
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }
        public int ID { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get ; set ; }
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get ; set ; }
        public List<string> BranchActions { get; set; } = new List<string>();
        public EntityStructure EntityStructure { get ; set ; }
        public int MiscID { get ; set ; }
        public string Name { get ; set ; }
        public string BranchText { get; set; } = "AI";
        public int Level { get; set; } 
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Root;
        public int BranchID { get ; set ; }
        public string IconImageName { get; set; } = "ai.ico";
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
        public string BranchClass { get; set; } = "IRONPYTHON";
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                CreateNodes();
                foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "AI"))
                {

                    CreateCategoryNode(i);


                }
                //  CreateNodes();

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
        [BranchDelegate(Caption = "New", Hidden = false)]
        public IErrorsInfo NewWK()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);

                PassedArgs Passedarguments = new PassedArgs
                {  // Obj= obj,
                    Addin = null,
                    AddinName = null,
                    AddinType = null,
                    DMView = null,
                    CurrentEntity = null,
                    ObjectName = null,
                    Id = BranchID,
                    ObjectType = "WORKFLOW",
                    DataSource = DataSource,
                    EventType = "NEW",
                    Objects = ob

                };


                Visutil.ShowUserControlInContainer("uc_WorkFlowManagerMainScreen", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);




                DMEEditor.AddLogMessage("Success", "Shown Module " + BranchText, DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Show Module " + BranchText;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

        [BranchDelegate(Caption = "DoubleClick", Hidden = true, DoubleClick = true)]
        public IErrorsInfo DoubleClick()
        {

            try
            {
                string[] args = { BranchText };
                PassedArgs Passedarguments = new PassedArgs
                {  // Obj= obj,
                    Addin = null,
                    AddinName = null,
                    AddinType = null,
                    DMView = null,
                    CurrentEntity = null,
                    ObjectName = null,
                    Id = BranchID,
                    ObjectType = "UserControl",
                    DataSource = DataSource,
                    EventType = "Run"

                };


                Visutil.ShowUserControlInContainer("uc_WorkFlowManagerMainScreen", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);

                DMEEditor.AddLogMessage("Success", "Shown Module " + BranchText, DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Show Module " + BranchText;
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
                TreeEditor.RemoveChildBranchs(this);
                foreach (AssemblyClassDefinition item in DMEEditor.ConfigEditor.BranchesClasses.Where(o=>o.RootName=="AI" ))
                {
                    if (item.PackageName != this.Name)
                    {
                        Type adc = DMEEditor.Utilfunction.GetType(item.PackageName);
                        ConstructorInfo ctor = adc.GetConstructors().First();
                        ObjectActivator<IBranch> createdActivator = GetActivator<IBranch>(ctor);
                        IBranch br = createdActivator();
                        int id = TreeEditor.SeqID;
                        br.Name = item.PackageName;
                        br.ID = id;
                        br.BranchID = id;
                        br.TreeEditor = TreeEditor;
                        br.BranchID = id;
                        br.ID = id;
                        TreeEditor.AddBranch(this, br);
                        ITreeView treeView = (ITreeView)br;
                        treeView.Visutil = Visutil;

                        br.DMEEditor = DMEEditor;

                        ChildBranchs.Add(br);
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
        private IErrorsInfo CreateCategoryNode(CategoryFolder p)
        {
            try
            {
                AICategoryNode categoryBranch = new AICategoryNode(TreeEditor, DMEEditor, this, p.FolderName, TreeEditor.SeqID, EnumBranchType.Category, "category.ico");
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
        #endregion"Other Methods"
    }
}

    