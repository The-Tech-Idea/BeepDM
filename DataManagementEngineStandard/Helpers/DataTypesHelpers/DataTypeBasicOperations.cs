using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Helper class for basic .NET data type operations and utilities.
    /// </summary>
    public static class DataTypeBasicOperations
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
        /// </remarks>
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

        /// <summary>
        /// Gets a custom data type using a custom converter function.
        /// </summary>
        /// <param name="DSname">Data source name</param>
        /// <param name="fld">Entity field</param>
        /// <param name="DMEEditor">DME Editor instance</param>
        /// <param name="customTypeConverter">Custom type converter function</param>
        /// <returns>Custom data type string</returns>
        public static string GetCustomDataType(string DSname, EntityField fld, IDMEEditor DMEEditor, Func<string, string> customTypeConverter)
        {
            string standardType = DataTypeMappingLookup.GetDataType(DSname, fld, DMEEditor);
            return customTypeConverter?.Invoke(standardType) ?? standardType;
        }

        /// <summary>
        /// Validates if a field mapping is valid for the given data source.
        /// </summary>
        /// <param name="DSname">Data source name</param>
        /// <param name="fld">Entity field</param>
        /// <param name="DMEEditor">DME Editor instance</param>
        /// <returns>True if valid field mapping exists</returns>
        public static bool IsValidFieldMapping(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            string dataType = DataTypeMappingLookup.GetDataType(DSname, fld, DMEEditor);
            return !string.IsNullOrEmpty(dataType);
        }
    }
}