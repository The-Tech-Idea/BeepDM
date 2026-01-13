using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for File-based data source connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all File-based connection configurations
        /// </summary>
        /// <returns>List of File-based connection configurations</returns>
        public static List<ConnectionDriversConfig> GetFileConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateCSVFileReaderConfig(),
                CreateCSVDataSourceConfig(),
                CreateJsonDataSourceConfig(),
                CreateTxtXlsCSVFileSourceConfig(),
                CreateDataViewConfig(),
                CreateXMLDataSourceConfig(),
                CreateYAMLDataSourceConfig(),
                CreateINIDataSourceConfig(),
                CreateTextFileDataSourceConfig(),
                CreatePDFDataSourceConfig(),
                CreateParquetDataSourceConfig(),
                CreateAvroDataSourceConfig(),
                CreateORCDataSourceConfig(),
                CreateFeatherDataSourceConfig(),
                CreateHdf5DataSourceConfig(),
                CreateLibSVMDataSourceConfig(),
                CreateGraphMLDataSourceConfig(),
                CreateDICOMDataSourceConfig(),
                CreateLASDataSourceConfig(),
                CreateRecordIODataSourceConfig(),
                CreateMarkdownDataSourceConfig(),
                CreateLogFileDataSourceConfig(),
                CreateFlatFileDataSourceConfig()
            };

            return configs;
        }

        /// <summary>
        /// Creates a configuration object for a CSV file reader connection driver.
        /// </summary>
        /// <returns>A configuration object for a CSV file reader connection driver.</returns>
        public static ConnectionDriversConfig CreateCSVFileReaderConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "a5c687c5-71b6-4f2c-b2d9-6291972763ea",
                PackageName = "FileReader",
                DriverClass = "FileReader",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "csv.svg",
                classHandler = "CSVDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                extensionstoHandle = "csv",
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.CSV,
                IsMissing = false,
                NuggetVersion = "1",
                NuggetSource = "FileReader",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>
        /// Creates a configuration object for a CSV data source.
        /// </summary>
        /// <returns>A configuration object for a CSV data source.</returns>
        public static ConnectionDriversConfig CreateCSVDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "b88f871b-fd5b-4516-b1b3-65e2c54b3fe7",
                PackageName = "CSVDataSource",
                DriverClass = "CSVDataSource",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "csv.svg",
                classHandler = "CSVDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "csv",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.CSV,
                IsMissing = false,
                NuggetVersion = "1",
                NuggetSource = "CSVDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>
        /// Creates a configuration object for a JSON data source connection driver.
        /// </summary>
        /// <returns>A configuration object for a JSON data source connection driver.</returns>
        public static ConnectionDriversConfig CreateJsonDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "970bfedf-0503-474e-b936-79d2d66065c9",
                PackageName = "JSONSource",
                DriverClass = "JSONSource",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "json.svg",
                classHandler = "JsonDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "json",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Json,
                IsMissing = false,
                NuggetVersion = "1",
                NuggetSource = "JsonDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>
        /// Creates a configuration object for a text, xls, or csv file data source.
        /// </summary>
        /// <returns>A ConnectionDriversConfig object representing the configuration for the file data source.</returns>
        public static ConnectionDriversConfig CreateTxtXlsCSVFileSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "ac76ffa3-bb78-49dc-bda8-e9b26c9633d2",
                PackageName = "TxtXlsCSVFileSource",
                DriverClass = "TxtXlsCSVFileSource",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "xls.svg",
                classHandler = "TxtXlsCSVFileSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "xls,xlsx",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Xls,
                IsMissing = false,
                NuggetVersion = "1",
                NuggetSource = "TxtXlsCSVFileSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for connection drivers.</summary>
        /// <returns>A configuration object for connection drivers.</returns>
        public static ConnectionDriversConfig CreateDataViewConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "ad729953-9010-4d1c-9459-3d1a3fab2de8",
                PackageName = "DataViewReader",
                DriverClass = "DataViewReader",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "dataview.svg",
                classHandler = "DataViewDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VIEWS,
                DatasourceType = DataSourceType.Json,
                IsMissing = false,
                NuggetVersion = "1",
                NuggetSource = "DataViewReader",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for XML data source connection drivers.</summary>
        /// <returns>A configuration object for XML data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateXMLDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "XMLDataSource",
                DriverClass = "XMLDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.XMLDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "xml.svg",
                classHandler = "XMLDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "xml",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.XML,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "XMLDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for YAML data source connection drivers.</summary>
        /// <returns>A configuration object for YAML data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateYAMLDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "YAMLDataSource",
                DriverClass = "YAMLDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.YAMLDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "yaml.svg",
                classHandler = "YAMLDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "yml,yaml",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.YAML,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "YAMLDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for INI data source connection drivers.</summary>
        /// <returns>A configuration object for INI data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateINIDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "INIDataSource",
                DriverClass = "INIDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.INIDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "ini.svg",
                classHandler = "INIDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "ini,cfg",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.INI,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "INIDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Text file data source connection drivers.</summary>
        /// <returns>A configuration object for Text file data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateTextFileDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "TextFileDataSource",
                DriverClass = "TextFileDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.TextFileDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "text.svg",
                classHandler = "TextFileDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "txt,text",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Text,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "TextFileDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for PDF data source connection drivers.</summary>
        /// <returns>A configuration object for PDF data source connection drivers.</returns>
        public static ConnectionDriversConfig CreatePDFDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "PDFDataSource",
                DriverClass = "PDFDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.PDFDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "pdf.svg",
                classHandler = "PDFDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "pdf",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.PDF,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "PDFDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Parquet data source connection drivers.</summary>
        /// <returns>A configuration object for Parquet data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateParquetDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ParquetDataSource",
                DriverClass = "ParquetDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ParquetDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "parquet.svg",
                classHandler = "ParquetDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "parquet",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Parquet,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ParquetDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Avro data source connection drivers.</summary>
        /// <returns>A configuration object for Avro data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateAvroDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AvroDataSource",
                DriverClass = "AvroDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.AvroDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "avro.svg",
                classHandler = "AvroDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "avro",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Avro,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "AvroDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for ORC data source connection drivers.</summary>
        /// <returns>A configuration object for ORC data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateORCDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ORCDataSource",
                DriverClass = "ORCDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ORCDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "orc.svg",
                classHandler = "ORCDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "orc",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.ORC,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ORCDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Feather data source connection drivers.</summary>
        /// <returns>A configuration object for Feather data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateFeatherDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "FeatherDataSource",
                DriverClass = "FeatherDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.FeatherDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "feather.svg",
                classHandler = "FeatherDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "feather",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Feather,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "FeatherDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for HDF5 data source connection drivers.</summary>
        /// <returns>A configuration object for HDF5 data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateHdf5DataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Hdf5DataSource",
                DriverClass = "Hdf5DataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.Hdf5DataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "hdf5.svg",
                classHandler = "Hdf5DataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "h5,hdf5",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Hdf5,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Hdf5DataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for LibSVM data source connection drivers.</summary>
        /// <returns>A configuration object for LibSVM data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateLibSVMDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "LibSVMDataSource",
                DriverClass = "LibSVMDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.LibSVMDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "libsvm.svg",
                classHandler = "LibSVMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "libsvm,svm",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.LibSVM,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "LibSVMDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for GraphML data source connection drivers.</summary>
        /// <returns>A configuration object for GraphML data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateGraphMLDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "GraphMLDataSource",
                DriverClass = "GraphMLDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.GraphMLDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "graphml.svg",
                classHandler = "GraphMLDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "graphml",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.GraphFile,
                DatasourceType = DataSourceType.GraphML,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "GraphMLDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for DICOM data source connection drivers.</summary>
        /// <returns>A configuration object for DICOM data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateDICOMDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "DICOMDataSource",
                DriverClass = "DICOMDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.DICOMDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "dicom.svg",
                classHandler = "DICOMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "dcm,dicom",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.DICOM,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "DICOMDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for LAS data source connection drivers.</summary>
        /// <returns>A configuration object for LAS data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateLASDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "LASDataSource",
                DriverClass = "LASDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.LASDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "las.svg",
                classHandler = "LASDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "las",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.LAS,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "LASDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for RecordIO data source connection drivers.</summary>
        /// <returns>A configuration object for RecordIO data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateRecordIODataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RecordIODataSource",
                DriverClass = "RecordIODataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.RecordIODataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "recordio.svg",
                classHandler = "RecordIODataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "recordio",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.RecordIO,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "RecordIODataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Markdown data source connection drivers.</summary>
        /// <returns>A configuration object for Markdown data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateMarkdownDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "MarkdownDataSource",
                DriverClass = "MarkdownDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MarkdownDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "markdown.svg",
                classHandler = "MarkdownDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "md,markdown",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Markdown,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "MarkdownDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Log file data source connection drivers.</summary>
        /// <returns>A configuration object for Log file data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateLogFileDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "LogFileDataSource",
                DriverClass = "LogFileDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.LogFileDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "log.svg",
                classHandler = "LogFileDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "log",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.Log,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "LogFileDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }

        /// <summary>Creates a configuration object for Flat file data source connection drivers.</summary>
        /// <returns>A configuration object for Flat file data source connection drivers.</returns>
        public static ConnectionDriversConfig CreateFlatFileDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "FlatFileDataSource",
                DriverClass = "FlatFileDataSource",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.FlatFileDataSource.dll",
                AdapterType = "DEFAULT",
                iconname = "flatfile.svg",
                classHandler = "FlatFileDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "dat,flat",
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE,
                DatasourceType = DataSourceType.FlatFile,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "FlatFileDataSource",
                NuggetMissing = false,NeedDrivers=false
            };
        }
    }
}