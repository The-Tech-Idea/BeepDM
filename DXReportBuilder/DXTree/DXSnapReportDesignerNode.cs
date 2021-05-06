using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace DXReportBuilder.DXTree
{
    public class DXSnapReportDesignerNode: IBranch, ITreeView
    {
        public DXSnapReportDesignerNode()
    {

    }
    public DXSnapReportDesignerNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename, string ConnectionName)
    {



        TreeEditor = pTreeEditor;
        DMEEditor = pDMEEditor;
        ParentBranchID = pParentNode.ID;
        BranchText = pBranchText;
        BranchType = pBranchType;
            ReportDefinition = ConnectionName;
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
        public string BranchText { get; set; } = "Snap Report Designer";
    public IDMEEditor DMEEditor { get; set; }
    public IDataSource DataSource { get; set; }
    public string DataSourceName { get; set; }
    public int Level { get; set; }
    public EnumBranchType BranchType { get; set; } = EnumBranchType.DataPoint;
    public int BranchID { get; set; }
    public string IconImageName { get; set; } = "snapdesigner.ico";
    public string BranchStatus { get; set; }
    public int ParentBranchID { get; set; }
    public string BranchDescription { get; set; }
    public string BranchClass { get; set; } = "REPORTING";
    public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
    public ITree TreeEditor { get; set; }
    public List<string> BranchActions { get; set; }
    public object TreeStrucure { get; set; }
    public IVisUtil Visutil { get; set; }
    public int MiscID { get; set; }
        public string ReportDefinition;
    public AddinTreeStructure AddinTreeStructure { get; set; }

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
    [BranchDelegate(Caption = "Edit", Hidden = false)]
    public IErrorsInfo Edit()
    {

        try
        {
                string[] args = { BranchText };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = DMEEditor.ConfigEditor.ReportsDefinition.Where(p => p.Name.Equals(ReportDefinition, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); ;
                it.Name = "ReportDefinition";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
            {  // Obj= obj,
                Addin = null,
                AddinName = null,
                AddinType = null,
                DMView = null,
                CurrentEntity = BranchText,
                ObjectName = ReportDefinition,
                Id = BranchID,
                Objects=ob,
                ObjectType = "DXSNAPREPORT",
                DataSource = DataSource,
                EventType = "Run"

            };

           
                Visutil.ShowFormFromAddin("Frm_DxSnapDesigner", DMEEditor, args, Passedarguments);
           

            DMEEditor.AddLogMessage("Success", "Shown Module " + BranchText, DateTime.Now, 0, null, Errors.Ok);
        }
        catch (Exception ex)
        {
            string mes = "Could not Show Module " + BranchText;
            DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        };
        return DMEEditor.ErrorObject;
    }
        [BranchDelegate(Caption = "Preview", Hidden = false)]
        public IErrorsInfo Preview()
        {

            try
            {
                string[] args = { BranchText };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = DMEEditor.ConfigEditor.ReportsDefinition.Where(p => p.Name.Equals(ReportDefinition, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); ;
                it.Name = "ReportDefinition";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {  // Obj= obj,
                    Addin = null,
                    AddinName = null,
                    AddinType = null,
                    DMView = null,
                    CurrentEntity = BranchText,
                    ObjectName = ReportDefinition,
                    Id = BranchID,
                    Objects = ob,
                    ObjectType = "DXSNAPREPORT",
                    DataSource = DataSource,
                    EventType = "Run"

                };


                Visutil.ShowFormFromAddin("uc_snapreportviewer", DMEEditor, args, Passedarguments);


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
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = DMEEditor.ConfigEditor.ReportsDefinition.Where(p => p.Name.Equals(ReportDefinition, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); ;
                it.Name = "ReportDefinition";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {  // Obj= obj,
                    Addin = null,
                    AddinName = null,
                    AddinType = null,
                    DMView = null,
                    CurrentEntity = BranchText,
                    ObjectName = ReportDefinition,
                    Id = BranchID,
                    Objects = ob,
                    ObjectType = "DXSNAPREPORT",
                    DataSource = DataSource,
                    EventType = "Run"

                };


                Visutil.ShowFormFromAddin("Frm_DxSnapDesigner", DMEEditor, args, Passedarguments);


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


            DMEEditor.AddLogMessage("Success", "Created child Nodes", DateTime.Now, 0, null, Errors.Ok);
        }
        catch (Exception ex)
        {
            string mes = "Could not Create child Nodes";
            DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        };
        return DMEEditor.ErrorObject;

    }
    #endregion"Other Methods"

    }
}
