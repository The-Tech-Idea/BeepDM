using TheTechIdea.Beep.DataBase;
using System;
using System.Collections.Generic;
using TheTechIdea.Util;
using System.Linq;
using DataManagementModels.DriversConfigurations;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Helper class for mapping data types to field names.
    /// </summary>
    public class DataTypesHelper : IDataTypesHelper
    {

        /// <summary>Initializes a new instance of the DataTypesHelper class.</summary>
        /// <param name="pDMEEditor">The IDMEEditor instance to be associated with the helper.</param>
        public DataTypesHelper(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }

        /// <summary>Gets or sets the DME editor.</summary>
        /// <value>The DME editor.</value>
        public IDMEEditor DMEEditor { get; set; }
        /// <summary>Gets or sets the list of datatype mappings.</summary>
        /// <value>The list of datatype mappings.</value>
        public List<DatatypeMapping> mapping { get; set; }
        //    private string NetDataTypeDef1 = "byte,sbyte,int,uint,short,ushort,long,ulong,float,double,char,bool,object,string,decimal,DateTime";
        //   private string NetDataTypeDef2 = ",System.Byte[],System.SByte[],System.Byte,System.SByte,System.Int32,System.UInt32,System.Int16,System.UInt16,System.Int64,System.UInt64,System.Single,System.Double,System.Char,System.Boolean,System.Object,System.String,System.Decimal,System.DateTime,System.TimeSpan,System.DateTimeOffset,System.Guid,System.Xml";
        private bool disposedValue;

        /// <summary>Gets a list of data classes from the configuration editor.</summary>
        /// <returns>A list of data classes.</returns>
        public List<string> GetDataClasses()
        {
            return DMEEditor.ConfigEditor.DataSourcesClasses.Select(p => p.className).ToList(); ;
        }
        /// <summary>Gets the data type of a field in a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public string GetDataType(string DSname, EntityField fld)
        {

            return DataTypeFieldMappingHelper.GetDataType(DSname, fld, DMEEditor);
        }
        /// <summary>Gets the field type without conversion.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The field type without conversion.</returns>
        public string GetFieldTypeWoConversion(string DSname, EntityField fld)
        {

            return DataTypeFieldMappingHelper.GetFieldTypeWoConversion(DSname, fld, DMEEditor);
        }
        /// <summary>Returns an array of .NET data types.</summary>
        /// <returns>An array of .NET data types.</returns>
        public string[] GetNetDataTypes()
        {
            //string[] a = NetDataTypeDef1.Split(',');
            //Array.Sort(a);
            return DataTypeFieldMappingHelper.GetNetDataTypes();
        }
        /// <summary>Returns an array of .NET data types.</summary>
        /// <returns>An array of .NET data types.</returns>
        public string[] GetNetDataTypes2()
        {
            //string[] a = NetDataTypeDef2.Split(',');
            ////mapping = new BindingList<DatatypeMapping>();
            //Array.Sort(a);
            return DataTypeFieldMappingHelper.GetNetDataTypes2();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DataTypesHelper()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
