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

namespace TheTechIdea.Beep.Helpers
{
    public static class FileConnectionHelper
    {
        private static readonly object _lock = new object();
        private static bool _isInitialized;

        /// <summary>
        /// Gets the editor instance for configuration and logging.
        /// </summary>
        public static IDMEEditor DMEEditor { get; private set; }

        /// <summary>
        /// Event raised when a project is created.
        /// </summary>
        public static event Action<RootFolder> ProjectCreated;

        /// <summary>
        /// Initializes the FileConnectionHelper with the required editor.
        /// </summary>
        /// <param name="editor">The editor instance for configuration and logging.</param>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        public static void Initialize(IDMEEditor editor)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("FileConnectionHelper is already initialized.");

                DMEEditor = editor ?? throw new ArgumentNullException(nameof(editor));
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Resets the FileConnectionHelper, clearing the editor.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                DMEEditor = null;
                _isInitialized = false;
            }
        }
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
                string extens = DMEEditor.ConfigEditor.CreateFileExtensionString();
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
                string extens = DMEEditor.ConfigEditor.CreateFileExtensionString();
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

            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                string extens = DMEEditor.ConfigEditor.CreateFileExtensionString();
                var folder = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataFiles);
                if (folder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No data files folder configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }

                retval = DMEEditor.Utilfunction.LoadFiles(filenames.ToArray());
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

            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                string extens = DMEEditor.ConfigEditor.CreateFileExtensionString();
                var folder = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataFiles);
                if (folder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No data files folder configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }

                retval = await Task.Run(() => DMEEditor.Utilfunction.LoadFiles(filenames.ToArray()));
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

            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                retval = DMEEditor.Utilfunction.LoadFiles(filenames);
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

            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                retval = await Task.Run(() => DMEEditor.Utilfunction.LoadFiles(filenames));
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

        /// <summary>
        /// Adds a folder as a project using the specified folder path.
        /// </summary>
        /// <param name="foldername">The path of the folder to add as a project.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddFolder(string foldername)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(foldername))
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Folder path cannot be null or empty";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return DMEEditor.ErrorObject;
            }

            try
            {
                if (!Directory.Exists(foldername))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Folder {foldername} does not exist";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var result = CreateProject(foldername, ProjectFolderType.Files);
                if (result.Item1.Flag == Errors.Ok)
                {
                    ProjectCreated?.Invoke(result.Item2);
                }
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                DMEEditor.AddLogMessage(result.Item1.Flag == Errors.Ok ? "Success" : "Error", result.Item1.Message, DateTime.Now, result.Item1.Flag == Errors.Ok ? 0 : -1, foldername, result.Item1.Flag);
                return result.Item1;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add folder {foldername}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Asynchronously adds a folder as a project using the specified folder path.
        /// </summary>
        /// <param name="foldername">The path of the folder to add as a project.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> AddFolderAsync(string foldername)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(foldername))
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Folder path cannot be null or empty";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return DMEEditor.ErrorObject;
            }

            try
            {
                if (!Directory.Exists(foldername))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Folder {foldername} does not exist";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var result = await CreateProjectAsync(foldername, ProjectFolderType.Files);
                if (result.Item1.Flag == Errors.Ok)
                {
                    ProjectCreated?.Invoke(result.Item2);
                }
                await Task.Run(() => DMEEditor.ConfigEditor.SaveDataconnectionsValues());
                DMEEditor.AddLogMessage(result.Item1.Flag == Errors.Ok ? "Success" : "Error", result.Item1.Message, DateTime.Now, result.Item1.Flag == Errors.Ok ? 0 : -1, foldername, result.Item1.Flag);
                return result.Item1;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add folder asynchronously {foldername}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Creates a new project from the specified folder path.
        /// </summary>
        /// <param name="folderpath">The folder path for the new project.</param>
        /// <param name="folderType">The type of project folder (e.g., Files, Project).</param>
        /// <returns>A tuple containing error information and the created project folder, or null on failure.</returns>
        public static Tuple<IErrorsInfo, RootFolder> CreateProject(string folderpath, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            EnsureInitialized();
            RootFolder projectFolder = new RootFolder(folderpath) { FolderType = folderType };

            try
            {
                if (string.IsNullOrEmpty(folderpath))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "Project folder path cannot be null or empty";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, null);
                }

                if (!Directory.Exists(folderpath))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Project folder {folderpath} does not exist";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, null);
                }

                string dirname = new DirectoryInfo(folderpath).Name;
                projectFolder.Name = dirname;
                Folder folder = CreateFolderStructure(folderpath);
                projectFolder.Folders.Add(folder);

                // Add project to configuration
                if (DMEEditor.ConfigEditor.Projects == null)
                {
                    DMEEditor.ConfigEditor.Projects = new List<RootFolder>();
                }
                if (DMEEditor.ConfigEditor.Projects.Any(p => p.Url == projectFolder.Url))
                {
                    int idx = DMEEditor.ConfigEditor.Projects.FindIndex(p => p.Url == projectFolder.Url);
                    DMEEditor.ConfigEditor.Projects[idx] = projectFolder;
                }
                else
                {
                    DMEEditor.ConfigEditor.Projects.Add(projectFolder);
                }
                DMEEditor.ConfigEditor.SaveProjects();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Created project folder: {dirname}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, folderpath, Errors.Ok);
                return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, projectFolder);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not create project {folderpath}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, null);
            }
        }

        /// <summary>
        /// Asynchronously creates a new project from the specified folder path.
        /// </summary>
        /// <param name="folderpath">The folder path for the new project.</param>
        /// <param name="folderType">The type of project folder (e.g., Files, Project).</param>
        /// <returns>A task that returns a tuple containing error information and the created project folder, or null on failure.</returns>
        public static async Task<Tuple<IErrorsInfo, RootFolder>> CreateProjectAsync(string folderpath, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            EnsureInitialized();
            RootFolder projectFolder = new RootFolder(folderpath) { FolderType = folderType };

            try
            {
                if (string.IsNullOrEmpty(folderpath))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "Project folder path cannot be null or empty";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, null);
                }

                if (!Directory.Exists(folderpath))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Project folder {folderpath} does not exist";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, null);
                }

                string dirname = new DirectoryInfo(folderpath).Name;
                projectFolder.Name = dirname;
                Folder folder = await CreateFolderStructureAsync(folderpath);
                projectFolder.Folders.Add(folder);

                // Add project to configuration
                if (DMEEditor.ConfigEditor.Projects == null)
                {
                    DMEEditor.ConfigEditor.Projects = new List<RootFolder>();
                }
                if (DMEEditor.ConfigEditor.Projects.Any(p => p.Url == projectFolder.Url))
                {
                    int idx = DMEEditor.ConfigEditor.Projects.FindIndex(p => p.Url == projectFolder.Url);
                    DMEEditor.ConfigEditor.Projects[idx] = projectFolder;
                }
                else
                {
                    DMEEditor.ConfigEditor.Projects.Add(projectFolder);
                }
                await Task.Run(() => DMEEditor.ConfigEditor.SaveProjects());

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Created project folder asynchronously: {dirname}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, folderpath, Errors.Ok);
                return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, projectFolder);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not create project asynchronously {folderpath}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                return new Tuple<IErrorsInfo, RootFolder>(DMEEditor.ErrorObject, null);
            }
        }

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

        /// <summary>
        /// Refreshes the metadata for a specified project by re-scanning its folder for new files.
        /// </summary>
        /// <param name="projectName">The name of the project to refresh.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RefreshProject(string projectName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                var project = DMEEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                // Scan project folder for new files
                var files = Directory.GetFiles(project.Url, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsFileValid(f))
                    .ToList();
                foreach (var file in files)
                {
                    string filename = Path.GetFileName(file);
                    if (!DMEEditor.ConfigEditor.DataConnectionExist(filename))
                    {
                        var connection = DMEEditor.Utilfunction.CreateFileDataConnection(filename);
                        if (connection != null)
                        {
                            DMEEditor.ConfigEditor.AddDataConnection(connection);
                            DMEEditor.AddLogMessage("Success", $"Added new file connection: {filename}", DateTime.Now, 0, filename, Errors.Ok);
                        }
                    }
                }

                // Update LastModifiedDate
                project.LastModifiedDate = DateTime.Now;
                DMEEditor.ConfigEditor.SaveProjects();
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Refreshed project {projectName} with {files.Count} files";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Error refreshing project {projectName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Asynchronously refreshes the metadata for a specified project by re-scanning its folder for new files.
        /// </summary>
        /// <param name="projectName">The name of the project to refresh.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> RefreshProjectAsync(string projectName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                var project = DMEEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                // Scan project folder for new files asynchronously
                var files = await Task.Run(() => Directory.GetFiles(project.Url, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsFileValid(f))
                    .ToList());
                foreach (var file in files)
                {
                    string filename = Path.GetFileName(file);
                    if (!DMEEditor.ConfigEditor.DataConnectionExist(filename))
                    {
                        var connection = await Task.Run(() => DMEEditor.Utilfunction.CreateFileDataConnection(filename));
                        if (connection != null)
                        {
                            await Task.Run(() => DMEEditor.ConfigEditor.AddDataConnection(connection));
                            DMEEditor.AddLogMessage("Success", $"Added new file connection asynchronously: {filename}", DateTime.Now, 0, filename, Errors.Ok);
                        }
                    }
                }

                // Update LastModifiedDate
                project.LastModifiedDate = DateTime.Now;
                await Task.Run(() => DMEEditor.ConfigEditor.SaveProjects());
                await Task.Run(() => DMEEditor.ConfigEditor.SaveDataconnectionsValues());

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Refreshed project {projectName} with {files.Count} files asynchronously";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Error refreshing project asynchronously {projectName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

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
        /// Finds the category folder containing the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to check.</param>
        /// <param name="rootName">The root name of the category (e.g., "FILE").</param>
        /// <returns>The name of the category folder containing the file, or null if not found.</returns>
        public static string GetCategoryForFile(string fileName, string rootName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(rootName))
                return null;

            List<CategoryFolder> ls = DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == rootName).ToList();
            foreach (CategoryFolder item in ls)
            {
                if (item.items.Contains(fileName))
                {
                    return item.FolderName;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves a list of projects filtered by folder type.
        /// </summary>
        /// <param name="folderType">The type of project folder to filter (e.g., Files, Project).</param>
        /// <returns>A list of projects matching the specified folder type.</returns>
        public static List<RootFolder> GetProjects(ProjectFolderType folderType)
        {
            EnsureInitialized();
            try
            {
                if (DMEEditor.ConfigEditor.Projects == null)
                {
                    DMEEditor.AddLogMessage("Info", "No projects configured", DateTime.Now, 0, null, Errors.Ok);
                    return new List<RootFolder>();
                }

                var projects = DMEEditor.ConfigEditor.Projects.Where(p => p.FolderType == folderType).ToList();
                DMEEditor.AddLogMessage("Success", $"Retrieved {projects.Count} projects of type {folderType}", DateTime.Now, 0, null, Errors.Ok);
                return projects;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Error retrieving projects: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Retrieves all projects, optionally filtered by active status.
        /// </summary>
        /// <param name="onlyActive">If true, returns only active projects.</param>
        /// <returns>A list of projects.</returns>
        public static List<RootFolder> GetAllProjects(bool onlyActive = false)
        {
            EnsureInitialized();
            try
            {
                if (DMEEditor.ConfigEditor.Projects == null)
                {
                    DMEEditor.AddLogMessage("Info", "No projects configured", DateTime.Now, 0, null, Errors.Ok);
                    return new List<RootFolder>();
                }

                var projects = onlyActive
                    ? DMEEditor.ConfigEditor.Projects.Where(p => p.IsActive).ToList()
                    : DMEEditor.ConfigEditor.Projects.ToList();
                DMEEditor.AddLogMessage("Success", $"Retrieved {projects.Count} projects (onlyActive: {onlyActive})", DateTime.Now, 0, null, Errors.Ok);
                return projects;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Error retrieving projects: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Updates metadata for a specified project.
        /// </summary>
        /// <param name="projectName">The name of the project to update.</param>
        /// <param name="updateAction">The action to update the project's metadata.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectMetadata(string projectName, Action<RootFolder> updateAction)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");
            if (updateAction == null)
                throw new ArgumentNullException(nameof(updateAction));

            try
            {
                if (DMEEditor.ConfigEditor.Projects == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No projects configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var project = DMEEditor.ConfigEditor.Projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                updateAction(project);
                project.LastModifiedDate = DateTime.Now;
                DMEEditor.ConfigEditor.SaveProjects();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Updated metadata for project {projectName}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Error updating project metadata {projectName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Removes a project from the configuration.
        /// </summary>
        /// <param name="projectName">The name of the project to remove.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RemoveProject(string projectName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                if (DMEEditor.ConfigEditor.Projects == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No projects configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var project = DMEEditor.ConfigEditor.Projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                DMEEditor.ConfigEditor.Projects.Remove(project);
                DMEEditor.ConfigEditor.SaveProjects();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Removed project {projectName}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Error removing project {projectName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Archives a project by marking it as inactive.
        /// </summary>
        /// <param name="projectName">The name of the project to archive.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ArchiveProject(string projectName)
        {
            return UpdateProjectMetadata(projectName, p => p.IsActive = false);
        }

        /// <summary>
        /// Validates a project path for existence, write permissions, and valid files.
        /// </summary>
        /// <param name="path">The folder path to validate.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ValidateProjectPath(string path)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(path))
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Project path cannot be null or empty";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return DMEEditor.ErrorObject;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Project path {path} does not exist";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                // Test write permissions
                string testFile = Path.Combine(path, "test.temp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                // Check for valid files
                var validFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsFileValid(f))
                    .ToList();
                if (!validFiles.Any())
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Project path {path} contains no valid files";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Project path {path} is valid with {validFiles.Count} valid files";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, path, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Invalid project path {path}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Adds a new category folder to the configuration.
        /// </summary>
        /// <param name="folderName">The name of the category folder.</param>
        /// <param name="rootName">The root name of the category (e.g., "FILE").</param>
        /// <param name="fileNames">Optional list of file names to add to the category.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddCategoryFolder(string folderName, string rootName, List<string> fileNames = null)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(rootName))
                throw new ArgumentException("Folder name and root name cannot be null or empty");

            try
            {
                if (DMEEditor.ConfigEditor.CategoryFolders == null)
                {
                    DMEEditor.ConfigEditor.CategoryFolders = new List<CategoryFolder>();
                }

                if (DMEEditor.ConfigEditor.CategoryFolders.Any(f => f.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase) && f.RootName == rootName))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Category folder {folderName} already exists for root {rootName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var categoryFolder = new CategoryFolder
                {
                    FolderName = folderName,
                    RootName = rootName,
                    items = fileNames != null ? new BindingList<string>(fileNames) : new BindingList<string>()
                };
                DMEEditor.ConfigEditor.CategoryFolders.Add(categoryFolder);
                DMEEditor.ConfigEditor.SaveCategoryFoldersValues();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Added category folder {folderName} for root {rootName}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, folderName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not add category folder {folderName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Removes a category folder from the configuration.
        /// </summary>
        /// <param name="folderName">The name of the category folder to remove.</param>
        /// <param name="rootName">The root name of the category (e.g., "FILE").</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RemoveCategoryFolder(string folderName, string rootName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(rootName))
                throw new ArgumentException("Folder name and root name cannot be null or empty");

            try
            {
                if (DMEEditor.ConfigEditor.CategoryFolders == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No category folders configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var categoryFolder = DMEEditor.ConfigEditor.CategoryFolders.FirstOrDefault(f => f.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase) && f.RootName == rootName);
                if (categoryFolder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Category folder {folderName} not found for root {rootName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                DMEEditor.ConfigEditor.CategoryFolders.Remove(categoryFolder);
                DMEEditor.ConfigEditor.SaveCategoryFoldersValues();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Removed category folder {folderName} for root {rootName}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, folderName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not remove category folder {folderName}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, folderName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Moves a file from one category folder to another.
        /// </summary>
        /// <param name="fileName">The name of the file to move.</param>
        /// <param name="sourceCategory">The source category folder name.</param>
        /// <param name="targetCategory">The target category folder name.</param>
        /// <param name="rootName">The root name of the categories (e.g., "FILE").</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo MoveFileToCategory(string fileName, string sourceCategory, string targetCategory, string rootName)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(sourceCategory) || string.IsNullOrEmpty(targetCategory) || string.IsNullOrEmpty(rootName))
                throw new ArgumentException("File name, source category, target category, and root name cannot be null or empty");

            try
            {
                if (DMEEditor.ConfigEditor.CategoryFolders == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "No category folders configured";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, fileName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                var sourceFolder = DMEEditor.ConfigEditor.CategoryFolders.FirstOrDefault(f => f.FolderName.Equals(sourceCategory, StringComparison.OrdinalIgnoreCase) && f.RootName == rootName);
                var targetFolder = DMEEditor.ConfigEditor.CategoryFolders.FirstOrDefault(f => f.FolderName.Equals(targetCategory, StringComparison.OrdinalIgnoreCase) && f.RootName == rootName);

                if (sourceFolder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Source category folder {sourceCategory} not found for root {rootName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, fileName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                if (targetFolder == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"Target category folder {targetCategory} not found for root {rootName}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, fileName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                if (!sourceFolder.items.Contains(fileName))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = $"File {fileName} not found in source category {sourceCategory}";
                    DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, fileName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                sourceFolder.items.Remove(fileName);
                targetFolder.items.Add(fileName);
                DMEEditor.ConfigEditor.SaveCategoryFoldersValues();

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Moved file {fileName} from {sourceCategory} to {targetCategory}";
                DMEEditor.AddLogMessage("Success", DMEEditor.ErrorObject.Message, DateTime.Now, 0, fileName, Errors.Ok);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Could not move file {fileName} from {sourceCategory} to {targetCategory}: {ex.Message}";
                DMEEditor.AddLogMessage("Error", DMEEditor.ErrorObject.Message, DateTime.Now, -1, fileName, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }

        private static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (!_isInitialized || DMEEditor == null)
                    throw new InvalidOperationException("FileConnectionHelper must be initialized with a valid DMEEditor.");
            }
        }
    }
}