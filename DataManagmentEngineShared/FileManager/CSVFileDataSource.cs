using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class CSVFileDataSource : IDataSource
    {

        public CSVFileDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Dataconnection = new FileConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            Dataconnection.ConnectionStatus = ConnectionStatus;
            if (ConnectionStatus == ConnectionState.Open)
            {
                // Entities = Reader.Entities;
                EntitiesNames = new List<string>() { datasourcename };
            }
            // GetEntitesList();


        }

        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

        public event EventHandler<PassedArgs> PassEvent;
        #region "Read File Routines"
        private static DataTable GetDataTabletFromCSVFile(string csv_file_path)
        {
            DataTable csvData = new DataTable();

            try
            {

                using (CsvTextFieldParser csvReader = new CsvTextFieldParser(csv_file_path))
                {
                    csvReader.SetDelimiter(',');
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        csvData.Columns.Add(datecolumn);
                    }
                    int j = 0;
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();

                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }
                        j += 1;
                        csvData.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return csvData;
        }
        private static List<string[]> ReadCSVFile(string filename, char csvDelimiter, bool ignoreHeadline, bool removeQuoteSign)
        {
            string[] result = new string[0];
            List<string[]> lst = new List<string[]>();

            string line;
            int currentLineNumner = 0;
            int columnCount = 0;

            // Read the file and display it line by line.  
            using (System.IO.StreamReader file = new System.IO.StreamReader(filename))
            {
                while ((line = file.ReadLine()) != null)
                {
                    currentLineNumner++;
                    string[] strAr = line.Split(csvDelimiter);
                    // save column count of dirst line
                    if (currentLineNumner == 1)
                    {
                        columnCount = strAr.Count();
                    }
                    else
                    {
                        //Check column count of every other lines
                        if (strAr.Count() != columnCount)
                        {
                            throw new Exception(string.Format("CSV Import Exception: Wrong column count in line {0}", currentLineNumner));
                        }
                    }

                    if (removeQuoteSign) strAr = RemoveQouteSign(strAr);

                    if (ignoreHeadline)
                    {
                        if (currentLineNumner != 1) lst.Add(strAr);
                    }
                    else
                    {
                        lst.Add(strAr);
                    }
                }

            }

            return lst;
        }
        private static string[] RemoveQouteSign(string[] ar)
        {
            for (int i = 0; i < ar.Count(); i++)
            {
                if (ar[i].StartsWith("\"") || ar[i].StartsWith("'")) ar[i] = ar[i].Substring(1);
                if (ar[i].EndsWith("\"") || ar[i].EndsWith("'")) ar[i] = ar[i].Substring(0, ar[i].Length - 1);

            }
            return ar;
        }
        #endregion
     

        public bool CheckEntityExist(string EntityName)
        {
            if (DatasourceName == EntityName)
            {
                return true;
 
            } else
                return false;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            return EntitiesNames;
        }

        public DataTable GetEntity(string EntityName, string filterstr)
        {
            return GetDataTabletFromCSVFile(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
        }

        public Task<object> GetEntityDataAsync(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public DataTable RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IMapping_rep Mapping = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping = null)
        {
            throw new NotImplementedException();
        }
    }
}
