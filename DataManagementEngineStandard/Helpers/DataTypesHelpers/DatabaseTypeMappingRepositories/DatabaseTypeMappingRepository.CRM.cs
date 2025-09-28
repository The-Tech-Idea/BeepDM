using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing CRM (Customer Relationship Management) platform specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Salesforce data type mappings.</summary>
        /// <returns>A list of Salesforce data type mappings.</returns>
        public static List<DatatypeMapping> GetSalesforceDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Salesforce field data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "textarea", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "email", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "phone", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "url", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "SalesforceDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "SalesforceDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "currency", DataSourceName = "SalesforceDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "percent", DataSourceName = "SalesforceDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "SalesforceDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "SalesforceDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "SalesforceDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "time", DataSourceName = "SalesforceDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "picklist", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "multipicklist", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "combobox", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "reference", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "base64", DataSourceName = "SalesforceDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "id", DataSourceName = "SalesforceDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "location", DataSourceName = "SalesforceDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "address", DataSourceName = "SalesforceDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of HubSpot data type mappings.</summary>
        /// <returns>A list of HubSpot data type mappings.</returns>
        public static List<DatatypeMapping> GetHubSpotDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // HubSpot property data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "HubSpotDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "HubSpotDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bool", DataSourceName = "HubSpotDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "HubSpotDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "HubSpotDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enumeration", DataSourceName = "HubSpotDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "phone_number", DataSourceName = "HubSpotDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "HubSpotDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Zoho data type mappings.</summary>
        /// <returns>A list of Zoho data type mappings.</returns>
        public static List<DatatypeMapping> GetZohoDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Zoho CRM field data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "textarea", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "email", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "phone", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "website", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "ZohoDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "ZohoDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "currency", DataSourceName = "ZohoDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "percent", DataSourceName = "ZohoDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "ZohoDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "ZohoDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "ZohoDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "picklist", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "multiselectpicklist", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "lookup", DataSourceName = "ZohoDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ownerlookup", DataSourceName = "ZohoDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "autonumber", DataSourceName = "ZohoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "formula", DataSourceName = "ZohoDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Pipedrive data type mappings.</summary>
        /// <returns>A list of Pipedrive data type mappings.</returns>
        public static List<DatatypeMapping> GetPipedriveDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Pipedrive field data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "varchar", DataSourceName = "PipedriveDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "varchar_auto", DataSourceName = "PipedriveDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "PipedriveDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "PipedriveDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "monetary", DataSourceName = "PipedriveDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "PipedriveDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "daterange", DataSourceName = "PipedriveDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "time", DataSourceName = "PipedriveDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timerange", DataSourceName = "PipedriveDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "PipedriveDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enum", DataSourceName = "PipedriveDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "PipedriveDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "user", DataSourceName = "PipedriveDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "org", DataSourceName = "PipedriveDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "people", DataSourceName = "PipedriveDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "phone", DataSourceName = "PipedriveDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "address", DataSourceName = "PipedriveDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Microsoft Dynamics 365 data type mappings.</summary>
        /// <returns>A list of Microsoft Dynamics 365 data type mappings.</returns>
        public static List<DatatypeMapping> GetMicrosoftDynamics365DataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Dynamics 365 field data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Memo", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BigInt", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Money", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Lookup", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Customer", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Owner", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Picklist", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MultiSelectPicklist", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "State", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Status", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Uniqueidentifier", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "PartyList", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ManagedProperty", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "EntityName", DataSourceName = "MicrosoftDynamics365DataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of SAP CRM data type mappings.</summary>
        /// <returns>A list of SAP CRM data type mappings.</returns>
        public static List<DatatypeMapping> GetSAPCRMDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // SAP CRM ABAP data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "C", DataSourceName = "SAPCRMDataSource", NetDataType = "System.String", Fav = false }, // Character
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "D", DataSourceName = "SAPCRMDataSource", NetDataType = "System.DateTime", Fav = false }, // Date
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "F", DataSourceName = "SAPCRMDataSource", NetDataType = "System.Double", Fav = false }, // Float
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "I", DataSourceName = "SAPCRMDataSource", NetDataType = "System.Int32", Fav = false }, // Integer
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "N", DataSourceName = "SAPCRMDataSource", NetDataType = "System.String", Fav = false }, // Numeric text
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "P", DataSourceName = "SAPCRMDataSource", NetDataType = "System.Decimal", Fav = false }, // Packed number
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "T", DataSourceName = "SAPCRMDataSource", NetDataType = "System.TimeSpan", Fav = false }, // Time
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "X", DataSourceName = "SAPCRMDataSource", NetDataType = "System.Byte[]", Fav = false }, // Byte sequence
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "SAPCRMDataSource", NetDataType = "System.String", Fav = false }, // String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "XSTRING", DataSourceName = "SAPCRMDataSource", NetDataType = "System.Byte[]", Fav = false } // Byte string
            };
        }

        /// <summary>Returns a list of Oracle CRM data type mappings.</summary>
        /// <returns>A list of Oracle CRM data type mappings.</returns>
        public static List<DatatypeMapping> GetOracleCRMDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Oracle CRM data types (based on Oracle database types)
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR2", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NVARCHAR2", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCHAR", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMBER", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY_FLOAT", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY_DOUBLE", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "OracleCRMDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "OracleCRMDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTERVAL", DataSourceName = "OracleCRMDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "RAW", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONG", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONG RAW", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ROWID", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UROWID", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CLOB", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCLOB", DataSourceName = "OracleCRMDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BFILE", DataSourceName = "OracleCRMDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}