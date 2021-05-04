﻿using DXReportBuilder.DXTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace DXReportBuilder.Tree
{
    public class DXTreeRootNode : IBranch, ITreeView, IOrder
    {
        #region "Properties"
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public int Order { get; set; } = 17;
        public string Name { get; set; }
        public string BranchText { get; set; } = "Reporting";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Root;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "reports.ico";
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

        //#region "Reports L/S"
        //public List<ReportTemplate> Reports { get; set; } = new List<ReportTemplate>();
        //public void SaveReportsValues()
        //{
        //    string path = Path.Combine(DMEEditor.ConfigEditor.ConfigPath, "DXreports.json");
        //    DMEEditor.ConfigEditor.JsonLoader.Serialize(path, Reports);

        //}
        //public List<ReportTemplate> LoadReportsValues()
        //{
        //    string path = Path.Combine(DMEEditor.ConfigEditor.ConfigPath, "DXreports.json");
        //    Reports = DMEEditor.ConfigEditor.JsonLoader.DeserializeObject<ReportTemplate>(path);
        //    return Reports;
        //}
        //#endregion "Reports L/S"
      
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
        [BranchDelegate(Caption = "Create New Report")]
        public IErrorsInfo CreateReport()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
               
                //Reports
              
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = null,
                    ObjectType = "GENERATEREPORT",
                    DataSource = null,
                    ObjectName = null,
                    Id = 1,
                    Objects = ob,
                    DatasourceName = null,
                    EventType = "GENERATEREPORT"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_reportdefinition", DMEEditor, args, Passedarguments);

                DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
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
                foreach (ReportTemplate item in DMEEditor.ConfigEditor.ReportsDefinition)
                {
                    if (!ChildBranchs.Where(p => p.BranchText.Equals(item.Name, StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        DXReportDefinitionNode entityNode = new DXReportDefinitionNode(TreeEditor, DMEEditor, this, item.Name, TreeEditor.SeqID, EnumBranchType.DataPoint, "reportdefinition.ico", item.Name);
                        TreeEditor.AddBranch(this, entityNode);
                        ChildBranchs.Add(entityNode);
                        entityNode.CreateChildNodes();
                    }
                  
                }
                //foreach (AddinTreeStructure item in DMEEditor.ConfigEditor.AddinTreeStructure)
                //{
                //    if (BranchText == item.RootName)
                //    {
                //        DXTreeEntityNode entityNode = new DXTreeEntityNode(TreeEditor, DMEEditor, this, item.NodeName, TreeEditor.SeqID, EnumBranchType.Entity, item.Imagename, item.className);
                //        entityNode.AddinTreeStructure = item;
                //        TreeEditor.AddBranch(this, entityNode);
                //        ChildBranchs.Add(entityNode);
                //    }
                //}

                DMEEditor.AddLogMessage("Success", "Created report Definitions nodes", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create report Definitions Nodes";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        #endregion"Other Methods"
    }
}