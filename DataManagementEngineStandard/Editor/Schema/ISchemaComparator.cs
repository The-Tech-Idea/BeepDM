namespace TheTechIdea.Beep.Editor.Schema
{
    public interface ISchemaComparator
    {
        SchemaDriftReport Compare(SchemaSnapshot baseline, SchemaSnapshot current);
        SchemaDriftReport Compare(SchemaSnapshot baseline, SchemaSnapshot current, SchemaComparisonOptions options);
    }
}
