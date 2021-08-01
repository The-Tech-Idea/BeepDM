using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Beep;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.Util;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.CompositeLayer;

namespace TheTechIdea.ETL
{
    public partial class uc_linkentitytoanother :  UserControl, IDM_Addin
    {
        public uc_linkentitytoanother()
    {
        InitializeComponent();
    }

    public string AddinName { get; set; } = "Entity to Entity Link";
    public string Description { get; set; } = "Entity to Entity Link";
    public string ObjectName { get; set; }
    public string ObjectType { get; set; } = "UserControl";
    public string DllName { get; set; }
    public string DllPath { get; set; }
    public string NameSpace { get; set; }
    public string ParentName { get; set; }
    public Boolean DefaultCreate { get; set; } = true;
    public IDMLogger Logger { get; set; }
    public IDataSource SourceConnection { get; set; }

    public IDMEEditor DMEEditor { get; set; }
    public string EntityName { get; set; }
    public EntityStructure EntityStructure { get; set; }
    public DataSet Dset { get; set; }
    public IErrorsInfo ErrorObject { get; set; }
    private IDMDataView MyDataView;
   
    public IPassedArgs Passedarg { get; set; }
    public IUtil util { get; set; }
        //  public IDataViewEditor ViewEditor { get; set; }
        public IVisUtil Visutil { get; set; }
        IBranch branch = null;
    IBranch Parentbranch = null;
    public EntityStructure ParentEntity { get; set; } = null;
    DataViewDataSource vds;
    CompositeLayerDataSource cds;
    public void Run(IPassedArgs pPassedarg)
    {

    }

    public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs obj, IErrorsInfo per)
    {
        Passedarg = obj;

        Logger = plogger;
        util = putil;
        ErrorObject = per;
        DMEEditor = pDMEEditor;
        vds = (DataViewDataSource)DMEEditor.GetDataSource(obj.DMView.DataViewDataSourceID);
        Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
        branch = (IBranch)obj.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            EntityStructure = (EntityStructure)obj.Objects.Where(c => c.Name == "EntityStructure").FirstOrDefault().obj;
            if (obj.Objects.Where(c => c.Name == "ParentBranch").Any())
        {
            Parentbranch = (IBranch)obj.Objects.Where(c => c.Name == "ParentBranch").FirstOrDefault().obj;
            ParentEntity = vds.GetEntityStructure(Parentbranch.BranchText, true);
        }


        if (obj.CurrentEntity != null)
        {
            EntityName = obj.CurrentEntity;

        }
        else
        {
            EntityName = "";
        }
        this.entitiesBindingNavigatorSaveItem.Click += EntitiesBindingNavigatorSaveItem_Click;
        this.otherentitiesbindingSource.DataSource = vds.Entities.Where(o=>o.Id>0 && o.Id!=EntityStructure.Id);
    
        this.entitiesBindingSource.DataSource = EntityStructure;
            this.fieldsBindingSource.DataSource = this.entitiesBindingSource;
        //this.otherentityfieldsbindingSource.ResetBindings(true);
        this.ParentEntitycomboBox.SelectedIndexChanged += ParentEntitycomboBox_SelectedIndexChanged;
    
    }

        private void ParentEntitycomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fieldsBindingSource.Count == 0)
            {
                fieldsBindingSource.AddNew();
            }
            if (fieldsBindingSource.Count > 0)
            {
                EntityStructure entity = vds.GetEntityStructure(ParentEntitycomboBox.Text, true);
                if (entity != null)
                {
                    if (!string.IsNullOrEmpty(entity.EntityName))
                    {
                        ParentFieldcomboBox.Items.Clear();

                        foreach (EntityField s in entity.Fields)
                        {
                            ParentFieldcomboBox.Items.Add(s.fieldname);
                        }
                    }
                }
               
            }
        }

        private void EntitiesBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            try

            {

                EntityStructure.Drawn = true;


                if (vds.Entities.Where(o => o.DatasourceEntityName == EntityStructure.DatasourceEntityName).Any())
                {
                    vds.Entities[vds.Entities.FindIndex(i => i.DatasourceEntityName.Equals(EntityStructure.DatasourceEntityName, StringComparison.OrdinalIgnoreCase))] = EntityStructure;
                }
                else
                {
                    vds.CreateEntityAs(EntityStructure);
                }

                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new Beep.ConfigUtil.DatasourceEntities { datasourcename = Passedarg.DatasourceName, Entities = vds.Entities });
                vds.WriteDataViewFile(vds.DatasourceName);
                MessageBox.Show("Entity Saved successfully", "Beep");



            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error Saving Function Mapping ";
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                errmsg = ErrorObject.Message;
                MessageBox.Show(errmsg, "Beep");
                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
        }
  
    public void RaiseObjectSelected()
    {

    }

    }
}
