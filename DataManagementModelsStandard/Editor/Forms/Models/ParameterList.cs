using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Represents a named parameter list (PARAMETER_LIST in Oracle Forms terminology).
    /// A named collection of key-value pairs that can be passed between forms or to the engine.
    /// </summary>
    public class ParameterList
    {
        public string Name { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ParameterList() { }

        public ParameterList(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void AddParameter(string name, object value)
        {
            Parameters[name] = value;
        }

        public object GetParameter(string name)
        {
            return Parameters.TryGetValue(name, out var value) ? value : null;
        }

        public T GetParameter<T>(string name)
        {
            var v = GetParameter(name);
            return v is T t ? t : default;
        }

        public bool RemoveParameter(string name) => Parameters.Remove(name);

        public bool HasParameter(string name) => Parameters.ContainsKey(name);

        public void Clear() => Parameters.Clear();

        public int Count => Parameters.Count;
    }
}
