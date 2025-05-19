using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using System.IO;

namespace TheTechIdea.Beep.Enums
{


    // Custom attribute to hold descriptions (could extend for localization)
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class DynamicEnumDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public DynamicEnumDescriptionAttribute(string description) => Description = description;
    }

    /// <summary>
    /// Represents one entry in DynamicEnum.
    /// </summary>
    public class DynamicEnumEntry
    {
        [JsonPropertyName("name")]
        [XmlAttribute("Name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("value")]
        [XmlAttribute("Value")]
        public int Value { get; set; }

        [JsonPropertyName("description")]
        [XmlAttribute("Description")]
        public string Description { get; set; } = "";

        public DynamicEnumEntry() { } // For serialization

        public DynamicEnumEntry(string name, int value, string description = "")
        {
            Name = name;
            Value = value;
            Description = description;
        }

        public override string ToString() => $"{Name} = {Value} ({Description})";
    }

    /// <summary>
    /// A thread-safe, fully featured dynamic enum replacement with flag support, serialization, events, descriptions, and data binding.
    /// </summary>
    [JsonConverter(typeof(DynamicEnumJsonConverter))]
    [XmlRoot("DynamicEnum")]
    public class DynamicEnum : IEnumerable<DynamicEnumEntry>, INotifyPropertyChanged
    {
        private readonly ConcurrentDictionary<string, DynamicEnumEntry> _entriesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<int, DynamicEnumEntry> _entriesByValue = new();

        private int _defaultValue;

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DynamicEnumEntry>? EntryAdded;
        public event EventHandler<DynamicEnumEntry>? EntryRemoved;
        public event EventHandler? Cleared;
        #endregion

        public DynamicEnum()
        {
            // Initialize with None = 0
            Add("None", 0, "Default none value");
            SetDefault("None");
        }

        #region Add / Remove / Update

        /// <summary>
        /// Add or update entry by name and value.
        /// Throws if duplicate value for different name.
        /// </summary>
        public void Add(string name, int value, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name must be non-empty", nameof(name));

            lock (_entriesByName)
            {
                if (_entriesByName.TryGetValue(name, out var existingEntry))
                {
                    if (existingEntry.Value != value)
                    {
                        // Value clash check
                        if (_entriesByValue.ContainsKey(value) && _entriesByValue[value].Name != name)
                            throw new ArgumentException($"Value {value} already exists for another name.");

                        _entriesByValue.TryRemove(existingEntry.Value, out _);
                        var newEntry = new DynamicEnumEntry(name, value, description);
                        _entriesByName[name] = newEntry;
                        _entriesByValue[value] = newEntry;
                        OnEntryAdded(newEntry);
                        OnPropertyChanged(nameof(Count));
                    }
                    else
                    {
                        // Update description only
                        var newEntry = new DynamicEnumEntry(name, value, description);
                        _entriesByName[name] = newEntry;
                        _entriesByValue[value] = newEntry;
                        OnPropertyChanged(nameof(Count));
                    }
                }
                else
                {
                    if (_entriesByValue.ContainsKey(value))
                        throw new ArgumentException($"Value {value} already exists.");

                    var entry = new DynamicEnumEntry(name, value, description);
                    _entriesByName[name] = entry;
                    _entriesByValue[value] = entry;
                    OnEntryAdded(entry);
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        /// <summary>
        /// Remove entry by name.
        /// </summary>
        public bool Remove(string name)
        {
            lock (_entriesByName)
            {
                if (_entriesByName.TryRemove(name, out var entry))
                {
                    _entriesByValue.TryRemove(entry.Value, out _);
                    OnEntryRemoved(entry);
                    OnPropertyChanged(nameof(Count));
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Clear all entries and reset default to None=0.
        /// </summary>
        public void Clear()
        {
            lock (_entriesByName)
            {
                _entriesByName.Clear();
                _entriesByValue.Clear();
                Add("None", 0, "Default none value");
                SetDefault("None");
                OnCleared();
                OnPropertyChanged(nameof(Count));
            }
        }

        #endregion

        #region Lookup Methods

        public int Count => _entriesByName.Count;

        public void SetDefault(string name)
        {
            if (!_entriesByName.TryGetValue(name, out var entry))
                throw new ArgumentException($"Entry '{name}' not found.");
            _defaultValue = entry.Value;
            OnPropertyChanged(nameof(DefaultValue));
        }

        public int DefaultValue => _defaultValue;

        public bool TryGetValue(string name, out int value)
        {
            if (_entriesByName.TryGetValue(name, out var entry))
            {
                value = entry.Value;
                return true;
            }
            value = _defaultValue;
            return false;
        }

        public int GetValue(string name)
        {
            if (TryGetValue(name, out var val))
                return val;
            throw new KeyNotFoundException($"Name '{name}' not found.");
        }

        public bool TryGetName(int value, out string name)
        {
            if (_entriesByValue.TryGetValue(value, out var entry))
            {
                name = entry.Name;
                return true;
            }
            name = "";
            return false;
        }

        public string GetName(int value)
        {
            if (TryGetName(value, out var name))
                return name;
            throw new KeyNotFoundException($"Value '{value}' not found.");
        }

        public string GetDescription(string name)
        {
            if (_entriesByName.TryGetValue(name, out var entry))
                return entry.Description;
            throw new KeyNotFoundException($"Name '{name}' not found.");
        }

        #endregion

        #region Flags Support

        public int Combine(params string[] names)
        {
            int combined = 0;
            foreach (var name in names)
                combined |= GetValue(name);
            return combined;
        }

        public int ParseFlags(string combinedFlags)
        {
            if (string.IsNullOrWhiteSpace(combinedFlags)) return 0;
            var parts = combinedFlags.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return Combine(parts.Select(p => p.Trim()).ToArray());
        }

        public bool HasFlag(int combinedValue, string flagName)
        {
            int flagValue = GetValue(flagName);
            return (combinedValue & flagValue) == flagValue;
        }

        public IEnumerable<string> GetFlags(int combinedValue)
        {
            return _entriesByValue.Values
                .Where(e => e.Value != 0 && (combinedValue & e.Value) == e.Value)
                .Select(e => e.Name);
        }

        public string GetFlagsString(int combinedValue)
        {
            var flags = GetFlags(combinedValue);
            return string.Join(", ", flags);
        }

        #endregion

        #region Enumeration

        public IEnumerator<DynamicEnumEntry> GetEnumerator() => _entriesByName.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Property Changed and Events

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected void OnEntryAdded(DynamicEnumEntry entry) => EntryAdded?.Invoke(this, entry);

        protected void OnEntryRemoved(DynamicEnumEntry entry) => EntryRemoved?.Invoke(this, entry);

        protected void OnCleared() => Cleared?.Invoke(this,EventArgs.Empty);

        #endregion

        #region Implicit and explicit conversions

        public static implicit operator int(DynamicEnum de) => de._defaultValue;

        public static explicit operator DynamicEnum(int value)
        {
            var de = new DynamicEnum();
            de.Add("Value", value);
            de.SetDefault("Value");
            return de;
        }

        public override string ToString() => $"DynamicEnum(Default={_defaultValue}, Count={Count})";

        #endregion

        #region XML Serialization

        [XmlArray("Entries")]
        [XmlArrayItem("Entry")]
        public List<DynamicEnumEntry> XmlEntries
        {
            get => _entriesByName.Values.ToList();
            set
            {
                Clear();
                if (value == null) return;
                foreach (var entry in value)
                    Add(entry.Name, entry.Value, entry.Description);
            }
        }

        public string ToXml()
        {
            var serializer = new XmlSerializer(typeof(DynamicEnum));
            using var sw = new StringWriter();
            serializer.Serialize(sw, this);
            return sw.ToString();
        }

        public static DynamicEnum FromXml(string xml)
        {
            var serializer = new XmlSerializer(typeof(DynamicEnum));
            using var sr = new StringReader(xml);
            return (DynamicEnum)serializer.Deserialize(sr)!;
        }

        #endregion

        #region JSON Serialization (System.Text.Json)

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public static DynamicEnum FromJson(string json)
        {
            return JsonSerializer.Deserialize<DynamicEnum>(json) ?? new DynamicEnum();
        }

        #endregion
    }

    /// <summary>
    /// Custom JSON converter for DynamicEnum to handle serialization/deserialization properly.
    /// </summary>
    public class DynamicEnumJsonConverter : JsonConverter<DynamicEnum>
    {
        public override DynamicEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var entries = JsonSerializer.Deserialize<List<DynamicEnumEntry>>(ref reader, options);
            var de = new DynamicEnum();
            de.Clear();
            if (entries != null)
            {
                foreach (var entry in entries)
                    de.Add(entry.Name, entry.Value, entry.Description);
            }
            return de;
        }

        public override void Write(Utf8JsonWriter writer, DynamicEnum value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.XmlEntries, options);
        }
    }
    public static class EnumExtensions
    {
        public static DynamicEnum ToDynamicEnum<T>() where T : Enum
        {
            var dynamicEnum = new DynamicEnum();
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                string name = Enum.GetName(typeof(T), value)!;
                dynamicEnum.Add(name, Convert.ToInt32(value));
            }
            return dynamicEnum;
        }

        public static DynamicEnum GetDynamicEnum(this Enum enumValue)
        {
            Type enumType = enumValue.GetType();
            var dynamicEnum = new DynamicEnum();

            foreach (var value in Enum.GetValues(enumType))
            {
                string name = Enum.GetName(enumType, value)!;
                dynamicEnum.Add(name, Convert.ToInt32(value));
            }

            // Set default value to match the provided enum value
            string defaultName = Enum.GetName(enumType, enumValue)!;
            dynamicEnum.SetDefault(defaultName);

            return dynamicEnum;
        }

        /// <summary>
        /// Converts a DynamicEnum value to an enum of type T.
        /// </summary>
        /// <typeparam name="T">The enum type to convert to</typeparam>
        /// <param name="dynamicEnum">The DynamicEnum instance</param>
        /// <param name="valueName">The name of the value to convert</param>
        /// <returns>The enum value of type T</returns>
        public static T ToEnum<T>(this DynamicEnum dynamicEnum, string valueName) where T : Enum
        {
            int value = dynamicEnum.GetValue(valueName);

            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentException($"The value {value} ({valueName}) does not exist in enum {typeof(T).Name}");

            return (T)Enum.ToObject(typeof(T), value);
        }

        /// <summary>
        /// Attempts to convert a DynamicEnum value to an enum of type T.
        /// </summary>
        /// <typeparam name="T">The enum type to convert to</typeparam>
        /// <param name="dynamicEnum">The DynamicEnum instance</param>
        /// <param name="valueName">The name of the value to convert</param>
        /// <param name="result">When successful, contains the converted enum value</param>
        /// <returns>True if conversion succeeded, false otherwise</returns>
        public static bool TryToEnum<T>(this DynamicEnum dynamicEnum, string valueName, out T result) where T : Enum
        {
            result = default!;

            if (!dynamicEnum.TryGetValue(valueName, out int value))
                return false;

            if (!Enum.IsDefined(typeof(T), value))
                return false;

            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        /// <summary>
        /// Creates a dictionary mapping between a standard enum and DynamicEnum.
        /// Useful when you need to maintain relationships between them.
        /// </summary>
        /// <typeparam name="T">The enum type</typeparam>
        /// <param name="dynamicEnum">The DynamicEnum instance</param>
        /// <returns>Dictionary mapping enum values to their dynamic counterparts</returns>
        public static Dictionary<T, string> CreateEnumMapping<T>(this DynamicEnum dynamicEnum) where T : Enum
        {
            var mapping = new Dictionary<T, string>();

            foreach (var entry in dynamicEnum)
            {
                if (Enum.IsDefined(typeof(T), entry.Value))
                {
                    var enumValue = (T)Enum.ToObject(typeof(T), entry.Value);
                    mapping[enumValue] = entry.Name;
                }
            }

            return mapping;
        }

        /// <summary>
        /// Adds DynamicEnum entries to a dictionary that can be used in switch statements
        /// or other places where enums are expected.
        /// </summary>
        /// <typeparam name="TResult">The result type of the dictionary values</typeparam>
        /// <param name="dynamicEnum">The DynamicEnum instance</param>
        /// <param name="valueFactory">Function to create a value for each entry</param>
        /// <returns>Dictionary mapping int values to results</returns>
        public static Dictionary<int, TResult> ToValueDictionary<TResult>(
            this DynamicEnum dynamicEnum,
            Func<DynamicEnumEntry, TResult> valueFactory)
        {
            return dynamicEnum
                .ToDictionary(entry => entry.Value, entry => valueFactory(entry));
        }

        /// <summary>
        /// Creates a switch-like function that maps DynamicEnum values to actions
        /// </summary>
        /// <param name="dynamicEnum">The DynamicEnum instance</param>
        /// <returns>A function that executes the appropriate action for a given enum value</returns>
        public static Func<int, Action> CreateDynamicSwitch(this DynamicEnum dynamicEnum)
        {
            var handlers = new Dictionary<int, Action>();

            return value => {
                if (handlers.TryGetValue(value, out var action))
                    return action;
                return () => { }; // Default no-op action
            };
        }
    }

}
