using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using DataManagementModels.Editor;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    public class ObservableBindingListConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // We only allow converting from a compatible list type or string
            return sourceType == typeof(string) || sourceType == typeof(List<Entity>) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                // Handle conversion from string if necessary
                return new ObservableBindingList<Entity>();
            }
            else if (value is List<Entity> entityList)
            {
                // Handle conversion from List<Entity>
                return new ObservableBindingList<Entity>(entityList);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                // Customize how the ObservableBindingList<Entity> appears in the designer
                return "ObservableBindingList<Entity>";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
