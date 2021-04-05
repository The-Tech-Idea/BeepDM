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
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public partial class uc_CrudControl : UserControl,IDM_Addin
    {
        public uc_CrudControl()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "CRUD Control";
        public string Description { get; set; } = "WinForm Application";
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
        public PassedArgs Passedarg { get ; set ; }

      //  public event EventHandler<PassedArgs> OnObjectSelected;
        IVisUtil Visutil { get; set; }
        IDataSource ds;
        DataTable EntityData = new DataTable();
      //  App app;
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
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            if (ds != null)
            {
                EntityStructure = ds.GetEntityStructure(e.CurrentEntity, true);
                if (EntityStructure != null)
                {
                    if (EntityStructure.Fields != null)
                    {
                        if (EntityStructure.Fields.Count > 0)
                        {
                            EntityName = EntityStructure.EntityName;
                            EntityData = (DataTable)ds.GetEntity(EntityStructure.EntityName, null);
                            ShowCRUD();
                        }
                    }
                }
            }
            this.SaveButton.Click += SaveButton_Click;
            this.Controlpanel.Resize += Controlpanel_Resize;
        }

        private void Controlpanel_Resize(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try

            {
                bindingSource1.EndEdit();
                ds.UpdateEntities(EntityName,EntityData);


                DMEEditor.AddLogMessage("Success", $"Saving Data changes", DateTime.Now, 0, null, Errors.Ok);
               
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Saving Data changes";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
               
            }
           
        
        }

        private void ShowCRUD()
        {
           
            bindingSource1.DataSource = EntityData;
            bindingSource1.ResetBindings(true);
          //  EntityNameLabeel.Text = EntityName;
            Visutil.controlEditor.GenerateTableViewOnControl(EntityName, ref Controlpanel, EntityData, ref bindingSource1, 200, ds.DatasourceName);
            this.Controls.Add(Controlpanel);
            Controlpanel.Dock = DockStyle.Fill;
            Controlpanel.AutoScroll = true;
          
            bindingNavigator1.BindingSource = bindingSource1;
            bindingNavigator1.SendToBack();
        }
    }
}
