using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>Captured desired schema and observed baseline for a governed operation.</summary>
    public sealed class MigrationEntitySnapshot
    {
        public EntityStructure DesiredSchema { get; set; }
        public bool ExpectedEntityExists { get; set; }
        public EntityStructure ExpectedSchema { get; set; }
    }
}
