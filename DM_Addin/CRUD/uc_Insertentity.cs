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
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.DataManagment_Engine.AppBuilder.UserControls
{
    public partial class uc_Insertentity : UserControl, IDM_Addin
    {
        public uc_Insertentity()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; } = "Entity Data Insert";
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get ; set ; } = "Entity Data Insert";
        public string Description { get ; set ; } = "Entity Data Insert";
        public bool DefaultCreate { get; set; } = false;
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
        object ob;
        object ti;
        EntityStructure TableCurrentEntity;
        string DisplayField;
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
            ob = (object)e.Objects.Where(c => c.Name == e.CurrentEntity).FirstOrDefault().obj;
            EntitybindingSource.DataSource = ob;
            EntitybindingSource.AllowNew = false;
            SaveEntitybutton.Click += SaveEntitybutton_Click;
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            ds.Dataconnection.OpenConnection();
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                EntityStructure = ds.GetEntityStructure(e.CurrentEntity, true);
                if (EntityStructure != null)
                {
                    if (EntityStructure.Fields != null)
                    {
                        if (EntityStructure.Fields.Count > 0)
                        {
                            EntityNamelabel.Text = EntityStructure.EntityName;
                            subtitlelabel.Text = $"From Data Source {EntityStructure.DataSourceID}";
                            EntityName = EntityStructure.EntityName;
                            Visutil.controlEditor.GenerateEntityonControl(EntityName, ref panel1, GetObjectType(), ref EntitybindingSource, 150, EntityStructure.DataSourceID);
                        }
                    }
                }
            }
        }

        private void SaveEntitybutton_Click(object sender, EventArgs e)
        {
            ds.InsertEntity(EntityName, ti);
        }
        private object GetObjectType()
        {
            Type enttype = ds.GetEntityType(EntityName);

            ti = Activator.CreateInstance(enttype);
            // ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
            if (ob.GetType().GetInterfaces().Contains(typeof(ICustomTypeDescriptor)))
            {
                DataRowView dv = (DataRowView)ob;
                DataRow dr = dv.Row;
                foreach (EntityField col in EntityStructure.Fields)
                {
                    try
                    {
                        if (dr[col.fieldname] != System.DBNull.Value)
                        {
                            System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                            PropAInfo.SetValue(ti, dr[col.fieldname], null);
                        }
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                    // TrySetProperty<enttype>(ti, dr[col.fieldname], null);


                }


            }
            else
            {
                ti = ob;
            }
            return ti;
        }

        
    }
}
