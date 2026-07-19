using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TheTechIdea.Beep.Editor;

namespace Beep.Phase7.DiscoverySamples
{
    /// <summary>Bare POCO — discoverable only when a namespace filter scopes the scan (Phase 7 W9).</summary>
    public class PlainDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Explicit opt-out — never an entity, even though it looks discoverable.</summary>
    [BeepIgnore]
    public class IgnoredEntity
    {
        public int Id { get; set; }
    }

    /// <summary>Explicit opt-in — accepted even in an unscoped scan.</summary>
    [BeepEntity]
    public class OptInEntity
    {
        public int X { get; set; }
    }

    /// <summary>EF-decorated — accepted by the class-creator's EF recognition.</summary>
    [Table("EF_T")]
    public class EfEntity
    {
        [Key] public int Id { get; set; }
    }
}
