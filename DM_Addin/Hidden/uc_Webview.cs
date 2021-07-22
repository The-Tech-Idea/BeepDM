using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.Vis;


namespace TheTechIdea.Hidden
{
    public partial class uc_Webview : UserControl, IDM_Addin
    {
        public uc_Webview()
        {
            InitializeComponent();
           
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Web View";
        public string Description { get; set; } = "Web View";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public IPassedArgs Passedarg { get ; set ; }
      
       // public event EventHandler<PassedArgs> OnObjectSelected;
        string Url;
        public IVisUtil Visutil { get; set; }
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {

            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            //this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            InitializeAsync();
            Url = e.CurrentEntity;
            ShowReport();

        }
        private async void InitializeAsync()
        {
            var env = await CoreWebView2Environment.CreateAsync(null, "C:\\temp");
            await webView21.EnsureCoreWebView2Async(env);
        }
       
        

       

        public async void ShowReport()
        {
            await webView21.EnsureCoreWebView2Async(null);
            this.webView21.Source = new Uri(Url); 
            Titlelabel.Text = Url;
        }

      
    }
}
