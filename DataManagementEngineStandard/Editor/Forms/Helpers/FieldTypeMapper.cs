using System;
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

        return Normalize(fieldType) switch
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

    /// <summary>
    /// Reduces a declared field type to the bare, lower-cased name the switches
    /// below match on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Datasources report .NET type names — <c>System.Int32</c>,
    /// <c>System.Single</c>, <c>System.DateTime</c> (see
    /// <c>JsonExtensions.DetermineFieldtype</c>) — while RDBMS drivers report
    /// SQL names like <c>int</c> or <c>nvarchar</c>. Matching only the bare
    /// forms meant every qualified name fell through to <c>Text</c>, so a
    /// numeric or date column from any such datasource rendered as a plain text
    /// box in both single-record and grid mode, and the IDE assigned it a text
    /// editor key. Nothing detected it because the fallback is a legitimate
    /// value.
    /// </para>
    /// <para>
    /// Also strips a nullable marker, so <c>System.Int32?</c> and
    /// <c>Nullable&lt;Int32&gt;</c> resolve like the underlying type.
    /// </para>
    /// </remarks>
    private static string Normalize(string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType)) return string.Empty;

        var text = fieldType.Trim();

        // Nullable<Int32> / System.Nullable`1[System.Int32] -> the inner type.
        var open = text.IndexOf('[');
        if (open >= 0 && text.EndsWith("]", StringComparison.Ordinal))
        {
            text = text[(open + 1)..^1];
        }
        else
        {
            open = text.IndexOf('<');
            if (open >= 0 && text.EndsWith(">", StringComparison.Ordinal))
            {
                text = text[(open + 1)..^1];
            }
        }

        text = text.TrimEnd('?');

        // System.Int32 -> Int32. Namespace-qualified names are what .NET-typed
        // datasources report.
        var lastDot = text.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < text.Length - 1)
        {
            text = text[(lastDot + 1)..];
        }

        return text.ToLowerInvariant();
    }

    public static DbFieldCategory ResolveCategory(string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType)) return DbFieldCategory.String;
        string t = Normalize(fieldType);
        if (t.Contains("int") || t.Contains("bit")) return DbFieldCategory.Integer;
        if (t.Contains("decimal") || t.Contains("double") || t.Contains("float") || t.Contains("numeric") || t.Contains("money")) return DbFieldCategory.Decimal;
        if (t.Contains("date") || t.Contains("time")) return DbFieldCategory.DateTime;
        if (t.Contains("bool")) return DbFieldCategory.Boolean;
        if (t.Contains("text") || t.Contains("char") || t.Contains("varchar") || t.Contains("nchar") || t.Contains("nvarchar")) return DbFieldCategory.String;
        return DbFieldCategory.String;
    }
}
