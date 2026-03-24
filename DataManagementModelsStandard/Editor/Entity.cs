using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TheTechIdea.Beep.Editor
{
    public class Entity : INotifyPropertyChanged, IEntity
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Gets a custom attribute from a member in a null-safe way.
        /// Helpful when converting EF Core decorated classes to framework entities.
        /// </summary>
        protected static TAttribute GetAttribute<TAttribute>(MemberInfo memberInfo)
            where TAttribute : Attribute
        {
            return memberInfo?.GetCustomAttribute<TAttribute>(inherit: false);
        }

        /// <summary>
        /// Sets a property by name through reflection.
        /// Useful for generic POCO/EF model projection scenarios.
        /// </summary>
        public virtual bool TrySetPropertyValue(string propertyName, object value, bool ignoreCase = true)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return false;

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var prop = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var p in prop)
            {
                if (!p.CanWrite) continue;
                if (!string.Equals(p.Name, propertyName, comparison)) continue;

                if (value == null)
                {
                    p.SetValue(this, null);
                    return true;
                }

                var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var converted = targetType.IsAssignableFrom(value.GetType())
                    ? value
                    : Convert.ChangeType(value, targetType);
                p.SetValue(this, converted);
                return true;
            }

            return false;
        }
    }
}
