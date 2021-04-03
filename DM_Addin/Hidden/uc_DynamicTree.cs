using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Hidden
{
    public partial class uc_DynamicTree : UserControl,IDM_Addin
    {
        public uc_DynamicTree()
        {
            InitializeComponent();
        }

        public string AddinName { get; set; } = "Dynamic Data View Tree";
        public string Description { get; set; } = "Dynamic Data View Tree";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = false;
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Args { get; set; }
        public IVisUtil Visutil { get; set; }
        ITree TreeEditor;
       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Args = e;

            Logger = plogger;
            ErrorObject = per;
            // visutil = new VisUtil(Logger,putil,per);

            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            TreeEditor =(ITree) DMEEditor.Utilfunction.GetInstance("TheTechIdea.Winforms.VIS.Tree");
            ITreeView treeView = (ITreeView)TreeEditor;
            treeView.Visutil = Visutil;
            TreeEditor.DMEEditor = DMEEditor;
            Visutil.treeEditor = TreeEditor;
            try
            {
                TreeEditor.TreeStrucure = treeView1;
               // TreeEditor.ColumnsListViewstructure = listView1;
            }
            catch (Exception )
            {

                throw;
            }
         
            TreeEditor.CreateRootTree();
        }
    }
}
