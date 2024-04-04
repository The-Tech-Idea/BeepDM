using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.Linq;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Helper class for mapping data types to field names.
    /// </summary>
    public static class DataTypeFieldMappingHelper
    {
        /// <summary>
        /// A string representing a collection of .NET data types.
        /// </summary>
        public static string NetDataTypeDef1 = "byte, sbyte, int, uint, short, ushort, long, ulong, float, double, char, bool, object, string, decimal, DateTime";

        /// <summary>
        /// A string representing a list of .NET data types.
        /// </summary>
        /// <remarks>
        /// The string contains a comma-separated list of .NET data types, including:
        /// - System.Byte[]
        /// - System.SByte[]
        /// - System.Byte
        /// - System.SByte
        /// - System.Int32
        /// - System.UInt32
        /// - System.Int16
        /// - System.UInt16
        /// - System.Int64
        /// - System.UInt64
        /// - System.Single
        /// - System.Double
        /// - System.Char
        /// - System.Boolean
        /// - System.Object
        /// - System.String
        /// - System.Decimal
        /// - System.DateTime
        /// - System.TimeSpan
        /// - System.DateTimeOffset
        public static string NetDataTypeDef2 = ",System.Byte[],System.SByte[],System.Byte,System.SByte,System.Int32,System.UInt32,System.Int16,System.UInt16,System.Int64,System.UInt64,System.Single,System.Double,System.Char,System.Boolean,System.Object,System.String,System.Decimal,System.DateTime,System.TimeSpan,System.DateTimeOffset,System.Guid,System.Xml";
        /// <summary>Returns an array of .NET data types.</summary>
        /// <returns>An array of .NET data types.</returns>
        public static string[] GetNetDataTypes()
        {
            string[] a = NetDataTypeDef1.Split(',');
            Array.Sort(a);
            return a;
        }
        /// <summary>Returns an array of .NET data types.</summary>
        /// <returns>An array of .NET data types.</returns>
        public static string[] GetNetDataTypes2()
        {
            string[] a = NetDataTypeDef2.Split(',');
            Array.Sort(a);
            return a;
        }
        /// <summary>Gets the datatype mapping for a given class name, field type, entity field, and DME editor.</summary>
        /// <param name="className">The name of the class.</param>
        /// <param name="fieldType">The type of the field.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The datatype mapping for the given parameters.</returns>
        /// <remarks>
        /// This method retrieves the datatype mapping from the DME editor's configuration for a specific class name and field type.
        /// If the entity field has a size greater than zero, it checks if there is a datatype mapping with the same
        public static DatatypeMapping GetDataTypeMappingForString(string className, string fieldType, EntityField fld, IDMEEditor DMEEditor)
        {
            DatatypeMapping dt = DMEEditor.ConfigEditor.DataTypesMap
          .Where(x => x.DataSourceName.Equals(className, StringComparison.InvariantCultureIgnoreCase)
                      && x.NetDataType.Equals(fieldType, StringComparison.InvariantCultureIgnoreCase))
          .FirstOrDefault();

            if (fld.Size1 > 0)
            {
                dt = dt ?? DMEEditor.ConfigEditor.DataTypesMap
                    .Where(x => x.DataSourceName.Equals(className, StringComparison.InvariantCultureIgnoreCase)
                                && x.NetDataType.Equals(fieldType, StringComparison.InvariantCultureIgnoreCase)
                                && x.DataType.Contains("N"))
                    .FirstOrDefault();

                if (dt != null)
                {
                    dt.DataType = dt.DataType.Replace("(N)", "(" + fld.Size1.ToString() + ")");
                }
            }

            return dt;
        }
        /// <summary>Gets the data type of a field in a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataType(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            string retval = null;
            IDataSource ds;
            try
            {
                if (DSname != null)
                {
                    ds = DMEEditor.GetDataSource(DSname);
                    if (DMEEditor.ConfigEditor.DataTypesMap == null)
                    {
                        DMEEditor.ConfigEditor.ReadDataTypeFile();
                    }
                    if (!fld.fieldtype.Contains("System."))
                    {
                        retval = GetFieldTypeWoConversion(DSname, fld, DMEEditor);
                    }
                    else
                    {
                        AssemblyClassDefinition classhandler = DMEEditor.GetDataSourceClass(DSname);
                        if (classhandler != null)
                        {
                            DatatypeMapping dt = null;
                            if (fld.fieldtype.Equals("System.String", StringComparison.InvariantCultureIgnoreCase))  //-- Fist Check all Data field that Contain Length
                            {
                                if (fld.Size1 > 0) //-- String Type first
                                {
                                    dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.Fav && x.DataType.Contains("N")).FirstOrDefault();
                                    if (dt == null)
                                    {
                                        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.DataType.Contains("N")).FirstOrDefault();
                                    }
                                    if (dt != null)
                                        retval = dt.DataType.Replace("(N)", "(" + fld.Size1.ToString() + ")");
                                }
                                else
                                {
                                    dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    retval = dt.DataType;
                                }

                            }
                            else
                            if (!fld.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (fld.fieldtype.Equals("System.Decimal", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (fld.NumericPrecision == 0)
                                    {
                                        fld.NumericPrecision = 28;
                                    }
                                    if (fld.NumericScale == 0)
                                    {
                                        fld.NumericScale = 8;
                                    }

                                }
                                if (fld.NumericPrecision > 0)
                                {
                                    if (fld.NumericScale > 0)
                                    {
                                        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.Fav && x.DataType.Contains("P,S")).FirstOrDefault();
                                        if (dt == null)
                                        {
                                            dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.DataType.Contains("P,S")).FirstOrDefault();
                                        }
                                        if (dt != null)
                                        {
                                            retval = dt.DataType.Replace("(P,S)", "(" + fld.NumericPrecision.ToString() + "," + fld.NumericScale.ToString() + ")");
                                        }


                                    }
                                    else
                                    {
                                        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.Fav && x.DataType.Contains("(N)")).FirstOrDefault();
                                        if (dt != null)
                                        {
                                            dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.DataType.Contains("(N)")).FirstOrDefault();
                                        }
                                        if (dt != null)
                                        {
                                            retval = dt.DataType.Replace("(N)", "(" + fld.NumericPrecision.ToString() + ")");

                                        }
                                        else
                                        {
                                            dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.Fav && x.DataType.Contains("(P,S)")).FirstOrDefault();
                                            if (dt == null)
                                            {
                                                dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && x.DataType.Contains("(P,S)")).FirstOrDefault();
                                            }


                                        }
                                        if (dt != null)
                                        {

                                            retval = dt.DataType.Replace("(P,S)", "(" + fld.NumericPrecision.ToString() + "," + fld.NumericScale.ToString() + ")");
                                        }
                                    }
                                }

                            }
                            if (retval == null)
                            {
                                dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase) && x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (dt != null)
                                {
                                    retval = dt.DataType;
                                }

                            }
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Fail", "Could not Find Class Handler " + fld.EntityName + "_" + fld.fieldname, DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Could not Convert Field Type to Provider Type " + fld.EntityName + "_" + fld.fieldname, DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = "";
                retval = null;
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert Field Type to Provider Type " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return retval;
        }
        /// <summary>Gets the field type without conversion.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The field type without conversion.</returns>
        public static string GetFieldTypeWoConversion(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            string retval = null;
            try
            {
                if (DSname != null)
                {
                    if (DMEEditor.ConfigEditor.DataTypesMap == null)
                    {
                        DMEEditor.ConfigEditor.ReadDataTypeFile();
                    }
                    AssemblyClassDefinition classhandler = DMEEditor.GetDataSourceClass(DSname);
                    if (classhandler != null)
                    {
                        DatatypeMapping dt = null;
                        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.DataType == fld.fieldtype && x.DataType.Contains("N")).FirstOrDefault();
                        if (dt != null)
                            retval = dt.DataType.Replace("(N)", "(" + fld.Size1.ToString() + ")");
                        if (fld.NumericPrecision > 0)
                        {
                            if (fld.NumericScale > 0)
                            {
                                dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.DataType == fld.fieldtype && x.DataType.Contains("N,S")).FirstOrDefault();
                                if (dt != null)
                                {
                                    retval = dt.DataType.Replace("(N,S)", "(" + fld.NumericPrecision.ToString() + "," + fld.NumericScale.ToString() + ")");
                                }


                            }
                            else
                            {
                                dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.DataType == fld.fieldtype && x.DataType.Contains("(N)")).FirstOrDefault();
                                if (dt != null)
                                {
                                    retval = dt.DataType.Replace("(N)", "(" + fld.NumericPrecision.ToString() + ")");

                                }
                                else
                                {
                                    dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.DataType == fld.fieldtype && x.DataType.Contains("(N,S)")).FirstOrDefault();

                                }
                                if (dt != null)
                                {
                                    retval = dt.DataType.Replace("(N,S)", "(" + fld.NumericPrecision.ToString() + ",0)");
                                }

                            }
                        }
                        if (retval == null)
                        {
                            dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.DataType == fld.fieldtype).FirstOrDefault();
                            retval = dt.DataType;
                        }

                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", "Could not Find Class Handler " + fld.EntityName + "_" + fld.fieldname, DateTime.Now, -1, null, Errors.Failed);
                    }

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Could not Convert Field Type to Provider Type " + fld.EntityName + "_" + fld.fieldname, DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = "";
                retval = null;
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert Field Type to Provider Type " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return retval;
        }
        /// <summary>Returns a list of datatype mappings.</summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GetMappings()
        {
            List<DatatypeMapping> ls = new List<DatatypeMapping>();
            ls.AddRange(GenerateOracleDataTypesMapping());
            ls.AddRange(GenerateSqlServerDataTypesMapping());
            ls.AddRange(GenerateSQLiteDataTypesMapping());
            ls.AddRange(GenerateSqlCompactDataTypesMapping());
            ls.AddRange(GetPostgreDataTypesMapping());
            ls.AddRange(GetMySqlDataTypesMapping());
            ls.AddRange(GetFireBirdDataTypesMapping());
            ls.AddRange(GetLiteDBDataTypesMapping());
            ls.AddRange(GetDuckDBDataTypesMapping());
            ls.AddRange(GetDB2DataTypeMappings());
            ls.AddRange(GetMongoDBDataTypeMappings());
            ls.AddRange(GetCassandraDataTypeMappings());
            ls.AddRange(GetRedisDataTypeMappings());
            ls.AddRange(GetDynamoDBDataTypeMappings());
            ls.AddRange(GetInfluxDBDataTypeMappings());
            ls.AddRange(GetSybaseDataTypeMappings());
            ls.AddRange(GetHBaseDataTypeMappings());
            ls.AddRange(GetCockroachDBDataTypeMappings());
            ls.AddRange(GetBerkeleyDBDataTypesMapping());
            ls.AddRange(GetSnowflakeDataTypesMapping());
            ls.AddRange(GetAzureCosmosDBDataTypesMapping());
            ls.AddRange(GetVerticaDataTypesMapping());
            ls.AddRange(GetTeradataDataTypeMappings());
            ls.AddRange(GetArangoDBDataTypeMappings());
            ls.AddRange(GetInfluxDBDataTypeMappings());
            ls.AddRange(GetFirebaseDataTypeMappings());

            return ls;

        }
        /// <summary>Returns a list of datatype mappings for CouchDB.</summary>
        /// <returns>A list of datatype mappings for CouchDB.</returns>
        public static List<DatatypeMapping> GetCouchDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "CouchDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "CouchDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "CouchDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "CouchDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "CouchDBDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "CouchDBDataSource", NetDataType = "System.Object", Fav = false }
    };
        }
        /// <summary>Returns a list of Firebase data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Firebase data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetFirebaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "FirebaseDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "FirebaseDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "FirebaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Map", DataSourceName = "FirebaseDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "FirebaseDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Timestamp", DataSourceName = "FirebaseDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Geopoint", DataSourceName = "FirebaseDataSource", NetDataType = "GeopointCustomType", Fav = false }, // Replace GeopointCustomType with whatever .NET type you're using to represent a Geopoint
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Reference", DataSourceName = "FirebaseDataSource", NetDataType = "DocumentReferenceCustomType", Fav = false }, // Replace DocumentReferenceCustomType with whatever .NET type you're using to represent a Document Reference
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "FirebaseDataSource", NetDataType = "System.Object", Fav = false }
    };
        }
        /// <summary>
        /// Generates a list of datatype mappings for Oracle database.
        /// </summary>
        /// <returns>A list of datatype mappings for Oracle database.</returns>
        public static List<DatatypeMapping> GenerateOracleDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "bffb7e56-8027-4a94-aacf-4eb7f12226bf", DataType = "BFILE", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ce0cc840-dcdc-4c50-87c0-b0fcd6d7d098", DataType = "VARCHAR(N)", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "f689373e-8732-41c0-98c2-c304826420fe", DataType = "BINARY_DOUBLE", DataSourceName = "OracleDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a071d1a8-4d6b-4b69-8eb0-dc62a19a2745", DataType = "BLOB", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ddeb8169-b370-4f5f-a2a1-3a50284a06fa", DataType = "CHAR", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "42acc5ee-e831-4c98-8e95-ff12d37a098c", DataType = "CLOB", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5256a22f-95f8-44a2-ab22-18e6896b59c6", DataType = "INTERVAL DAY TO SECOND", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "206862cd-f204-4fae-93c0-82cb8578b1f5", DataType = "INTERVAL YEAR TO MONTH", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "fbe447b3-6e11-4a29-bfe9-c32e7f646d24", DataType = "LONG", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "62da0061-76f7-42f6-b29e-cb9421f3c470", DataType = "LONG RAW", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "6637f246-5998-4aad-b0be-a4d4bb60ea6a", DataType = "NCHAR(N)", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ec2f27a6-9759-4710-9406-659ab8265094", DataType = "NUMBER(P,S)", DataSourceName = "OracleDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "1819ee57-7c3b-4598-a7f7-ba37b9425cb3", DataType = "PLS_INTEGER", DataSourceName = "OracleDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b48b34c4-12d3-4ed2-ba50-88af84e0fbaa", DataType = "RAW", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "6c4edb03-1d0d-4fd1-8879-f7d35b54df1c", DataType = "REF", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "cd303042-afae-4d5c-a451-97c2098793eb", DataType = "ROWID", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a6768c2c-d283-4027-8346-4a608558e55d", DataType = "TIMESTAMP", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "468fe152-227c-47a0-a3a0-ac675eaa8b49", DataType = "TIMESTAMP WITH LOCAL TIME ZONE", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "d45036a5-17b2-495e-b8b0-d0a2e46ea9a2", DataType = "UROWID", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "dfac1056-5c99-40ef-b76d-b60ec95eb777", DataType = "NCLOB", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },

             // Float mapping (as previously discussed)
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(), // Provide a unique GUID
            DataType = "FLOAT",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Double", // or System.Double based on the precision you require
            Fav = false
        },

        // Additional mappings

        // INTEGER (Oracle's INTEGER is a synonym for NUMBER(38), can be safely mapped to C# int or long based on the size)
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "INTEGER",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Int32", // or System.Int64 if you expect large values
            Fav = false
        },

        // SMALLINT
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "SMALLINT",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Int16",
            Fav = false
        },

        // REAL
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "REAL",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Single",
            Fav = false
        },

        // DOUBLE PRECISION
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "DOUBLE PRECISION",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Double",
            Fav = false
        },

        // BINARY_FLOAT
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "BINARY_FLOAT",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Single",
            Fav = false
        },

        // BINARY_DOUBLE
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "BINARY_DOUBLE",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.Double",
            Fav = false
        },

        // VARCHAR2
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "VARCHAR2",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.String",
            Fav = false
        },

        // NVARCHAR2
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "NVARCHAR2",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.String",
            Fav = false
        },

        // DATE (Oracle DATE includes time as well)
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "DATE",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.DateTime",
            Fav = false
        },

        // TIMESTAMP WITH TIME ZONE
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "TIMESTAMP WITH TIME ZONE",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.DateTimeOffset",
            Fav = false
        },

        // XMLTYPE
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "XMLTYPE",
            DataSourceName = "OracleDataSource",
            NetDataType = "System.String", // Or another data type that can handle XML content
            Fav = false
        },

        };
        }
        /// <summary>
        /// Generates a list of datatype mappings for SQLite.
        /// </summary>
        /// <returns>A list of datatype mappings for SQLite.</returns>
        public static List<DatatypeMapping> GenerateSQLiteDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "cd1cb478-2eee-43ee-bb76-d924086a1e79", DataType = "BOOLEAN", DataSourceName = "SQLiteDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5fa77cbf-2d1d-40a9-b429-d53e5d9996cc", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "601f3268-7001-43cf-a7ca-a7b91d19916c", DataType = "BLOB", DataSourceName = "SQLiteDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "42d04b99-132c-4639-b8db-1333a88de869", DataType = "CHARACTER(1)", DataSourceName = "SQLiteDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a989fa08-53e9-48f8-bef9-9b242a6934e1", DataType = "DATETIME", DataSourceName = "SQLiteDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "de7ca8c8-9658-4591-b564-459827360284", DataType = "TEXT", DataSourceName = "SQLiteDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "eb1c8f4d-ab33-420e-b099-cc73effa161c", DataType = "NUMERIC", DataSourceName = "SQLiteDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ecbfc523-5654-465c-8c46-870056d50e0d", DataType = "REAL", DataSourceName = "SQLiteDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5adeb0b4-4d07-4ca0-a161-6db7a4ce67ae", DataType = "TEXT", DataSourceName = "SQLiteDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bb0bfc0a-ee18-4228-9a3a-5924d5af1344", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "46902969-44d1-477e-acdb-0c52265f71e3", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7253187c-9dbb-4902-9d1b-19a8b1d77181", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a6bff8c1-5ebc-4c8d-9a72-757ba63321f4", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "10a8bf0d-6e20-428f-b970-7e41a4b5e754", DataType = "REAL", DataSourceName = "SQLiteDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "d331a968-d394-437c-a060-977da08ff0cf", DataType = "VARCHAR(N)", DataSourceName = "SQLiteDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "33aecc69-d54e-4d65-85b5-89b33b2e4b25", DataType = "TEXT", DataSourceName = "SQLiteDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b5adba16-90d6-4b4a-a6f3-c64c5ed36bc1", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b3abdcd5-662e-40a9-93e2-0b6ade2348de", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "9295b8a4-ce98-42eb-bd97-a56ba1313cd5", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.UInt64", Fav = false },
        };
        }
        /// <summary>
        /// Generates a list of datatype mappings between SQL Server data types and corresponding .NET data types.
        /// </summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GenerateSqlServerDataTypesMapping()
        {
            List<DatatypeMapping> mappings = new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 1, GuidID = Guid.NewGuid().ToString(), DataType = "bigint", DataSourceName = "SQLServerDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 2, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 3, GuidID = Guid.NewGuid().ToString(), DataType = "bit", DataSourceName = "SQLServerDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 4, GuidID = Guid.NewGuid().ToString(), DataType = "char", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 5, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "SQLServerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 6, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "SQLServerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 7, GuidID = Guid.NewGuid().ToString(), DataType = "datetime2", DataSourceName = "SQLServerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 8, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "SQLServerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 9, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "SQLServerDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 10, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 11, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "SQLServerDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 12, GuidID = Guid.NewGuid().ToString(), DataType = "money", DataSourceName = "SQLServerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 13, GuidID = Guid.NewGuid().ToString(), DataType = "nchar", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 14, GuidID = Guid.NewGuid().ToString(), DataType = "ntext", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 15, GuidID = Guid.NewGuid().ToString(), DataType = "numeric", DataSourceName = "SQLServerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 16, GuidID = Guid.NewGuid().ToString(), DataType = "nvarchar", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 17, GuidID = Guid.NewGuid().ToString(), DataType = "real", DataSourceName = "SQLServerDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 18, GuidID = Guid.NewGuid().ToString(), DataType = "smallint", DataSourceName = "SQLServerDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 19, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 20, GuidID = Guid.NewGuid().ToString(), DataType = "tinyint", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 21, GuidID = Guid.NewGuid().ToString(), DataType = "varbinary", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 22, GuidID = Guid.NewGuid().ToString(), DataType = "varchar", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 23, GuidID = Guid.NewGuid().ToString(), DataType = "xml", DataSourceName = "SQLServerDataSource", NetDataType = "System.Xml.XmlDocument", Fav = false },
                // smallmoney
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "smallmoney",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "System.Decimal",
            Fav = false
        },

        // datetimeoffset
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "datetimeoffset",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "System.DateTimeOffset",
            Fav = false
        },

        // time
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "time",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "System.TimeSpan",
            Fav = false
        },

        // uniqueidentifier
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "uniqueidentifier",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "System.Guid",
            Fav = false
        },

        // sql_variant (can be complex to handle as it can store multiple data types)
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "sql_variant",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "System.Object",
            Fav = false
        },

        // geography
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "geography",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "Microsoft.SqlServer.Types.SqlGeography",
            Fav = false
        },

        // geometry
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "geometry",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "Microsoft.SqlServer.Types.SqlGeometry",
            Fav = false
        },

        // hierarchyid
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "hierarchyid",
            DataSourceName = "SQLServerDataSource",
            NetDataType = "Microsoft.SqlServer.Types.SqlHierarchyId",
            Fav = false
        }

            };

            return mappings;
        }
        /// <summary>
        /// Generates a list of datatype mappings for SQL Server Compact Edition.
        /// </summary>
        /// <returns>A list of datatype mappings for SQL Server Compact Edition.</returns>
        public static List<DatatypeMapping> GenerateSqlCompactDataTypesMapping()
        {
            return new List<DatatypeMapping>
            {
    new DatatypeMapping { ID = 0, GuidID = "584adb89-91fd-4b89-9c88-8ff5ca2c12e8", DataType = "bit", DataSourceName = "DatatypeMapping", NetDataType = "System.Boolean", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "8124aa72-967a-4df3-aa99-f9058cdf58cb", DataType = "tinyint", DataSourceName = "DatatypeMapping", NetDataType = "System.Byte", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "9a542d3b-5dd9-499f-8e4d-1ca90cf03957", DataType = "numeric(3,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.SByte", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "73cef364-9363-4420-85c3-22122aa37e2d", DataType = "bit", DataSourceName = "DatatypeMapping", NetDataType = "System.Boolean", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "9237c66a-a4f1-4f65-88df-5aca6d8921e9", DataType = "nchar(1)", DataSourceName = "DatatypeMapping", NetDataType = "System.Char", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "48a805c9-f4f6-471a-bd40-772b610d0335", DataType = "numeric(19,4)", DataSourceName = "DatatypeMapping", NetDataType = "System.Decimal", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "d7dc53fc-e5d5-4e38-9913-4b0d45534f98", DataType = "float", DataSourceName = "DatatypeMapping", NetDataType = "System.Double", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "185295f2-d10b-49ff-8261-5f7b915457a8", DataType = "real", DataSourceName = "DatatypeMapping", NetDataType = "System.Single", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "8f4befbe-bf9c-4577-8063-a8157310f4ce", DataType = "bit", DataSourceName = "DatatypeMapping", NetDataType = "System.Boolean", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "6c8a507d-6035-4d55-ba6f-0af597656ff7", DataType = "smallint", DataSourceName = "DatatypeMapping", NetDataType = "System.Int16", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "7469dc2e-b422-4331-9617-e8de81943e4e", DataType = "numeric(5,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.UInt16", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "6616e01d-a324-48db-8c6c-b2aac9a946b8", DataType = "int", DataSourceName = "DatatypeMapping", NetDataType = "System.Int32", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "6de0d55e-5ad8-46bd-99e4-a48db7e8506b", DataType = "numeric(10,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.UInt32", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "c04dae52-8da0-4846-901f-b7bccc9d4011", DataType = "bigint", DataSourceName = "DatatypeMapping", NetDataType = "System.Int64", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "978c7ad0-6194-4c89-96c1-80c081e06618", DataType = "numeric(20,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.UInt64", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "ca9d585c-75bf-406f-ae6e-08778f9a2f04", DataType = "uniqueidentifier", DataSourceName = "DatatypeMapping", NetDataType = "System.Guid", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "bc523cdf-3fcd-4a04-b48c-847a6cfcfcaa", DataType = "nvarchar(N)", DataSourceName = "DatatypeMapping", NetDataType = "System.String", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "71d52da7-0569-4945-87a9-06b6c09f16c9", DataType = "datetime", DataSourceName = "DatatypeMapping", NetDataType = "System.DateTime", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "0ec10616-b8d8-4eaf-bba7-b9f480c7d2ae", DataType = "float", DataSourceName = "DatatypeMapping", NetDataType = "System.TimeSpan", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "22629847-6984-4752-976b-8347c94799ea", DataType = "varbinary(N)", DataSourceName = "DatatypeMapping", NetDataType = "System.Byte[]", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "055eecb6-7dde-4c64-beff-04d6d3327241", DataType = "Image", DataSourceName = "DatatypeMapping", NetDataType = "System.Byte[]", Fav = false },
    new DatatypeMapping { ID = 0, GuidID = "afa57ab4-d24d-47ae-853c-88066a0181a6", DataType = "ntext", DataSourceName = "DatatypeMapping", NetDataType = "System.String", Fav = false }
};

        }
        /// <summary>Returns a list of datatype mappings for PostgreSQL.</summary>
        /// <returns>A list of datatype mappings for PostgreSQL.</returns>
        public static List<DatatypeMapping> GetPostgreDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "a0462937-cf4d-48cd-88ab-406f195ab256", DataType = "bool", DataSourceName = "PostgreDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8e057539-b9e4-4a42-8fa0-8647293872a2", DataType = "smallint", DataSourceName = "PostgreDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bfcdab96-6f35-4513-bc00-2487fca0b064", DataType = "smallint", DataSourceName = "PostgreDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "cd4b8eae-4093-42a1-b907-511a07f71ca9", DataType = "char(1)", DataSourceName = "PostgreDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "4ec6d7b5-fd5b-42eb-83e1-03a2a1233b96", DataType = "decimal(28,8)", DataSourceName = "PostgreDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b82f13b0-d933-475c-a9b9-6e14b5008284", DataType = "double precision", DataSourceName = "PostgreDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a068c9d5-5618-4a61-b2a3-a6d8472aef69", DataType = "real", DataSourceName = "PostgreDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "f92ddd3c-302a-44e6-93d4-1ae77cd74d5b", DataType = "smallint", DataSourceName = "PostgreDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e4eb09bd-38f0-4cf2-884b-01df85f13ef2", DataType = "numeric(5,0)", DataSourceName = "PostgreDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7b87f4ad-2114-4634-aed7-ed83f60df1fe", DataType = "int", DataSourceName = "PostgreDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "040fac6c-f381-405e-9950-fc83fbfe0aac", DataType = "numeric(10,0)", DataSourceName = "PostgreDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "2da3fe2d-3a4e-479f-af4a-010673048bea", DataType = "bigint", DataSourceName = "PostgreDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ab4f85b2-9559-4f11-86b1-83b0bedb1803", DataType = "numeric(20,0)", DataSourceName = "PostgreDataSource", NetDataType = "System.UInt64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "549d44ac-05d1-4fab-8e83-e46b2f3aa351", DataType = "uuid", DataSourceName = "PostgreDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "2bf31711-81df-4076-85e3-48270dca1b33", DataType = "varchar(N)", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "a756ee8d-af4c-44c8-8308-f38e7d5a3a6d", DataType = "timestamp", DataSourceName = "PostgreDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a7329dde-29ad-40f0-93a6-dc4830131543", DataType = "double precision", DataSourceName = "PostgreDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e1efb191-fdb1-443e-a969-8492a45b6744", DataType = "bytea", DataSourceName = "PostgreDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bytea", DataSourceName = "PostgreDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp without time zone", DataSourceName = "PostgreDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp with time zone", DataSourceName = "PostgreDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "interval", DataSourceName = "PostgreDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "jsonb", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "cidr", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "inet", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "macaddr", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tsvector", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tsquery", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "xml", DataSourceName = "PostgreDataSource", NetDataType = "System.Xml.Linq.XElement", Fav = false },
        // Add more mappings as needed

        };
        }
        /// <summary>Returns a list of datatype mappings between MySQL and .NET data types.</summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GetMySqlDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "0f2dda35-a3f7-4d73-8ae9-50d5d2ebc9a6", DataType = "bit", DataSourceName = "MySQLDataSource", NetDataType = "System.Boolean", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "1715cf14-1d7a-46fd-abee-9fbf0e97fb1d", DataType = "tiny unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "43139130-3531-4252-8e2c-efd8999849f6", DataType = "tinyint", DataSourceName = "MySQLDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bd1c2f8c-6c00-4545-a250-98cd706d7520", DataType = "char", DataSourceName = "MySQLDataSource", NetDataType = "System.Char", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "7c1380a6-b976-4896-ac61-c1dd95829128", DataType = "decimal", DataSourceName = "MySQLDataSource", NetDataType = "System.Decimal", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "4340401d-6058-47fe-98bb-2c92bf37ffb7", DataType = "double", DataSourceName = "MySQLDataSource", NetDataType = "System.Double", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "e225d674-0f64-432e-86bb-7c6f25f15362", DataType = "real", DataSourceName = "MySQLDataSource", NetDataType = "System.Single", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "e52090b4-dff2-403d-9bf6-dc95e92380ca", DataType = "smallint", DataSourceName = "MySQLDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a0fe7651-e9f4-441f-bc73-28defc35ad57", DataType = "smallint unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "42affd50-333b-4a84-803a-fb2803575e36", DataType = "int", DataSourceName = "MySQLDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "918c4769-b8fe-4175-9c94-089e9b7d21a9", DataType = "int unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "111cf764-016d-40e3-97c1-881f0f212e66", DataType = "bigint", DataSourceName = "MySQLDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "63ab5e03-0a2d-40d1-ae46-b3cc86902ed1", DataType = "bigint unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.UInt64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b28dd595-d0a8-4703-bae7-b97939d3d20f", DataType = "char(38)", DataSourceName = "MySQLDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "daf8b650-039e-4519-bc67-4dcf860faa66", DataType = "varchar(N)", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "50d0c48f-8b86-48f2-94db-d3aa4bf700de", DataType = "datetime(6)", DataSourceName = "MySQLDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bfdcffea-bf66-499b-93be-d8cd22382458", DataType = "double", DataSourceName = "MySQLDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "6825dddb-4459-4c2f-b3b3-fe750d1988e0", DataType = "TINYBLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "78627e34-63b8-4ab2-b31c-84f28256b481", DataType = "BLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "8bc228bc-301f-40c5-a8a6-5ca44c83a5e5", DataType = "MEDIUMBLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e4b66269-b72d-492a-80c5-31d61a455ff7", DataType = "LONGBLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = false },
             // Additional mappings
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tinytext", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "mediumtext", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "longtext", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enum", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "MySQLDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "time", DataSourceName = "MySQLDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "year", DataSourceName = "MySQLDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
        // Add more mappings as needed
        };
        }
        /// <summary>Returns a list of datatype mappings for Firebird database.</summary>
        /// <returns>A list of datatype mappings for Firebird database.</returns>
        public static List<DatatypeMapping> GetFireBirdDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "18399b26-ec58-494a-b018-39e852e12439", DataType = "char(1)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "51a6afdf-c4c8-4972-b4e4-847697a0f0f0", DataType = "numeric(3,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b4a0040f-beed-4e46-98eb-3b364b09490c", DataType = "numeric(3,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "711bc9fe-f122-49e5-a8e7-747619b82928", DataType = "char", DataSourceName = "FireBirdDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "c9edda9b-1710-4f09-ad6d-9d4c0c620359", DataType = "decimal(18,4)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "4428d11c-ffb7-41ce-84c7-9e2f4905eba1", DataType = "double precision", DataSourceName = "FireBirdDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "25290082-e54f-45de-bb8b-2fc0dee541db", DataType = "float", DataSourceName = "FireBirdDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "96ef68ab-ee2f-4436-a5f9-2f8254cb8556", DataType = "small int", DataSourceName = "FireBirdDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "702f96ae-53a3-449d-af0c-4dccf3ae05a3", DataType = "numeric(5,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "249d4f90-67d6-4b5e-bd05-d208da821bce", DataType = "integer", DataSourceName = "FireBirdDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e4a808f2-494d-4600-b8c2-ae3816b5097c", DataType = "numeric(10,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8b33c04e-e841-49ed-a338-4beca3fee468", DataType = "bigint", DataSourceName = "FireBirdDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8bd33d9d-9e76-4b3d-86de-d872d1c4b9cf", DataType = "numeric(18,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.UInt64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "4b38b9a2-4fa1-4ccd-bde8-72d62264c238", DataType = "char(36)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5881f031-4e6c-4a38-b090-f62c25928ab7", DataType = "char(N)", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "86dd903d-0ef3-40de-9f2a-3b4536fd2b79", DataType = "timestamp", DataSourceName = "FireBirdDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "c5c155d3-af68-4c59-8afc-860ddcbe5888", DataType = "double precision", DataSourceName = "FireBirdDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a77ff872-fbf9-4e0d-b1f5-50fc2c6036bb", DataType = "BLOB", DataSourceName = "FireBirdDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "28dd3198-4a74-4d4c-a078-65a62ca605cf", DataType = "TEXT", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = false },
             // Additional mappings
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = true },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "FireBirdDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "FireBirdDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "FireBirdDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "FireBirdDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB SUB_TYPE TEXT", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB SUB_TYPE BINARY", DataSourceName = "FireBirdDataSource", NetDataType = "System.Byte[]", Fav = false },
        // Add more mappings as needed
        };
        }
        /// <summary>Returns a list of LiteDB data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between LiteDB data types and their corresponding .NET data types.</returns>
        public static List<DatatypeMapping> GetLiteDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "0e375c24-4d38-4bb3-98c2-35d89aaf77fe", DataType = "Int32", DataSourceName = "LiteDBDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "88fa3009-285d-4d15-9da7-aa2a2f1abc97", DataType = "Int64", DataSourceName = "LiteDBDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "eaec481e-6975-47a8-b4ff-112441a517a1", DataType = "Double", DataSourceName = "LiteDBDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "0da22d75-e558-4066-ad6d-cfb41b695e02", DataType = "String", DataSourceName = "LiteDBDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "9758daa0-2c49-4224-b8a1-569b352728db", DataType = "Document", DataSourceName = "LiteDBDataSource", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7129a003-2550-4f3c-9c48-8ed208cd1a30", DataType = "Array", DataSourceName = "LiteDBDataSource", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "11ba6cb3-bde6-4267-8833-59d434babb00", DataType = "Binary", DataSourceName = "LiteDBDataSource", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "3258239f-0e87-4e1c-8d1f-72bf50abafa9", DataType = "ObjectId", DataSourceName = "LiteDBDataSource", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b51bec25-90d5-4990-bf78-1c4b24b68b30", DataType = "Guid", DataSourceName = "LiteDBDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7b99280e-9912-4c87-87b5-be571ea98555", DataType = "DateTime", DataSourceName = "LiteDBDataSource", Fav = false }
        };
        }
        /// <summary>Returns a list of datatype mappings for DuckDB.</summary>
        /// <returns>A list of datatype mappings for DuckDB.</returns>
        public static List<DatatypeMapping> GetDuckDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
            {
            new DatatypeMapping { ID = 0, GuidID = "dda45fdb-d6e6-4f34-8b70-04ce2cf8ed46", DataType = "BOOLEAN", DataSourceName = "DuckDBDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8e8c7dd3-8104-4b94-83b9-d8f0ffa815ab", DataType = "TINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "65188632-bfd4-4845-91c9-685fca0126bb", DataType = "BLOB", DataSourceName = "DuckDBDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "eec16db7-652f-4eb4-8638-4237595f87be", DataType = "VARCHAR", DataSourceName = "DuckDBDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7c90542c-9627-4be6-9a88-85a2971684b3", DataType = "TIMESTAMP", DataSourceName = "DuckDBDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "93a6e1af-f633-408d-8b42-fdf6249e734c", DataType = "DECIMAL", DataSourceName = "DuckDBDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "cf14597f-c353-47b2-9b0d-abdca99e9576", DataType = "DOUBLE", DataSourceName = "DuckDBDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "78179a6e-a90a-4e79-8138-20212396eb71", DataType = "UUID", DataSourceName = "DuckDBDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "154658eb-a5a7-468c-98da-e483de388200", DataType = "SMALLINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "23743f61-47d2-41bb-95a6-26b3ef6fa9a2", DataType = "INTEGER", DataSourceName = "DuckDBDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "1da0e8db-0cc1-4a12-95a4-debb63f12c38", DataType = "BIGINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "11aa1eba-ac25-4b86-ac80-2a0c76200f39", DataType = "BLOB", DataSourceName = "DuckDBDataSource", NetDataType = "System.Object", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "9842130c-a655-46b8-9840-40ed3808d51d", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "dff6fbef-1c7b-4730-a37f-aae5d73d9738", DataType = "BLOB", DataSourceName = "DuckDBDataSource", NetDataType = "System.SByte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5b56a4d4-38e8-4c8f-85b1-c4073efd8c42", DataType = "REAL", DataSourceName = "DuckDBDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b6890f86-c0be-4b72-8411-13bda33e6e49", DataType = "INTERVAL", DataSourceName = "DuckDBDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e3e0b246-46bf-452b-a56c-1724c1e6f1c4", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "29f78f94-4295-4c55-aa26-1371d2d07a67", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e72647f9-2e3f-46a6-a044-5a33b4b9d3e6", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.UInt64", Fav = false },
             new DatatypeMapping { ID = 0, GuidID = "eec16db7-652f-4eb4-8638-4237595f87be", DataType = "VARCHAR", DataSourceName = "DuckDBDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 3, GuidID =Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "DuckDBDataSource", NetDataType = "System.String", Fav = true },
           new DatatypeMapping { ID = 4, GuidID =Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Single", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "08c661ba-0247-466b-9984-0ccb4f3eda89", DataType = "VARCHAR", DataSourceName = "DuckDBDataSource", NetDataType = "System.Xml", Fav = false }
            };

        }
        /// <summary>Returns a list of datatype mappings for DB2.</summary>
        /// <returns>A list of datatype mappings for DB2.</returns>
        public static List<DatatypeMapping> GetDB2DataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CLOB", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "DB2DataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "DB2DataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "DB2DataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "DB2DataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "DB2DataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "DB2DataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "DB2DataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "DB2DataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "DB2DataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "DB2DataSource", NetDataType = "System.Byte[]", Fav = false },
         // Additional mappings
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GRAPHIC", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARGRAPHIC", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DBCLOB", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECFLOAT", DataSourceName = "DB2DataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "DB2DataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "XML", DataSourceName = "DB2DataSource", NetDataType = "System.Xml.Linq.XElement", Fav = false },
        // You might need to handle XML data type specifically depending on your use case
        // Add more mappings as needed

    };
        }
        /// <summary>Returns a list of MongoDB data type mappings.</summary>
        /// <returns>A list of DataTypeMapping objects representing the mappings between .NET data types and MongoDB data types.</returns>
        public static List<DatatypeMapping> GetMongoDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ObjectId", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.ObjectId", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "MongoDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int32", DataSourceName = "MongoDBDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int64", DataSourceName = "MongoDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "MongoDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "MongoDBDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "MongoDBDataSource", NetDataType = "System.Collections.Generic.List<>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "MongoDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "MongoDBDataSource", NetDataType = "System.DBNull", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "RegularExpression", DataSourceName = "MongoDBDataSource", NetDataType = "System.Text.RegularExpressions.Regex", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Document", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.BsonDocument", Fav = false },
          // Additional mappings
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal128", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.Decimal128", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JavaScript", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JavaScriptWithScope", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.BsonJavaScriptWithScope", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MaxKey", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.BsonMaxKey", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MinKey", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.BsonMinKey", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ObjectId", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.ObjectId", Fav = true },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Symbol", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.BsonSymbol", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Timestamp", DataSourceName = "MongoDBDataSource", NetDataType = "MongoDB.Bson.BsonTimestamp", Fav = false },

    };
        }
        /// <summary>Returns a list of mappings between .NET data types and Cassandra data types.</summary>
        /// <returns>A list of <see cref="DatatypeMapping"/> objects representing the mappings.</returns>
        public static List<DatatypeMapping> GetCassandraDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ascii", DataSourceName = "CassandraDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bigint", DataSourceName = "CassandraDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "CassandraDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "CassandraDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "CassandraDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "CassandraDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "CassandraDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "CassandraDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "CassandraDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "uuid", DataSourceName = "CassandraDataSource", NetDataType = "System.Guid", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "varint", DataSourceName = "CassandraDataSource", NetDataType = "System.Numerics.BigInteger", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "CassandraDataSource", NetDataType = "System.Collections.Generic.List<>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "map", DataSourceName = "CassandraDataSource", NetDataType = "System.Collections.Generic.Dictionary<,>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "CassandraDataSource", NetDataType = "System.Collections.Generic.HashSet<>", Fav = false },
         // Additional mappings
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "blob", DataSourceName = "CassandraDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "counter", DataSourceName = "CassandraDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "CassandraDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "inet", DataSourceName = "CassandraDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "smallint", DataSourceName = "CassandraDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "time", DataSourceName = "CassandraDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tinyint", DataSourceName = "CassandraDataSource", NetDataType = "System.Byte", Fav = false },


    };
        }
        /// <summary>Returns a list of Redis data type mappings.</summary>
        /// <returns>A list of Redis data type mappings.</returns>
        public static List<DatatypeMapping> GetRedisDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "RedisDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.List<System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.HashSet<System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "sorted set", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.SortedSet<System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hash", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.Dictionary<System.String, System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bitmap", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.BitArray", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hyperloglog", DataSourceName = "RedisDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "geospatial index", DataSourceName = "RedisDataSource", NetDataType = "System.Object", Fav = false },
          new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "RedisDataSource", NetDataType = "System.String", Fav = false }
            };
        }
        /// <summary>Returns a list of Couchbase data type mappings.</summary>
        /// <returns>A list of Couchbase data type mappings.</returns>
        public static List<DatatypeMapping> GetCouchbaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
       new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON Document", DataSourceName = "CouchbaseDataSource", NetDataType = "Newtonsoft.Json.Linq.JObject", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "CouchbaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Collections.Generic.List<dynamic>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Dynamic.ExpandoObject", Fav = false }, // More specific type for dynamic objects
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "CouchbaseDataSource", NetDataType = "System.DateTime", Fav = false }
    };
        }
        /// <summary>Returns a list of DynamoDB data type mappings.</summary>
        /// <returns>A list of DynamoDB data type mappings.</returns>
        public static List<DatatypeMapping> GetDynamoDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "DynamoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "List", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Map", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String Set", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.HashSet<string>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number Set", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.HashSet<double>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary Set", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.HashSet<byte[]>", Fav = false }
    };
        }
        /// <summary>Returns a list of datatype mappings for InfluxDB.</summary>
        /// <returns>A list of datatype mappings for InfluxDB.</returns>
        public static List<DatatypeMapping> GetInfluxDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "InfluxDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "InfluxDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "InfluxDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "InfluxDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Timestamp", DataSourceName = "InfluxDBDataSource", NetDataType = "System.DateTime", Fav = false }
    };
        }
        /// <summary>Returns a list of datatype mappings for Sybase database.</summary>
        /// <returns>A list of datatype mappings for Sybase database.</returns>
        public static List<DatatypeMapping> GetSybaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "SybaseDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "SybaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "SybaseDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "SybaseDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "SybaseDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MONEY", DataSourceName = "SybaseDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "SybaseDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "SybaseDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "SybaseDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IMAGE", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UNICHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UNIVARCHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "SybaseDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLMONEY", DataSourceName = "SybaseDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLDATETIME", DataSourceName = "SybaseDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }
        /// <summary>Returns a list of HBase data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between HBase data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetHBaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Bytes", DataSourceName = "HBaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "HBaseDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int32", DataSourceName = "HBaseDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int64", DataSourceName = "HBaseDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "HBaseDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "HBaseDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "HBaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "HBaseDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }
        /// <summary>Returns a list of datatype mappings for CockroachDB.</summary>
        /// <returns>A list of datatype mappings for CockroachDB.</returns>
        public static List<DatatypeMapping> GetCockroachDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "CockroachDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "CockroachDBDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Guid", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Array", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "CockroachDBDataSource", NetDataType = "Newtonsoft.Json.Linq.JObject", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSONB", DataSourceName = "CockroachDBDataSource", NetDataType = "Newtonsoft.Json.Linq.JObject", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INET", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ENUM", DataSourceName = "CockroachDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Guid", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SERIAL", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "CockroachDBDataSource", NetDataType = "System.TimeSpan", Fav = false }
    };
        }
        /// <summary>Returns a list of datatype mappings for Berkeley DB.</summary>
        /// <returns>A list of datatype mappings for Berkeley DB.</returns>
        public static List<DatatypeMapping> GetBerkeleyDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Key", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Value", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StringKey", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StringValue", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IntKey", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IntValue", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Int32", Fav = false }
    };
        }
        /// <summary>Returns a list of Snowflake data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Snowflake data types and their corresponding .NET data types.</returns>
        public static List<DatatypeMapping> GetSnowflakeDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "SnowflakeDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "SnowflakeDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OBJECT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOGRAPHY", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARIANT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MONEY", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Decimal", Fav = false }
    };
        }
        /// <summary>Returns a list of Azure Cosmos DB data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Azure Cosmos DB data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetAzureCosmosDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AzureCosmosDB", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "AzureCosmosDB", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "AzureCosmosDB", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "AzureCosmosDB", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "AzureCosmosDB", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "AzureCosmosDB", NetDataType = "System.Collections.Generic.List<System.Object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "AzureCosmosDB", NetDataType = "System.DateTime", Fav = false }
    };
        }
        /// <summary>Returns a list of datatype mappings for Vertica database.</summary>
        /// <returns>A list of datatype mappings for Vertica database.</returns>
        public static List<DatatypeMapping> GetVerticaDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "VerticaDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "VerticaDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "VerticaDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "VerticaDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE PRECISION", DataSourceName = "VerticaDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "VerticaDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "VerticaDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "VerticaDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "VerticaDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "VerticaDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "VerticaDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "VerticaDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "VerticaDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "VerticaDataSource", NetDataType = "System.Guid", Fav = false }
    };
        }
        /// <summary>Returns a list of Teradata data type mappings.</summary>
        /// <returns>A list of Teradata data type mappings.</returns>
        public static List<DatatypeMapping> GetTeradataDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTEINT", DataSourceName = "TeradataDataSource", NetDataType = "System.SByte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "TeradataDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "TeradataDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "TeradataDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "TeradataDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "TeradataDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "TeradataDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "TeradataDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "TeradataDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "TeradataDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "TeradataDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTE", DataSourceName = "TeradataDataSource", NetDataType = "System.Byte[]", Fav = false },
    };
        }
        /// <summary>Returns a list of datatype mappings for ArangoDB.</summary>
        /// <returns>A list of datatype mappings for ArangoDB.</returns>
        public static List<DatatypeMapping> GetArangoDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ArangoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Object", Fav = false },
    };
        }
    }

}
