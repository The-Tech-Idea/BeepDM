using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace DataManagmentEngineShared.DataBase
{
    public class FireBirdEmbeddedDataSource : RDBSource, ILocalDB
    {
        public bool CanCreateLocal { get ; set ; }

        public FireBirdEmbeddedDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.FireBird;
        }
        public  bool CopyDB(string DestDbName, string DesPath)
        {

            try
            {
                if (!System.IO.File.Exists(base.Dataconnection.ConnectionProp.ConnectionString))
                {

                    File.Copy(base.Dataconnection.ConnectionProp.ConnectionString, Path.Combine(DesPath, DestDbName));

                }

                DMEEditor.AddLogMessage("Success", "Copy FireBird Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Copy FireBird Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };

        }
        static void CreateFBDatabase(string host, string fileName, string user, string password, int pageSize, bool forcedWrites, bool overwrite)
        {
            FbConnectionStringBuilder csb = new FbConnectionStringBuilder();
            csb.Database = fileName;
            csb.DataSource = "127.0.0.1";
            csb.UserID = "SYSDBA";
            csb.Password = "masterkey";
            csb.ServerType = FbServerType.Embedded;
            FbConnection.CreateDatabase(csb.ConnectionString, pageSize, forcedWrites, overwrite);
        }
        public  bool CreateDB()
        {
            try
            {
                if (!Path.HasExtension(base.Dataconnection.ConnectionProp.FileName))
                {
                    base.Dataconnection.ConnectionProp.FileName = base.Dataconnection.ConnectionProp.FileName + ".fdb";
                }
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                 
                    CreateFBDatabase("localhost",Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName), base.Dataconnection.ConnectionProp.UserID, base.Dataconnection.ConnectionProp.Password,100,true,true);

                }
                DMEEditor.AddLogMessage("Success", "Create FireBird Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Embedded Firebird Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }

        public  bool DeleteDB()
        {
            try
            {
                if (!System.IO.File.Exists(base.Dataconnection.ConnectionProp.FilePath))
                {
                    File.Delete(base.Dataconnection.ConnectionProp.FilePath);

                }

                DMEEditor.AddLogMessage("Success", "Deleted FireBird Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Delete FireBird Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  IErrorsInfo DropEntity(string EntityName)
        {
            try

            {

                String cmdText = $"drop table  '{EntityName}'";
                DMEEditor.ErrorObject = base.ExecuteSql(cmdText);

                if (!base.CheckEntityExist(EntityName))
                {
                    DMEEditor.AddLogMessage("Success", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {

                    DMEEditor.AddLogMessage("Error", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                }

            }
            catch (Exception ex)
            {
                string errmsg = $"Error Droping Entity {EntityName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public  IErrorsInfo CloseConnection()
        {
            try

            {

                base.Dataconnection.DbConn.Close();


                DMEEditor.AddLogMessage("Success", $"Closing connection to SQL Compact Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to SQL Compact Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
}
