using System;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Configuration
{
    public interface ISettingsProvider : IDisposable
    {
        T GetValue<T>(string key, T defaultValue = default);
        void SetValue<T>(string key, T value);
        bool RemoveValue(string key);
        bool HasKey(string key);
        void SaveChanges();
        Task SaveChangesAsync();
        void Reload();
        string[] GetAllKeys();
        void Clear();
    }
}
