using System;

using System.Data;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using Confluent.Kafka;
using System.Net;

namespace TheTechIdea.Beep.EventStream.Kafka
{
    public class KafkaDataConnection : IDataConnection
    {
        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDbConnection DbConn { get; set; }
        public ProducerConfig ProdConfig { get; set; }
        public ConsumerConfig ConsConfig { get; set; }

        public ConnectionState CloseConn()
        {
            throw new NotImplementedException();
        }

        public ConnectionState OpenConnection()
        {
            try
            { 
                if (ConnectionStatus== ConnectionState.Closed)
                {
                    ProdConfig = new ProducerConfig
                    {
                        BootstrapServers = ConnectionProp.Host,// "host1:9092,host2:9092",
                        ClientId = Dns.GetHostName(),
                     
                        SaslMechanism = SaslMechanism.Plain,
                        SecurityProtocol = SecurityProtocol.SaslSsl,
                        SaslUsername = ConnectionProp.UserID,
                        SaslPassword = ConnectionProp.Password,


                    };
                    ConsConfig = new ConsumerConfig
                    {
                        BootstrapServers = ConnectionProp.Host,// "host1:9092,host2:9092",
                        GroupId = ConnectionProp.Database,
                        ClientId = Dns.GetHostName(),
                        AutoOffsetReset = AutoOffsetReset.Earliest,
                        SaslMechanism = SaslMechanism.Plain,
                        SecurityProtocol = SecurityProtocol.SaslSsl,
                        SaslUsername = ConnectionProp.UserID,
                        SaslPassword = ConnectionProp.Password,

                    };
                }
               
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception )
            {

                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Closed)
                {
                    ProdConfig = new ProducerConfig
                    {
                        BootstrapServers = host,// "host1:9092,host2:9092",
                        ClientId = Dns.GetHostName(),
                        SaslMechanism = SaslMechanism.Plain,
                        SecurityProtocol = SecurityProtocol.SaslSsl,
                        SaslUsername = userid,
                        SaslPassword = password,


                    };
                    ConsConfig = new ConsumerConfig
                    {
                        BootstrapServers = host,// "host1:9092,host2:9092",
                        ClientId= Dns.GetHostName(),
                        GroupId = database,
                        AutoOffsetReset = AutoOffsetReset.Earliest,
                        SaslMechanism = SaslMechanism.Plain,
                        SecurityProtocol = SecurityProtocol.SaslSsl,
                        SaslUsername = userid,
                        SaslPassword = password,

                    };
                }
                   
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception )
            {

                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            throw new NotImplementedException();
        }

        public string ReplaceValueFromConnectionString()
        {
            throw new NotImplementedException();
        }
    }
}
