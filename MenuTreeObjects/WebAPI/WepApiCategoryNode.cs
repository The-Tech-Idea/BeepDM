﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS.WebAPI
{

    public class WepApiCategoryNode : IBranch, ITreeView

    {
        public WepApiCategoryNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
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
                BranchID = ID;
            }

        }

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
        public string BranchText { get ; set ; }
        public int Level { get ; set ; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Category;
        public int BranchID { get ; set ; }
        public string IconImageName { get ; set ; }
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
        public string BranchClass { get; set; } = "WEBAPI";
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }

       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;

        public IErrorsInfo CreateChildNodes()
        {
            try
            {

                foreach (CategoryFolder p in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "WEBAPI" && x.FolderName == BranchText))
                {
                    foreach (string item in p.items)
                    {
                       ConnectionProperties i = DMEEditor.ConfigEditor.DataConnections.Where(x => x.ConnectionName == item).FirstOrDefault();

                        if (i != null)
                        {
                            CreateWebApiNode(i.ConnectionName); //Path.Combine(i.FilePath, i.FileName)
                        }

                    }

                }
                //DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add View to Category";
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
            throw new NotImplementedException();
        }

        private IBranch CreateWebApiNode(string WebApiName)
        {
            WebApiNode viewbr = null;
            try
            {

                viewbr = new WebApiNode(TreeEditor, DMEEditor, this, WebApiName, TreeEditor.SeqID, EnumBranchType.DataPoint, "app.ico");
                TreeEditor.AddBranch(this, viewbr);
                ChildBranchs.Add(viewbr);
                viewbr.CreateChildNodes();

                //    DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                viewbr = null;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };

            return viewbr;
        }
    }
}
