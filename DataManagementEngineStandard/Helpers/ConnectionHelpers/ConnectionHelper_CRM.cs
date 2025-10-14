using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for CRM (Customer Relationship Management) connector configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all CRM connector configurations
        /// </summary>
        /// <returns>List of CRM connector configurations</returns>
        public static List<ConnectionDriversConfig> GetCRMConnectorConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateSalesforceConfig(),
                CreateHubSpotConfig(),
                CreateZohoConfig(),
                CreatePipedriveConfig(),
                CreateMicrosoftDynamics365Config(),
                CreateFreshsalesConfig(),
                CreateSugarCRMConfig(),
                CreateInsightlyConfig(),
                CreateCopperConfig(),
                CreateNutshellConfig(),
                CreateSAPCRMConfig(),
                CreateOracleCRMConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Salesforce connection drivers.</summary>
        /// <returns>A configuration object for Salesforce connection drivers.</returns>
        public static ConnectionDriversConfig CreateSalesforceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Salesforce.Client",
                DriverClass = "Salesforce.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SalesforceConnector.dll",
                ConnectionString = "LoginUrl={Url};Username={UserID};Password={Password};SecurityToken={SecurityToken};",
                iconname = "salesforce.svg",
                classHandler = "SalesforceDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Salesforce,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Salesforce.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for HubSpot connection drivers.</summary>
        /// <returns>A configuration object for HubSpot connection drivers.</returns>
        public static ConnectionDriversConfig CreateHubSpotConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "HubSpot.Client",
                DriverClass = "HubSpot.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.HubSpotConnector.dll",
                ConnectionString = "ApiKey={Password};BaseUrl={Url};",
                iconname = "hubspot.svg",
                classHandler = "HubSpotDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.HubSpot,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "HubSpot.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Zoho CRM connection drivers.</summary>
        /// <returns>A configuration object for Zoho CRM connection drivers.</returns>
        public static ConnectionDriversConfig CreateZohoConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Zoho.CRM.Client",
                DriverClass = "Zoho.CRM.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ZohoConnector.dll",
                ConnectionString = "ClientId={UserID};ClientSecret={Password};RefreshToken={RefreshToken};DataCenter={Host};",
                iconname = "zoho.svg",
                classHandler = "ZohoDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Zoho,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Zoho.CRM.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Pipedrive connection drivers.</summary>
        /// <returns>A configuration object for Pipedrive connection drivers.</returns>
        public static ConnectionDriversConfig CreatePipedriveConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Pipedrive.Client",
                DriverClass = "Pipedrive.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.PipedriveConnector.dll",
                ConnectionString = "ApiToken={Password};CompanyDomain={Host};",
                iconname = "pipedrive.svg",
                classHandler = "PipedriveDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Pipedrive,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Pipedrive.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Microsoft Dynamics 365 connection drivers.</summary>
        /// <returns>A configuration object for Microsoft Dynamics 365 connection drivers.</returns>
        public static ConnectionDriversConfig CreateMicrosoftDynamics365Config()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.Dynamics365.Client",
                DriverClass = "Microsoft.Dynamics365.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.Dynamics365Connector.dll",
                ConnectionString = "ServiceUrl={Url};Username={UserID};Password={Password};",
                iconname = "dynamics365.svg",
                classHandler = "MicrosoftDynamics365DataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.MicrosoftDynamics365,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Microsoft.Dynamics365.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Freshsales connection drivers.</summary>
        /// <returns>A configuration object for Freshsales connection drivers.</returns>
        public static ConnectionDriversConfig CreateFreshsalesConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Freshsales.Client",
                DriverClass = "Freshsales.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.FreshsalesConnector.dll",
                ConnectionString = "Domain={Host};ApiKey={Password};",
                iconname = "freshsales.svg",
                classHandler = "FreshsalesDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Freshsales,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Freshsales.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for SugarCRM connection drivers.</summary>
        /// <returns>A configuration object for SugarCRM connection drivers.</returns>
        public static ConnectionDriversConfig CreateSugarCRMConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "SugarCRM.Client",
                DriverClass = "SugarCRM.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SugarCRMConnector.dll",
                ConnectionString = "BaseUrl={Url};Username={UserID};Password={Password};",
                iconname = "sugarcrm.svg",
                classHandler = "SugarCRMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.SugarCRM,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "SugarCRM.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Insightly connection drivers.</summary>
        /// <returns>A configuration object for Insightly connection drivers.</returns>
        public static ConnectionDriversConfig CreateInsightlyConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Insightly.Client",
                DriverClass = "Insightly.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.InsightlyConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "insightly.svg",
                classHandler = "InsightlyDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Insightly,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Insightly.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Copper CRM connection drivers.</summary>
        /// <returns>A configuration object for Copper CRM connection drivers.</returns>
        public static ConnectionDriversConfig CreateCopperConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Copper.Client",
                DriverClass = "Copper.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.CopperConnector.dll",
                ConnectionString = "ApiKey={Password};Email={UserID};",
                iconname = "copper.svg",
                classHandler = "CopperDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Copper,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Copper.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Nutshell CRM connection drivers.</summary>
        /// <returns>A configuration object for Nutshell CRM connection drivers.</returns>
        public static ConnectionDriversConfig CreateNutshellConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Nutshell.Client",
                DriverClass = "Nutshell.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.NutshellConnector.dll",
                ConnectionString = "Username={UserID};ApiKey={Password};",
                iconname = "nutshell.svg",
                classHandler = "NutshellDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Nutshell,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Nutshell.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for SAP CRM connection drivers.</summary>
        /// <returns>A configuration object for SAP CRM connection drivers.</returns>
        public static ConnectionDriversConfig CreateSAPCRMConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "SAP.CRM.Client",
                DriverClass = "SAP.CRM.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SAPCRMConnector.dll",
                ConnectionString = "Server={Host};SystemNumber={Port};Client={Client};Username={UserID};Password={Password};",
                iconname = "sapcrm.svg",
                classHandler = "SAPCRMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.SAPCRM,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "SAP.CRM.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Oracle CRM connection drivers.</summary>
        /// <returns>A configuration object for Oracle CRM connection drivers.</returns>
        public static ConnectionDriversConfig CreateOracleCRMConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Oracle.CRM.Client",
                DriverClass = "Oracle.CRM.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.OracleCRMConnector.dll",
                ConnectionString = "ServiceUrl={Url};Username={UserID};Password={Password};",
                iconname = "oraclecrm.svg",
                classHandler = "OracleCRMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.OracleCRM,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Oracle.CRM.Client",
                NuggetMissing = false
            };
        }
    }
}