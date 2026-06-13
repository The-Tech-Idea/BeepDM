namespace TheTechIdea.Beep.Editor.Schema
{
    public sealed class SchemaComparisonOptions
    {
        public static SchemaComparisonOptions Default { get; } = new();

        public bool IgnoreCaseInTypeNames { get; init; } = true;
        public bool NormalizeMaxLengthZero { get; init; } = true;
        public bool IncludePrecisionScale { get; init; }
        public bool IncludeNullableChanges { get; init; } = true;
    }
}
