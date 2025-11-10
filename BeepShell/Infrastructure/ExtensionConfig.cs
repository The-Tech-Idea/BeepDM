using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Base implementation of extension configuration
    /// </summary>
    public class ExtensionConfig : IExtensionConfig
    {
        private readonly Dictionary<string, object> _values = new();
        private readonly JsonSerializerOptions _jsonOptions;

        public string ConfigPath { get; private set; }

        public ExtensionConfig(string configPath)
        {
            ConfigPath = configPath;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                // Create default config
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);
                
                if (values != null)
                {
                    foreach (var kvp in values)
                    {
                        _values[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {ConfigPath}", ex);
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_values, _jsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration to {ConfigPath}", ex);
            }
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (!_values.TryGetValue(key, out var value))
            {
                return defaultValue;
            }

            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions);
                }

                if (value is T typedValue)
                {
                    return typedValue;
                }

                // Try to convert
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            _values[key] = value;
        }

        public bool HasKey(string key)
        {
            return _values.ContainsKey(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _values.Keys;
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }
    }

    /// <summary>
    /// Strongly-typed extension configuration base class
    /// </summary>
    /// <typeparam name="T">Configuration model type</typeparam>
    public class ExtensionConfig<T> where T : class, new()
    {
        private T _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public string ConfigPath { get; private set; }
        public T Config => _config;

        public ExtensionConfig(string configPath)
        {
            ConfigPath = configPath;
            _config = new T();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                _config = new T();
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                _config = JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {ConfigPath}", ex);
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_config, _jsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration to {ConfigPath}", ex);
            }
        }

        public void Update(Action<T> updater)
        {
            updater(_config);
            Save();
        }
    }
}
