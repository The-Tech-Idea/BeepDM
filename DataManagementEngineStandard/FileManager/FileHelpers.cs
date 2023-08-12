using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public bool IsFileValid(string filename)
        {
            bool retval=false;
            string ext = Path.GetExtension(filename).Replace(".", "").ToLower();
            List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            if (clss != null)
            {
                IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();
                retval = exts.Contains(ext);
            }
            return retval;
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
                foreach (string file in filenames)
                {
                    {
                        if (File.Exists(file))
                        {
                            ConnectionProperties c = CreateFileDataConnection(file);
                            if (c != null)
                            {
                                retval.Add(c);
                            }
                        }else
                        {
                            DMEEditor.AddLogMessage("Bepp", $"File {file} Exist ", DateTime.Now, -1, null, Errors.Failed);
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

           
              
                ConnectionProperties f = new ConnectionProperties
                {
                    FileName = filename,
                    FilePath = Path.GetDirectoryName(file),
                    Ext =ext,
                    ConnectionName = filename


                };
                if (f.FilePath.Contains(DMEEditor.ConfigEditor.ExePath))
                {
                    f.FilePath = f.FilePath.Replace(DMEEditor.ConfigEditor.ExePath, ".\\");
                }
               
                
                ConnectionDriversConfig c = GetConnectionDrivers(ext);
                if (c == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Error Could not Find Drivers for {filename}", DateTime.Now, 0, null, TheTechIdea.Util.Errors.Failed);
                    return null;
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
                            f.DatabaseType = DataSourceType.XML;
                            break;
                        case "json":
                            f.DatabaseType = DataSourceType.Json;
                            break;
                        case "xls":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        case "xlsx":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        case "tsv":
                            f.DatabaseType = DataSourceType.TSV;
                            break;
                        case "parquet":
                            f.DatabaseType = DataSourceType.Parquet;
                            break;
                        case "avro":
                            f.DatabaseType = DataSourceType.Avro;
                            break;
                        case "orc":
                            f.DatabaseType = DataSourceType.ORC;
                            break;
                        case "onnx":
                            f.DatabaseType = DataSourceType.Onnx;
                            break;
                        case "html":
                        case "htm":
                            f.DatabaseType = DataSourceType.HTML;
                            break;
                        case "sql":
                            f.DatabaseType = DataSourceType.SQL;
                            break;
                        case "ini":
                        case "cfg":
                            f.DatabaseType = DataSourceType.INI;
                            break;
                        case "log":
                            f.DatabaseType = DataSourceType.Log;
                            break;
                        case "pdf":
                            f.DatabaseType = DataSourceType.PDF;
                            break;
                        case "doc":
                        case "docx":
                            f.DatabaseType = DataSourceType.Doc;
                            break;
                        case "ppt":
                        case "pptx":
                            f.DatabaseType = DataSourceType.PPT;
                            break;
                        case "yaml":
                        case "yml":
                            f.DatabaseType = DataSourceType.YAML;
                            break;
                        case "md":
                        case "markdown":
                            f.DatabaseType = DataSourceType.Markdown;
                            break;
                        case "feather":
                            f.DatabaseType = DataSourceType.Feather;
                            break;
                        case "tfrecord":
                            f.DatabaseType = DataSourceType.TFRecord;
                            break;
                        case "recordio":
                            f.DatabaseType = DataSourceType.RecordIO;
                            break;
                        case "libsvm":
                            f.DatabaseType = DataSourceType.LibSVM;
                            break;
                        case "graphml":
                            f.DatabaseType = DataSourceType.GraphML;
                            break;
                        case "dicom":
                            f.DatabaseType = DataSourceType.DICOM;
                            break;
                        case "las":
                            f.DatabaseType = DataSourceType.LAS;
                            break;
                        default:
                            f.DatabaseType = DataSourceType.NONE;
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
            ConnectionDriversConfig driversConfig = null;
            configs = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null &&  p.extensionstoHandle.Contains(ext)).ToList();
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
        public Tuple<IErrorsInfo,RootFolder> CreateProject(string folderpath, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            RootFolder projectFolder = new RootFolder();
            projectFolder.FolderType=folderType;
            projectFolder.Folders = new List<Folder>();
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
                        projectFolder.Folders.Add(folder);
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
               
                ConnectionProperties conn = CreateFileDataConnection(file);
                if (conn != null)
                {
                    FFile files1 = new FFile(file);
                    files1.GuidID= conn.GuidID;
                    folder.Files.Add(files1);
                    
                    DMEEditor.ConfigEditor.AddDataConnection(conn);
                }
               
              }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    Folder folder1 = new Folder(drpath);
                    folder1.Name = new DirectoryInfo(drpath).Name;
                    CreateFolderStructure(folder1, drpath);
                }
            }
            return folder;
           
        }
        public Folder CreateFolderStructure( string path)
        {
            Folder folder=new Folder(path);
            folder.Folders = new List<Folder>();
            folder.Name = new DirectoryInfo(path).Name;
            folder.Url = path;
            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {
               
                ConnectionProperties conn = CreateFileDataConnection(file);
                if (conn != null)
                {
                    FFile files1 = new FFile(file);
                    files1.GuidID = conn.GuidID;
                    folder.Files.Add(files1);
                   
                    DMEEditor.ConfigEditor.AddDataConnection(conn);
                }
                
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
        public RootFolder CreateFolderStructure(string path, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            RootFolder rootFolder=new RootFolder(path);
            rootFolder.Folders = new List<Folder>();
            rootFolder.FolderType = folderType;
            rootFolder.Name = new DirectoryInfo(path).Name;
            Folder folder = new Folder(path);

            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {
               

                ConnectionProperties conn = CreateFileDataConnection(file);
                if(conn != null)
                {
                    FFile files1 = new FFile(file);
                    files1.GuidID = conn.GuidID;
                    folder.Files.Add(files1);
                 
                    DMEEditor.ConfigEditor.AddDataConnection(conn);
                }
               
            }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    folder.Folders.Add(CreateFolderStructure(drpath));
                }
            }
            rootFolder.Folders.Add(folder);
            return rootFolder;

        }
    }
}
