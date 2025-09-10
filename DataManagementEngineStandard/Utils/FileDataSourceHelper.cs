using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Utils
{
    internal static class FileDataSourceHelper
    {
        internal static List<ConnectionProperties> CreateFileConnections(IDMEEditor dme, string[] filenames)
        {
            var retval = new List<ConnectionProperties>();
            foreach (var file in filenames)
            {
                if (!File.Exists(file)) continue;
                if (FileHelper.FileExists(dme, file) != null) continue;
                var cn = CreateFileDataConnection(dme, file);
                if (cn != null) retval.Add(cn);
            }
            return retval;
        }

        internal static ConnectionProperties CreateFileDataConnection(IDMEEditor dme, string file)
        {
            if (!File.Exists(file))
            {
                dme.AddLogMessage("Beep", $"File not found {file}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
            var existing = FileHelper.FileExists(dme, file);
            if (existing != null) return existing;

            string filename = Path.GetFileName(file);
            string ext = Path.GetExtension(file).Replace(".", "").ToLowerInvariant();

            var cn = new ConnectionProperties
            {
                FileName = filename,
                FilePath = Path.GetDirectoryName(file),
                Ext = ext,
                ConnectionName = filename,
                Category = DatasourceCategory.FILE
            };

            var drv = GetDriverForExt(dme, ext);
            if (drv == null)
            {
                dme.AddLogMessage("Beep", $"No driver for extension {ext}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
            cn.DriverName = drv.PackageName;
            cn.DriverVersion = drv.version;
            cn.DatabaseType = MapExtToType(ext);
            return cn;
        }

        private static ConnectionDriversConfig GetDriverForExt(IDMEEditor dme, string ext)
        {
            var configs = dme.ConfigEditor.DataDriversClasses
                .Where(p => p.extensionstoHandle != null && p.extensionstoHandle.Contains(ext))
                .ToList();
            if (configs.Count == 0) return null;
            var fav = configs.FirstOrDefault(c => c.Favourite);
            return fav ?? configs.OrderByDescending(c => c.version).First();
        }

        private static DataSourceType MapExtToType(string ext) =>
            ext switch
            {
                "txt" => DataSourceType.Text,
                "csv" => DataSourceType.CSV,
                "xml" => DataSourceType.XML,
                "json" => DataSourceType.Json,
                "xls" => DataSourceType.Xls,
                "xlsx" => DataSourceType.Xls,
                "tsv" => DataSourceType.TSV,
                "parquet" => DataSourceType.Parquet,
                "avro" => DataSourceType.Avro,
                "orc" => DataSourceType.ORC,
                "onnx" => DataSourceType.Onnx,
                "ini" or "cfg" => DataSourceType.INI,
                "log" => DataSourceType.Log,
                "pdf" => DataSourceType.PDF,
                "doc" or "docx" => DataSourceType.Doc,
                "ppt" or "pptx" => DataSourceType.PPT,
                "yaml" or "yml" => DataSourceType.YAML,
                "md" or "markdown" => DataSourceType.Markdown,
                "feather" => DataSourceType.Feather,
                "tfrecord" => DataSourceType.TFRecord,
                "recordio" => DataSourceType.RecordIO,
                "libsvm" => DataSourceType.LibSVM,
                "graphml" => DataSourceType.GraphML,
                "dicom" => DataSourceType.DICOM,
                "las" => DataSourceType.LAS,
                _ => DataSourceType.NONE
            };
    }
}