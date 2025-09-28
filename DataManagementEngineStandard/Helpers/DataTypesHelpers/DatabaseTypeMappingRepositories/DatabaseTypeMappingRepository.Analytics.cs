using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Analytics and Reporting platform specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Google Analytics data type mappings.</summary>
        /// <returns>A list of Google Analytics data type mappings.</returns>
        public static List<DatatypeMapping> GetGoogleAnalyticsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Google Analytics data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "PERCENT", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CURRENCY", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "US_CURRENCY", DataSourceName = "GoogleAnalyticsDataSource", NetDataType = "System.Decimal", Fav = false }
            };
        }

        /// <summary>Returns a list of Mixpanel data type mappings.</summary>
        /// <returns>A list of Mixpanel data type mappings.</returns>
        public static List<DatatypeMapping> GetMixpanelDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Mixpanel data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "MixpanelDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "MixpanelDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "MixpanelDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "MixpanelDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "MixpanelDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "MixpanelDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Hotjar data type mappings.</summary>
        /// <returns>A list of Hotjar data type mappings.</returns>
        public static List<DatatypeMapping> GetHotjarDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Hotjar data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "HotjarDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "HotjarDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "HotjarDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "HotjarDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "HotjarDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "url", DataSourceName = "HotjarDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Amplitude data type mappings.</summary>
        /// <returns>A list of Amplitude data type mappings.</returns>
        public static List<DatatypeMapping> GetAmplitudeDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Amplitude data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "AmplitudeDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "AmplitudeDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "AmplitudeDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "AmplitudeDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "AmplitudeDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "AmplitudeDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Heap data type mappings.</summary>
        /// <returns>A list of Heap data type mappings.</returns>
        public static List<DatatypeMapping> GetHeapDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Heap data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "HeapDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "HeapDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "HeapDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "HeapDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "HeapDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false }
            };
        }

        /// <summary>Returns a list of Databox data type mappings.</summary>
        /// <returns>A list of Databox data type mappings.</returns>
        public static List<DatatypeMapping> GetDataboxDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Databox data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "DataboxDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "DataboxDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "DataboxDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "DataboxDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "percentage", DataSourceName = "DataboxDataSource", NetDataType = "System.Double", Fav = false }
            };
        }

        /// <summary>Returns a list of Geckoboard data type mappings.</summary>
        /// <returns>A list of Geckoboard data type mappings.</returns>
        public static List<DatatypeMapping> GetGeckoboardDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Geckoboard data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "GeckoboardDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "GeckoboardDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "percentage", DataSourceName = "GeckoboardDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "GeckoboardDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "duration", DataSourceName = "GeckoboardDataSource", NetDataType = "System.TimeSpan", Fav = false }
            };
        }

        /// <summary>Returns a list of Cyfe data type mappings.</summary>
        /// <returns>A list of Cyfe data type mappings.</returns>
        public static List<DatatypeMapping> GetCyfeDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Cyfe data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "CyfeDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "CyfeDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "CyfeDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "currency", DataSourceName = "CyfeDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "percentage", DataSourceName = "CyfeDataSource", NetDataType = "System.Double", Fav = false }
            };
        }

        /// <summary>Returns a list of Tableau data type mappings.</summary>
        /// <returns>A list of Tableau data type mappings.</returns>
        public static List<DatatypeMapping> GetTableauDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Tableau data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TableauDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number (whole)", DataSourceName = "TableauDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number (decimal)", DataSourceName = "TableauDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "TableauDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date & time", DataSourceName = "TableauDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TableauDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "geographic role", DataSourceName = "TableauDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Power BI data type mappings.</summary>
        /// <returns>A list of Power BI data type mappings.</returns>
        public static List<DatatypeMapping> GetPowerBIDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Power BI data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Text", DataSourceName = "PowerBIDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Whole Number", DataSourceName = "PowerBIDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal Number", DataSourceName = "PowerBIDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Fixed Decimal Number", DataSourceName = "PowerBIDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Date/Time", DataSourceName = "PowerBIDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Date", DataSourceName = "PowerBIDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Time", DataSourceName = "PowerBIDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "True/False", DataSourceName = "PowerBIDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "PowerBIDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }
    }
}