using System;
using System.Linq;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Base helper for fluent data-source driver registration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per-datasource extension methods (<c>AddOracleDatabase</c>, <c>AddSqliteDatabase</c>,
    /// <c>AddCsvFiles</c>, ...) live in each <c>IDataSource</c> implementation's own project
    /// (under <c>BeepDataSources/DataSourcesPluginsCore/&lt;Name&gt;DataSourceCore/</c>) and
    /// delegate to <see cref="AddDataSourceDriver(IBeepService, ConnectionDriversConfig, bool)"/>.
    /// </para>
    /// <para>
    /// That way a fluent method only appears in IntelliSense when the developer has referenced
    /// the matching datasource project / NuGet - a true opt-in surface.
    /// </para>
    /// <para>
    /// The driver config is appended to <see cref="IConfigEditor.DataDriversClasses"/>, deduped
    /// by <see cref="ConnectionDriversConfig.classHandler"/> (case-insensitive). That is the same
    /// field <c>DMEEditor.CreateNewDataSourceConnection</c> uses to resolve the
    /// <see cref="IDataSource"/> implementation class at runtime, so deduping on it keeps the
    /// config list consistent with how drivers are looked up.
    /// </para>
    /// </remarks>
    public static class BeepServiceDataSourceExtensions
    {
        /// <summary>
        /// Adds a data source driver configuration to
        /// <see cref="IConfigEditor.DataDriversClasses"/>. No-op when a config with the same
        /// <see cref="ConnectionDriversConfig.classHandler"/> is already present.
        /// </summary>
        /// <param name="beep">The Beep service whose <see cref="IBeepService.Config_editor"/> holds the driver list.</param>
        /// <param name="driverConfig">The driver config to register (typically from <c>ConnectionHelper.Create&lt;X&gt;Config()</c>).</param>
        /// <param name="skipIfPresent">
        /// When <c>true</c> (default), the config is skipped if its <see cref="ConnectionDriversConfig.classHandler"/>
        /// already exists in the list (case-insensitive match). Set <c>false</c> to force-append a duplicate.
        /// </param>
        /// <returns>The <paramref name="beep"/> instance for chaining.</returns>
        public static IBeepService AddDataSourceDriver(
            this IBeepService beep,
            ConnectionDriversConfig driverConfig,
            bool skipIfPresent = true)
        {
            if (beep?.Config_editor == null || driverConfig == null)
                return beep;

            var list = beep.Config_editor.DataDriversClasses;
            if (list == null)
            {
                list = new System.Collections.Generic.List<ConnectionDriversConfig>();
                beep.Config_editor.DataDriversClasses = list;
            }

            if (skipIfPresent &&
                list.Any(c => string.Equals(c.classHandler, driverConfig.classHandler,
                                            StringComparison.OrdinalIgnoreCase)))
            {
                return beep;
            }

            list.Add(driverConfig);
            return beep;
        }

        /// <summary>
        /// Factory overload - pass the <c>Create&lt;X&gt;Config</c> method group directly.
        /// Resolves the config lazily and forwards to
        /// <see cref="AddDataSourceDriver(IBeepService, ConnectionDriversConfig, bool)"/>.
        /// </summary>
        /// <example>
        /// <code>beep.AddDataSourceDriver(ConnectionHelper.CreateOracleConfig);</code>
        /// </example>
        public static IBeepService AddDataSourceDriver(
            this IBeepService beep,
            Func<ConnectionDriversConfig> configFactory,
            bool skipIfPresent = true)
        {
            if (configFactory == null)
                return beep;

            return beep.AddDataSourceDriver(configFactory(), skipIfPresent);
        }
    }
}
