using System;

namespace TheTechIdea.Beep.Editor
{
    public class Tracking
    {
        public Guid UniqueId { get; set; }
        public int OriginalIndex { get; set; }
        public int CurrentIndex { get; set; }
        public EntityState EntityState { get; set; } = EntityState.Unchanged;
        public bool IsSaved { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public string EntityName { get; set; }
        public string PKFieldName { get; set; }
        public string PKFieldValue { get; set; }
        public string PKFieldNameType { get; set; } // Can Int or string or Guid

        public Tracking(Guid uniqueId, int originalIndex)
        {
            UniqueId = uniqueId;
            OriginalIndex = originalIndex;
            CurrentIndex = originalIndex;
        }

        public Tracking(Guid uniqueId, int originalIndex, int currentindex)
        {
            UniqueId = uniqueId;
            OriginalIndex = originalIndex;
            CurrentIndex = currentindex;
        }
    }
}
