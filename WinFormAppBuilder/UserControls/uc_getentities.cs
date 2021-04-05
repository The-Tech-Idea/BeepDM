using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.DataManagment_Engine.AppBuilder.UserControls
{
    public partial class uc_getentities : UserControl,IDM_Addin
    {
        public uc_getentities()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string ObjectType { get ; set ; }
        public string AddinName { get ; set ; }
        public string Description { get ; set ; }
        public bool DefaultCreate { get ; set ; }
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }
        IVisUtil Visutil { get; set; }
        IDataSource ds;
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            ds.Dataconnection.OpenConnection();
            if (ds != null && ds.ConnectionStatus== ConnectionState.Open)
            {
                EntityStructure = ds.GetEntityStructure(e.CurrentEntity, true);
                if (EntityStructure != null)
                {
                    if (EntityStructure.Fields != null)
                    {
                        if (EntityStructure.Fields.Count > 0)
                        {
                            EntityName = EntityStructure.EntityName;
                            //EntityData = (DataTable)ds.GetEntity(EntityStructure.EntityName, null);
                            //ShowCRUD();
                        }
                    }
                }
            }
            SubmitFilterbutton.Click += SubmitFilterbutton_Click;
            expandbutton.Click += Expandbutton_Click;
            InsertNewEntitybutton.Click += InsertNewEntitybutton_Click;
            DeleteSelectedbutton.Click += DeleteSelectedbutton_Click;
            EditSelectedbutton.Click += EditSelectedbutton_Click;
        }

        private void EditSelectedbutton_Click(object sender, EventArgs e)
        {
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    object ob=EntitybindingSource.Current;
                    if (Passedarg.Objects.Where(i => i.Name == EntityName).Any())
                    {
                        Passedarg.Objects.Remove(Passedarg.Objects.Where(i => i.Name == EntityName).FirstOrDefault());
                    }
                   
                    Passedarg.Objects.Add(new ObjectItem() { Name = EntityName, obj = ob });
                    Visutil.ShowUserControlPopUp("uc_updateentity", DMEEditor, new string[] { "" }, Passedarg);
                }
            }
        }
        private void DeleteSelectedbutton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void InsertNewEntitybutton_Click(object sender, EventArgs e)
        {
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                Type enttype = ds.GetEntityType(EntityName);
                var ti = Activator.CreateInstance(enttype);
                EntitybindingSource.DataSource = ti;
              
                    if (Passedarg.Objects.Where(i => i.Name == EntityName).Any())
                    {
                        Passedarg.Objects.Remove(Passedarg.Objects.Where(i => i.Name == EntityName).FirstOrDefault());
                    }

                    Passedarg.Objects.Add(new ObjectItem() { Name = EntityName, obj = ti });
                    Visutil.ShowUserControlPopUp("uc_Insertentity", DMEEditor, new string[] { "" }, Passedarg);
               
            }
        }

        private void Expandbutton_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
        }

        private void SubmitFilterbutton_Click(object sender, EventArgs e)
        {
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                object retval = ds.GetEntity(EntityName, null);
                EntitybindingSource.DataSource = retval;
                EntitybindingSource.ResetBindings(true);
                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.DataSource = EntitybindingSource;
                dataGridView1.Refresh();
                EntityNamelabel.Text = EntityName;
                subtitlelabel.Text = $"From Data Source : {ds.DatasourceName}";
            }
        }
    }
}
