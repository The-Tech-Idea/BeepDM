using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Beep Proxy / Cluster connection configurations.
    /// These entries let the engine discover and present proxy-cluster
    /// data sources in the same driver catalogue as every other backend.
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Returns <see cref="ConnectionDriversConfig"/> descriptors for the
        /// two built-in proxy data-source types:
        /// <list type="bullet">
        ///   <item><term>BeepProxyNode</term>  — a single remote node reached over HTTP.</item>
        ///   <item><term>BeepProxyCluster</term> — a load-balanced cluster of proxy nodes.</item>
        /// </list>
        /// </summary>
        public static List<ConnectionDriversConfig> GetProxyConfigs()
        {
            return new List<ConnectionDriversConfig>
            {
                CreateBeepProxyNodeConfig(),
                CreateBeepProxyClusterConfig()
            };
        }

        /// <summary>
        /// Driver descriptor for a single Beep remote proxy node
        /// (<see cref="DataSourceType.BeepProxyNode"/>).
        /// </summary>
        /// <remarks>
        /// <para>Expected <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionProperties"/> fields:</para>
        /// <list type="table">
        ///   <item><term>Url</term>          <description>Base URL of the remote node, e.g. <c>http://worker-a:5100</c></description></item>
        ///   <item><term>ApiKey</term>        <description>Bearer / X-Proxy-Api-Key header value</description></item>
        ///   <item><term>Timeout</term>       <description>Request timeout in seconds (default 30)</description></item>
        ///   <item><term>IsRemote</term>      <description><c>true</c></description></item>
        ///   <item><term>ParameterList["ClusterName"]</term>  <description>Owning cluster name</description></item>
        ///   <item><term>ParameterList["NodeRole"]</term>     <description>Primary | Replica | Standby</description></item>
        ///   <item><term>ParameterList["Weight"]</term>       <description>Routing weight (int, default 1)</description></item>
        ///   <item><term>ParameterList["HmacSecret"]</term>   <description>HMAC-SHA256 signing key (separate from ApiKey)</description></item>
        /// </list>
        /// </remarks>
        public static ConnectionDriversConfig CreateBeepProxyNodeConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID               = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                PackageName          = "BeepProxyNode",
                DriverClass          = "RemoteProxyDataSource",
                classHandler         = "RemoteProxyDataSource",
                version              = "1.0.0",
                dllname              = string.Empty,          // built-in, no DLL path
                iconname             = "proxy-node.svg",
                ADOType              = false,
                CreateLocal          = false,
                InMemory             = false,
                NeedDrivers          = false,
                IsMissing            = false,
                NuggetMissing        = false,
                DatasourceCategory   = DatasourceCategory.Proxy,
                DatasourceType       = DataSourceType.BeepProxyNode,
                ConnectionString     = "Url=http://localhost:5100;ApiKey=;HmacSecret=",
                parameter1           = "Url",
                parameter2           = "ApiKey",
                parameter3           = "HmacSecret"
            };
        }

        /// <summary>
        /// Driver descriptor for a Beep proxy cluster
        /// (<see cref="DataSourceType.BeepProxyCluster"/>).
        /// </summary>
        /// <remarks>
        /// Clusters are composed of one or more <see cref="DataSourceType.BeepProxyNode"/>
        /// entries that all share the same <c>ParameterList["ClusterName"]</c>.
        /// The cluster itself is identified by its <c>ConnectionName</c>.
        /// </remarks>
        public static ConnectionDriversConfig CreateBeepProxyClusterConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID               = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
                PackageName          = "BeepProxyCluster",
                DriverClass          = "ProxyCluster",
                classHandler         = "ProxyCluster",
                version              = "1.0.0",
                dllname              = string.Empty,
                iconname             = "proxy-cluster.svg",
                ADOType              = false,
                CreateLocal          = false,
                InMemory             = false,
                NeedDrivers          = false,
                IsMissing            = false,
                NuggetMissing        = false,
                DatasourceCategory   = DatasourceCategory.Proxy,
                DatasourceType       = DataSourceType.BeepProxyCluster,
                ConnectionString     = "ClusterName=;Policy=RoundRobin",
                parameter1           = "ClusterName",
                parameter2           = "Policy",
                parameter3           = string.Empty
            };
        }
    }
}
