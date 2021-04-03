using System;
using System.Linq;
using System.Net;


namespace SimpleODM.systemconfigandutil
{
    public class cls_HealthCheck
    {
        public PPDMConfig SimplePPDMConfig;
        private bool pRegistry;

        public bool Registry
        {
            get
            {
                return pRegistry; // CheckIfApplicationTabsExist()
            }
        }

        private bool pDefaults;

        public bool Defaults
        {
            get
            {
                return pDefaults; // CheckDefaultsExistinDDB()
            }
        }

        private bool pTabs;

        public bool Tables
        {
            get
            {
                return pTabs; // CheckIfApplicationTabsExist()
            }
        }

        private bool pAppID;

        public bool AppID
        {
            get
            {
                return pAppID; // CheckIfApplicationIDExist()
            }
        }

        private bool pInternet;

        public bool Internet
        {
            get
            {
                return pInternet; // CheckForInternetConnection()
            }
        }

        public void CheckHealth()
        {
            pInternet = CheckForInternetConnection();
            pAppID = CheckIfApplicationIDExist();
            pTabs = CheckPPDMTabsExist();
            pDefaults = CheckDefaultRequiredValuesExist();
            pRegistry = CheckIfApplicationTabsExist();
        }

        public string CreateSIAREGISTRY()
        {
            string retval = "";
            //if (SimplePPDMConfig.PPDMContext.UserSetting.DATABASETYPE == systemconfigandutil.DbTypeEnum.OracleDB)
            //{
            //    var cmd = new Devart.Data.Oracle.OracleScript();
            //    cmd.Connection = (Devart.Data.Oracle.OracleConnection)SimplePPDMConfig.PPDMContext.EntityConn.StoreConnection;
            //    cmd.ScriptText = "CREATE TABLE SIA_REGISTERY (ID NUMBER NOT NULL , REG1 VARCHAR2(100 CHAR) , REG2 VARCHAR2(100 CHAR) , REG3 VARCHAR2(100 CHAR) , REG4 VARCHAR2(100 CHAR) " + "\n" + ", CONSTRAINT SIA_REGISTERY_PK PRIMARY KEY ( ID ) ENABLE); " + "\n" + "CREATE SEQUENCE SIA_REGISTERY_SEQ;" + "\n" + "CREATE TRIGGER SIA_REGISTERY_TRG " + "\n" + "  BEFORE INSERT ON SIA_REGISTERY " + "\n" + "  FOR EACH ROW " + "\n" + "          BEGIN" + "\n" + "            BEGIN" + "\n" + "    IF INSERTING AND :NEW.ID IS NULL THEN" + "\n" + "      SELECT SIA_REGISTERY_SEQ.NEXTVAL INTO :NEW.ID FROM SYS.DUAL;" + "\n" + "    END IF;" + "\n" + "  END COLUMN_SEQUENCES;" + "\n" + "END;  " + "\n" + "/";












            //    try
            //    {
            //        cmd.Execute();
            //        retval = "OK";
            //    }
            //    catch (Exception ex)
            //    {
            //        retval = ex.Message;
            //    }
            //}
            //else
            //{
            //    var cmd = new System.Data.SqlClient.SqlCommand();
            //    cmd.Connection = (System.Data.SqlClient.SqlConnection)SimplePPDMConfig.PPDMContext.EntityConn.StoreConnection;
            //    try
            //    {
            //        cmd.BeginExecuteNonQuery();
            //        retval = "OK";
            //    }
            //    catch (Exception ex)
            //    {
            //        retval = ex.Message;
            //    }
            //}

            return retval;
        }

        private bool CheckDefaultRequiredValuesExist()
        {
            return (SimplePPDMConfig.Defaults.WELLBORECOMPLETIONIDENTIFIER!=null)&&
                   (SimplePPDMConfig.Defaults.WELLBORESEGMENTIDENTIFIER!=null)&&
                   (SimplePPDMConfig.Defaults.WELLBOREIDENTIFIER!=null) &&
                   (SimplePPDMConfig.Defaults.WELLCONTACTINTERVALIDENTIFIER!=null)
                   &&(SimplePPDMConfig.Defaults.WELLHEADSTREAMIDENTIFIER!=null)
                   &&(SimplePPDMConfig.Defaults.WELLIDENTIFIER!=null)
                   &&(SimplePPDMConfig.Defaults.WELLORIGINIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.WELLREPORTINGHEADSTREAMIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLBORECOMPLETIONIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLBOREIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLBORESEGMENTIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLCONTACTINTERVALIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLHEADSTREAMIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLORIGINIDENTIFIER != null) && 
                   (SimplePPDMConfig.Defaults.XREFWELLREPORTINGHEADSTREAMIDENTIFIER != null);
        }

        private bool CheckDefaultsExistinDDB()
        {
            bool retval;
            int xnt;
            string pcmd = "select count(*) from  " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".RM_FILE_CONTENT Where FILE_ID = 'SODMDEFUALTS'";
            if (SimplePPDMConfig.PPDMContext.UserSetting.DATABASETYPE == systemconfigandutil.DbTypeEnum.SqlServerDB)
            {
                pcmd = pcmd.Replace(SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".", " ");
            }

            xnt = SimplePPDMConfig.PPDMContext.EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<int>(pcmd, null).FirstOrDefault();
            if (xnt > 0)
            {
                // SimplePPDMConfig.PPDMContext.LoadDefaults()
                retval = true;
            }
            else
            {
                retval = false;
            }

            return retval;
        }

        private bool CheckIfApplicationTabsExist()
        {
            bool retval=false;
            try
            {
                // Dim SIA_REGISTERY As DataTable = SimplePPDMConfig.PPDMContext.EntityConn.StoreConnection.GetSchema("SIA_REGISTERY")
              //  retval = SimplePPDMConfig.PPDMContext.DBUtil.CheckTableExit("SIA_REGISTERY");
            }
            catch (Exception ex)
            {
                retval = Convert.ToBoolean(0);
            }

            return retval;
        }

        private bool CheckPPDMTabsExist()
        {
            bool retval=false;
            try
            {
                // Dim SIA_REGISTERY As DataTable = SimplePPDMConfig.PPDMContext.EntityConn.StoreConnection.GetSchema("SIA_REGISTERY")
              //  retval = SimplePPDMConfig.PPDMContext.DBUtil.CheckTableExit("WELL");
            }
            catch (Exception ex)
            {
                retval = false;
            }

            return retval;
        }

        private bool CheckIFhasCreatePriv()
        {
            bool retval;
            var xnt = default(int);
            try
            {
                switch (SimplePPDMConfig.PPDMContext.UserSetting.DATABASETYPE)
                {
                    case systemconfigandutil.DbTypeEnum.OracleDB:
                        {
                            xnt = SimplePPDMConfig.PPDMContext.EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<int>("select privilege from session_privs where privilege='CREATE TABLE'", null).FirstOrDefault();
                            break;
                        }

                    case systemconfigandutil.DbTypeEnum.SqlServerDB:
                        {
                            xnt = SimplePPDMConfig.PPDMContext.EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<int>("SELECT HAS_PERMS_BY_NAME(db_name(),'DATABASE', 'CREATE TABLE');'", null).FirstOrDefault();
                            break;
                        }

                    case systemconfigandutil.DbTypeEnum.Sqlite:
                        {
                            break;
                        }

                    case systemconfigandutil.DbTypeEnum.MySQLDB:
                        {
                            break;
                        }

                    case systemconfigandutil.DbTypeEnum.DB2:
                        {
                            break;
                        }
                }

                if (xnt > 0)
                {
                    // SimplePPDMConfig.PPDMContext.LoadDefaults()
                    retval = true;
                }
                else
                {
                    retval = false;
                }
            }
            catch (Exception ex)
            {
                retval = false;
            }

            return retval;
        }

        private bool CheckIfApplicationIDExist()
        {
            bool retval;
            int xnt;
            try
            {
                string qry = "select count(*) from  " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_SW_APPLICATION x   Where x.SW_APPLICATION_ID = 'SODM'";
                if (SimplePPDMConfig.PPDMContext.UserSetting.DATABASETYPE == systemconfigandutil.DbTypeEnum.SqlServerDB)
                {
                    qry = qry.Replace(SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".", " ");
                }

                xnt = SimplePPDMConfig.PPDMContext.EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<int>(qry, null).FirstOrDefault();
                if (xnt > 0)
                {
                    // SimplePPDMConfig.PPDMContext.LoadDefaults()
                    retval = true;
                }
                else
                {
                    retval = false;
                }
            }
            catch (Exception ex)
            {
                retval = false;
            }

            return retval;
        }

        private bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        pInternet = true;
                        return true;
                    }
                }
            }
            catch
            {
                pInternet = false;
                return false;
            }
        }

        public void CheckInternetOn()
        {
            pInternet = CheckForInternetConnection();
        }

        public string CreateApplicationRecordForSODM()
        {
            string retval = "";
          //  retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_SW_APPLICATION  (SW_APPLICATION_ID, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('SODM','Simple Oil and Gas Data Manager','SODM','SODM','Y')", "R_SW_APPLICATION");
            return retval;
        }
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public string InsertStandardDefaults()
        {
            string retval = "";
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WELL','WELL','WELL','WELL','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WO','WELL ORIGIN','WELL ORIGIN','WO','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WB','WELLBORE','WELLBORE','WB','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WS','WELLBORE SEGMENT','WELLBORE SEGMENT','WS','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('C','WELLBORE COMPLETION','WELLBORE COMPLETION','C','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('CI','WELLBORE CONTACT INTERVAL','WELLBORE CI','CI','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WHS','WELLHEAD STREAM','WELLHEAD STREAM','WHS','Y')", "R_WELL_XREF_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_XREF_TYPE (XREF_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WRS','WELLHEAD REPORTING STREAM','WELLHEAD REPORTING STREAM','WRS','Y')", "R_WELL_XREF_TYPE");

            //// --------------------------------------------------------------

            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WELL','WELL','WELL','WELL','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WO','WELL ORIGIN','WELL ORIGIN','WO','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WB','WELLBORE','WELLBORE','WB','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WS','WELLBORE SEGMENT','WELLBORE SEGMENT','WS','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('C','WELLBORE COMPLETION','WELLBORE COMPLETION','C','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('CI','WELLBORE CONTACT INTERVAL','WELLBORE CI','CI','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WHS','WELLHEAD STREAM','WELLHEAD STREAM','WHS','Y')", "R_WELL_LEVEL_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_LEVEL_TYPE (WELL_LEVEL_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('WRS','WELLHEAD REPORTING STREAM','WELLHEAD REPORTING STREAM','WRS','Y')", "R_WELL_LEVEL_TYPE");

            // -----------------------------------------------------------------------

            return retval;
        }

        public string CreateActivityandLogDefaultsValues()
        {
            string retval = "";
            // R_WELL_ACTIVITY_COMP_TYPE
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".R_WELL_ACTIVITY_COMP_TYPE (WELL_ACTIVITY_COMPONENT_TYPE, LONG_NAME, SHORT_NAME,ABBREVIATION,ACTIVE_IND) VALUES ('LOG','LOG','LOG','LOG','Y')", "R_WELL_ACTIVITY_COMP_TYPE");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".WELL_ACTIVITY_SET (ACTIVITY_SET_ID, ACTIVE_IND) VALUES ('LOG','Y')", "WELL_ACTIVITY_SET");
            //retval += SimplePPDMConfig.RunCommandonDB("INSERT INTO " + SimplePPDMConfig.PPDMContext.UserSetting.SCHEMA_NAME + ".WELL_ACTIVITY_TYPE (ACTIVITY_SET_ID, ACTIVITY_TYPE,ACTIVE_IND) VALUES ('LOG','LOG','Y')", "WELL_ACTIVITY_TYPE");
            return retval;
        }

        public string SetDefaultStandard()
        {
            string retval = "";
            SimplePPDMConfig.Defaults.WELLIDENTIFIER = "WELL";
            SimplePPDMConfig.Defaults.WELLBOREIDENTIFIER = "WB";
            SimplePPDMConfig.Defaults.WELLBORECOMPLETIONIDENTIFIER = "C";
            SimplePPDMConfig.Defaults.WELLBORESEGMENTIDENTIFIER = "WS";
            SimplePPDMConfig.Defaults.WELLCONTACTINTERVALIDENTIFIER = "CI";
            SimplePPDMConfig.Defaults.WELLHEADSTREAMIDENTIFIER = "WHS";
            SimplePPDMConfig.Defaults.WELLREPORTINGHEADSTREAMIDENTIFIER = "WRS";
            SimplePPDMConfig.Defaults.WELLORIGINIDENTIFIER = "WO";
            SimplePPDMConfig.Defaults.XREFWELLIDENTIFIER = "WELL";
            SimplePPDMConfig.Defaults.XREFWELLBOREIDENTIFIER = "WB";
            SimplePPDMConfig.Defaults.XREFWELLBORECOMPLETIONIDENTIFIER = "C";
            SimplePPDMConfig.Defaults.XREFWELLBORESEGMENTIDENTIFIER = "WS";
            SimplePPDMConfig.Defaults.XREFWELLCONTACTINTERVALIDENTIFIER = "CI";
            SimplePPDMConfig.Defaults.XREFWELLHEADSTREAMIDENTIFIER = "WHS";
            SimplePPDMConfig.Defaults.XREFWELLREPORTINGHEADSTREAMIDENTIFIER = "WRS";
            SimplePPDMConfig.Defaults.XREFWELLORIGINIDENTIFIER = "WO";
            return retval;
        }

        public string SetDefaultStandardForLogActivity()
        {
            string retval = "";
            SimplePPDMConfig.Defaults.WELL_ACTIVITY_COMPONENT_TYPE = "LOG";
            SimplePPDMConfig.Defaults.ACTIVITY_SET_ID_FOR_LOG = "LOG";
            SimplePPDMConfig.Defaults.ACTIVITY_SET_TYPE_FOR_LOG = "LOG";
            return retval;
        }
        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    }
}