using TheTechIdea.Logger;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine.DataBase;

using System.ComponentModel;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class WorkFlowEditor : IWorkFlowEditor
    {
        public WorkFlowEditor()
        {
            //IDMEEditor pbl, IDMLogger plogger, IErrorsInfo per
            //DMEEditor = pbl;
            //logger = plogger;
            //ErrorObject = per;
            WorkFlows = new List<DataWorkFlow>();
           // ReadWork();


        }
        IWorkFlowActionClassImplementation flowAction;
        public IDMEEditor DMEEditor { get; set; }

        // public BindingList<Mapping_rep> Mappings { get; set; } = new BindingList<Mapping_rep>();
        public List<DataWorkFlow> WorkFlows { get; set; } = new List<DataWorkFlow>();
        public List<AssemblyClassDefinition> WorkFlowActions { get; set; } = new List<AssemblyClassDefinition>();
       
        public IErrorsInfo CopyEntity(IDataSource src, string SourceEntityName, IDataSource dest, string DestEntityName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DMEEditor.Logger.WriteLog($"Successed in Copying Table");
            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;

                DMEEditor.Logger.WriteLog($"Error in Copying Table ({ex.Message})");


            }

            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SyncEntity(IDataSource src, string SourceEntityName, IDataSource dest, string DestEntityName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DMEEditor.Logger.WriteLog($"Successed in Syncing Table ");
            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.Logger.WriteLog($"Error in Syncing Table ({ex.Message})");


            }

            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SyncDatabase(IDataSource src, IRDBSource dest)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DMEEditor.Logger.WriteLog($"Successed in Syncing Database ");
            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.Logger.WriteLog($"Error in Syncing Database ({ex.Message})");


            }

            return DMEEditor.ErrorObject;
        }
        public IMapping_rep CreateMapping(IMapping_rep x)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                IDataSource ds1 = DMEEditor.GetDataSource(x.Entity1DataSource);
                IDataSource ds2 = DMEEditor.GetDataSource(x.Entity2DataSource);
                if (ds1 == null || ds2 == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "one of the Datasources Dont Exist";
                    DMEEditor.Logger.WriteLog($"one of the Datasources Dont Exist");

                }
                else
                {
                    EntityStructure dt1 = ds1.GetEntityStructure(x.EntityName1, false);
                    EntityStructure dt2 = ds2.GetEntityStructure(x.EntityName2, false);
                    x.Entity1Fields = dt1.Fields;

                    x.Entity2Fields = dt2.Fields;
                    DMEEditor.Logger.WriteLog($"Successful in Creating Mapping");


                }


            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                x = null;
                DMEEditor.Logger.WriteLog($"Error in Creating Mapping({ex.Message})");


            }

            return x;
        }
        public IErrorsInfo StopWorkFlow()
        {

         
            try
            {
                flowAction.StopAction();
                DMEEditor.AddLogMessage("WorkFlow Stopped", "Success Stopeed WorkFlow", DateTime.Now, -1, "", Errors.Failed);

            }
            catch (Exception ex)
            {
                string mes = "Stop WorkFlow";
                DMEEditor.AddLogMessage(ex.Message, "Could not Stop WorkFlow work" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo RunWorkFlow(string WorkFlowName)
        {

            var CurrnetWorkFlow = WorkFlows.Where(x => x.DataWorkFlowName == WorkFlowName).FirstOrDefault();
            try
            {
                for (int i = 0; i < CurrnetWorkFlow.Datasteps.Count; i++)
                {
                    WorkFlowStep step = CurrnetWorkFlow.Datasteps[i];
                    AssemblyClassDefinition action = DMEEditor.WorkFlowEditor.WorkFlowActions.Where(x => x.className == step.ActionName).FirstOrDefault();
                     flowAction = (IWorkFlowActionClassImplementation)DMEEditor.assemblyHandler.GetInstance(action.PackageName);
                    flowAction.InParameters = new List<IPassedArgs>();
                    flowAction.OutParameters = new List<IPassedArgs>();
                    flowAction.DMEEditor = DMEEditor;
                    List<PassedArgs> inparam = step.InParameters;
                    List<PassedArgs> outparam = step.OutParameters;
                   
                 //   flowAction.Mapping = DMEEditor.ConfigEditor.Mappings.Where(x => x.MappingName == step.Mapping).FirstOrDefault();
                    foreach (IPassedArgs item in inparam)
                    {
                        IPassedArgs p = item;
                        flowAction.InParameters.Add(p);

                    }
                    foreach (IPassedArgs item in outparam)
                    {
                        IPassedArgs p = item;
                        flowAction.OutParameters.Add(p);

                    }

                    flowAction.PerformAction();
                }
              
            }
            catch (Exception ex)
            {
                string mes = CurrnetWorkFlow.DataWorkFlowName;
                DMEEditor.AddLogMessage(ex.Message, "Could not  Get WorkFlow work" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CreateMappingForFolder(CategoryFolder fodler)
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not create fodler map" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IMapping_rep CreateMapping(string src1, string entity1, string src2, string entity2)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            IMapping_rep x = new Mapping_rep { EntityName1 = entity1, EntityName2 = entity2, Entity1DataSource = src1, Entity2DataSource = src2 };
            try
            {

                IDataSource ds1 = DMEEditor.GetDataSource(src1);
                IDataSource ds2 = DMEEditor.GetDataSource(src2);
                if (ds1 == null || ds2 == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "one of the Datasources Dont Exist";
                    DMEEditor.Logger.WriteLog($"one of the Datasources Dont Exist");

                }
                else
                {
                    EntityStructure dt1 = ds1.GetEntityStructure(entity1, false);
                    EntityStructure dt2 = ds2.GetEntityStructure(entity2, false);
                    x.Entity1Fields = dt1.Fields;

                    x.Entity2Fields = dt2.Fields;
                    DMEEditor.Logger.WriteLog($"Successful in Creating Mapping");
                }


            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                x = null;
                DMEEditor.Logger.WriteLog($"Error in Creating Mapping({ex.Message})");


            }

            return x;
        }
        //public IErrorsInfo ReadWork()
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        string path = Path.Combine(DMEEditor.ConfigEditor.ExePath, "DataWorkFlow.json");
        //        WorkFlows=DMEEditor.ConfigEditor.JsonLoader.DeserializeObject<DataWorkFlow>(path);

        //        DMEEditor.Logger.WriteLog($"Successed in Loading Workflows");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        DMEEditor.ErrorObject.Ex = ex;

        //        DMEEditor.Logger.WriteLog($"Error in Loading WorkFlows ({ex.Message})");


        //    }

        //    return DMEEditor.ErrorObject;
        //}
        //public IErrorsInfo SaveWork()
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        string path = Path.Combine(DMEEditor.ConfigEditor.ExePath, "DataWorkFlow.json");
        //        DMEEditor.ConfigEditor.JsonLoader.Serialize(path,WorkFlows);

        //        DMEEditor.Logger.WriteLog($"Successed in saving DataWorkFlow");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        DMEEditor.ErrorObject.Ex = ex;

        //        DMEEditor.Logger.WriteLog($"Error in saving DataWorkFlow ({ex.Message})");


        //    }

        //    return DMEEditor.ErrorObject;
        //}
        //public IErrorsInfo ReadMapping()
        //{
        //    ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        string path = Path.Combine(DMEEditor.ConfigEditor.ConfigPath, "Mapping.json");
        //        //File.WriteAllText(path, JsonConvert.SerializeObject(ts));
        //        // serialize JSON directly to a file
        //        if (File.Exists(path))
        //        {
        //            String JSONtxt = File.ReadAllText(path);

        //            Mappings = JsonConvert.DeserializeObject<BindingList<Mapping_rep>>(JSONtxt);
        //        }

        //        logger.WriteLog($"Successed in Loading Workflows");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        ErrorObject.Flag = Errors.Failed;
        //        ErrorObject.Ex = ex;

        //        logger.WriteLog($"Error in Loading WorkFlows ({ex.Message})");


        //    }

        //    return ErrorObject;
        //}
        //public IErrorsInfo SaveMapping()
        //{
        //    ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        string path = Path.Combine(DMEEditor.ConfigEditor.ConfigPath, "Mapping.json");
        //        //File.WriteAllText(path, JsonConvert.SerializeObject(ts));
        //        // serialize JSON directly to a file
        //        using (StreamWriter file = File.CreateText(path))
        //        {
        //            JsonSerializer serializer = new JsonSerializer();
        //            serializer.Serialize(file, Mappings);
        //        }
        //        logger.WriteLog($"Successed in saving fieldtypes");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        ErrorObject.Flag = Errors.Failed;
        //        ErrorObject.Ex = ex;

        //        logger.WriteLog($"Error in saving fieldtypes ({ex.Message})");


        //    }

        //    return ErrorObject;
        //}



    }
}
