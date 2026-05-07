using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - NuGet Source Management
    /// Manages custom NuGet package sources with persistence.
    /// </summary>
    public partial class AssemblyHandler
    {
        #region NuGet Source Fields

        private List<NuGetSourceConfig> _nugetSources;
        private string _nugetSourcesFilePath;
        private readonly object _sourcesLock = new object();

        private const string DefaultNuGetSource = "https://api.nuget.org/v3/index.json";
        private const string NuGetSourcesFileName = "nuget_sources.json";

        #endregion

        #region NuGet Source Management

        /// <summary>
        /// Gets all configured NuGet sources.
        /// </summary>
        public List<NuGetSourceConfig> GetNuGetSources()
        {
            EnsureNuGetSourcesLoaded();
            lock (_sourcesLock)
            {
                return _nugetSources.ToList();
            }
        }

        /// <summary>
        /// Adds a new NuGet source.
        /// </summary>
        public void AddNuGetSource(string name, string url, bool isEnabled = true)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));

            EnsureNuGetSourcesLoaded();
            lock (_sourcesLock)
            {
                if (_nugetSources.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    Logger?.WriteLog($"AddNuGetSource: Source '{name}' already exists, updating URL");
                    var existing = _nugetSources.First(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    existing.Url = url;
                    existing.IsEnabled = isEnabled;
                }
                else
                {
                    _nugetSources.Add(new NuGetSourceConfig
                    {
                        Name = name,
                        Url = url,
                        IsEnabled = isEnabled,
                        DateAdded = DateTime.UtcNow
                    });
                }
            }
            SaveNuGetSources();
        }

        /// <summary>
        /// Removes a NuGet source by name.
        /// </summary>
        public void RemoveNuGetSource(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            EnsureNuGetSourcesLoaded();
            lock (_sourcesLock)
            {
                _nugetSources.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
            SaveNuGetSources();
        }

        /// <summary>
        /// Enables a NuGet source.
        /// </summary>
        public void EnableNuGetSource(string name)
        {
            SetNuGetSourceEnabled(name, true);
        }

        /// <summary>
        /// Disables a NuGet source.
        /// </summary>
        public void DisableNuGetSource(string name)
        {
            SetNuGetSourceEnabled(name, false);
        }

        /// <summary>
        /// Gets the list of active (enabled) source URLs for NuGet operations.
        /// </summary>
        public List<string> GetActiveSourceUrls()
        {
            EnsureNuGetSourcesLoaded();
            lock (_sourcesLock)
            {
                return _nugetSources
                    .Where(s => s.IsEnabled)
                    .Select(s => s.Url)
                    .ToList();
            }
        }

        #endregion

        #region NuGet Source Persistence

        private void SaveNuGetSources()
        {
            try
            {
                EnsureNuGetSourcesFilePath();
                lock (_sourcesLock)
                {
                    var json = JsonConvert.SerializeObject(_nugetSources, Formatting.Indented);
                    File.WriteAllText(_nugetSourcesFilePath, json);
                }
                Logger?.WriteLog($"SaveNuGetSources: Saved {_nugetSources.Count} sources to {_nugetSourcesFilePath}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"SaveNuGetSources: Error - {ex.Message}");
            }
        }

        private void LoadNuGetSources()
        {
            try
            {
                EnsureNuGetSourcesFilePath();
                if (File.Exists(_nugetSourcesFilePath))
                {
                    var json = File.ReadAllText(_nugetSourcesFilePath);
                    _nugetSources = JsonConvert.DeserializeObject<List<NuGetSourceConfig>>(json) ?? new List<NuGetSourceConfig>();
                }
                else
                {
                    _nugetSources = new List<NuGetSourceConfig>();
                }

                // Ensure default source always exists
                if (!_nugetSources.Any(s => s.Url.Equals(DefaultNuGetSource, StringComparison.OrdinalIgnoreCase)))
                {
                    _nugetSources.Insert(0, new NuGetSourceConfig
                    {
                        Name = "nuget.org",
                        Url = DefaultNuGetSource,
                        IsEnabled = true,
                        DateAdded = DateTime.UtcNow
                    });
                }

                Logger?.WriteLog($"LoadNuGetSources: Loaded {_nugetSources.Count} sources");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadNuGetSources: Error - {ex.Message}");
                _nugetSources = new List<NuGetSourceConfig>
                {
                    new NuGetSourceConfig { Name = "nuget.org", Url = DefaultNuGetSource, IsEnabled = true, DateAdded = DateTime.UtcNow }
                };
            }
        }

        private void EnsureNuGetSourcesLoaded()
        {
            if (_nugetSources == null)
            {
                LoadNuGetSources();
            }
        }

        private void EnsureNuGetSourcesFilePath()
        {
            if (string.IsNullOrEmpty(_nugetSourcesFilePath))
            {
                var configPath = ConfigEditor?.ExePath ?? AppContext.BaseDirectory;
                _nugetSourcesFilePath = Path.Combine(configPath, NuGetSourcesFileName);
            }
        }

        private void SetNuGetSourceEnabled(string name, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            EnsureNuGetSourcesLoaded();
            lock (_sourcesLock)
            {
                var source = _nugetSources.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (source != null)
                {
                    source.IsEnabled = enabled;
                }
            }
            SaveNuGetSources();
        }

        #endregion
    }
}
