using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.Util;


namespace TheTechIdea.DataManagment_Engine.Vis
{
    public interface ITree
    {
        string CategoryIcon { get; set; }
        string SelectIcon { get; set; }
        IBranch CurrentBranch { get; set; }
        IDMEEditor DMEEditor { get; set; }
        int SeqID { get; }
        List<IBranch> Branches { get; }
        object TreeStrucure { get; set; }
         int SelectedBranchID { get; set; } 
         List<int> SelectedBranchs { get; set; } 
        PassedArgs args { get; set; }

        IErrorsInfo CreateBranch(IBranch Branch);
        IErrorsInfo AddBranch(IBranch ParentBranch, IBranch Branch);
        bool RemoveEntityFromCategory(string root, string foldername, string entityname);
        IErrorsInfo RemoveBranch(int id);
        IErrorsInfo RemoveBranch(IBranch Branch);
        IErrorsInfo RemoveChildBranchs(IBranch branch);
        IErrorsInfo MoveBranchToParent(IBranch ParentBranch, IBranch CurrentBranch);
        IErrorsInfo RemoveCategoryBranch(int id);
        IErrorsInfo SendActionFromBranchToBranch(IBranch ParentBranch, IBranch CurrentBranch, string ActionType);
        IBranch GetBranch(int pID);
        IBranch GetBranchByMiscID(int pID);

        IErrorsInfo AddCategory(IBranch Rootbr);

        string CheckifBranchExistinCategory(string BranchName, string pRootName);
        int GetImageIndexFromConnectioName(string Connectioname);
        int GetImageIndex(string imagename);
        IErrorsInfo RunMethod(object branch, string MethodName);
        IErrorsInfo CreateRootTree();
        IErrorsInfo CopySelectedEntities();
        void ShowWaiting();
        void HideWaiting();
        void ChangeWaitingCaption(string Caption);
        void AddCommentsWaiting(string comment);

        LScriptHeader CreateScriptToCopyEntities(IDataSource dest, List<EntityStructure> entities, IProgress<int> progress, bool copydata = true);
        IErrorsInfo ShowRunScriptGUI(IBranch RootBranch, IBranch Branch ,IDataSource ds, LScriptHeader script);

    }
}
