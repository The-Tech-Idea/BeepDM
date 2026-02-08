using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace TheTechIdea.Beep.Editor
{
    public class PropertyComparer<T> : IComparer<T>
    {
        private readonly PropertyDescriptor _property;
        private readonly ListSortDirection _direction;

        public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
        {
            _property = property;
            _direction = direction;
        }

        public int Compare(T x, T y)
        {
            var valueX = _property.GetValue(x);
            var valueY = _property.GetValue(y);

            int result = Comparer.Default.Compare(valueX, valueY);

            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }
}
