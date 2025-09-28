using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing File Format specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Flat File data type mappings.</summary>
        /// <returns>A list of Flat File data type mappings.</returns>
        public static List<DatatypeMapping> GetFlatFileDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Flat file formats - typically text-based
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "FlatFileDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "FlatFileDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "numeric", DataSourceName = "FlatFileDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "FlatFileDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "FlatFileDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "FlatFileDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "FlatFileDataSource", NetDataType = "System.Boolean", Fav = false }
            };
        }

        /// <summary>Returns a list of TSV (Tab-Separated Values) data type mappings.</summary>
        /// <returns>A list of TSV data type mappings.</returns>
        public static List<DatatypeMapping> GetTSVDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // TSV files - similar to CSV but tab-delimited
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TSVDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "TSVDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "TSVDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "TSVDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TSVDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "TSVDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Text file data type mappings.</summary>
        /// <returns>A list of Text file data type mappings.</returns>
        public static List<DatatypeMapping> GetTextDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Plain text files
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "TextDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "line", DataSourceName = "TextDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "paragraph", DataSourceName = "TextDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "character", DataSourceName = "TextDataSource", NetDataType = "System.Char", Fav = false }
            };
        }

        /// <summary>Returns a list of YAML data type mappings.</summary>
        /// <returns>A list of YAML data type mappings.</returns>
        public static List<DatatypeMapping> GetYAMLDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // YAML data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "str", DataSourceName = "YAMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "YAMLDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "YAMLDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bool", DataSourceName = "YAMLDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "null", DataSourceName = "YAMLDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "YAMLDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "YAMLDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "seq", DataSourceName = "YAMLDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "map", DataSourceName = "YAMLDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false }
            };
        }

        /// <summary>Returns a list of Markdown data type mappings.</summary>
        /// <returns>A list of Markdown data type mappings.</returns>
        public static List<DatatypeMapping> GetMarkdownDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Markdown elements
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "MarkdownDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "heading", DataSourceName = "MarkdownDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "paragraph", DataSourceName = "MarkdownDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "link", DataSourceName = "MarkdownDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "MarkdownDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "code", DataSourceName = "MarkdownDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "MarkdownDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "table", DataSourceName = "MarkdownDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of HTML data type mappings.</summary>
        /// <returns>A list of HTML data type mappings.</returns>
        public static List<DatatypeMapping> GetHTMLDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // HTML elements and attributes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "HTMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "element", DataSourceName = "HTMLDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "attribute", DataSourceName = "HTMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "url", DataSourceName = "HTMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "HTMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "script", DataSourceName = "HTMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "style", DataSourceName = "HTMLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "form", DataSourceName = "HTMLDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of SQL file data type mappings.</summary>
        /// <returns>A list of SQL file data type mappings.</returns>
        public static List<DatatypeMapping> GetSQLDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // SQL script elements
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "statement", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "query", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "procedure", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "function", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "comment", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "identifier", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "literal", DataSourceName = "SQLDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of INI file data type mappings.</summary>
        /// <returns>A list of INI file data type mappings.</returns>
        public static List<DatatypeMapping> GetINIDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // INI file structure
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "section", DataSourceName = "INIDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "key", DataSourceName = "INIDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "value", DataSourceName = "INIDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "comment", DataSourceName = "INIDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "INIDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "INIDataSource", NetDataType = "System.Boolean", Fav = false }
            };
        }

        /// <summary>Returns a list of Log file data type mappings.</summary>
        /// <returns>A list of Log file data type mappings.</returns>
        public static List<DatatypeMapping> GetLogDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Log file elements
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "LogDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "level", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "message", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "logger", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "thread", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "exception", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ip_address", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "user_agent", DataSourceName = "LogDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "status_code", DataSourceName = "LogDataSource", NetDataType = "System.Int32", Fav = false }
            };
        }

        /// <summary>Returns a list of PDF data type mappings.</summary>
        /// <returns>A list of PDF data type mappings.</returns>
        public static List<DatatypeMapping> GetPDFDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // PDF document elements
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "PDFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "page", DataSourceName = "PDFDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "PDFDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "metadata", DataSourceName = "PDFDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "annotation", DataSourceName = "PDFDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "form_field", DataSourceName = "PDFDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "font", DataSourceName = "PDFDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Microsoft Word document data type mappings.</summary>
        /// <returns>A list of Microsoft Word document data type mappings.</returns>
        public static List<DatatypeMapping> GetDocDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Word document elements
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "DocDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "paragraph", DataSourceName = "DocDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "heading", DataSourceName = "DocDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "table", DataSourceName = "DocDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "DocDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hyperlink", DataSourceName = "DocDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "style", DataSourceName = "DocDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "comment", DataSourceName = "DocDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Microsoft Word DOCX data type mappings.</summary>
        /// <returns>A list of Microsoft Word DOCX data type mappings.</returns>
        public static List<DatatypeMapping> GetDocxDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // DOCX document elements (similar to DOC but XML-based)
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "DocxDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "paragraph", DataSourceName = "DocxDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "run", DataSourceName = "DocxDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "table", DataSourceName = "DocxDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "drawing", DataSourceName = "DocxDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "DocxDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hyperlink", DataSourceName = "DocxDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bookmark", DataSourceName = "DocxDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "content_control", DataSourceName = "DocxDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Microsoft PowerPoint data type mappings.</summary>
        /// <returns>A list of Microsoft PowerPoint data type mappings.</returns>
        public static List<DatatypeMapping> GetPPTDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // PowerPoint presentation elements
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "slide", DataSourceName = "PPTDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "PPTDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "title", DataSourceName = "PPTDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "shape", DataSourceName = "PPTDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "PPTDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "chart", DataSourceName = "PPTDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "table", DataSourceName = "PPTDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "animation", DataSourceName = "PPTDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hyperlink", DataSourceName = "PPTDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Microsoft PowerPoint PPTX data type mappings.</summary>
        /// <returns>A list of Microsoft PowerPoint PPTX data type mappings.</returns>
        public static List<DatatypeMapping> GetPPTXDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // PPTX presentation elements (XML-based)
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "slide", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text_run", DataSourceName = "PPTXDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "paragraph", DataSourceName = "PPTXDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "shape", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "picture", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "chart", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "table", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "slide_layout", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "theme", DataSourceName = "PPTXDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}