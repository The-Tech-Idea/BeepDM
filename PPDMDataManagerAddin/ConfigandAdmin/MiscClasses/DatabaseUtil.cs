using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace SimpleODM.systemconfigandutil.MiscClasses
{
    public class DatabaseUtil
    {
        public DatabaseUtil()
        {

        }
        private PPDMContext SimplePPDMContext;
        public DatabaseUtil(PPDMContext pconfig)
        {
            SimplePPDMContext = pconfig;
        }

        public bool SaveFileintoPPDMFileRM(string FileID, string FileNameWithPath, string FileType, string pRemarks = "", string pREFERENCE_NUM = "", string pACCESS_CONDITION = "", string pGROUP_IND = "")
        {
            string PathOnly = Path.GetDirectoryName(FileNameWithPath);
            string FileName = Path.GetFileName(FileNameWithPath);
            FileInfo oFile;
            oFile = new FileInfo(FileNameWithPath);
            byte[] fileData = null;
            var oFileStream = oFile.OpenRead();
            long lBytes = oFileStream.Length;
            if (lBytes > 0)
            {
                fileData = new byte[lBytes - 1 + 1];
                // Read the file into a byte array
                oFileStream.Read(fileData, 0, Convert.ToInt32(lBytes));
                oFileStream.Close();
            }

            var myRM_INFO_ITEM = new RM_INFORMATION_ITEM();
            RM_FILE_CONTENT myRM_FILE_CONTENT;
            try
            {

                myRM_FILE_CONTENT = (from x in SimplePPDMContext.InfoandProdManagementEntitiesDataContext.RM_FILE_CONTENT
                                     where x.FILE_ID == FileID
                                     select x).FirstOrDefault();
                if (myRM_FILE_CONTENT == null)
                {
                    myRM_FILE_CONTENT = new RM_FILE_CONTENT();
                    myRM_INFO_ITEM.INFORMATION_ITEM_ID = FileID;
                    myRM_INFO_ITEM.ACTIVE_IND = "Y";
                    myRM_INFO_ITEM.INFO_ITEM_TYPE = "RM_DOCUMENT"; // "SODM_FILE_LOAD_TEMEPLETE"
                    myRM_INFO_ITEM.TITLE = FileName;
                    myRM_INFO_ITEM.REFERENCE_NUM = pREFERENCE_NUM;
                    myRM_INFO_ITEM.ACCESS_CONDITION = pACCESS_CONDITION;
                    myRM_INFO_ITEM.GROUP_IND = pGROUP_IND;
                    myRM_INFO_ITEM.PURPOSE = FileType;
                    myRM_FILE_CONTENT.ACTIVE_IND = "Y";
                    myRM_FILE_CONTENT.INFO_ITEM_TYPE = "RM_DOCUMENT"; // "SODM_FILE_LOAD_TEMEPLETE"
                    myRM_FILE_CONTENT.FILE_ID = FileID;
                    if (pRemarks.Length > 0)
                    {
                        myRM_INFO_ITEM.REMARK = pRemarks;
                    }

                    myRM_FILE_CONTENT.INFORMATION_ITEM_ID = FileID;
                    myRM_FILE_CONTENT.FILE_CONTENT = fileData;
                    SimplePPDMContext.InfoandProdManagementEntitiesDataContext.RM_INFORMATION_ITEM.Add(myRM_INFO_ITEM);
                    SimplePPDMContext.InfoandProdManagementEntitiesDataContext.RM_FILE_CONTENT.Add(myRM_FILE_CONTENT);
                }
                else
                {
                    myRM_FILE_CONTENT.FILE_CONTENT = fileData;
                }

                SimplePPDMContext.InfoandProdManagementEntitiesDataContext.SaveChanges();
                oFileStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                oFileStream.Close();
                return false;
            }
        }

        public bool LoadFileFromPPDMFileRM(string FileID, string FileNameWithPath)
        {
            string PathOnly = Path.GetDirectoryName(FileNameWithPath);
            string FileName = Path.GetFileName(FileNameWithPath);
            FileStream oFileStream;
            if (File.Exists(FileNameWithPath))
            {
                oFileStream = new FileStream(FileNameWithPath, FileMode.Create);
                var RM_FILE_CONTENT = new RM_FILE_CONTENT();
                try
                {
                    IQueryable<RM_FILE_CONTENT> rm1 = from x in SimplePPDMContext.InfoandProdManagementEntitiesDataContext.RM_FILE_CONTENT
                                                      where x.FILE_ID == FileID
                                                      select x;
                    RM_FILE_CONTENT = rm1.FirstOrDefault();
                    oFileStream.Write(RM_FILE_CONTENT.FILE_CONTENT, 0, RM_FILE_CONTENT.FILE_CONTENT.Length);
                    oFileStream.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    oFileStream.Close();
                    return false;
                }
            }

            return true;
        }


        //public List<clslistoftables> GetTableListFromSchema(string SchemaName)
        //{
        //    var tb = new DataTable();
        //    var ls = new List<clslistoftables>();


        //    foreach (DataRow row in tb.Rows)
        //    {
        //        foreach (DataColumn col in tb.Columns)
        //        {
        //            if (col.ColumnName == "TABLE_NAME")
        //            {
        //                var x = new clslistoftables();
        //                x.Table_Name =row[col].ToString());
        //                ls.Add(x);
        //            }
        //        }
        //    }

        //    return ls;
        //}

        //public string RunCommandonDB(string pCmd, string pTableName)
        //{
        //    string retval = "";
        //    switch (SimplePPDMContext.UserSetting.DATABASETYPE)
        //    {
        //        case var @case when @case == DbTypeEnum.OracleDB:
        //            {
        //                var cmd = new OracleCommand();
        //                cmd.CommandText = pCmd;
        //                cmd.Connection = SimplePPDMContext.OracleDatabaseConnection;
        //                try
        //                {
        //                    int aff = cmd.ExecuteNonQuery();
        //                }
        //                catch
        //                {
        //                    retval += Constants.vbCrLf + "Error encountered while running script on table " + pTableName;
        //                }
        //                finally
        //                {
        //                }

        //                break;
        //            }

        //        case var case1 when case1 == DbTypeEnum.SqlServerDB:
        //            {
        //                if (SimplePPDMContext.UserSetting.LocalDb == false)
        //                {
        //                    var cmd = new SqlCommand();
        //                    cmd.CommandText = pCmd;
        //                    cmd.Connection = SimplePPDMContext.SQLDatabaseConnection;
        //                    try
        //                    {
        //                        int aff = cmd.ExecuteNonQuery();
        //                    }
        //                    catch
        //                    {
        //                        retval += Constants.vbCrLf + "Error encountered while running script  on table " + pTableName;
        //                    }
        //                    finally
        //                    {
        //                    }
        //                }
        //                else
        //                {
        //                    var cmd = new SqlServerCe.SqlCeCommand();
        //                    cmd.CommandText = pCmd;
        //                    cmd.Connection = SimplePPDMContext.LocalSQLDatabaseConnection;
        //                    try
        //                    {
        //                        int aff = cmd.ExecuteNonQuery();
        //                    }
        //                    catch
        //                    {
        //                        retval += Constants.vbCrLf + "Error encountered while running script  on table " + pTableName;
        //                    }
        //                    finally
        //                    {
        //                    }
        //                }

        //                break;
        //            }

        //        case var case2 when case2 == DbTypeEnum.Sqlite:
        //            {
        //                var cmd = new Devart.Data.SQLite.SQLiteCommand();
        //                cmd.CommandText = pCmd;
        //                cmd.Connection = SimplePPDMContext.SQLiteDataBaseConnection;
        //                try
        //                {
        //                    int aff = cmd.ExecuteNonQuery();
        //                }
        //                catch
        //                {
        //                    retval += Constants.vbCrLf + "Error encountered while running script  on table " + pTableName;
        //                }
        //                finally
        //                {
        //                }

        //                break;
        //            }

        //        case var case3 when case3 == DbTypeEnum.MySQLDB:
        //            {
        //                break;
        //            }

        //        case var case4 when case4 == DbTypeEnum.DB2:
        //            {
        //                break;
        //            }
        //    }

        //    return retval;
        //}

    }
}
