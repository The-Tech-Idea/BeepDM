namespace TheTechIdea.Beep.Rules
{
    public partial class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
            SchemaVersion = RuleStructure.CurrentSchemaVersion;
        }

        public Token(TokenType type, string value, int start, int length)
        {
            Type = type;
            Value = value;
            Start = start;
            Length = length;
            SchemaVersion = RuleStructure.CurrentSchemaVersion;
        }

        public override string ToString() => $"{Type}({Value})@{Start}+{Length}";
    }
}

