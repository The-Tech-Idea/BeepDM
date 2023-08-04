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
                if (!File.Exists(file))
                {
                    DMEEditor.AddLogMessage("Beep", $"Error Could not Find File {file}", DateTime.Now, 0, null, TheTechIdea.Util.Errors.Failed);
                    return null;
                }
                string filename = Path.GetFileName(file);
                string ext = Path.GetExtension(file).Replace(".", "").ToLower();

                ConnectionDriversConfig driversConfig = GetConnectionDrivers(ext);
                if (driversConfig == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Error Could not Find Drivers for {filename}", DateTime.Now, 0, null, TheTechIdea.Util.Errors.Failed);
                    return null;
                }
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
               
                
                ConnectionDriversConfig c = GetConnectionDrivers(ext);
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
        public ConnectionDriversConfig GetConnectionDrivers(string ext)
        {
            List<ConnectionDriversConfig> configs = new List<ConnectionDriversConfig>();
            ConnectionDriversConfig driversConfig = new ConnectionDriversConfig();
            configs = DMEEditor.ConfigEditor.DriverDefinitionsConfig.Where(p => p.extensionstoHandle != null &&  p.extensionstoHandle.Contains(ext)).ToList();
            if (configs.Count > 0)
            {
                //-----------  Get Favourite

                driversConfig = configs.FirstOrDefault(p => p.Favourite);
                //----------- Get Latest version if Favourite not available
                if (driversConfig == null)
                {
                    driversConfig = configs.OrderByDescending(p => p.version).FirstOrDefault();
                }

            }
            else
            {
                DMEEditor.AddLogMessage("Beep", $"Error Could not Find Drivers for extension {ext}", DateTime.Now, 0, null, TheTechIdea.Util.Errors.Failed);
                return null;
            }
            return driversConfig;

        }
        public IDataSource CreateDataSource(string filepath)
        {
            IDataSource ds=null;
            if(!File.Exists(filepath))
            {
                DMEEditor.AddLogMessage("Beep", $"Error Could not Find File {filepath}", DateTime.Now, 0, null, TheTechIdea.Util.Errors.Failed);
                return null;
            }
            string filename = Path.GetFileNameWithoutExtension(filepath);
            string ext = Path.GetExtension(filepath);
            // Find Drivers
            ConnectionDriversConfig driversConfig = GetConnectionDrivers(ext);
            if (driversConfig == null)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Could not Find Drivers for {filename}", DateTime.Now, 0, null, TheTechIdea.Util.Errors.Failed);
                return null;
            }
            // Found Drivers
            ConnectionProperties cn= CreateFileDataConnection(filepath);
            if(cn != null) { 
                DMEEditor.ConfigEditor.AddDataConnection(cn);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                ds = DMEEditor.GetDataSource(filename);
            }
            return ds;

        }
        public Tuple<IErrorsInfo,Project> CreateProject(string folderpath)
        {
            Project projectFolder = new Project();
            try
            {
                if (!string.IsNullOrEmpty(folderpath))
                {
                    if (Directory.Exists(folderpath))
                    {
                        string dirname = new System.IO.DirectoryInfo(folderpath).Name;
                        projectFolder.Url = folderpath;
                        projectFolder.Name = dirname;
                        Folder folder = new Folder(folderpath);
                        folder= CreateFolderStructure( folderpath);
                        DMEEditor.AddLogMessage("Success", "Added Project Folder ", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        projectFolder = null;
                        DMEEditor.AddLogMessage("Failed", "Project Folder not found ", DateTime.Now, 0, null, Errors.Failed);

                    }
                }
                else
                {
                    projectFolder = null;
                    DMEEditor.AddLogMessage("Failed", "Project Folder string is empty ", DateTime.Now, 0, null, Errors.Failed);
                }
                
            }
            catch (Exception ex)
            {
                string mes = "Could not Show File";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return new (DMEEditor.ErrorObject, projectFolder);
        }
        public Folder CreateFolderStructure(Folder folder, string path)
        {
           
            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {
                IFile files1 = new FFile(file);
                folder.Files.Add(files1);
                string filename = Path.GetFileName(file);
                ConnectionProperties conn = CreateFileDataConnection(file);
                DMEEditor.ConfigEditor.AddDataConnection(conn);
              }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    Folder folder1 = new Folder(drpath);
                    CreateFolderStructure(folder1, drpath);
                }
            }
            return folder;
           
        }
        public Folder CreateFolderStructure( string path)
        {
            Folder folder=new Folder(path);
            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {
                IFile files1 = new FFile(file);
                folder.Files.Add(files1);
                string filename = Path.GetFileName(file);
                ConnectionProperties conn = CreateFileDataConnection(file);
                DMEEditor.ConfigEditor.AddDataConnection(conn);
            }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    folder.Folders.Add(CreateFolderStructure( drpath));
                }
            }
            return folder;

        }
    }
}
