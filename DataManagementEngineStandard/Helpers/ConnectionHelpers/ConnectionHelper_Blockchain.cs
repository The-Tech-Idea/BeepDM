using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Blockchain and distributed ledger technology connector configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Blockchain connector configurations
        /// </summary>
        /// <returns>List of Blockchain connector configurations</returns>
        public static List<ConnectionDriversConfig> GetBlockchainConnectorConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateEthereumConfig(),
                CreateHyperledgerConfig(),
                CreateBitcoinCoreConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Ethereum connection drivers.</summary>
        /// <returns>A configuration object for Ethereum connection drivers.</returns>
        public static ConnectionDriversConfig CreateEthereumConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Nethereum",
                DriverClass = "Nethereum.Web3",
                version = "4.0.0.0",
                dllname = "TheTechIdea.Beep.EthereumConnector.dll",
                ConnectionString = "RpcUrl={Url};PrivateKey={Password};ChainId={ChainId};",
                iconname = "ethereum.svg",
                classHandler = "EthereumDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.Blockchain,
                DatasourceType = DataSourceType.Ethereum,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Hyperledger Fabric connection drivers.</summary>
        /// <returns>A configuration object for Hyperledger Fabric connection drivers.</returns>
        public static ConnectionDriversConfig CreateHyperledgerConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Hyperledger.Fabric.SDK",
                DriverClass = "Hyperledger.Fabric.SDK",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.HyperledgerConnector.dll",
                ConnectionString = "NetworkUrl={Url};MspId={UserID};PrivateKey={Password};Certificate={Certificate};",
                iconname = "hyperledger.svg",
                classHandler = "HyperledgerDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Blockchain,
                DatasourceType = DataSourceType.Hyperledger,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Bitcoin Core connection drivers.</summary>
        /// <returns>A configuration object for Bitcoin Core connection drivers.</returns>
        public static ConnectionDriversConfig CreateBitcoinCoreConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "NBitcoin",
                DriverClass = "NBitcoin.RPC.RPCClient",
                version = "7.0.0.0",
                dllname = "TheTechIdea.Beep.BitcoinCoreConnector.dll",  
                ConnectionString = "RpcUrl={Url};Username={UserID};Password={Password};Network={Network};",
                iconname = "bitcoin.svg",
                classHandler = "BitcoinCoreDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Blockchain,
                DatasourceType = DataSourceType.BitcoinCore,
                IsMissing = false
            };
        }
    }
}