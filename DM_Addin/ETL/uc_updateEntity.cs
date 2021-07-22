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
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_updateEntity : UserControl,IDM_Addin
    {
        public uc_updateEntity()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Entity Editor";
        public string Description { get; set; } = "Entity Editor";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public IPassedArgs Passedarg { get ; set ; }
        public IVisUtil Visutil { get; set; }
        IBranch branch;
        IDataSource ds;
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
            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            foreach (ConnectionProperties c in DMEEditor.ConfigEditor.DataConnections)
            {
                var t = dataSourceIDComboBox.Items.Add(c.ConnectionName);

            }
            foreach (var item in Enum.GetValues(typeof(DataSourceType)))
            {
                var t = databaseTypeComboBox.Items.Add(item);

            }
            foreach (var item in Enum.GetValues(typeof(DatasourceCategory)))
            {
                var t = categoryComboBox.Items.Add(item);

            }
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            if (!string.IsNullOrEmpty(e.CurrentEntity))
            {
                this.entitiesBindingSource.DataSource = ds.Entities[ds.Entities.FindIndex(p=> string.Equals(p.EntityName,e.CurrentEntity, StringComparison.OrdinalIgnoreCase))];
            }
          
           

        }



    }
}
