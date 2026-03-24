namespace TheTechIdea.Beep.Rules
{
    public partial class Token
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public string SchemaVersion { get; set; } = RuleStructure.CurrentSchemaVersion;
    }
}
