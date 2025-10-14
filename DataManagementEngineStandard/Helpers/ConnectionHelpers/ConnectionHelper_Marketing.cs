using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Marketing platform connector configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Marketing connector configurations
        /// </summary>
        /// <returns>List of Marketing connector configurations</returns>
        public static List<ConnectionDriversConfig> GetMarketingConnectorConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateMailchimpConfig(),
                CreateMarketoConfig(),
                CreateGoogleAdsConfig(),
                CreateActiveCampaignConfig(),
                CreateConstantContactConfig(),
                CreateKlaviyoConfig(),
                CreateSendinblueConfig(),
                CreateCampaignMonitorConfig(),
                CreateConvertKitConfig(),
                CreateDripConfig(),
                CreateMailerLiteConfig(),
                CreateHootsuiteMarketingConfig(),
                CreateMailgunConfig(),
                CreateSendGridConfig(),
                CreateCriteoConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Mailchimp connection drivers.</summary>
        /// <returns>A configuration object for Mailchimp connection drivers.</returns>
        public static ConnectionDriversConfig CreateMailchimpConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Mailchimp.Client",
                DriverClass = "Mailchimp.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MailchimpConnector.dll",
                ConnectionString = "ApiKey={Password};DataCenter={Host};",
                iconname = "mailchimp.svg",
                classHandler = "MailchimpDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Mailchimp,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Mailchimp.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Marketo connection drivers.</summary>
        /// <returns>A configuration object for Marketo connection drivers.</returns>
        public static ConnectionDriversConfig CreateMarketoConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Marketo.Client",
                DriverClass = "Marketo.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MarketoConnector.dll",
                ConnectionString = "BaseUrl={Url};ClientId={UserID};ClientSecret={Password};",
                iconname = "marketo.svg",
                classHandler = "MarketoDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Marketo,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Marketo.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Google Ads connection drivers.</summary>
        /// <returns>A configuration object for Google Ads connection drivers.</returns>
        public static ConnectionDriversConfig CreateGoogleAdsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Google.Ads.Client",
                DriverClass = "Google.Ads.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.GoogleAdsConnector.dll",
                ConnectionString = "DeveloperToken={Password};CustomerId={UserID};",
                iconname = "googleads.svg",
                classHandler = "GoogleAdsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.GoogleAds,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Google.Ads.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for ActiveCampaign connection drivers.</summary>
        /// <returns>A configuration object for ActiveCampaign connection drivers.</returns>
        public static ConnectionDriversConfig CreateActiveCampaignConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ActiveCampaign.Client",
                DriverClass = "ActiveCampaign.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ActiveCampaignConnector.dll",
                ConnectionString = "BaseUrl={Url};ApiKey={Password};",
                iconname = "activecampaign.svg",
                classHandler = "ActiveCampaignDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.ActiveCampaign,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ActiveCampaign.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Constant Contact connection drivers.</summary>
        /// <returns>A configuration object for Constant Contact connection drivers.</returns>
        public static ConnectionDriversConfig CreateConstantContactConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ConstantContact.Client",
                DriverClass = "ConstantContact.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ConstantContactConnector.dll",
                ConnectionString = "ApiKey={Password};AccessToken={AccessToken};",
                iconname = "constantcontact.svg",
                classHandler = "ConstantContactDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.ConstantContact,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ConstantContact.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Klaviyo connection drivers.</summary>
        /// <returns>A configuration object for Klaviyo connection drivers.</returns>
        public static ConnectionDriversConfig CreateKlaviyoConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Klaviyo.Client",
                DriverClass = "Klaviyo.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.KlaviyoConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "klaviyo.svg",
                classHandler = "KlaviyoDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Klaviyo,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Klaviyo.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Sendinblue connection drivers.</summary>
        /// <returns>A configuration object for Sendinblue connection drivers.</returns>
        public static ConnectionDriversConfig CreateSendinblueConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Sendinblue.Client",
                DriverClass = "Sendinblue.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SendinblueConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "sendinblue.svg",
                classHandler = "SendinblueDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Sendinblue,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Sendinblue.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Campaign Monitor connection drivers.</summary>
        /// <returns>A configuration object for Campaign Monitor connection drivers.</returns>
        public static ConnectionDriversConfig CreateCampaignMonitorConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "CampaignMonitor.Client",
                DriverClass = "CampaignMonitor.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.CampaignMonitorConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "campaignmonitor.svg",
                classHandler = "CampaignMonitorDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.CampaignMonitor,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "CampaignMonitor.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for ConvertKit connection drivers.</summary>
        /// <returns>A configuration object for ConvertKit connection drivers.</returns>
        public static ConnectionDriversConfig CreateConvertKitConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ConvertKit.Client",
                DriverClass = "ConvertKit.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ConvertKitConnector.dll",
                ConnectionString = "ApiKey={Password};ApiSecret={ApiSecret};",
                iconname = "convertkit.svg",
                classHandler = "ConvertKitDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.ConvertKit,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ConvertKit.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Drip connection drivers.</summary>
        /// <returns>A configuration object for Drip connection drivers.</returns>
        public static ConnectionDriversConfig CreateDripConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Drip.Client",
                DriverClass = "Drip.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.DripConnector.dll",
                ConnectionString = "ApiToken={Password};AccountId={UserID};",
                iconname = "drip.svg",
                classHandler = "DripDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Drip,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Drip.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for MailerLite connection drivers.</summary>
        /// <returns>A configuration object for MailerLite connection drivers.</returns>
        public static ConnectionDriversConfig CreateMailerLiteConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "MailerLite.Client",
                DriverClass = "MailerLite.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MailerLiteConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "mailerlite.svg",
                classHandler = "MailerLiteDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.MailerLite,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "MailerLite.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Hootsuite Marketing connection drivers.</summary>
        /// <returns>A configuration object for Hootsuite Marketing connection drivers.</returns>
        public static ConnectionDriversConfig CreateHootsuiteMarketingConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Hootsuite.Marketing.Client",
                DriverClass = "Hootsuite.Marketing.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.HootsuiteMarketingConnector.dll",
                ConnectionString = "AccessToken={Password};",
                iconname = "hootsuitemarketing.svg",
                classHandler = "HootsuiteMarketingDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.HootsuiteMarketing,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Hootsuite.Marketing.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Mailgun connection drivers.</summary>
        /// <returns>A configuration object for Mailgun connection drivers.</returns>
        public static ConnectionDriversConfig CreateMailgunConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Mailgun.Client",
                DriverClass = "Mailgun.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MailgunConnector.dll",
                ConnectionString = "ApiKey={Password};Domain={Host};",
                iconname = "mailgun.svg",
                classHandler = "MailgunDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Mailgun,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Mailgun.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for SendGrid connection drivers.</summary>
        /// <returns>A configuration object for SendGrid connection drivers.</returns>
        public static ConnectionDriversConfig CreateSendGridConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "SendGrid.Client",
                DriverClass = "SendGrid.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SendGridConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "sendgrid.svg",
                classHandler = "SendGridDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.SendGrid,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "SendGrid.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Criteo connection drivers.</summary>
        /// <returns>A configuration object for Criteo connection drivers.</returns>
        public static ConnectionDriversConfig CreateCriteoConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Criteo.Client",
                DriverClass = "Criteo.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.CriteoConnector.dll",
                ConnectionString = "ClientId={UserID};ClientSecret={Password};",
                iconname = "criteo.svg",
                classHandler = "CriteoDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Criteo,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Criteo.Client",
                NuggetMissing = false
            };
        }
    }
}