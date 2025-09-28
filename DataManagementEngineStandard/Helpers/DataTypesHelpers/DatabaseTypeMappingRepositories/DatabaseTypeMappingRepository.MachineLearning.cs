using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Machine Learning and File Format specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of TFRecord data type mappings.</summary>
        /// <returns>A list of TFRecord data type mappings.</returns>
        public static List<DatatypeMapping> GetTFRecordDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // TensorFlow Record format data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bytes_list", DataSourceName = "TFRecordDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float_list", DataSourceName = "TFRecordDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int64_list", DataSourceName = "TFRecordDataSource", NetDataType = "System.Collections.Generic.List<long>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "feature", DataSourceName = "TFRecordDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "example", DataSourceName = "TFRecordDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of ONNX data type mappings.</summary>
        /// <returns>A list of ONNX data type mappings.</returns>
        public static List<DatatypeMapping> GetONNXDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // ONNX tensor data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "ONNXDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UINT8", DataSourceName = "ONNXDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT8", DataSourceName = "ONNXDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UINT16", DataSourceName = "ONNXDataSource", NetDataType = "System.UInt16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT16", DataSourceName = "ONNXDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT32", DataSourceName = "ONNXDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT64", DataSourceName = "ONNXDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "ONNXDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "ONNXDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT16", DataSourceName = "ONNXDataSource", NetDataType = "System.Half", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "ONNXDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UINT32", DataSourceName = "ONNXDataSource", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UINT64", DataSourceName = "ONNXDataSource", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "COMPLEX64", DataSourceName = "ONNXDataSource", NetDataType = "System.Numerics.Complex", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "COMPLEX128", DataSourceName = "ONNXDataSource", NetDataType = "System.Numerics.Complex", Fav = false }
            };
        }

        /// <summary>Returns a list of PyTorch data type mappings.</summary>
        /// <returns>A list of PyTorch data type mappings.</returns>
        public static List<DatatypeMapping> GetPyTorchDataDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // PyTorch tensor data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float32", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float64", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float16", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Half", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bfloat16", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Half", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "complex64", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Numerics.Complex", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "complex128", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Numerics.Complex", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "uint8", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int8", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int16", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int32", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int64", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bool", DataSourceName = "PyTorchDataDataSource", NetDataType = "System.Boolean", Fav = false }
            };
        }

        /// <summary>Returns a list of Scikit-Learn data type mappings.</summary>
        /// <returns>A list of Scikit-Learn data type mappings.</returns>
        public static List<DatatypeMapping> GetScikitLearnDataDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Scikit-Learn uses NumPy data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float64", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float32", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int64", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int32", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bool", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "category", DataSourceName = "ScikitLearnDataDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of HDF5 data type mappings.</summary>
        /// <returns>A list of HDF5 data type mappings.</returns>
        public static List<DatatypeMapping> GetHdf5DataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // HDF5 native data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_FLOAT", DataSourceName = "Hdf5DataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_DOUBLE", DataSourceName = "Hdf5DataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_INT", DataSourceName = "Hdf5DataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_LONG", DataSourceName = "Hdf5DataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_SHORT", DataSourceName = "Hdf5DataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_CHAR", DataSourceName = "Hdf5DataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_UCHAR", DataSourceName = "Hdf5DataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_UINT", DataSourceName = "Hdf5DataSource", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_ULONG", DataSourceName = "Hdf5DataSource", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_NATIVE_USHORT", DataSourceName = "Hdf5DataSource", NetDataType = "System.UInt16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_C_S1", DataSourceName = "Hdf5DataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_COMPOUND", DataSourceName = "Hdf5DataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_ARRAY", DataSourceName = "Hdf5DataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "H5T_ENUM", DataSourceName = "Hdf5DataSource", NetDataType = "System.Enum", Fav = false }
            };
        }

        /// <summary>Returns a list of LibSVM data type mappings.</summary>
        /// <returns>A list of LibSVM data type mappings.</returns>
        public static List<DatatypeMapping> GetLibSVMDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // LibSVM sparse format - primarily numeric
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "label", DataSourceName = "LibSVMDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "feature_value", DataSourceName = "LibSVMDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "feature_index", DataSourceName = "LibSVMDataSource", NetDataType = "System.Int32", Fav = false }
            };
        }

        /// <summary>Returns a list of GraphML data type mappings.</summary>
        /// <returns>A list of GraphML data type mappings.</returns>
        public static List<DatatypeMapping> GetGraphMLDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // GraphML attribute data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "GraphMLDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "GraphMLDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "long", DataSourceName = "GraphMLDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "GraphMLDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "GraphMLDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "GraphMLDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of DICOM data type mappings.</summary>
        /// <returns>A list of DICOM data type mappings.</returns>
        public static List<DatatypeMapping> GetDICOMDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // DICOM Value Representations (VR)
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "AE", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Application Entity
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "AS", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Age String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "AT", DataSourceName = "DICOMDataSource", NetDataType = "System.UInt32", Fav = false }, // Attribute Tag
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CS", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Code String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DA", DataSourceName = "DICOMDataSource", NetDataType = "System.DateTime", Fav = false }, // Date
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DS", DataSourceName = "DICOMDataSource", NetDataType = "System.Double", Fav = false }, // Decimal String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DT", DataSourceName = "DICOMDataSource", NetDataType = "System.DateTime", Fav = false }, // Date Time
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FL", DataSourceName = "DICOMDataSource", NetDataType = "System.Single", Fav = false }, // Floating Point Single
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FD", DataSourceName = "DICOMDataSource", NetDataType = "System.Double", Fav = false }, // Floating Point Double
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IS", DataSourceName = "DICOMDataSource", NetDataType = "System.Int32", Fav = false }, // Integer String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LO", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Long String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LT", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Long Text
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OB", DataSourceName = "DICOMDataSource", NetDataType = "System.Byte[]", Fav = false }, // Other Byte
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OD", DataSourceName = "DICOMDataSource", NetDataType = "System.Double[]", Fav = false }, // Other Double
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OF", DataSourceName = "DICOMDataSource", NetDataType = "System.Single[]", Fav = false }, // Other Float
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OL", DataSourceName = "DICOMDataSource", NetDataType = "System.UInt32[]", Fav = false }, // Other Long
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OW", DataSourceName = "DICOMDataSource", NetDataType = "System.UInt16[]", Fav = false }, // Other Word
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "PN", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Person Name
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SH", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Short String
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SL", DataSourceName = "DICOMDataSource", NetDataType = "System.Int32", Fav = false }, // Signed Long
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SS", DataSourceName = "DICOMDataSource", NetDataType = "System.Int16", Fav = false }, // Signed Short
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ST", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Short Text
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TM", DataSourceName = "DICOMDataSource", NetDataType = "System.TimeSpan", Fav = false }, // Time
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UC", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Unlimited Characters
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UI", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Unique Identifier
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UL", DataSourceName = "DICOMDataSource", NetDataType = "System.UInt32", Fav = false }, // Unsigned Long
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UR", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }, // Universal Resource
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "US", DataSourceName = "DICOMDataSource", NetDataType = "System.UInt16", Fav = false }, // Unsigned Short
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UT", DataSourceName = "DICOMDataSource", NetDataType = "System.String", Fav = false }  // Unlimited Text
            };
        }

        /// <summary>Returns a list of LAS (Log ASCII Standard) data type mappings.</summary>
        /// <returns>A list of LAS data type mappings.</returns>
        public static List<DatatypeMapping> GetLASDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // LAS well log data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DEPTH", DataSourceName = "LASDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CURVE_VALUE", DataSourceName = "LASDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NULL_VALUE", DataSourceName = "LASDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "WELL_INFO", DataSourceName = "LASDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "PARAMETER", DataSourceName = "LASDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of RecordIO data type mappings.</summary>
        /// <returns>A list of RecordIO data type mappings.</returns>
        public static List<DatatypeMapping> GetRecordIODataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // RecordIO format - primarily binary records
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "record", DataSourceName = "RecordIODataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "header", DataSourceName = "RecordIODataSource", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "payload", DataSourceName = "RecordIODataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }
    }
}