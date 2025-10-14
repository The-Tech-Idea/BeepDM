using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Project Management platform connector configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Project Management connector configurations
        /// </summary>
        /// <returns>List of Project Management connector configurations</returns>
        public static List<ConnectionDriversConfig> GetProjectManagementConnectorConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateJiraConfig(),
                CreateTrelloConfig(),
                CreateAsanaConfig(),
                CreateMondayConfig(),
                CreateClickUpConfig(),
                CreateBasecampConfig(),
                CreateNotionConfig(),
                CreateWrikeConfig(),
                CreateSmartsheetConfig(),
                CreateTeamworkConfig(),
                CreatePodioConfig(),
                CreateAzureBoardsConfig(),
                CreateSmartsheetPMConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Jira connection drivers.</summary>
        /// <returns>A configuration object for Jira connection drivers.</returns>
        public static ConnectionDriversConfig CreateJiraConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Atlassian.Jira.Client",
                DriverClass = "Atlassian.Jira.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.JiraConnector.dll",
                ConnectionString = "ServerUrl={Url};Username={UserID};ApiToken={Password};",
                iconname = "jira.svg",
                classHandler = "JiraDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Jira,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Atlassian.Jira.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Trello connection drivers.</summary>
        /// <returns>A configuration object for Trello connection drivers.</returns>
        public static ConnectionDriversConfig CreateTrelloConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Trello.Client",
                DriverClass = "Trello.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.TrelloConnector.dll",
                ConnectionString = "ApiKey={UserID};Token={Password};",
                iconname = "trello.svg",
                classHandler = "TrelloDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Trello,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Trello.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Asana connection drivers.</summary>
        /// <returns>A configuration object for Asana connection drivers.</returns>
        public static ConnectionDriversConfig CreateAsanaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Asana.Client",
                DriverClass = "Asana.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.AsanaConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "asana.svg",
                classHandler = "AsanaDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Asana,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Asana.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Monday.com connection drivers.</summary>
        /// <returns>A configuration object for Monday.com connection drivers.</returns>
        public static ConnectionDriversConfig CreateMondayConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Monday.Client",
                DriverClass = "Monday.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MondayConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "monday.svg",
                classHandler = "MondayDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Monday,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Monday.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for ClickUp connection drivers.</summary>
        /// <returns>A configuration object for ClickUp connection drivers.</returns>
        public static ConnectionDriversConfig CreateClickUpConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ClickUp.Client",
                DriverClass = "ClickUp.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ClickUpConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "clickup.svg",
                classHandler = "ClickUpDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.ClickUp,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ClickUp.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Basecamp connection drivers.</summary>
        /// <returns>A configuration object for Basecamp connection drivers.</returns>
        public static ConnectionDriversConfig CreateBasecampConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Basecamp.Client",
                DriverClass = "Basecamp.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.BasecampConnector.dll",
                ConnectionString = "AccountId={Host};AccessToken={Password};",
                iconname = "basecamp.svg",
                classHandler = "BasecampDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Basecamp,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Basecamp.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Notion connection drivers.</summary>
        /// <returns>A configuration object for Notion connection drivers.</returns>
        public static ConnectionDriversConfig CreateNotionConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Notion.Client",
                DriverClass = "Notion.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.NotionConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "notion.svg",
                classHandler = "NotionDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Notion,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Notion.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Wrike connection drivers.</summary>
        /// <returns>A configuration object for Wrike connection drivers.</returns>
        public static ConnectionDriversConfig CreateWrikeConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Wrike.Client",
                DriverClass = "Wrike.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.WrikeConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "wrike.svg",
                classHandler = "WrikeDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Wrike,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Wrike.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Smartsheet connection drivers.</summary>
        /// <returns>A configuration object for Smartsheet connection drivers.</returns>
        public static ConnectionDriversConfig CreateSmartsheetConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Smartsheet.Client",
                DriverClass = "Smartsheet.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SmartsheetConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "smartsheet.svg",
                classHandler = "SmartsheetDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Smartsheet,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Smartsheet.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Teamwork connection drivers.</summary>
        /// <returns>A configuration object for Teamwork connection drivers.</returns>
        public static ConnectionDriversConfig CreateTeamworkConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Teamwork.Client",
                DriverClass = "Teamwork.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.TeamworkConnector.dll",
                ConnectionString = "SiteName={Host};ApiKey={Password};",
                iconname = "teamwork.svg",
                classHandler = "TeamworkDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Teamwork,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Teamwork.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Podio connection drivers.</summary>
        /// <returns>A configuration object for Podio connection drivers.</returns>
        public static ConnectionDriversConfig CreatePodioConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Podio.Client",
                DriverClass = "Podio.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.PodioConnector.dll",
                ConnectionString = "ClientId={UserID};ClientSecret={Password};Username={Username};UserPassword={UserPassword};",
                iconname = "podio.svg",
                classHandler = "PodioDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Podio,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Podio.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Boards connection drivers.</summary>
        /// <returns>A configuration object for Azure Boards connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureBoardsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.TeamFoundation.Client",
                DriverClass = "Microsoft.TeamFoundation.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.AzureBoardsConnector.dll",
                ConnectionString = "Organization={Host};Project={Database};PersonalAccessToken={Password};",
                iconname = "azureboards.svg",
                classHandler = "AzureBoardsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.AzureBoards,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Microsoft.TeamFoundation.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Smartsheet PM connection drivers.</summary>
        /// <returns>A configuration object for Smartsheet PM connection drivers.</returns>
        public static ConnectionDriversConfig CreateSmartsheetPMConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "SmartsheetPM.Client",
                DriverClass = "SmartsheetPM.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SmartsheetPMConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "smartsheetpm.svg",
                classHandler = "SmartsheetPMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.SmartsheetPM,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "SmartsheetPM.Client",
                NuggetMissing = false
            };
        }
    }
}