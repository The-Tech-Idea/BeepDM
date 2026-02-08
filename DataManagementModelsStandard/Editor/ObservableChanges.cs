using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor
{
    public class ObservableChanges<T>
    {
        public List<T> Added { get; set; } = new();
        public List<T> Modified { get; set; } = new();
        public List<T> Deleted { get; set; } = new();
    }
}
