using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for E-commerce platform connector configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all E-commerce connector configurations
        /// </summary>
        /// <returns>List of E-commerce connector configurations</returns>
        public static List<ConnectionDriversConfig> GetECommerceConnectorConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateShopifyConfig(),
                CreateWooCommerceConfig(),
                CreateMagentoConfig(),
                CreateBigCommerceConfig(),
                CreateSquarespaceConfig(),
                CreateWixConfig(),
                CreateEtsyConfig(),
                CreateOpenCartConfig(),
                CreateEcwidConfig(),
                CreateVolusionConfig(),
                CreatePrestaShopConfig(),
                CreateBigCartelConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Shopify connection drivers.</summary>
        /// <returns>A configuration object for Shopify connection drivers.</returns>
        public static ConnectionDriversConfig CreateShopifyConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Shopify.Client",
                DriverClass = "Shopify.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ShopifyConnector.dll",
                ConnectionString = "ShopDomain={Host};AccessToken={Password};ApiKey={UserID};",
                iconname = "shopify.svg",
                classHandler = "ShopifyDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Shopify,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for WooCommerce connection drivers.</summary>
        /// <returns>A configuration object for WooCommerce connection drivers.</returns>
        public static ConnectionDriversConfig CreateWooCommerceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "WooCommerce.Client",
                DriverClass = "WooCommerce.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.WooCommerceConnector.dll",
                ConnectionString = "SiteUrl={Url};ConsumerKey={UserID};ConsumerSecret={Password};",
                iconname = "woocommerce.svg",
                classHandler = "WooCommerceDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.WooCommerce,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Magento connection drivers.</summary>
        /// <returns>A configuration object for Magento connection drivers.</returns>
        public static ConnectionDriversConfig CreateMagentoConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Magento.Client",
                DriverClass = "Magento.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MagentoConnector.dll",
                ConnectionString = "BaseUrl={Url};AccessToken={Password};Username={UserID};",
                iconname = "magento.svg",
                classHandler = "MagentoDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Magento,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for BigCommerce connection drivers.</summary>
        /// <returns>A configuration object for BigCommerce connection drivers.</returns>
        public static ConnectionDriversConfig CreateBigCommerceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "BigCommerce.Client",
                DriverClass = "BigCommerce.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.BigCommerceConnector.dll",
                ConnectionString = "StoreHash={Host};AccessToken={Password};ClientId={UserID};",
                iconname = "bigcommerce.svg",
                classHandler = "BigCommerceDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.BigCommerce,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Squarespace connection drivers.</summary>
        /// <returns>A configuration object for Squarespace connection drivers.</returns>
        public static ConnectionDriversConfig CreateSquarespaceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Squarespace.Client",
                DriverClass = "Squarespace.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.SquarespaceConnector.dll",
                ConnectionString = "SiteUrl={Url};ApiKey={Password};",
                iconname = "squarespace.svg",
                classHandler = "SquarespaceDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Squarespace,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Wix connection drivers.</summary>
        /// <returns>A configuration object for Wix connection drivers.</returns>
        public static ConnectionDriversConfig CreateWixConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Wix.Client",
                DriverClass = "Wix.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.WixConnector.dll",
                ConnectionString = "SiteId={Host};RefreshToken={Password};",
                iconname = "wix.svg",
                classHandler = "WixDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Wix,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Etsy connection drivers.</summary>
        /// <returns>A configuration object for Etsy connection drivers.</returns>
        public static ConnectionDriversConfig CreateEtsyConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Etsy.Client",
                DriverClass = "Etsy.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.EtsyConnector.dll",
                ConnectionString = "ApiKey={Password};",
                iconname = "etsy.svg",
                classHandler = "EtsyDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Etsy,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for OpenCart connection drivers.</summary>
        /// <returns>A configuration object for OpenCart connection drivers.</returns>
        public static ConnectionDriversConfig CreateOpenCartConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "OpenCart.Client",
                DriverClass = "OpenCart.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.OpenCartConnector.dll",
                ConnectionString = "StoreUrl={Url};Username={UserID};ApiKey={Password};",
                iconname = "opencart.svg",
                classHandler = "OpenCartDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.OpenCart,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Ecwid connection drivers.</summary>
        /// <returns>A configuration object for Ecwid connection drivers.</returns>
        public static ConnectionDriversConfig CreateEcwidConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Ecwid.Client",
                DriverClass = "Ecwid.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.EcwidConnector.dll",
                ConnectionString = "StoreId={Host};AccessToken={Password};",
                iconname = "ecwid.svg",
                classHandler = "EcwidDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Ecwid,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Volusion connection drivers.</summary>
        /// <returns>A configuration object for Volusion connection drivers.</returns>
        public static ConnectionDriversConfig CreateVolusionConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Volusion.Client",
                DriverClass = "Volusion.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.VolusionConnector.dll",
                ConnectionString = "StoreUrl={Url};Username={UserID};EncryptedPassword={Password};",
                iconname = "volusion.svg",
                classHandler = "VolusionDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.Volusion,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for PrestaShop connection drivers.</summary>
        /// <returns>A configuration object for PrestaShop connection drivers.</returns>
        public static ConnectionDriversConfig CreatePrestaShopConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "PrestaShop.Client",
                DriverClass = "PrestaShop.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.PrestaShopConnector.dll",
                ConnectionString = "ShopUrl={Url};WebserviceKey={Password};",
                iconname = "prestashop.svg",
                classHandler = "PrestaShopDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.PrestaShop,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Big Cartel connection drivers.</summary>
        /// <returns>A configuration object for Big Cartel connection drivers.</returns>
        public static ConnectionDriversConfig CreateBigCartelConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "BigCartel.Client",
                DriverClass = "BigCartel.Client",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.BigCartelConnector.dll",
                ConnectionString = "SubDomain={Host};AccessToken={Password};",
                iconname = "bigcartel.svg",
                classHandler = "BigCartelDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.BigCartel,
                IsMissing = false
            };
        }
    }
}