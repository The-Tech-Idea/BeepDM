using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.CompositeLayer
{
    public class CompositeLayerDataSource : RDBSource
    {
       
        public CompositeLayerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            // LayerInfo = new CompositeLayer();
        }

        public CompositeLayer LayerInfo
        {
            get
            {
                return DMEEditor.ConfigEditor.CompositeQueryLayers[DMEEditor.ConfigEditor.CompositeQueryLayers.FindIndex(x=>x.LayerName==DatasourceName)];
            }
            set
            {
                DMEEditor.ConfigEditor.CompositeQueryLayers[DMEEditor.ConfigEditor.CompositeQueryLayers.FindIndex(x => x.LayerName == DatasourceName)] = value;
            }
        }
       
        public DataViewDataSource DataViewSource { get; set; }
        public ILocalDB LocalDB { get; set; }
        public string DatabaseType { get => base.Dataconnection.ConnectionProp.DriverName + "." + base.Dataconnection.ConnectionProp.DriverVersion; }
        public IErrorsInfo DropEntity(string EntityName)
        {  
            try

            {
                bool ok=true;
                if (base.CheckEntityExist(EntityName))
                {
                    ILocalDB ldb = (ILocalDB)this;
                    if (ldb.DropEntity(EntityName).Flag == Errors.Ok)
                    {
                        ok = true;
                    }else
                    {
                        ok = false;
                    }
                    LayerInfo.Entities[LayerInfo.Entities.FindIndex(x => x.EntityName == EntityName)].Created=ok ;
                }
                DMEEditor.AddLogMessage("Success", $"Removed Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = $"Error Removing Entity {EntityName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo CreateLayer()
        {
            try

            {
                if (DataViewSource == null)
                {
                    DataViewSource = (DataViewDataSource)DMEEditor.GetDataSource(LayerInfo.DataViewDataSourceName);
                }
                   
                    if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                    {
                        ILocalDB l = (ILocalDB) this;
                        l.CreateDB();

                    }
              


                DMEEditor.AddLogMessage("Success", $"Creating Layer {LayerInfo.LayerName}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = $"Error Creating Layer {LayerInfo.LayerName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public bool GetAllEntitiesFromDataView()
        {
            try
            {
                if (DataViewSource == null && !string.IsNullOrEmpty(LayerInfo.DataViewDataSourceName))
                {
                    DataViewSource = (DataViewDataSource)DMEEditor.GetDataSource(LayerInfo.DataViewDataSourceName);
                    IEnumerable<string> ls = DataViewSource.GetEntitesList().Distinct();
                    if (LayerInfo.Entities == null)
                    {
                        LayerInfo.Entities = new List<EntityStructure>();
                    }
                    List<string> ents = ls.Except(LayerInfo.Entities.Select(p => p.EntityName).Distinct()).ToList();
                    // 
                    foreach (string item in ents)
                    {
                        try
                        {
                           // string entityname = Regex.Replace(item, @"\s+", "_");
                            //if (!LayerInfo.Entities.Where(x => x.EntityName.Equals(item,StringComparison.OrdinalIgnoreCase)).Any())
                            //{
                                EntityStructure a = new EntityStructure();
                                try
                                {
                                    a = (EntityStructure)DataViewSource.GetEntityStructure(item, false);
                                    a.Created = false;
                                    a.DataSourceID = DatasourceName;
                                    LayerInfo.Entities.Add(a);
                                }
                                catch (Exception eee)
                                {

                                   
                                }
                              
                           // }

                        }
                        catch (Exception er)
                        {
                            DMEEditor.AddLogMessage("Fail", $"{item}:{er.Message}", DateTime.Now, 0, null, Errors.Failed);
                        }

                    }
                    DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                    return true;
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", $"Please update View Source for Composite Layer", DateTime.Now, 0, null, Errors.Failed);
                    return false;
                }
               
            }
            catch (Exception ex)
            {
               
                DMEEditor.AddLogMessage("Fail", $"Could not Get DataView entities :{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }
        private int EntityIndex(string entityname)
        {
            return LayerInfo.Entities.FindIndex(o => o.EntityName.ToLower() == entityname.ToLower());
        }
        public override EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (EntityIndex(fnd.EntityName) > 0)
            {
                return LayerInfo.Entities[EntityIndex(fnd.EntityName)]; //base.GetEntityStructure(fnd, refresh);
            }
            else
                return null;
           
        }
        public override EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            if (EntityIndex(EntityName) > 0)
            {
                return LayerInfo.Entities[EntityIndex(EntityName)]; //base.GetEntityStructure(fnd, refresh);
            }
            else
                return null;
        }
        public bool DropDatabase()
        {
            try
            {
                Dataconnection.DbConn.Close();
            }
            catch (Exception)
            {

               // throw;
            }
           

          if (LocalDB != null)
            {
                return LocalDB.DeleteDB();
            }
            else
            { return true; }
           
        }
       public bool AddEntitytoLayer(EntityStructure entity)
        {
            try
            {
                EntityStructure a = new EntityStructure();
                a = (EntityStructure)entity.Clone();
                a.EntityName = a.EntityName;
                a.Created = false;
              
                LayerInfo.Entities.Add(a);
                DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

       

     
    }
}
