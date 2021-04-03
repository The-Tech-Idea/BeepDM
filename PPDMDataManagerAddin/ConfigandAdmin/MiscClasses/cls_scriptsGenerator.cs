using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




 namespace SimpleODM.systemconfigandutil
    {
       
        public class cls_scriptsGenerator
        {
            public PPDMConfig SimpleODMConfig { get; set; }
            private StreamReader PPDMTAB { get; set; }
            private StreamReader PPDMPK { get; set; }
            private StreamReader PPDMCK { get; set; }
            private StreamReader PPDMUK { get; set; }
            private StreamReader PPDMFK { get; set; }

            // Public Property PPDMTargetDb As SimpleODM.systemconfigandutil.DbTypeEnum
            public List<PPDMModel> PPDMModelScripts { get; set; } = new List<PPDMModel>();


            // Dim v As Int16
            // Public Property CurrVersion As PPDMVERSION
            // Get
            // CurrVersion = v
            // End Get
            // Set(value As PPDMVERSION)
            // v = value
            // Select Case value
            // Case PPDMVERSION.v38
            // VersionString = "38"
            // Case PPDMVERSION.v39
            // VersionString = "39"
            // End Select
            // End Set
            // End Property
            private string VersionString { get; set; } = "38";

            public void GenerateScriptForPPDM38Model_Oracle()
            {
                if (PPDMModelScripts.Count > 0)
                {
                    PPDMModelScripts.Clear();
                    this.GenerateScriptUnits(systemconfigandutil.DbTypeEnum.OracleDB, PPDMVERSION.v38);
                }
            }

            public void GenerateScriptForPPDM38Model_Microsoft()
            {
                if (PPDMModelScripts.Count > 0)
                {
                    PPDMModelScripts.Clear();
                    this.GenerateScriptUnits(systemconfigandutil.DbTypeEnum.SqlServerDB, PPDMVERSION.v38);
                }
            }

            public void GenerateScriptForPPDM39Model_Oracle()
            {
                if (PPDMModelScripts.Count > 0)
                {
                    PPDMModelScripts.Clear();
                    this.GenerateScriptUnits(systemconfigandutil.DbTypeEnum.OracleDB, PPDMVERSION.v39);
                }
            }

            public void GenerateScriptForPPDM39Model_Microsoft()
            {
                if (PPDMModelScripts.Count > 0)
                {
                    PPDMModelScripts.Clear();
                    this.GenerateScriptUnits(systemconfigandutil.DbTypeEnum.SqlServerDB, PPDMVERSION.v39);
                }
            }

            public void SetVersionString()
            {
                switch (SimpleODMConfig.PPDMContext.VERSION)
                {
                    case PPDMVERSION.v38:
                        {
                            VersionString = "38";
                            break;
                        }

                    case PPDMVERSION.v39:
                        {
                            VersionString = "39";
                            break;
                        }
                }
            }

            public void SaveScripts2Local()
            {
                SetVersionString();
                WritePPDMscripts(PPDMModelScripts.FirstOrDefault());
            }

            public void LoadScriptsFromLocal(systemconfigandutil.DbTypeEnum PPDMTargetDb, PPDMVERSION CurrVersion)
            {
                PPDMModelScripts.Clear();
                LoadPPDMscripts();
                // GenerateScriptUnits(PPDMTargetDb, CurrVersion)
            }
            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            private string loadTableCreationsResourceFile()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        PPDMTAB = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + ".TAB");
                    }
                    else
                    {
                        PPDMTAB = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + "_TAB.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " Table Generation Scripts";
                }

                return res;
            }

            private string loadPKCreationsResourceFile()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        PPDMPK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + ".pk");
                    }
                    else
                    {
                        PPDMPK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + "_PK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " PK Generation Scripts";
                }

                return res;
            }

            private string loadUKCreationsResourceFile()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        PPDMUK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + ".UK");
                    }
                    else
                    {
                        PPDMUK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + "_UK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " CK Generation Scripts";
                }

                return res;
            }

            private string loadCKCreationsResourceFile()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        PPDMCK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + ".CK");
                    }
                    else
                    {
                        PPDMCK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + "_CK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " CK Generation Scripts";
                }

                return res;
            }

            private string loadFKCreationsResourceFile()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        PPDMFK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + ".FK");
                    }
                    else
                    {
                        PPDMFK = SimpleODMConfig.GetEmbeddedResourceStreamReader("PPDM" + VersionString + "_FK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " FK Generation Scripts";
                }

                return res;
            }
            /* TODO ERROR: Skipped EndRegionDirectiveTrivia *//* TODO ERROR: Skipped RegionDirectiveTrivia */
            private StreamReader str;

            private string GetDB()
            {
                switch (SimpleODMConfig.PPDMContext.UserSetting.DATABASETYPE)
                {
                    case systemconfigandutil.DbTypeEnum.OracleDB:
                        {
                            return "ora";
                        }

                    case systemconfigandutil.DbTypeEnum.SqlServerDB:
                        {
                            return "ms";
                        }

                    case systemconfigandutil.DbTypeEnum.MySQLDB:
                        {
                            return "Mysql";
                        }

                    case systemconfigandutil.DbTypeEnum.Sqlite:
                        {
                            return "sqlite";
                        }

                    case systemconfigandutil.DbTypeEnum.DB2:
                        {
                            return "DB2";
                        }

                    default:
                        {
                            return "";
                        }
                }
            }

            private string loadTableCreationsResourceFileFromAppFolder()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        if (GetDB() == "ms")
                        {
                            PPDMTAB = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_TAB.SQL");
                        }
                        else
                        {
                            PPDMTAB = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + ".TAB");
                        }
                    }
                    else
                    {
                        PPDMTAB = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_TAB.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " Table Generation Scripts";
                }

                return res;
            }

            private string loadPKCreationsResourceFileFromAppFolder()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        if (GetDB() == "ms")
                        {
                            PPDMPK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_pk.sql");
                        }
                        else
                        {
                            PPDMPK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + ".pk");
                        }
                    }
                    else
                    {
                        PPDMPK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_PK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " PK Generation Scripts";
                }

                return res;
            }

            private string loadUKCreationsResourceFileFromAppFolder()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        if (GetDB() == "ms")
                        {
                            PPDMUK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_UK.sql");
                        }
                        else
                        {
                            PPDMUK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + ".UK");
                        }
                    }
                    else
                    {
                        PPDMUK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_UK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " CK Generation Scripts";
                }

                return res;
            }

            private string loadCKCreationsResourceFileFromAppFolder()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        if (GetDB() == "ms")
                        {
                            PPDMCK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_CK.sql");
                        }
                        else
                        {
                            PPDMCK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + ".CK");
                        }
                    }
                    else
                    {
                        PPDMCK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_CK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " CK Generation Scripts";
                }

                return res;
            }

            private string loadFKCreationsResourceFileFromAppFolder()
            {
                string res = "";
                try
                {
                    if (VersionString == "38")
                    {
                        if (GetDB() == "ms")
                        {
                            PPDMFK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_FK.sql");
                        }
                        else
                        {
                            PPDMFK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + ".FK");
                        }
                    }
                    else
                    {
                        PPDMFK = new StreamReader(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_FK.SQL");
                    }
                }
                catch (Exception ex)
                {
                    res = "Error in Readin PPDM" + VersionString + " FK Generation Scripts";
                }

                return res;
            }

            /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
            public void GenerateScriptUnits(systemconfigandutil.DbTypeEnum PPDMTargetDb, PPDMVERSION CurrVersion)
            {
                var x = new PPDMModel();
                PPDMModelScripts.Clear();
                switch (CurrVersion)
                {
                    case PPDMVERSION.v38:
                        {
                            VersionString = "38";
                            break;
                        }

                    case PPDMVERSION.v39:
                        {
                            VersionString = "39";
                            break;
                        }
                }

                x.CurrVersion = CurrVersion;
                x.PPDMTargetDb = PPDMTargetDb;
                try
                {
                    loadTableCreationsResourceFileFromAppFolder();
                    while (PPDMTAB.EndOfStream == false)
                    {
                        var y = new script_unit();
                        y = ReadUnitFromStream(PPDMTAB);
                        y.ScriptType = "TAB";
                        x.PPDMTAB.Add(y);
                        y.TableGenerationStatus = false;
                        x.RunScripts.Add(y);
                    }

                    PPDMTAB.Close();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    loadPKCreationsResourceFileFromAppFolder();
                    while (PPDMPK.EndOfStream == false)
                    {
                        var y = new script_unit();
                        y = ReadUnitFromStream(PPDMPK);
                        y.ScriptType = "PK";
                        y.TableGenerationStatus = false;
                        x.PPDMPK.Add(y);
                        x.RunScripts.Add(y);
                    }

                    PPDMPK.Close();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    loadCKCreationsResourceFileFromAppFolder();
                    while (PPDMCK.EndOfStream == false)
                    {
                        var y = new script_unit();
                        y = ReadUnitFromStream(PPDMCK);
                        y.ScriptType = "CK";
                        y.TableGenerationStatus = false;
                        x.PPDMCK.Add(y);
                        x.RunScripts.Add(y);
                    }

                    PPDMCK.Close();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    loadUKCreationsResourceFileFromAppFolder();
                    while (PPDMUK.EndOfStream == false)
                    {
                        var y = new script_unit();
                        y = ReadUnitFromStream(PPDMUK);
                        y.ScriptType = "UK";
                        y.TableGenerationStatus = false;
                        x.PPDMUK.Add(y);
                        x.RunScripts.Add(y);
                    }

                    PPDMUK.Close();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    loadFKCreationsResourceFileFromAppFolder();
                    while (PPDMFK.EndOfStream == false)
                    {
                        var y = new script_unit();
                        y = ReadUnitFromStream(PPDMFK);
                        y.ScriptType = "FK";
                        y.TableGenerationStatus = false;
                        x.PPDMFK.Add(y);
                        x.RunScripts.Add(y);
                    }

                    PPDMFK.Close();
                }
                catch (Exception ex)
                {
                }

                // Try
                // Dim scrpts As New List(Of script_unit)
                // scrpts.AddRange(PPDMTAB)
                // scrpts.AddRange(PPDMPK)
                // scrpts.AddRange(PPDMUK)
                // scrpts.AddRange(PPDMFK)
                // x.RunScripts.AddRange(scrpts)
                // Catch ex As Exception

                // End Try
                PPDMModelScripts.Add(x);
            }

            private script_unit ReadUnitFromStream(StreamReader pstr)
            {
                string res = "";
                var x = new script_unit();
                bool exitcond = true;
                bool startflag = false;
                while (exitcond)
                {
                    if (pstr.EndOfStream == true)
                    {
                        exitcond = false;
                    }

                    if (exitcond)
                    {
                        try
                        {
                            res = pstr.ReadLine();
                        }
                        catch (Exception ex)
                        {
                            break;
                        }
                    }

                    try
                    {
                        if (res.Length > 0)
                        {
                            var paresdline = res.Split(' ');
                            if (SimpleODMConfig.PPDMContext.UserSetting.DATABASETYPE == systemconfigandutil.DbTypeEnum.SqlServerDB)
                            {
                                if (!res.StartsWith("PRINT"))
                                {
                                    if (res.Trim() == "GO")
                                    {
                                        if (startflag == true)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (paresdline[0].ToUpper() == "CREATE" | paresdline[0].ToUpper() == "ALTER")
                                        {
                                            x.TableName = paresdline[2];
                                            // If x.TableName = "WELL" Then
                                            // MsgBox("Fnd")
                                            // End If
                                            startflag = true;
                                        }
                                        // If (res.Contains("EXTENSION_CONDITION")) Then
                                        // MsgBox("!")
                                        // End If

                                        if (!res.StartsWith("PRINT"))
                                        {
                                            x.Script = x.Script + res +"\n";
                                        }
                                    }
                                }
                            }

                            if (SimpleODMConfig.PPDMContext.UserSetting.DATABASETYPE == systemconfigandutil.DbTypeEnum.OracleDB)
                            {
                                if (paresdline[0].ToUpper() == "CREATE" | paresdline[0].ToUpper() == "ALTER")
                                {
                                    x.TableName = paresdline[2];
                                    startflag = true;
                                    x.Script = x.Script + res + "\n";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                return x;
            }

            private void LoadPPDMscripts()
            {
                var reader = new System.Xml.Serialization.XmlSerializer(typeof(PPDMModel));
                var myfile = default(StreamReader);
                string mydocpath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                var jso = new Newtonsoft.Json.JsonSerializer();
                string Filename;
                try
                {
                    if (VersionString == "38")
                    {
                        Filename = SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_ModelScripts.xml";
                        if (File.Exists(Filename))
                        {
                            myfile = new StreamReader(Filename); // My.Application.Info.DirectoryPath &
                            PPDMModelScripts.Add((PPDMModel)jso.Deserialize(myfile, typeof(PPDMModel)));
                            myfile.Close();
                        }
                    }
                    else
                    {
                        Filename = SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_ModelScripts.xml";
                        if (File.Exists(Filename))
                        {
                            myfile = new StreamReader(Filename); // My.Application.Info.DirectoryPath &
                            PPDMModelScripts.Add((PPDMModel)jso.Deserialize(myfile, typeof(PPDMModel)));
                            myfile.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // myfile.Close()
                    // MsgBox("Error : Could not Load Default Values for Application. ignore if it is first time running the program", MsgBoxStyle.Critical, "Simple ODM")
                }

                try
                {
                    myfile.Close();
                }
                catch (Exception ex)
                {
                }
            }

            private void WritePPDMscripts(PPDMModel x)
            {
                var writer = new System.Xml.Serialization.XmlSerializer(typeof(PPDMModel));
                var myfile = default(StreamWriter);
                var jso = new Newtonsoft.Json.JsonSerializer();
                string mydocpath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                // jsw.Path = mydocpath
                // jso.Serialize()
                try
                {
                    if (VersionString == "38")
                    {
                        myfile = new StreamWriter(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_ModelScripts.xml");
                        jso.Serialize(myfile, x);
                        myfile.Close();
                    }
                    else
                    {
                        myfile = new StreamWriter(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_ModelScripts.xml");
                        jso.Serialize(myfile, x);
                        myfile.Close();
                    }
                }
                catch (Exception ex)
                {
                    myfile.Close();
                    // MsgBox("Error : Could not Write Default Values for Application !!!", MsgBoxStyle.Critical, "Simple ODM")
                }
            }

            public bool CheckifFileexist()
            {
                if (VersionString == "38")
                {
                    return File.Exists(SimpleODMConfig.appFolder + @"\files_scripts\DML_38\" + GetDB() + @"\PPDM" + VersionString + "_ModelScripts.xml");
                }
                else
                {
                    return File.Exists(SimpleODMConfig.appFolder + @"\files_scripts\DML_39\" + GetDB() + @"\PPDM" + VersionString + "_ModelScripts.xml");
                }
            }

            //public string RunCommandsDMLScripts()
            //{
            //    string retval = "";
            //    foreach (script_unit pscript in PPDMModelScripts[0].RunScripts.Where(x => x.TableGenerationStatus == false))
            //    {
            //        switch (SimpleODMConfig.PPDMContext.UserSetting.DATABASETYPE)
            //        {
            //            case systemconfigandutil.DbTypeEnum.OracleDB:
            //                {
            //                    var cmd = new OracleScript();
            //                    cmd.ScriptText = pscript.Script;
            //                    cmd.Connection = SimpleODMConfig.PPDMContext.OracleDatabaseConnection;
            //                    try
            //                    {
            //                        cmd.Execute();
            //                        pscript.Status = EnumScriptRunStatus.Ok;
            //                        pscript.TableGenerationStatus = true;
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        pscript.Status = EnumScriptRunStatus.RunError;
            //                        pscript.TableGenerationStatus = false;
            //                        pscript.Errormsg = ex.Message;
            //                    }
            //                    // Exit For
            //                    finally
            //                    {
            //                    }

            //                    break;
            //                }

            //            case systemconfigandutil.DbTypeEnum.SqlServerDB:
            //                {
            //                    if (SimpleODMConfig.PPDMContext.UserSetting.LocalDb == false)
            //                    {
            //                        var cmd = new SqlCommand();
            //                        cmd.CommandText = pscript.Script;
            //                        cmd.Connection = SimpleODMConfig.PPDMContext.SQLDatabaseConnection;
            //                        try
            //                        {
            //                            int aff = cmd.ExecuteNonQuery();
            //                            pscript.Status = EnumScriptRunStatus.Ok;
            //                            pscript.TableGenerationStatus = true;
            //                        }
            //                        catch (Exception ex)
            //                        {
            //                            pscript.Status = EnumScriptRunStatus.RunError;
            //                            pscript.TableGenerationStatus = false;
            //                            pscript.Errormsg = ex.Message;
            //                        }
            //                        // Exit For
            //                        finally
            //                        {
            //                        }
            //                    }
            //                    else
            //                    {
            //                        var cmd = new System.Data.SqlServerCe.SqlCeCommand();
            //                        cmd.CommandText = pscript.Script;
            //                        cmd.Connection = SimpleODMConfig.PPDMContext.LocalSQLDatabaseConnection;
            //                        try
            //                        {
            //                            int aff = cmd.ExecuteNonQuery();
            //                            pscript.Status = EnumScriptRunStatus.Ok;
            //                            pscript.TableGenerationStatus = true;
            //                        }
            //                        catch (Exception ex)
            //                        {
            //                            pscript.Status = EnumScriptRunStatus.RunError;
            //                            pscript.TableGenerationStatus = false;
            //                            pscript.Errormsg = ex.Message;
            //                        }
            //                        // Exit For
            //                        finally
            //                        {
            //                        }
            //                    }

            //                    break;
            //                }

            //            case systemconfigandutil.DbTypeEnum.Sqlite:
            //                {
            //                    var cmd = new Devart.Data.SQLite.SQLiteCommand();
            //                    cmd.CommandText = pscript.Script;
            //                    cmd.Connection = SimpleODMConfig.PPDMContext.SQLiteDataBaseConnection;
            //                    try
            //                    {
            //                        int aff = cmd.ExecuteNonQuery();
            //                        pscript.Status = EnumScriptRunStatus.Ok;
            //                        pscript.TableGenerationStatus = true;
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        pscript.Status = EnumScriptRunStatus.RunError;
            //                        pscript.TableGenerationStatus = false;
            //                        pscript.Errormsg = ex.Message;
            //                    }
            //                    // Exit For
            //                    finally
            //                    {
            //                    }

            //                    break;
            //                }
            //        }
            //    }

            //    return retval;
            //}
            //// #Region "JSON Read and Write"

            // Public Sub WriteFieldJsonXML(ByRef pField As cls_FinderField)

            // Dim file As New System.IO.StreamWriter(My.Application.Info.DirectoryPath & "\" & pField.Field_Code & ".xml")

            // Dim serializer = New JsonSerializer()

            // serializer.Serialize(file, pField)

            // file.Close()

            // End Sub

            // Public Function ReadFieldJsonXML(Field_Code As String) As cls_FinderField

            // Dim file As System.IO.StreamReader

            // Dim pField As cls_FinderField

            // Try

            // file = New System.IO.StreamReader(My.Application.Info.DirectoryPath & "\" & Field_Code & ".xml")

            // Dim serializer = New JsonSerializer

            // pField = serializer.Deserialize(file, GetType(cls_FinderField))

            // Return pField

            // Catch ex As IOException

            // Return Nothing

            // End Try

            // End Function

            // Public Sub WriteGCJsonXML(ByRef pgc As cls_FinderGC)

            // Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(cls_FinderGC))

            // Dim file As New System.IO.StreamWriter(My.Application.Info.DirectoryPath & "\" & pgc.GC & ".xml")

            // Dim serializer = New JsonSerializer()

            // serializer.Serialize(file, pgc)

            // file.Close()

            // End Sub

            // Public Function ReadGCJsonXML(pGCID As String) As cls_FinderGC

            // Dim pgc As cls_FinderGC

            // Dim file As System.IO.StreamReader

            // Try

            // file = New System.IO.StreamReader(My.Application.Info.DirectoryPath & "\" & pGCID & ".xml")

            // Dim serializer = New JsonSerializer()

            // pgc = serializer.Deserialize(file, GetType(cls_FinderGC))

            // Return pgc

            // Catch ex As IOException

            // Return Nothing

            // End Try

            // End Function

            // #End Region

            public cls_scriptsGenerator()
            {
            }

            public cls_scriptsGenerator(systemconfigandutil.PPDMConfig pSimpleODMConfig)
            {
                SimpleODMConfig = pSimpleODMConfig;
            }
        }

        public enum EnumScriptRunStatus
        {
            Ok = 0,
            RunError = 1,
            Pending = 2
        }

        public class script_unit
        {
            public string TableName { get; set; } = "";
            public string Script { get; set; } = "";
            public bool TableGenerationStatus { get; set; } = false;
            public bool Selected { get; set; } = false;
            public string Errormsg { get; set; } = "";
            public string ScriptType { get; set; } = "";
            public EnumScriptRunStatus Status { get; set; }

            public script_unit()
            {
                Status = EnumScriptRunStatus.Pending;
            }
        }

        public class PPDMModel
        {
            public List<script_unit> PPDMTAB { get; set; } = new List<script_unit>();
            public List<script_unit> PPDMFK { get; set; } = new List<script_unit>();
            public List<script_unit> PPDMPK { get; set; } = new List<script_unit>();
            public List<script_unit> PPDMCK { get; set; } = new List<script_unit>();
            public List<script_unit> PPDMUK { get; set; } = new List<script_unit>();
            public List<script_unit> RunScripts { get; set; } = new List<script_unit>();
            public PPDMVERSION CurrVersion { get; set; }
            public bool RanStatus { get; set; } = false;
            public systemconfigandutil.DbTypeEnum PPDMTargetDb { get; set; }

            public PPDMModel()
            {
            }
        }
    }

