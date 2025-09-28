using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing IoT (Internet of Things) platform specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Twilio data type mappings.</summary>
        /// <returns>A list of Twilio data type mappings.</returns>
        public static List<DatatypeMapping> GetTwilioDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Twilio API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TwilioDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "TwilioDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TwilioDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date_time", DataSourceName = "TwilioDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "uri", DataSourceName = "TwilioDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "currency", DataSourceName = "TwilioDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "phone_number", DataSourceName = "TwilioDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Plaid data type mappings.</summary>
        /// <returns>A list of Plaid data type mappings.</returns>
        public static List<DatatypeMapping> GetPlaidDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Plaid API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "PlaidDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "PlaidDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "PlaidDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "PlaidDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "iso8601", DataSourceName = "PlaidDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "PlaidDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "PlaidDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of DocuSign data type mappings.</summary>
        /// <returns>A list of DocuSign data type mappings.</returns>
        public static List<DatatypeMapping> GetDocuSignDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // DocuSign API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "DocuSignDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "DocuSignDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dateTime", DataSourceName = "DocuSignDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "DocuSignDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "base64Binary", DataSourceName = "DocuSignDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "DocuSignDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Philips Hue data type mappings.</summary>
        /// <returns>A list of Philips Hue data type mappings.</returns>
        public static List<DatatypeMapping> GetPhilipsHueDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Philips Hue API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "PhilipsHueDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "PhilipsHueDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "PhilipsHueDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "PhilipsHueDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "xy", DataSourceName = "PhilipsHueDataSource", NetDataType = "System.Collections.Generic.List<double>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "PhilipsHueDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Nest data type mappings.</summary>
        /// <returns>A list of Nest data type mappings.</returns>
        public static List<DatatypeMapping> GetNestDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Nest API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "NestDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "NestDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "NestDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "NestDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "NestDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of SmartThings data type mappings.</summary>
        /// <returns>A list of SmartThings data type mappings.</returns>
        public static List<DatatypeMapping> GetSmartThingsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // SmartThings API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "SmartThingsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "SmartThingsDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "SmartThingsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "SmartThingsDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enum", DataSourceName = "SmartThingsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "SmartThingsDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "SmartThingsDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Tuya data type mappings.</summary>
        /// <returns>A list of Tuya data type mappings.</returns>
        public static List<DatatypeMapping> GetTuyaDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Tuya API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TuyaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "TuyaDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TuyaDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enum", DataSourceName = "TuyaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bitmap", DataSourceName = "TuyaDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "raw", DataSourceName = "TuyaDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "TuyaDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Particle data type mappings.</summary>
        /// <returns>A list of Particle data type mappings.</returns>
        public static List<DatatypeMapping> GetParticleDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Particle API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "ParticleDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "ParticleDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "ParticleDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "ParticleDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "ParticleDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "ParticleDataSource", NetDataType = "System.DateTime", Fav = false }
            };
        }

        /// <summary>Returns a list of Arduino Cloud data type mappings.</summary>
        /// <returns>A list of Arduino Cloud data type mappings.</returns>
        public static List<DatatypeMapping> GetArduinoCloudDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Arduino Cloud API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "coloredLight", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dimmedLight", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "location", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "schedule", DataSourceName = "ArduinoCloudDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}