using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public class appclass : IBranch, ITreeView, IOrder
    {
        #region "Properties"
        public int Order { get; set; } = 8;
        public int ID { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; } = new List<string>();
        public EntityStructure EntityStructure { get; set; } = new EntityStructure();
        public int MiscID { get; set; }
        public string Name { get; set; }
        public string BranchText { get; set; } = "Apps";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Root;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "designer.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "APP";
        public object TreeStrucure { get; set; }
        public IVisUtil Visutil { get; set; }

        
        // public event EventHandler<PassedArgs> ActionNeeded;
        #endregion "Properties"

        public IErrorsInfo CreateChildNodes()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
