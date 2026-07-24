using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// DI registration for app self-update (decision D12: the capability lives in BeepDM). Any
    /// Beep-based app calls <c>AddBeepAppUpdates()</c> alongside <c>AddBeepForDesktop()</c> and
    /// resolves <see cref="IAppUpdateService"/> — no dependency on Beep.Installer.
    /// </summary>
    public static class UpdateServiceExtensions
    {
        public const string DefaultSettingsFileName = "update-settings.json";

        /// <summary>Registers the update service with explicit settings.</summary>
        public static IServiceCollection AddBeepAppUpdates(this IServiceCollection services, UpdateSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            services.TryAddSingleton(settings);
            services.TryAddSingleton<IFeedTransport, HttpFeedTransport>();
            services.TryAddSingleton(sp => new UpdateFeedClient(sp.GetService<IFeedTransport>()));
            services.TryAddSingleton<IAppUpdateService>(sp =>
                new AppUpdateService(sp.GetRequiredService<UpdateSettings>(), sp.GetService<UpdateFeedClient>()));
            return services;
        }

        /// <summary>
        /// Registers the update service, loading settings from <c>update-settings.json</c> beside
        /// the app (or <paramref name="settingsPath"/> when given). A missing file yields defaults
        /// — the app still resolves the service, it simply has nothing to check until provisioned.
        /// </summary>
        public static IServiceCollection AddBeepAppUpdates(this IServiceCollection services, string? settingsPath = null)
            => services.AddBeepAppUpdates(LoadSettings(settingsPath));

        /// <summary>
        /// Loads <c>update-settings.json</c> (or defaults), applies the <c>BEEP_NO_UPDATE</c>
        /// opt-out, and fills in the running assembly's version when the file omits it.
        /// </summary>
        public static UpdateSettings LoadSettings(string? settingsPath = null)
        {
            var path = settingsPath ?? Path.Combine(AppContext.BaseDirectory, DefaultSettingsFileName);

            UpdateSettings settings;
            try
            {
                settings = File.Exists(path)
                    ? JsonSerializer.Deserialize<UpdateSettings>(File.ReadAllText(path),
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                        }) ?? new UpdateSettings()
                    : new UpdateSettings();
            }
            catch
            {
                // A malformed settings file must not stop the app from starting; fall back to
                // defaults (disabled by absence of a feed URL) rather than throwing at composition.
                settings = new UpdateSettings();
            }

            if (IsTruthy(Environment.GetEnvironmentVariable("BEEP_NO_UPDATE")))
                settings.Disabled = true;

            if (string.IsNullOrWhiteSpace(settings.CurrentVersion))
                settings.CurrentVersion = ResolveEntryVersion();

            return settings;
        }

        private static string ResolveEntryVersion()
            => (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
               .GetName().Version?.ToString() ?? "0.0.0";

        private static bool IsTruthy(string? v)
            => !string.IsNullOrWhiteSpace(v)
               && (v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || v.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
