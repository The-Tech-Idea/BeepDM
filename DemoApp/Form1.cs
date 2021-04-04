using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace DemoApp
{
    public partial class Form1 : Form,IDM_Addin
    {
        public Form1()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; } = "Main Form";
        public string ObjectType { get; set; } = "Form";
        public string AddinName { get; set; } = "Main Form";
        public string Description { get ; set ; } = "Main Form";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }


        // Added Property for Visualization
        public IVisUtil Visutil { get; set; }
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {

            //------------------ 
            // every Addin is Setup though VisUtil Class
            // It will allow engine to setup required parameters
            // every addin will be passed these 6 paramaters
            // all of them are setup though the VisUtil Class

            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            //---------------
            Datasourcesbutton.Click += Datasourcesbutton_Click;
        }

        private void Datasourcesbutton_Click(object sender, EventArgs e)
        {
            string[] args = new string[] {""};
            Visutil.ShowUserControlPopUp("uc_DataConnection", DMEEditor, args, Passedarg);
        }
        private void CreateLocalDB()
        {

        }
        private void CreateDataViewFromLocalDB()
        {

        }
        private void AddEntityFromOtherLocalDB()
        {

        }

    }
}
