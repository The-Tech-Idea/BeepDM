using System;
using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public interface IBranch: IBranchID
    {
        int ID { get; set; }
        IDMEEditor DMEEditor { get; set; }
        IDataSource DataSource { get; set; }
        string DataSourceName { get; set; }
        List<IBranch> ChildBranchs { get; set; }
        ITree TreeEditor { get; set; }
        List<string> BranchActions { get; set; }
        EntityStructure EntityStructure { get; set; }
        int MiscID { get; set; }
        IErrorsInfo ExecuteBranchAction(string ActionName);
        IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename);
        IErrorsInfo CreateChildNodes();
        IErrorsInfo RemoveChildNodes();
        IErrorsInfo MenuItemClicked(string ActionNam);
      




    }
}
