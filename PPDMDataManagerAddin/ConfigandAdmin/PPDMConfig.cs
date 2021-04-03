/* TODO ERROR: Skipped RegionDirectiveTrivia */using SimpleODM.systemconfigandutil.MiscClasses;
using System;
using System.Collections;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Forms;


namespace SimpleODM.systemconfigandutil
{
    public enum CTLTYPEENUM
    {
        WELL = 0,
        WELLBORE = 1,
        WELLBORESEGMENT = 2,
        WELLBORECOMPLETION = 3,
        CONTACTINTERVAL = 4,
        WELLBORESTREAM = 5
    }

    public enum DbDataDirectionEnum
    {
        Target = 1,
        Source = 2
    }

    public enum DbTypeEnum
    {
        OracleDB = 1,
        SqlServerDB = 2,
        MySQLDB = 3,
        Sqlite = 4,
        DB2 = 5
    }

    public enum ConnectionTypeEnum
    {
        Admin = 0,
        User = 1
    }

    public class PPDMConfig : IPPDMConfig
    {
        public PPDMConfig(IPPDMContext pPPDMContext)
        {
            PPDMContext = pPPDMContext;
            ConnectionPassWordOK = false;
            TripleDes.Key = TruncateHash("FATMAHALDHUBAIB", TripleDes.KeySize / 8);
            TripleDes.IV = TruncateHash("", TripleDes.BlockSize / 8);


            try
            {

                appFolder = Environment.CurrentDirectory;
                CreatemyFolder();
                OpenLocaDbConn();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Simple ODM");
            }
        }
        #region "Properties"
        public string appFolder { get; set; }
        public cls_HealthCheck healthchk { get; set; } = new cls_HealthCheck();
        public UserSettings UserSetting { get; set; } = new UserSettings();
        public IPPDMContext PPDMContext { get; set; }
        public bool ConnectionPassWordOK { get; set; }
        public systemconfigandutil.cls_Defaults Defaults { get; set; } = new systemconfigandutil.cls_Defaults();
        public Process MyProcess { get; set; } = Process.GetCurrentProcess();
        public Hashtable PKFields { get; set; } = new Hashtable();
        public BindingSource PassedBindingSource { get; set; } = new BindingSource();
        public string AppType { get; set; } = "";
        public cls_scriptsGenerator DMLScripter { get; set; } = new systemconfigandutil.cls_scriptsGenerator();
        private Version osVer = Environment.OSVersion.Version;
        private string machineOSPlatform;
        #endregion
        #region "Events"
        private void PPDMContext_ReleaseData()
        {
            ReleaseData?.Invoke();
        }
        public event ValidateRecordEventHandler ValidateRecord;
        public delegate void ValidateRecordEventHandler(ref ObjectStateEntry obj, ref ObjectContext context, string EntityType, string TransType, ref bool Cancel);
        public event PostUpdateEventHandler PostUpdate;
        public delegate void PostUpdateEventHandler(ref ObjectStateEntry obj, ref ObjectContext context, string EntityType, string TransType, ref bool Cancel);
        public event PostInsertEventHandler PostInsert;
        public delegate void PostInsertEventHandler(ref ObjectStateEntry obj, ref ObjectContext context, string EntityType, string TransType, ref bool Cancel);
        public event PreUpdateEventHandler PreUpdate;
        public delegate void PreUpdateEventHandler(ref ObjectStateEntry obj, ref ObjectContext context, string EntityType, string TransType, ref bool Cancel);
        public event PreInsertEventHandler PreInsert;
        public delegate void PreInsertEventHandler(ref ObjectStateEntry obj, ref ObjectContext context, string EntityType, string TransType, ref bool Cancel);
        public event ShowCtlInViewEventHandler ShowCtlInView;
        public delegate void ShowCtlInViewEventHandler(string Title, string pType, string trans);
        public event ShowCtlInTileEventHandler ShowCtlInTile;
        public delegate void ShowCtlInTileEventHandler(string Title, string pType, string trans);
        public event KeyDownEventHandler KeyDown;
        public delegate void KeyDownEventHandler(object sender, KeyEventArgs e);
        public event ReleaseDataEventHandler ReleaseData;
        public delegate void ReleaseDataEventHandler();
        public void RaisedKeyDown(object sender, KeyEventArgs e)
        {
            KeyDown?.Invoke(sender, e);
        }
        public event EventHappendEventHandler EventHappend;
        public delegate void EventHappendEventHandler(string EventDescription, string Value1, string Value2, string Value3);
        public void EvenJustHappend(string pEventDescription, string Value1, string Value2, string Value3)
        {
            EventHappend?.Invoke(pEventDescription, Value1, Value2, Value3);
        }
        public void ShowControlInTile(string Title, string pType, string trans)
        {
            ShowCtlInTile?.Invoke(Title, pType, trans);
        }
        public void ShowControl(string Title, string pType, string trans)
        {
            ShowCtlInView?.Invoke(Title, pType, trans);
        }
        #endregion
        #region "Misc"
        public Stream GetEmbeddedResourceStream(string res)
        {
            Assembly docassembly;
            Stream stre;
            try
            {
                docassembly = Assembly.GetExecutingAssembly();
                stre = docassembly.GetManifestResourceStream("SimpleODM.systemconfigandutil." + res);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : File " + res + " not Found", "Simple ODM");
                return null;
            }

            return stre;
        }
        public StreamReader GetEmbeddedResourceStreamReader(string res)
        {
            Assembly docassembly;
            var reader = default(StreamReader);
            Stream stre;
            try
            {
                docassembly = Assembly.GetExecutingAssembly();
                stre = docassembly.GetManifestResourceStream("SimpleODM.systemconfigandutil." + res);
                reader = new StreamReader(stre);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : File " + res + " not Found", "Simple ODM");
            }

            return reader;
        }
        private TripleDESCryptoServiceProvider TripleDes = new TripleDESCryptoServiceProvider();
        private byte[] TruncateHash(string key, int length)
        {
            var sha1 = new SHA1CryptoServiceProvider();

            // Hash the key. 
            var keyBytes = System.Text.Encoding.Unicode.GetBytes(key);
            var hash = sha1.ComputeHash(keyBytes);

            // Truncate or pad the hash. 
            Array.Resize(ref hash, length);
            return hash;
        }
        public string EncryptData(string plaintext)
        {

            // Convert the plaintext string to a byte array.
            var plaintextBytes = System.Text.Encoding.Unicode.GetBytes(plaintext);

            // Create the stream.
            var ms = new MemoryStream();
            // Create the encoder to write to the stream.
            var encStream = new CryptoStream(ms, TripleDes.CreateEncryptor(), CryptoStreamMode.Write);

            // Use the crypto stream to write the byte array to the stream.
            encStream.Write(plaintextBytes, 0, plaintextBytes.Length);
            encStream.FlushFinalBlock();

            // Convert the encrypted stream to a printable string.
            return Convert.ToBase64String(ms.ToArray());
        }
        public string DecryptData(string encryptedtext)
        {

            // Convert the encrypted text string to a byte array.
            var encryptedBytes = Convert.FromBase64String(encryptedtext);

            // Create the stream.
            var ms = new MemoryStream();
            // Create the decoder to write to the stream.
            var decStream = new CryptoStream(ms, TripleDes.CreateDecryptor(), CryptoStreamMode.Write);

            // Use the crypto stream to write the byte array to the stream.
            decStream.Write(encryptedBytes, 0, encryptedBytes.Length);
            decStream.FlushFinalBlock();

            // Convert the plaintext stream to a string.
            return System.Text.Encoding.Unicode.GetString(ms.ToArray());
        }
        #endregion
        // ------------------------------------------------------------
        #region "Local DB Connection"
        // public DbConnEntities   DbConnEntitiesDataContext { get; set; }
        public void RefershDbConnTable()
        {
            try
            {
                DbConnAdp.Connection = DBDefConn;
                DbConnAdp.Fill(DbConnTable);
            }
            catch (Exception ex)
            {
            }
        }
        public void OpenLocalETLDB()
        {
            try
            {
                string conn = "metadata=res://systemconfigandutil/localetl.csdl|res://systemconfigandutil/localetl.ssdl|res://systemconfigandutil/localetl.msl;provider=System.Data.SqlServerCe.4.0;provider connection string=" + "\"" + "Data Source=" + appFolder + @"\etl.sdf""";
                LocalEtlDbConn = new EntityConnection(conn);
                LocalEtlDbConn.Open();
                //  LocalETLDB = new localetlEntities(LocalEtlDbConn);
            }
            // Dim a As New etl_filelocations

            // a.filepath = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " :  Cannot Open Local DB ETL");
            }
        }
        public void OpenLocaDbConn()
        {
            try
            {
                // AppDomain.CurrentDomain.SetData("DataDirectory", appFolder)
                var strbld1 = new Devart.Data.SQLite.SQLiteConnectionStringBuilder();
                strbld1 = new Devart.Data.SQLite.SQLiteConnectionStringBuilder();
                strbld1.DataSource = appFolder + @"\refdblocal.s3db";
                try
                {
                    mylocaldbconn.ConnectionString = strbld1.ConnectionString;
                    mylocaldbconn.Open();
                }
                catch (Exception ex)
                {
                }

                var strbld2 = new Devart.Data.SQLite.SQLiteConnectionStringBuilder();
                try
                {
                    strbld2 = new Devart.Data.SQLite.SQLiteConnectionStringBuilder();
                    strbld2.DataSource = appFolder + @"\simpleodmdblite.s3db";
                    DBDefConn.ConnectionString = strbld2.ConnectionString;
                    DBDefConn.Open();
                    RefershDbConnTable();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    var adp = new RefValuesTableAdapters.PKColumnsTableAdapter();
                    var p = new RefValues.PKColumnsDataTable();
                    adp.Connection = mylocaldbconn;
                    adp.Fill(p);
                    // Dim p As List(Of cls_FldPK) = LocalDBEntitiesDataContext.ExecuteStoreQuery(Of cls_FldPK)("Select * from pkcolumns", Nothing).ToList
                    foreach (RefValues.PKColumnsRow p1 in p)
                        PKFields.Add(p1.TableName + p1.FieldName, p1);
                }
                catch (Exception ex)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot Connect");
            }
            finally
            {
            }

            OpenLocalETLDB();
        }
        public EntityConnection DbConnSqliteEntityConn { get; set; }
        public EntityConnection SqliteEntityConn { get; set; }
        public EntityConnection AuditEntityConn { get; set; }
        public Devart.Data.SQLite.SQLiteConnection DBDefConn { get; set; } = new Devart.Data.SQLite.SQLiteConnection();
        public Devart.Data.SQLite.SQLiteConnection mylocaldbconn { get; set; } = new Devart.Data.SQLite.SQLiteConnection();
        public DbConnTableAdapters.DatabaseConnectionsTableAdapter DbConnAdp { get; set; } = new DbConnTableAdapters.DatabaseConnectionsTableAdapter();
        public DbConn.DatabaseConnectionsDataTable DbConnTable { get; set; } = new DbConn.DatabaseConnectionsDataTable();
        public EntityConnection LocalEtlDbConn { get; set; }
        public localetlEntities LocalETLDB { get; set; }
        #endregion
        #region "Default Write/Read Functions"
        public void CreatemyFolder()
        {
            try
            {
                // string appd;


                //   appFolder = Path.Combine(appFolder, "TheTechIdea");
                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }

                if (Directory.Exists(appFolder + @"\DLL") == false)
                {
                    Directory.CreateDirectory(appFolder + @"\DLL");
                    // My.Computer.FileSystem.CopyDirectory(My.Application.Info.DirectoryPath & "\DLL", appFolder & "\DLL", True)

                }

                if (Directory.Exists(appFolder + @"\dbxml") == false)
                {
                    Directory.CreateDirectory(appFolder + @"\dbxml");
                }
            }
            catch (Exception ex)
            {
                // Throw ex

            }
        }
        public void LoadDefaults(string DbName)
        {
            var reader = new System.Xml.Serialization.XmlSerializer(typeof(systemconfigandutil.cls_Defaults));
            var myfile = default(StreamReader);
            string mydocpath = Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                if (File.Exists(appFolder + @"\" + DbName + ".xml"))
                {
                    myfile = new StreamReader(appFolder + @"\dbxml\" + DbName + ".xml"); // My.Application.Info.DirectoryPath &
                    Defaults = (systemconfigandutil.cls_Defaults)reader.Deserialize(myfile);
                    myfile.Close();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    myfile.Close();
                    MessageBox.Show("Error : Could not Load Default Values for Application. ignore if it is first time running the program", "Simple ODM");
                }
                catch (Exception ex1)
                {
                }
            }
        }
        public void WriteDefaults(string DbName)
        {
            var writer = new System.Xml.Serialization.XmlSerializer(typeof(systemconfigandutil.cls_Defaults));
            var myfile = default(StreamWriter);
            string mydocpath = Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                myfile = new StreamWriter(appFolder + @"\dbxml\" + DbName + ".xml");
                writer.Serialize(myfile, Defaults);
                myfile.Close();
            }
            catch (Exception ex)
            {
                try
                {
                    myfile.Close();
                    MessageBox.Show("Error : Could not Write Default Values for Application !!!", "Simple ODM");
                }
                catch (Exception ex1)
                {
                }
            }
        }
        public void LoadDefaults()
        {
            var reader = new System.Xml.Serialization.XmlSerializer(typeof(systemconfigandutil.cls_Defaults));
            var myfile = default(StreamReader);
            string mydocpath = Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                if (File.Exists(appFolder + @"\defaults.xml"))
                {
                    myfile = new StreamReader(appFolder + @"\defaults.xml"); // My.Application.Info.DirectoryPath &
                    Defaults = (systemconfigandutil.cls_Defaults)reader.Deserialize(myfile);
                    myfile.Close();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    myfile.Close();
                    MessageBox.Show("Error : Could not Load Default Values for Application. ignore if it is first time running the program", "Simple ODM");
                }
                catch (Exception ex1)
                {
                }
            }
        }
        public void WriteDefaults()
        {
            var writer = new System.Xml.Serialization.XmlSerializer(typeof(systemconfigandutil.cls_Defaults));
            var myfile = default(StreamWriter);
            string mydocpath = Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                myfile = new StreamWriter(appFolder + @"\defaults.xml");
                writer.Serialize(myfile, Defaults);
                myfile.Close();
            }
            catch (Exception ex)
            {
                try
                {
                    myfile.Close();
                    MessageBox.Show("Error : Could not Write Default Values for Application !!!", "Simple ODM");
                }
                catch (Exception ex1)
                {
                }
            }
        }
        #endregion
        public int LoadAutoLogin()
        {
            int retval = 0;
            var reader = new System.Xml.Serialization.XmlSerializer(typeof(UserSettings));
            StreamReader file;
            try
            {
                string myDB = Path.Combine(appFolder, "dbconndefault.xml");
                if (File.Exists(myDB))
                {
                    file = new StreamReader(appFolder + @"\dbconndefault.xml"); // My.Application.Info.DirectoryPath &
                    PPDMContext.UserSetting = (UserSettings)reader.Deserialize(file);
                    PPDMContext.UserSetting.PASSWORD = this.DecryptData(PPDMContext.UserSetting.PASSWORD);
                }
            }
            catch (Exception ex)
            {
                retval = 1;
            }

            return retval;
        }
        public void WriteAutoLogin()
        {
            var writer = new System.Xml.Serialization.XmlSerializer(typeof(UserSettings));
            var file = default(StreamWriter);
            try
            {
                string myDB = Path.Combine(appFolder, "dbconndefault.xml");
                // If (System.IO.File.Exists(myDB)) Then
                file = new StreamWriter(appFolder + @"\dbconndefault.xml");
                PPDMContext.UserSetting.PASSWORD = this.EncryptData(PPDMContext.UserSetting.PASSWORD);
                writer.Serialize(file, PPDMContext.UserSetting);
                file.Close();
            }
            // End If

            catch (Exception ex)
            {
                try
                {
                    file.Close();
                    MessageBox.Show("Error : Could not Load Default Values for Application. ignore if it is first time running the program", "Simple ODM");
                }
                catch (Exception ex1)
                {
                }
                // MsgBox("Error : Could not Write login Values for Application !!!",  "Simple ODM")
            }
        }


        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    }
}