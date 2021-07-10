using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;

using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Vis;
using System;

using System.Data;

using System.Linq;


using System.Windows.Forms;
using TheTechIdea.Util;
using System.Drawing;

namespace TheTechIdea.Hidden
{
    public partial class Frm_MainDisplayForm : Form,IDM_Addin
    {
        public Frm_MainDisplayForm()
        {
            InitializeComponent();
        }

        public string AddinName { get; set; } = "Beep";
        public string Description { get; set; } = "Data Management Main Display";
        public string DllName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "Form";
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public IRDBSource RdbmsDs { get; set; }
        public IDataSource FileDs { get; set; }
        public Boolean DefaultCreate { get; set; } = false;
        public IDMLogger Logger { get; set; }
       
        public IDMEEditor DMEEditor { get; set; }
        public string EntityName { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public DataSet Dset { get; set; }
       
        public IVisUtil Visutil { get; set; }
        public IErrorsInfo ErrorObject  { get; set; }
        public IUtil Util { get; set; }
        public IPassedArgs Passedarg { get; set; }
        public PassedArgs PassedArgsFromFunctionTree { get; set; }
        public PassedArgs PassedArgsFromDataTree { get; set; }
        public IDMDataView MyDataView { get; set; } 
       // public event EventHandler<PassedArgs> OnObjectSelected;
        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs obj, IErrorsInfo per)
        {
            Passedarg=  obj;
            //SourceConnection = pdataSource;

           ErrorObject  = per;
            Logger = plogger;
            DMEEditor = pDMEEditor;
         
            Util = putil;
            obj.AddinName = AddinName;
            string[] args1 = { AddinName, null, null };
            var o= obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault();
            Visutil = (IVisUtil)o.obj;
            Visutil.DisplayPanel=(Control)this.splitContainer1.Panel1;
            Visutil.ParentForm = this;
            this.uc_logpanel1.SetConfig(pDMEEditor, Logger, putil, args, obj, per);
            this.uc_DynamicTree1.SetConfig(pDMEEditor, Logger, putil, args, obj, per);
            this.Shown += Frm_MainDisplayForm_Shown;
          //  this.ResizeEnd += Frm_MainDisplayForm_ResizeEnd;
           
        }

        private void Frm_MainDisplayForm_ResizeEnd(object sender, EventArgs e)
        {
       //    this.uc_DynamicTree1.Dock = DockStyle.Fill;
        }

        private void Frm_MainDisplayForm_Shown(object sender, EventArgs e)
        {
            uc_logpanel1.startLoggin = true;
            Rectangle resolutionRect = System.Windows.Forms.Screen.FromControl(this).Bounds;
            if (this.Width != resolutionRect.Width || this.Height != resolutionRect.Height)
            {
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int w = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
                int h = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
                this.Location = new Point((screen.Width - w) / 2, (screen.Height - h) / 2);
                this.Size = new Size(w, h);
            }
        }

      

    }
}
