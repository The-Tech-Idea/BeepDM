using Devart.Data.SQLite;
using SimpleODM.DbConnTableAdapters;
using SimpleODM.systemconfigandutil.MiscClasses;
using System.Collections;
using System.Data.Entity.Core.EntityClient;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SimpleODM.systemconfigandutil
{
    public interface IPPDMConfig
    {
        string appFolder { get; set; }
        string AppType { get; set; }
        EntityConnection AuditEntityConn { get; set; }
        bool ConnectionPassWordOK { get; set; }
        DatabaseConnectionsTableAdapter DbConnAdp { get; set; }
        EntityConnection DbConnSqliteEntityConn { get; set; }
        DbConn.DatabaseConnectionsDataTable DbConnTable { get; set; }
        SQLiteConnection DBDefConn { get; set; }
        cls_Defaults Defaults { get; set; }
        cls_scriptsGenerator DMLScripter { get; set; }
        cls_HealthCheck healthchk { get; set; }
        localetlEntities LocalETLDB { get; set; }
        EntityConnection LocalEtlDbConn { get; set; }
        SQLiteConnection mylocaldbconn { get; set; }
        Process MyProcess { get; set; }
        BindingSource PassedBindingSource { get; set; }
        Hashtable PKFields { get; set; }
        IPPDMContext PPDMContext { get; set; }
        EntityConnection SqliteEntityConn { get; set; }
        UserSettings UserSetting { get; set; }

        event PPDMConfig.EventHappendEventHandler EventHappend;
        event PPDMConfig.KeyDownEventHandler KeyDown;
        event PPDMConfig.PostInsertEventHandler PostInsert;
        event PPDMConfig.PostUpdateEventHandler PostUpdate;
        event PPDMConfig.PreInsertEventHandler PreInsert;
        event PPDMConfig.PreUpdateEventHandler PreUpdate;
        event PPDMConfig.ReleaseDataEventHandler ReleaseData;
        event PPDMConfig.ShowCtlInTileEventHandler ShowCtlInTile;
        event PPDMConfig.ShowCtlInViewEventHandler ShowCtlInView;
        event PPDMConfig.ValidateRecordEventHandler ValidateRecord;

        void CreatemyFolder();
        string DecryptData(string encryptedtext);
        string EncryptData(string plaintext);
        void EvenJustHappend(string pEventDescription, string Value1, string Value2, string Value3);
        Stream GetEmbeddedResourceStream(string res);
        StreamReader GetEmbeddedResourceStreamReader(string res);
        int LoadAutoLogin();
        void LoadDefaults();
        void LoadDefaults(string DbName);
        void OpenLocaDbConn();
        void OpenLocalETLDB();
        void RaisedKeyDown(object sender, KeyEventArgs e);
        void RefershDbConnTable();
        void ShowControl(string Title, string pType, string trans);
        void ShowControlInTile(string Title, string pType, string trans);
        void WriteAutoLogin();
        void WriteDefaults();
        void WriteDefaults(string DbName);
    }
}