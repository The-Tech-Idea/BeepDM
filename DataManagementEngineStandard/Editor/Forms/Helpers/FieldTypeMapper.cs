using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

public static class FieldTypeMapper
{
    public static string GetCanonicalFieldType(EntityField field)
    {
        if (field == null) return "Text";
        return GetCanonicalFieldType(field.Fieldtype, field.IsIdentity);
    }

    /// <summary>
    /// Canonical field type from a bare type name, for callers that hold
    /// field metadata but not an <see cref="EntityField"/> — notably the IDE,
    /// which assigns editor keys from a designer file's parsed definitions.
    /// <para>
    /// The runtime presenter registries switch on exactly this value, so
    /// design-time and run-time must not each carry their own copy of the
    /// mapping. Overload added rather than duplicating the switch.
    /// </para>
    /// </summary>
    public static string GetCanonicalFieldType(string? fieldType, bool isIdentity = false)
    {
        if (isIdentity) return "ReadOnly";

        return fieldType?.ToLowerInvariant() switch
        {
            "int" or "int32" or "int64" or "integer" or "long" or "bigint" or "smallint" => "Numeric",
            "decimal" or "double" or "float" or "single" or "numeric" or "money" or "real" => "Numeric",
            "datetime" or "date" or "datetime2" or "timestamp" or "smalldatetime" or "datetimeoffset" => "Date",
            "bool" or "boolean" => "Boolean",
            "bit" => "Checkbox",
            "guid" or "uniqueidentifier" => "Text",
            "binary" or "varbinary" or "image" or "blob" => "Text",
            _ => "Text",
        };
    }

    public static DbFieldCategory ResolveCategory(string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType)) return DbFieldCategory.String;
        string t = fieldType.ToLowerInvariant();
        if (t.Contains("int") || t.Contains("bit")) return DbFieldCategory.Integer;
        if (t.Contains("decimal") || t.Contains("double") || t.Contains("float") || t.Contains("numeric") || t.Contains("money")) return DbFieldCategory.Decimal;
        if (t.Contains("date") || t.Contains("time")) return DbFieldCategory.DateTime;
        if (t.Contains("bool")) return DbFieldCategory.Boolean;
        if (t.Contains("text") || t.Contains("char") || t.Contains("varchar") || t.Contains("nchar") || t.Contains("nvarchar")) return DbFieldCategory.String;
        return DbFieldCategory.String;
    }
}
