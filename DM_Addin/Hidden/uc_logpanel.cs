using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea;

namespace TheTechIdea.Hidden
{
    public partial class uc_logpanel : UserControl, IDM_Addin
    {
        public uc_logpanel()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get; set; } = "Log Panel";
        public string Description { get; set; } = "Log all Messeges";
        public bool DefaultCreate { get; set; } = false;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IDataSource DestConnection { get ; set ; }
        public IDataSource SourceConnection { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject  { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public IPassedArgs Passedarg { get ; set ; }
        public delegate void InvokeDelegate(object sender, string e);
       // public event EventHandler<PassedArgs> OnObjectSelected;
        public bool startLoggin = false;
        public void RaiseObjectSelected()
        {
           
        }
        public void Run(string param1)
        {
        }
        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
           DMEEditor = pbl;
           ErrorObject  = pbl.ErrorObject;
           Logger = pbl.Logger;
           Logger.Onevent += Logger_Onevent;
         //  TextBox1.TextChanged += TextBox1_TextChanged;
        }
        //private void TextBox1_TextChanged(object sender, EventArgs e)
        //{
        //    TextBox1.SelectionStart = TextBox1.Text.Length;
        //    TextBox1.ScrollToCaret();
        //}
        private void Logger_Onevent(object sender, string e)
        {
           if (startLoggin)
            {
                this.TextBox1.BeginInvoke(new Action(() =>
                {
                    this.TextBox1.AppendText(e + Environment.NewLine);
                    TextBox1.SelectionStart = TextBox1.Text.Length;
                    TextBox1.ScrollToCaret();
                }));
            }
        
        }

     
    

    }
}
