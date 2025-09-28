using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;
using System.ComponentModel;

namespace TheTechIdea.Beep.Helpers.FileandFolderHelpers
{
    /// <summary>
    /// Helper class for file and folder operations, including loading, validation, and folder structure management.
    /// </summary>
    public static class FileOperationHelper
    {
        private static readonly object _lock = new object();
        private static bool _isInitialized;

        /// <summary>
        /// Gets the editor instance for configuration and logging.
        /// </summary>
        public static IDMEEditor DMEEditor { get; private set; }

        /// <summary>
        /// Initializes the FileOperationHelper with the required editor.
        /// </summary>
        /// <param name="editor">The editor instance for configuration and logging.</param>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        public static void Initialize(IDMEEditor editor)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("FileOperationHelper is already initialized.");

                DMEEditor = editor ?? throw new ArgumentNullException(nameof(editor));
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Resets the FileOperationHelper, clearing the editor.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                DMEEditor = null;
                _isInitialized = false;
            }
        }

        #region File Loading Operations

        /// <summary>
        /// Loads a single file from the provided file path and creates connection properties for it.
        /// </summary>
        /// <param name="filePath">The path of the file to load.</param>
        /// <returns>The connection properties for the loaded file, or null on failure.</returns>
        public static ConnectionProperties LoadFile(string filePath)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(filePath))
            {
                DMEEditor.AddLogMessage("Info", "No file path provided", DateTime.Now, 0, null, Errors.Ok);
                return null;
            }

            if (!File.Exists(filePath))
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"File {filePath} does not exist";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                return null;
            }

            try
            {
                var folder = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataFiles);
                if (folder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No data files folder configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                    return null;
                }

                var connections = DMEEditor.Utilfunction.LoadFiles(new[] { filePath });
                if (connections == null || !connections.Any())
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Failed to load file {filePath}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                    return null;
                }

                var connection = connections.First();
                DMEEditor.AddLogMessage("Success", $"Loaded file {filePath}", DateTime.Now, 0, filePath, Errors.Ok);
                return connection;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not load file {filePath}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Asynchronously loads a single file from the provided file path and creates connection properties for it.
        /// </summary>
        /// <param name="filePath">The path of the file to load.</param>
        /// <returns>A task that returns the connection properties for the loaded file, or null on failure.</returns>
        public static async Task<ConnectionProperties> LoadFileAsync(string filePath)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(filePath))
            {
                DMEEditor.AddLogMessage("Info", "No file path provided", DateTime.Now, 0, null, Errors.Ok);
                return null;
            }

            if (!File.Exists(filePath))
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"File {filePath} does not exist";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                return null;
            }

            try
            {
                var folder = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataFiles);
                if (folder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No data files folder configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                    return null;
                }

                var connections = await Task.Run(() => DMEEditor.Utilfunction.LoadFiles(new[] { filePath }));
                if (connections == null || !connections.Any())
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Failed to load file {filePath}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                    return null;
                }

                var connection = connections.First();
                DMEEditor.AddLogMessage("Success", $"Loaded file {filePath} asynchronously", DateTime.Now, 0, filePath, Errors.Ok);
                return connection;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not load file asynchronously {filePath}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, filePath, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Loads files from the provided list of file paths and creates connection properties for valid files.
        /// </summary>
        /// <param name="filenames">List of file paths to load.</param>
        /// <returns>A list of connection properties for loaded files, or null on failure.</returns>
        public static List<ConnectionProperties> LoadFiles(List<string> filenames)
        {
            EnsureInitialized();
            if (filenames == null || !filenames.Any())
            {
                DMEEditor.AddLogMessage("Info", "No files provided", DateTime.Now, 0, null, Errors.Ok);
                return new List<ConnectionProperties>();
            }

            try
            {
                var folder = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataFiles);
                if (folder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No data files folder configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }

                var retval = DMEEditor.Utilfunction.LoadFiles(filenames.ToArray());
                DMEEditor.AddLogMessage("Success", $"Loaded {retval.Count} files", DateTime.Now, 0, string.Join(", ", filenames), Errors.Ok);
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not load files: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, string.Join(", ", filenames), Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Asynchronously loads files from the provided list of file paths and creates connection properties for valid files.
        /// </summary>
        /// <param name="filenames">List of file paths to load.</param>
        /// <returns>A task that returns a list of connection properties for loaded files, or null on failure.</returns>
        public static async Task<List<ConnectionProperties>> LoadFilesAsync(List<string> filenames)
        {
            EnsureInitialized();
            if (filenames == null || !filenames.Any())
            {
                DMEEditor.AddLogMessage("Info", "No files provided", DateTime.Now, 0, null, Errors.Ok);
                return new List<ConnectionProperties>();
            }

            try
            {
                var folder = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataFiles);
                if (folder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No data files folder configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }

                var retval = await Task.Run(() => DMEEditor.Utilfunction.LoadFiles(filenames.ToArray()));
                DMEEditor.AddLogMessage("Success", $"Loaded {retval.Count} files asynchronously", DateTime.Now, 0, string.Join(", ", filenames), Errors.Ok);
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not load files asynchronously: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, string.Join(", ", filenames), Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Loads connection properties for the specified file paths.
        /// </summary>
        /// <param name="filenames">Array of file paths to load.</param>
        /// <returns>A list of connection properties for loaded files, or null on failure.</returns>
        public static List<ConnectionProperties> LoadFiles(string[] filenames)
        {
            EnsureInitialized();
            if (filenames == null || !filenames.Any())
                throw new ArgumentException("Filenames cannot be null or empty");

            try
            {
                var retval = DMEEditor.Utilfunction.LoadFiles(filenames);
                DMEEditor.AddLogMessage("Success", $"Loaded {retval.Count} files", DateTime.Now, 0, string.Join(", ", filenames), Errors.Ok);
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not load files: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, string.Join(", ", filenames), Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Asynchronously loads connection properties for the specified file paths.
        /// </summary>
        /// <param name="filenames">Array of file paths to load.</param>
        /// <returns>A task that returns a list of connection properties for loaded files, or null on failure.</returns>
        public static async Task<List<ConnectionProperties>> LoadFilesAsync(string[] filenames)
        {
            EnsureInitialized();
            if (filenames == null || !filenames.Any())
                throw new ArgumentException("Filenames cannot be null or empty");

            try
            {
                var retval = await Task.Run(() => DMEEditor.Utilfunction.LoadFiles(filenames));
                DMEEditor.AddLogMessage("Success", $"Loaded {retval.Count} files asynchronously", DateTime.Now, 0, string.Join(", ", filenames), Errors.Ok);
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not load files asynchronously: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, string.Join(", ", filenames), Errors.Failed);
                return null;
            }
        }

        #endregion

        #region File Management Operations

        /// <summary>
        /// Adds a single file as a data connection if valid.
        /// </summary>
        /// <param name="file">The connection properties for the file.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddFile(ConnectionProperties file)
        {
            EnsureInitialized();
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            try
            {
                if (!IsFileValid(file.FileName))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {file.FileName} has an unsupported extension";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, file.FileName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                bool isValidDataFile = false;
                if (!DMEEditor.ConfigEditor.DataConnectionExist(file))
                {
                    isValidDataFile = DMEEditor.ConfigEditor.AddDataConnection(file);
                }
                if (isValidDataFile)
                {
                    IDataSource dataSource = DMEEditor.GetDataSource(file.FileName);
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                    DMEEditor.AddLogMessage("Success", $"Added file connection: {file.FileName}", DateTime.Now, 0, file.FileName, Errors.Ok);
                }
                else
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {file.FileName} already exists or is invalid";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, file.FileName, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add file {file.FileName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, file.FileName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Asynchronously adds a single file as a data connection if valid.
        /// </summary>
        /// <param name="file">The connection properties for the file.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> AddFileAsync(ConnectionProperties file)
        {
            EnsureInitialized();
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            try
            {
                if (!IsFileValid(file.FileName))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {file.FileName} has an unsupported extension";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, file.FileName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                bool isValidDataFile = false;
                if (!DMEEditor.ConfigEditor.DataConnectionExist(file))
                {
                    isValidDataFile = await Task.Run(() => DMEEditor.ConfigEditor.AddDataConnection(file));
                }
                if (isValidDataFile)
                {
                    IDataSource dataSource = DMEEditor.GetDataSource(file.FileName);
                    await Task.Run(() => DMEEditor.ConfigEditor.SaveDataconnectionsValues());
                    DMEEditor.AddLogMessage("Success", $"Added file connection asynchronously: {file.FileName}", DateTime.Now, 0, file.FileName, Errors.Ok);
                }
                else
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {file.FileName} already exists or is invalid";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, file.FileName, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add file asynchronously {file.FileName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, file.FileName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Adds multiple files as data connections if valid.
        /// </summary>
        /// <param name="files">List of connection properties for the files.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddFiles(List<ConnectionProperties> files)
        {
            EnsureInitialized();
            if (files == null || !files.Any())
                throw new ArgumentException("Files cannot be null or empty");

            try
            {
                int addedCount = 0;
                foreach (ConnectionProperties f in files)
                {
                    if (!IsFileValid(f.FileName))
                    {
                        DMEEditor.AddLogMessage("Warning", $"Skipping file {f.FileName} due to unsupported extension", DateTime.Now, 0, f.FileName, Errors.Ok);
                        continue;
                    }

                    if (!DMEEditor.ConfigEditor.DataConnectionExist(f))
                    {
                        if (DMEEditor.ConfigEditor.AddDataConnection(f))
                        {
                            IDataSource dataSource = DMEEditor.GetDataSource(f.FileName);
                            addedCount++;
                        }
                    }
                }
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                DMEEditor.AddLogMessage("Success", $"Added {addedCount} file connections", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add files: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Asynchronously adds multiple files as data connections if valid.
        /// </summary>
        /// <param name="files">List of connection properties for the files.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> AddFilesAsync(List<ConnectionProperties> files)
        {
            EnsureInitialized();
            if (files == null || !files.Any())
                throw new ArgumentException("Files cannot be null or empty");

            try
            {
                int addedCount = 0;
                foreach (ConnectionProperties f in files)
                {
                    if (!IsFileValid(f.FileName))
                    {
                        DMEEditor.AddLogMessage("Warning", $"Skipping file {f.FileName} due to unsupported extension", DateTime.Now, 0, f.FileName, Errors.Ok);
                        continue;
                    }

                    if (!DMEEditor.ConfigEditor.DataConnectionExist(f))
                    {
                        if (await Task.Run(() => DMEEditor.ConfigEditor.AddDataConnection(f)))
                        {
                            IDataSource dataSource = DMEEditor.GetDataSource(f.FileName);
                            addedCount++;
                        }
                    }
                }
                await Task.Run(() => DMEEditor.ConfigEditor.SaveDataconnectionsValues());
                DMEEditor.AddLogMessage("Success", $"Added {addedCount} file connections asynchronously", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add files asynchronously: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        #endregion

        #region File Validation

        /// <summary>
        /// Checks if a file has a valid extension supported by the data drivers.
        /// </summary>
        /// <param name="filename">The name or path of the file to validate.</param>
        /// <returns>True if the file extension is supported, false otherwise.</returns>
        public static bool IsFileValid(string filename)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(filename))
                return false;

            string ext = Path.GetExtension(filename).Replace(".", "").ToLower();
            List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            if (!clss.Any())
                return false;

            // Split comma-separated extension strings into a list of extensions
            IEnumerable<string> extensionsList = clss.SelectMany(p => p.extensionstoHandle.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().ToLower()));
            List<string> exts = extensionsList.Distinct().ToList();
            return exts.Contains(ext);
        }

        /// <summary>
        /// Gets supported file extensions from the configuration.
        /// </summary>
        /// <returns>A list of supported file extensions.</returns>
        public static List<string> GetSupportedFileExtensions()
        {
            EnsureInitialized();
            List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            if (!clss.Any())
                return new List<string>();

            IEnumerable<string> extensionsList = clss.SelectMany(p => p.extensionstoHandle.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().ToLower()));
            return extensionsList.Distinct().ToList();
        }

        /// <summary>
        /// Validates a file path for existence, readability, and supported format.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ValidateFilePath(string filePath)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(filePath))
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "File path cannot be null or empty";
                return DMEEditor.ErrorObject;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {filePath} does not exist";
                    return DMEEditor.ErrorObject;
                }

                if (!IsFileValid(filePath))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {filePath} has an unsupported extension";
                    return DMEEditor.ErrorObject;
                }

                // Test read permissions
                using (var stream = File.OpenRead(filePath))
                {
                    // File can be opened for reading
                }

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"File {filePath} is valid";
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Invalid file path {filePath}: {ex.Message}";
                return DMEEditor.ErrorObject;
            }
        }

        #endregion

        #region Folder Structure Operations

        /// <summary>
        /// Creates a folder structure for the specified path, including files and subfolders.
        /// </summary>
        /// <param name="path">The folder path to process.</param>
        /// <returns>The created folder structure.</returns>
        public static Folder CreateFolderStructure(string path)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty");

            Folder folder = new Folder(path)
            {
                Folders = new List<Folder>(),
                Name = new DirectoryInfo(path).Name,
                Url = path
            };

            try
            {
                IEnumerable<string> files = Directory.EnumerateFiles(path);
                foreach (string file in files)
                {
                    if (!IsFileValid(file))
                    {
                        DMEEditor.AddLogMessage("Warning", $"Skipping invalid file {file}", DateTime.Now, 0, file, Errors.Ok);
                        continue;
                    }

                    ConnectionProperties conn = DMEEditor.Utilfunction.CreateFileDataConnection(file);
                    if (conn != null)
                    {
                        FFile fileEntry = new FFile(file) { GuidID = conn.GuidID };
                        folder.Files.Add(fileEntry);
                        DMEEditor.ConfigEditor.AddDataConnection(conn);
                    }
                }

                IEnumerable<string> directories = Directory.EnumerateDirectories(path);
                foreach (string dirPath in directories)
                {
                    folder.Folders.Add(CreateFolderStructure(dirPath));
                }

                DMEEditor.AddLogMessage("Success", $"Created folder structure for {path}", DateTime.Now, 0, path, Errors.Ok);
                return folder;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not create folder structure for {path}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously creates a folder structure for the specified path, including files and subfolders.
        /// </summary>
        /// <param name="path">The folder path to process.</param>
        /// <returns>A task that returns the created folder structure.</returns>
        public static async Task<Folder> CreateFolderStructureAsync(string path)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty");

            Folder folder = new Folder(path)
            {
                Folders = new List<Folder>(),
                Name = new DirectoryInfo(path).Name,
                Url = path
            };

            try
            {
                string[] files = await Task.Run(() => Directory.GetFiles(path));
                foreach (string file in files)
                {
                    if (!IsFileValid(file))
                    {
                        DMEEditor.AddLogMessage("Warning", $"Skipping invalid file {file}", DateTime.Now, 0, file, Errors.Ok);
                        continue;
                    }

                    ConnectionProperties conn = await Task.Run(() => DMEEditor.Utilfunction.CreateFileDataConnection(file));
                    if (conn != null)
                    {
                        FFile fileEntry = new FFile(file) { GuidID = conn.GuidID };
                        folder.Files.Add(fileEntry);
                        await Task.Run(() => DMEEditor.ConfigEditor.AddDataConnection(conn));
                    }
                }

                string[] directories = await Task.Run(() => Directory.GetDirectories(path));
                foreach (string dirPath in directories)
                {
                    folder.Folders.Add(await CreateFolderStructureAsync(dirPath));
                }

                DMEEditor.AddLogMessage("Success", $"Created folder structure asynchronously for {path}", DateTime.Now, 0, path, Errors.Ok);
                return folder;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not create folder structure asynchronously for {path}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                throw;
            }
        }

        #endregion

        #region Private Methods

        private static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (!_isInitialized || DMEEditor == null)
                    throw new InvalidOperationException("FileOperationHelper must be initialized with a valid DMEEditor.");
            }
        }

        #endregion
    }
}