using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Communication platform connector configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Communication connector configurations
        /// </summary>
        /// <returns>List of Communication connector configurations</returns>
        public static List<ConnectionDriversConfig> GetCommunicationConnectorConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateSlackConfig(),
                CreateMicrosoftTeamsConfig(),
                CreateZoomConfig(),
                CreateGoogleChatConfig(),
                CreateDiscordConfig(),
                CreateTelegramConfig(),
                CreateWhatsAppBusinessConfig(),
                CreateTwistConfig(),
                CreateChantyConfig(),
                CreateRocketChatConfig(),
                CreateFlockConfig(),
                CreateMattermostConfig(),
                CreateRocketChatCommConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Slack connection drivers.</summary>
        /// <returns>A configuration object for Slack connection drivers.</returns>
        public static ConnectionDriversConfig CreateSlackConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Slack.Webhooks",
                DriverClass = "Slack.Webhooks",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SlackConnector.dll",
                ConnectionString = "BotToken={Password};AppToken={AppToken};",
                iconname = "slack.svg",
                classHandler = "SlackDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Slack,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Slack.Webhooks",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Microsoft Teams connection drivers.</summary>
        /// <returns>A configuration object for Microsoft Teams connection drivers.</returns>
        public static ConnectionDriversConfig CreateMicrosoftTeamsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.Graph",
                DriverClass = "Microsoft.Graph",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MicrosoftTeamsConnector.dll",
                ConnectionString = "TenantId={TenantId};ClientId={UserID};ClientSecret={Password};",
                iconname = "microsoftteams.svg",
                classHandler = "MicrosoftTeamsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.MicrosoftTeams,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Microsoft.Graph",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Zoom connection drivers.</summary>
        /// <returns>A configuration object for Zoom connection drivers.</returns>
        public static ConnectionDriversConfig CreateZoomConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Zoom.Net",
                DriverClass = "Zoom.Net",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ZoomConnector.dll",
                ConnectionString = "ApiKey={UserID};ApiSecret={Password};",
                iconname = "zoom.svg",
                classHandler = "ZoomDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Zoom,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Zoom.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Google Chat connection drivers.</summary>
        /// <returns>A configuration object for Google Chat connection drivers.</returns>
        public static ConnectionDriversConfig CreateGoogleChatConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Google.Apis.HangoutsChat",
                DriverClass = "Google.Apis.HangoutsChat",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.GoogleChatConnector.dll",
                ConnectionString = "ServiceAccountJson={ServiceAccountJson};",
                iconname = "googlechat.svg",
                classHandler = "GoogleChatDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.GoogleChat,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Google.Apis.HangoutsChat",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Discord connection drivers.</summary>
        /// <returns>A configuration object for Discord connection drivers.</returns>
        public static ConnectionDriversConfig CreateDiscordConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Discord.Net",
                DriverClass = "Discord.Net",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.DiscordConnector.dll",
                ConnectionString = "BotToken={Password};",
                iconname = "discord.svg",
                classHandler = "DiscordDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Discord,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Discord.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Telegram connection drivers.</summary>
        /// <returns>A configuration object for Telegram connection drivers.</returns>
        public static ConnectionDriversConfig CreateTelegramConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Telegram.Bot",
                DriverClass = "Telegram.Bot",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.TelegramConnector.dll",
                ConnectionString = "BotToken={Password};",
                iconname = "telegram.svg",
                classHandler = "TelegramDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Telegram,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Telegram.Bot",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for WhatsApp Business connection drivers.</summary>
        /// <returns>A configuration object for WhatsApp Business connection drivers.</returns>
        public static ConnectionDriversConfig CreateWhatsAppBusinessConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "WhatsApp.Business.Client",
                DriverClass = "WhatsApp.Business.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.WhatsAppBusinessConnector.dll",
                ConnectionString = "AccessToken={Password};PhoneNumberId={PhoneNumberId};",
                iconname = "whatsappbusiness.svg",
                classHandler = "WhatsAppBusinessDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.WhatsAppBusiness,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "WhatsApp.Business.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Twist connection drivers.</summary>
        /// <returns>A configuration object for Twist connection drivers.</returns>
        public static ConnectionDriversConfig CreateTwistConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Twist.Client",
                DriverClass = "Twist.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.TwistConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "twist.svg",
                classHandler = "TwistDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Twist,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Twist.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Chanty connection drivers.</summary>
        /// <returns>A configuration object for Chanty connection drivers.</returns>
        public static ConnectionDriversConfig CreateChantyConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Chanty.Client",
                DriverClass = "Chanty.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ChantyConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "chanty.svg",
                classHandler = "ChantyDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Chanty,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Chanty.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Rocket.Chat connection drivers.</summary>
        /// <returns>A configuration object for Rocket.Chat connection drivers.</returns>
        public static ConnectionDriversConfig CreateRocketChatConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Rocket.Chat.Net",
                DriverClass = "Rocket.Chat.Net",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.RocketChatConnector.dll",
                ConnectionString = "ServerUrl={Url};Username={UserID};Password={Password};",
                iconname = "rocketchat.svg",
                classHandler = "RocketChatDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.RocketChat,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Rocket.Chat.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Flock connection drivers.</summary>
        /// <returns>A configuration object for Flock connection drivers.</returns>
        public static ConnectionDriversConfig CreateFlockConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Flock.Client",
                DriverClass = "Flock.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.FlockConnector.dll",
                ConnectionString = "Token={Password};",
                iconname = "flock.svg",
                classHandler = "FlockDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Flock,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Flock.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Mattermost connection drivers.</summary>
        /// <returns>A configuration object for Mattermost connection drivers.</returns>
        public static ConnectionDriversConfig CreateMattermostConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Mattermost.Client",
                DriverClass = "Mattermost.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MattermostConnector.dll",
                ConnectionString = "ServerUrl={Url};AccessToken={Password};",
                iconname = "mattermost.svg",
                classHandler = "MattermostDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Mattermost,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Mattermost.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Rocket.Chat Communication connection drivers.</summary>
        /// <returns>A configuration object for Rocket.Chat Communication connection drivers.</returns>
        public static ConnectionDriversConfig CreateRocketChatCommConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RocketChatComm.Client",
                DriverClass = "RocketChatComm.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.RocketChatCommConnector.dll",
                ConnectionString = "ServerUrl={Url};AccessToken={Password};",
                iconname = "rocketchatcomm.svg",
                classHandler = "RocketChatCommDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.RocketChatComm,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "RocketChatComm.Client",
                NuggetMissing = false
            };
        }
    }
}