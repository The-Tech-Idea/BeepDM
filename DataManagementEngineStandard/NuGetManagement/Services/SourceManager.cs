using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    /// <summary>
    /// Manages NuGet package sources including NuGet.org, local directories, and custom feeds.
    /// Persists source configuration to disk.
    /// </summary>
    public class SourceManager
    {
        private readonly IDMLogger _logger;
        private readonly string _configPath;
        private readonly List<NuGetSourceConfig> _sources;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the SourceManager.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="configDirectory">Optional directory for source configuration file. Defaults to application base directory.</param>
        public SourceManager(IDMLogger logger, string configDirectory = null)
        {
            _logger = logger;
            _configPath = Path.Combine(configDirectory ?? AppContext.BaseDirectory, "nuget_sources.json");
            _sources = LoadSources();
            
            // Ensure default nuget.org source exists
            if (!_sources.Any(s => s.Url.Contains("nuget.org")))
            {
                _sources.Add(new NuGetSourceConfig
                {
                    Name = "nuget.org",
                    Url = "https://api.nuget.org/v3/index.json",
                    IsEnabled = true,
                    Priority = 1,
                    IsLocal = false
                });
                SaveSources();
            }
        }

        /// <summary>
        /// Gets all configured sources (enabled and disabled).
        /// </summary>
        /// <returns>List of all source configurations.</returns>
        public List<NuGetSourceConfig> GetSources()
        {
            lock (_lock)
            {
                return _sources.ToList();
            }
        }

        /// <summary>
        /// Gets only the enabled (active) sources sorted by priority.
        /// </summary>
        /// <returns>List of active source configurations.</returns>
        public List<NuGetSourceConfig> GetActiveSources()
        {
            lock (_lock)
            {
                return _sources.Where(s => s.IsEnabled).OrderBy(s => s.Priority).ToList();
            }
        }

        /// <summary>
        /// Gets the URLs of all active sources ordered by priority.
        /// </summary>
        /// <returns>List of active source URLs.</returns>
        public List<string> GetActiveSourceUrls()
        {
            lock (_lock)
            {
                return _sources.Where(s => s.IsEnabled).OrderBy(s => s.Priority).Select(s => s.Url).ToList();
            }
        }

        /// <summary>
        /// Adds a new source or updates an existing one with the same name.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <param name="url">The source URL or local path.</param>
        /// <param name="isEnabled">Whether the source is initially enabled.</param>
        /// <param name="username">Optional username for authenticated feeds.</param>
        /// <param name="password">Optional password for authenticated feeds.</param>
        /// <param name="apiKey">Optional API key for publishing.</param>
        /// <exception cref="ArgumentNullException">Thrown when name or url is null or empty.</exception>
        public void AddSource(string name, string url, bool isEnabled = true, string username = null, string password = null, string apiKey = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            lock (_lock)
            {
                var existing = _sources.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Url = url;
                    existing.IsEnabled = isEnabled;
                    existing.Username = username;
                    existing.Password = password;
                    existing.ApiKey = apiKey;
                    _logger?.LogWithContext($"Updated NuGet source: {name}", null);
                }
                else
                {
                    var isLocal = !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                  !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                    
                    _sources.Add(new NuGetSourceConfig
                    {
                        Name = name,
                        Url = url,
                        IsEnabled = isEnabled,
                        IsLocal = isLocal,
                        Priority = _sources.Count > 0 ? _sources.Max(s => s.Priority) + 1 : 1,
                        Username = username,
                        Password = password,
                        ApiKey = apiKey,
                        DateAdded = DateTime.UtcNow
                    });
                    _logger?.LogWithContext($"Added NuGet source: {name} ({url})", null);
                }
                
                SaveSources();
            }
        }

        /// <summary>
        /// Removes a source by name.
        /// </summary>
        /// <param name="name">The source name to remove.</param>
        public void RemoveSource(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            lock (_lock)
            {
                var removed = _sources.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                {
                    _logger?.LogWithContext($"Removed NuGet source: {name}", null);
                    SaveSources();
                }
            }
        }

        /// <summary>
        /// Enables a previously disabled source.
        /// </summary>
        /// <param name="name">The source name.</param>
        public void EnableSource(string name)
        {
            SetSourceEnabled(name, true);
        }

        /// <summary>
        /// Disables a source without removing it.
        /// </summary>
        /// <param name="name">The source name.</param>
        public void DisableSource(string name)
        {
            SetSourceEnabled(name, false);
        }

        /// <summary>
        /// Sets the enabled state of a source.
        /// </summary>
        private void SetSourceEnabled(string name, bool enabled)
        {
            lock (_lock)
            {
                var source = _sources.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (source != null && source.IsEnabled != enabled)
                {
                    source.IsEnabled = enabled;
                    _logger?.LogWithContext($"{(enabled ? "Enabled" : "Disabled")} NuGet source: {name}", null);
                    SaveSources();
                }
            }
        }

        /// <summary>
        /// Sets the priority for source resolution ordering.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <param name="priority">Lower numbers have higher priority.</param>
        public void SetSourcePriority(string name, int priority)
        {
            lock (_lock)
            {
                var source = _sources.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (source != null)
                {
                    source.Priority = priority;
                    SaveSources();
                }
            }
        }

        /// <summary>
        /// Tests if a source is accessible and healthy.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <returns>True if the source is healthy; otherwise, false.</returns>
        public async Task<bool> TestSourceAsync(string name)
        {
            var source = GetSources().FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (source == null) return false;

            try
            {
                if (source.IsLocal)
                {
                    var isHealthy = Directory.Exists(source.Url);
                    source.IsHealthy = isHealthy;
                    source.LastChecked = DateTime.UtcNow;
                    return isHealthy;
                }
                else
                {
                    // For HTTP sources, try to access the feed
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        var response = await client.GetAsync(source.Url);
                        var isHealthy = response.IsSuccessStatusCode;
                        source.IsHealthy = isHealthy;
                        source.LastChecked = DateTime.UtcNow;
                        return isHealthy;
                    }
                }
            }
            catch (Exception ex)
            {
                source.IsHealthy = false;
                source.LastChecked = DateTime.UtcNow;
                _logger?.LogWithContext($"Source health check failed for {name}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task TestAllSourcesAsync()
        {
            var sources = GetSources();
            foreach (var source in sources)
            {
                await TestSourceAsync(source.Name);
            }
        }

        private List<NuGetSourceConfig> LoadSources()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    return JsonConvert.DeserializeObject<List<NuGetSourceConfig>>(json) ?? new List<NuGetSourceConfig>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error loading NuGet sources: {ex.Message}", ex);
            }
            return new List<NuGetSourceConfig>();
        }

        private void SaveSources()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_sources, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error saving NuGet sources: {ex.Message}", ex);
            }
        }
    }
}
