using System;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public interface ITreeEditor
    {
        TreeNode _RDBMSNode { get; set; }
        TreeNode _NOSQLNode { get; set; }
        TreeNode _DataViewNode { get; set; }
        TreeNode _FilesNode { get; set; }
        TreeNode _WebApiNode { get; set; }
        TreeNode _WorkFlowNode { get; set; }
        NodeID CurrentNode { get; set; }
        Control DisplayPanel { get; set; }
        IDMEEditor DME_Editor { get; set; }
        IErrorsInfo Erinfo { get; set; }
        IDMLogger Logger { get; set; }
        PassedArgs Passedarguments { get; set; }
        string SelectedViewName { get; set; }
        IDM_Addin sender { get; set; }
        TreeView Tree { get; set; }
        string Treetype { get; set; }
        IControlEditor controlEditor { get; set; }
        IUtil Util { get; set; }
        IVisUtil Visutil { get; set; }

        event EventHandler<PassedArgs> OnObjectSelected;

        IErrorsInfo ClearChildsfromTree(TreeNode n, int viewIndex);
        void CreateMenuItems();
        NodeID CreateNodeID(string pnodeType, int pNodeIndex, int pid, string pdescription, string pmisc);
        string CreateNodeIDString(string pnodeType, int pNodeIndex, int pid, string pdescription, string pmisc);
        void CreateTreeRootItemsForEditor();
        void CreateTreeRootItemsForMainDisplay();
        IErrorsInfo FillDataBaseNode(TreeNode node);
        IErrorsInfo FillFileNode(TreeNode node);
        NodeID GetNodeID(string NodeTag);
        NodeID GetNodeID(TreeNode node);
        IDMDataView GetViewFromNode(TreeNode node);
        TreeNode GetViewMainNode(string viewname);
        IErrorsInfo LoadViewFromFile(string viewfilename);
        void NodeEvent(TreeNodeMouseClickEventArgs e, int v, string t);
        IErrorsInfo RemoveDataView(string ViewName);
        TreeNode CreateDBNode(TreeNode parentnode, int id, string ConnectionName);
        IErrorsInfo SaveViewToFile();
        void SetConfig(IDMEEditor pDME_editor, TreeView ptree, string ptreetype, IDM_Addin psender);
        IErrorsInfo ShowTableonTree(int viewIndex, int Tableindex, bool showall = false);
        IErrorsInfo ShowViewonTree(int viewIndex, bool showall = false);
        IErrorsInfo AddNewFileDataSourceGUI();
    }
}