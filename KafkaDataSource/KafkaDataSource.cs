using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.EventStream.Kafka;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.EventStream
{
    [ClassProperties(Category = DatasourceCategory.STREAM, DatasourceType = DataSourceType.Kafka)]
    public class KafkaDataSource : IDataSource
    {

        public KafkaDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            Category = DatasourceCategory.STREAM;
            DatasourceType = DataSourceType.Kafka;
            DMEEditor = pDMEEditor;
            Dataconnection = new KafkaDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor

            };


            if (DMEEditor.DataSources.Where(o => o.DatasourceName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName == datasourcename).FirstOrDefault();
            }
            Kafkadataconnection = (KafkaDataConnection)Dataconnection;

        }
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }


        }
        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public KafkaDataConnection Kafkadataconnection { get; set; }
       // public ConsumerBuilder<Ignore, string> Consumer { get; set; } = new ConsumerBuilder<Ignore, string>(new ConsumerConfig());
        public bool StopConsume { get; set; } = true;
        #region "Kafka Methods"
        public Pipe pipe { get; set; }=new Pipe();
        public PipeReader reader { get; set; } 
        public PipeWriter writer { get; set; } 
        CancellationTokenSource cts = new CancellationTokenSource();
        public  void handler(DeliveryReport<Null, string> deliveryReport)
        {
            DMEEditor.AddLogMessage("Kafka Producer", $"{ deliveryReport.Status} {deliveryReport.Message}", deliveryReport.Timestamp.UtcDateTime,0, deliveryReport.Value,Errors.Ok);
        }
        private void ProduceTopic(string topic,List<string> Values)
        {
            if(Kafkadataconnection.OpenConnection()== ConnectionState.Open)
            {

            }
            using (var p = new ProducerBuilder<Null, string>(Kafkadataconnection.ProdConfig).Build())
            {
                for (int i = 0; Values.Count >=i; ++i)
                {
                    p.Produce(topic, new Message<Null, string> { Value = Values[i] }, handler);
                }
                // wait for up to 10 seconds for any inflight messages to be delivered.
                //p.Flush(TimeSpan.FromSeconds(10));
            }
        }
        private  Task ConsumeTopic(string topic)
        {
           
            using (var c = new ConsumerBuilder<Ignore, string>(Kafkadataconnection.ConsConfig).Build())
            {
                c.Subscribe(topic);
                
                CancellationTokenSource cts = new CancellationTokenSource();
             
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (!cts.IsCancellationRequested )
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            if (string.IsNullOrEmpty(cr.Message.Value))
                            {
                                cts.Cancel();
                            }

                            DMEEditor.AddLogMessage("Kafka Consumer", $"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.", cr.Message.Timestamp.UtcDateTime, 0, cr.Message.Value, Errors.Ok);
                          //  Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            DMEEditor.AddLogMessage("Kafka Consumer", $"Error occured: {e.Error.Reason}", DateTime.Now, 0, e.Error.Reason, Errors.Failed);
                            //Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException canc)
                {
                    // Close and Release all the resources held by this consumer
                    DMEEditor.AddLogMessage("Kafka Consumer", $"Cancelation occured: {canc.Message}", DateTime.Now, 0, canc.Message, Errors.Ok);
                    c.Close();
                }
            }
            return  Task.CompletedTask;
        }
        public Task StopAsync()
        {
            if(cts != null)
            {   
                    cts.Cancel();
            }
            
            return Task.CompletedTask;
        }
        #endregion
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { } }
   

        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            if (EntitiesNames.Count == 0)
            {
                GetEntitesList();
            }
            retval = EntitiesNames.ConvertAll(d => d.ToUpper()).Contains(EntityName.ToUpper());

            return retval;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;

            if (CheckEntityExist(entity.EntityName) == false)
            {

             //   CreateEntity(entity);
                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                {
                    retval = false;
                }
                else
                {
                    retval = true;
                }
            }
            else
            {
                retval = true;
            }

            return retval;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException("Not Implemented");
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            if (Entities.Count == 0)

            {
                DatasourceEntities f = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName);
                if (f != null)
                {
                    Entities = f.Entities;
                    EntitiesNames = Entities.Select(o => o.EntityName).ToList();
                }


            }
            return EntitiesNames;
        }

        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            try
            {
                Task r=ConsumeTopic(EntityName);
                r.Wait();
                
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Could not Send Topic {EntityName} messeges", DateTime.Now, 0, ex.Message, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
           
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            GetEntitesList();
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            GetEntitesList();
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }


        public Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName, false);
            DMTypeBuilder.CreateNewObject(EntityName, EntityName, x.Fields);
            return DMTypeBuilder.myType;
        }

        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo RunScript(SyncEntity dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<SyncEntity> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                ProduceTopic(EntityName, (List<string>)InsertedData);
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail",$"Could not Send Topic {EntityName} messeges", DateTime.Now, 0, ex.Message, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #region "dispose"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RDBSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
