using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.WorkFlowActions
{
    public class SelectDataSource : IWorkFlowAction, IWorkFlowActionClassImplementation
    {
        public string Id { get ; set ; }
        public string ClassName { get ; set ; }
        public string FullName { get ; set ; }
        public BackgroundWorker BackgroundWorker { get ; set ; }
      
        public IDMEEditor DMEEditor { get ; set ; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<EntityStructure> OutStructures { get; set; }
        public bool Finish { get ; set ; }
        public Mapping_rep Mapping { get; set; }
        public string Description { get ; set ; }

        private IPassedArgs par;
        private EntityStructure ent;

        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepRunning;

        public IErrorsInfo PerformAction()
        {

            return CreateDataSourceEntity();
        }
        private IErrorsInfo CreateDataSourceEntity()
        {
            OutParameters = new List<IPassedArgs>();
            OutStructures = new List<EntityStructure>();
            if (InParameters != null)
            {
                if (InParameters.Count() > 0)
                {
                    if (InParameters.Count() == 1)
                    {
                        GetDataSource(InParameters[0].DatasourceName);
                        OutParameters[0].CurrentEntity = InParameters[0].CurrentEntity;
                        try
                        {
                            IDataSource ds = DMEEditor.GetDataSource(InParameters[0].DatasourceName);
                            try
                            {
                                ent = ds.GetEntityStructure(OutParameters[0].CurrentEntity,false);
                            }
                            catch (Exception ex)
                            {
                                DMEEditor.AddLogMessage("Error", "Could not Load Entity Structure" + ex.Message, DateTime.Now, -1, "", Errors.Failed);

                            }

                        }
                        catch (Exception ex)
                        {
                            DMEEditor.AddLogMessage("Error", "Could not Open DataSource " + ex.Message, DateTime.Now, -1, "", Errors.Failed);

                        }


                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Error", "Only 1 Parameter for Datasource should be set", DateTime.Now, -1, "", Errors.Failed);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Error", "Could not find any Parameters", DateTime.Now, -1, "", Errors.Failed);
                }
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "Could not find any Parameters", DateTime.Now, -1, "", Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        private IErrorsInfo GetDataSource(string pDatasourceName)
        {
            try
            {
                ConnectionProperties i = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == pDatasourceName).FirstOrDefault();

                if (i != null) 
                {
                    par = new PassedArgs
                    {
                        Id = i.ID,
                        DatasourceName = i.ConnectionName,
                        ObjectType = EnumParameterType.DataSource.ToString()
                    };
                    OutParameters.Add(par);


                }
                else
                {
                    DMEEditor.AddLogMessage("Error", "Could not Load Data Sources", DateTime.Now, -1, "", Errors.Failed);
                }
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Could not Load Data Sources {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                //MessageBox.Show("Error Saving WorkFlow");
            }
            return DMEEditor.ErrorObject;
        }
        private IErrorsInfo GetDataSources()
        {
            try
            {
                foreach (ConnectionProperties i in DMEEditor.ConfigEditor.DataConnections)
                {
                    par = new PassedArgs();
                    par.Id = i.ID;
                    par.DatasourceName = i.ConnectionName;
                    par.EventType = EnumParameterType.DataSource.ToString();
                    OutParameters.Add(par);



                }
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Could not Load Data Sources " + ex.Message, DateTime.Now, -1, "", Errors.Failed);
                //MessageBox.Show("Error Saving WorkFlow");
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo StopAction()
        {
            return DMEEditor.ErrorObject;
        }
    }
}
