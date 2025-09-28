using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.ProjectandLibraryHelpers
{
    /// <summary>
    /// Helper class for project creation and management operations.
    /// </summary>
    public static class ProjectCreationHelper
    {
        /// <summary>
        /// Creates a new project from the specified folder path.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="folderpath">The folder path for the new project.</param>
        /// <param name="folderType">The type of project folder (e.g., Files, Project).</param>
        /// <returns>A tuple containing error information and the created project folder, or null on failure.</returns>
        public static Tuple<IErrorsInfo, RootFolder> CreateProject(IDMEEditor dmeEditor, string folderpath, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            RootFolder projectFolder = new RootFolder(folderpath) { FolderType = folderType };

            try
            {
                if (string.IsNullOrEmpty(folderpath))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = "Project folder path cannot be null or empty";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, null);
                }

                if (!Directory.Exists(folderpath))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Project folder {folderpath} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, null);
                }

                string dirname = new DirectoryInfo(folderpath).Name;
                projectFolder.Name = dirname;
                
                // Use FileOperationHelper to create folder structure
                var folder = FileandFolderHelpers.FileOperationHelper.CreateFolderStructure(folderpath);
                projectFolder.Folders.Add(folder);

                // Add project to configuration
                if (dmeEditor.ConfigEditor.Projects == null)
                {
                    dmeEditor.ConfigEditor.Projects = new List<RootFolder>();
                }
                
                if (dmeEditor.ConfigEditor.Projects.Any(p => p.Url == projectFolder.Url))
                {
                    int idx = dmeEditor.ConfigEditor.Projects.FindIndex(p => p.Url == projectFolder.Url);
                    dmeEditor.ConfigEditor.Projects[idx] = projectFolder;
                }
                else
                {
                    dmeEditor.ConfigEditor.Projects.Add(projectFolder);
                }
                
                dmeEditor.ConfigEditor.SaveProjects();

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Created project folder: {dirname}";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, folderpath, Errors.Ok);
                return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, projectFolder);
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Could not create project {folderpath}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, null);
            }
        }

        /// <summary>
        /// Asynchronously creates a new project from the specified folder path.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="folderpath">The folder path for the new project.</param>
        /// <param name="folderType">The type of project folder (e.g., Files, Project).</param>
        /// <returns>A task that returns a tuple containing error information and the created project folder, or null on failure.</returns>
        public static async Task<Tuple<IErrorsInfo, RootFolder>> CreateProjectAsync(IDMEEditor dmeEditor, string folderpath, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            RootFolder projectFolder = new RootFolder(folderpath) { FolderType = folderType };

            try
            {
                if (string.IsNullOrEmpty(folderpath))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = "Project folder path cannot be null or empty";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, null);
                }

                if (!Directory.Exists(folderpath))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Project folder {folderpath} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                    return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, null);
                }

                string dirname = new DirectoryInfo(folderpath).Name;
                projectFolder.Name = dirname;
                
                var folder = await FileandFolderHelpers.FileOperationHelper.CreateFolderStructureAsync(folderpath);
                projectFolder.Folders.Add(folder);

                // Add project to configuration
                if (dmeEditor.ConfigEditor.Projects == null)
                {
                    dmeEditor.ConfigEditor.Projects = new List<RootFolder>();
                }
                
                if (dmeEditor.ConfigEditor.Projects.Any(p => p.Url == projectFolder.Url))
                {
                    int idx = dmeEditor.ConfigEditor.Projects.FindIndex(p => p.Url == projectFolder.Url);
                    dmeEditor.ConfigEditor.Projects[idx] = projectFolder;
                }
                else
                {
                    dmeEditor.ConfigEditor.Projects.Add(projectFolder);
                }
                
                await Task.Run(() => dmeEditor.ConfigEditor.SaveProjects());

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Created project folder asynchronously: {dirname}";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, folderpath, Errors.Ok);
                return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, projectFolder);
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Could not create project asynchronously {folderpath}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, folderpath, Errors.Failed);
                return new Tuple<IErrorsInfo, RootFolder>(dmeEditor.ErrorObject, null);
            }
        }

        /// <summary>
        /// Creates a new project with custom configuration and metadata.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="folderpath">The folder path for the project.</param>
        /// <param name="description">The project description.</param>
        /// <param name="author">The project author.</param>
        /// <param name="version">The project version.</param>
        /// <param name="folderType">The type of project folder.</param>
        /// <returns>A tuple containing error information and the created project folder.</returns>
        public static Tuple<IErrorsInfo, RootFolder> CreateProjectWithMetadata(
            IDMEEditor dmeEditor,
            string projectName, 
            string folderpath, 
            string description = null, 
            string author = null, 
            string version = "1.0.0",
            ProjectFolderType folderType = ProjectFolderType.Files)
        {
            var result = CreateProject(dmeEditor, folderpath, folderType);
            if (result.Item1.Flag == Errors.Ok && result.Item2 != null)
            {
                result.Item2.Name = projectName;
                result.Item2.Description = description ?? string.Empty;
                result.Item2.Author = author ?? Environment.UserName;
                result.Item2.Version = version;
                result.Item2.IsActive = true;
                
                dmeEditor.ConfigEditor.SaveProjects();
                dmeEditor.AddLogMessage("Success", $"Created project {projectName} with metadata", DateTime.Now, 0, folderpath, Errors.Ok);
            }
            return result;
        }

        /// <summary>
        /// Adds a folder as a project using the specified folder path.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="foldername">The path of the folder to add as a project.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddFolder(IDMEEditor dmeEditor, string foldername)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(foldername))
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = "Folder path cannot be null or empty";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return dmeEditor.ErrorObject;
            }

            try
            {
                if (!Directory.Exists(foldername))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Folder {foldername} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                var result = CreateProject(dmeEditor, foldername, ProjectFolderType.Files);
                dmeEditor.ConfigEditor.SaveDataconnectionsValues();
                dmeEditor.AddLogMessage(result.Item1.Flag == Errors.Ok ? "Success" : "Error", result.Item1.Message, DateTime.Now, result.Item1.Flag == Errors.Ok ? 0 : -1, foldername, result.Item1.Flag);
                return result.Item1;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Could not add folder {foldername}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Asynchronously adds a folder as a project using the specified folder path.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="foldername">The path of the folder to add as a project.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> AddFolderAsync(IDMEEditor dmeEditor, string foldername)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(foldername))
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = "Folder path cannot be null or empty";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return dmeEditor.ErrorObject;
            }

            try
            {
                if (!Directory.Exists(foldername))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Folder {foldername} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                var result = await CreateProjectAsync(dmeEditor, foldername, ProjectFolderType.Files);
                await Task.Run(() => dmeEditor.ConfigEditor.SaveDataconnectionsValues());
                dmeEditor.AddLogMessage(result.Item1.Flag == Errors.Ok ? "Success" : "Error", result.Item1.Message, DateTime.Now, result.Item1.Flag == Errors.Ok ? 0 : -1, foldername, result.Item1.Flag);
                return result.Item1;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Could not add folder asynchronously {foldername}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, foldername, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }
    }
}