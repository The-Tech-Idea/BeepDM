using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.FileManager
{
    public class FileHelpers
    {
        public IDMEEditor DMEEditor { get; }

        public FileHelpers(IDMEEditor dMEEditor)
        {
            DMEEditor=dMEEditor;
        }
        public virtual List<ConnectionProperties> LoadFiles( string[] filenames)
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                string extens = DMEEditor.ConfigEditor.CreateFileExtensionString();
              
               
                retval = CreateFileConnections(filenames);
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public string CreateFileExtensionString()
        {
            List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            string retval = null;
            if (clss != null)
            {
                IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();

                foreach (string item in exts)
                {
                    retval += item + " files(*." + item + ")|*." + item + "|";


                }

            }

            retval += "All files(*.*)|*.*";
            return retval;
        }
        public string CreateFileExtensionString(string extens)
        {
        //    List<ConnectionDriversConfig> clss = DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            string retval = null;
            if (extens != null)
            {
                IEnumerable<string> extensionslist = extens.Split(',').AsEnumerable() ;
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();

                foreach (string item in exts)
                {
                    retval += item + " files(*." + item + ")|*." + item + "|";


                }

            }

            retval += "All files(*.*)|*.*";
            return retval;
        }
        public virtual List<ConnectionProperties> LoadFiles(string directoryname,string extens)
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
             //   string extens = CreateFileExtensionString();
               string[] filenames=Directory.GetFiles(directoryname, CreateFileExtensionString(extens));

                retval = CreateFileConnections(filenames);
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public List<ConnectionProperties> CreateFileConnections(string[] filenames)
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                foreach (String file in filenames)
                {
                    {
                        ConnectionProperties c = CreateFileDataConnection(file);
                        if (c != null)
                        {
                            retval.Add(c);
                        }
                    }
                }
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public ConnectionProperties CreateFileDataConnection(string file)
        {
            try
            {
                ConnectionProperties f = new ConnectionProperties
                {
                    FileName = Path.GetFileName(file),
                    FilePath = Path.GetDirectoryName(file),
                    Ext = Path.GetExtension(file).Replace(".", "").ToLower(),
                    ConnectionName = Path.GetFileName(file)


                };
                if (f.FilePath.Contains(DMEEditor.ConfigEditor.ExePath))
                {
                    f.FilePath = f.FilePath.Replace(DMEEditor.ConfigEditor.ExePath, ".\\");
                }
                //if (f.FilePath.Contains(DMEEditor.ConfigEditor.Config.DataFilePath))
                //{
                //    f.FilePath = f.FilePath.Replace(DMEEditor.ConfigEditor.Config.DataFilePath, ".");
                //}
                //if (f.FilePath.Contains(DMEEditor.ConfigEditor.Config.ProjectDataPath))
                //{
                //    f.FilePath = f.FilePath.Replace(DMEEditor.ConfigEditor.Config.ProjectDataPath, ".");
                //}
                string ext = Path.GetExtension(file).Replace(".", "").ToLower();
                List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null && p.extensionstoHandle.Contains(ext) && p.Favourite == true).ToList();
                ConnectionDriversConfig c = clss.Where(o => o.extensionstoHandle.Contains(ext) && o.Favourite == true).FirstOrDefault();
                if (c is null)
                {
                    c = clss.Where(o => o.classHandler.Equals("CSVDataSource", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }
                if (c != null)
                {
                    f.DriverName = c.PackageName;
                    f.DriverVersion = c.version;
                    f.Category = c.DatasourceCategory;

                    switch (f.Ext.ToLower())
                    {
                        case "txt":
                            f.DatabaseType = DataSourceType.Text;
                            break;
                        case "csv":
                            f.DatabaseType = DataSourceType.CSV;
                            break;
                        case "xml":
                            f.DatabaseType = DataSourceType.xml;
                            break;
                        case "json":
                            f.DatabaseType = DataSourceType.Json;
                            break;
                        case "xls":
                        case "xlsx":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        default:
                            f.DatabaseType = DataSourceType.Text;
                            break;
                    }
                    f.Category = DatasourceCategory.FILE;
                    return f;

                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Could not Load File {f.ConnectionName}", DateTime.Now, -1, null, Errors.Failed);
                }
                return f;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
    }
}
