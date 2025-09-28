using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers.FileandFolderHelpers;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Helpers.ProjectandLibraryHelpers
{
    /// <summary>
    /// Helper class for project refresh and synchronization operations.
    /// </summary>
    public static class ProjectSynchronizationHelper
    {
        /// <summary>
        /// Refreshes the metadata for a specified project by re-scanning its folder for new files.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to refresh.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RefreshProject(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                var project = dmeEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                // Scan project folder for new files
                var files = Directory.GetFiles(project.Url, "*.*", SearchOption.AllDirectories)
                    .Where(f => FileOperationHelper.IsFileValid(f))
                    .ToList();
                    
                foreach (var file in files)
                {
                    string filename = Path.GetFileName(file);
                    if (!dmeEditor.ConfigEditor.DataConnectionExist(filename))
                    {
                        var connection = dmeEditor.Utilfunction.CreateFileDataConnection(filename);
                        if (connection != null)
                        {
                            dmeEditor.ConfigEditor.AddDataConnection(connection);
                            dmeEditor.AddLogMessage("Success", $"Added new file connection: {filename}", DateTime.Now, 0, filename, Errors.Ok);
                        }
                    }
                }

                // Update LastModifiedDate
                project.LastModifiedDate = DateTime.Now;
                dmeEditor.ConfigEditor.SaveProjects();
                dmeEditor.ConfigEditor.SaveDataconnectionsValues();

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Refreshed project {projectName} with {files.Count} files";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error refreshing project {projectName}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Asynchronously refreshes the metadata for a specified project by re-scanning its folder for new files.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to refresh.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> RefreshProjectAsync(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                var project = dmeEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                // Scan project folder for new files asynchronously
                var files = await Task.Run(() => Directory.GetFiles(project.Url, "*.*", SearchOption.AllDirectories)
                    .Where(f => FileOperationHelper.IsFileValid(f))
                    .ToList());
                    
                foreach (var file in files)
                {
                    string filename = Path.GetFileName(file);
                    if (!dmeEditor.ConfigEditor.DataConnectionExist(filename))
                    {
                        var connection = await Task.Run(() => dmeEditor.Utilfunction.CreateFileDataConnection(filename));
                        if (connection != null)
                        {
                            await Task.Run(() => dmeEditor.ConfigEditor.AddDataConnection(connection));
                            dmeEditor.AddLogMessage("Success", $"Added new file connection asynchronously: {filename}", DateTime.Now, 0, filename, Errors.Ok);
                        }
                    }
                }

                // Update LastModifiedDate
                project.LastModifiedDate = DateTime.Now;
                await Task.Run(() => dmeEditor.ConfigEditor.SaveProjects());
                await Task.Run(() => dmeEditor.ConfigEditor.SaveDataconnectionsValues());

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Refreshed project {projectName} with {files.Count} files asynchronously";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error refreshing project asynchronously {projectName}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Synchronizes all projects by refreshing their file listings.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo SynchronizeAllProjects(IDMEEditor dmeEditor)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            try
            {
                var projects = ProjectRetrievalHelper.GetAllProjects(dmeEditor, true); // Only active projects
                int successCount = 0;
                int failCount = 0;

                foreach (var project in projects)
                {
                    var result = RefreshProject(dmeEditor, project.Name);
                    if (result.Flag == Errors.Ok)
                        successCount++;
                    else
                        failCount++;
                }

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Synchronized {successCount} projects successfully, {failCount} failed";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, null, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error synchronizing projects: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Asynchronously synchronizes all projects by refreshing their file listings.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> SynchronizeAllProjectsAsync(IDMEEditor dmeEditor)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            try
            {
                var projects = ProjectRetrievalHelper.GetAllProjects(dmeEditor, true); // Only active projects
                int successCount = 0;
                int failCount = 0;

                foreach (var project in projects)
                {
                    var result = await RefreshProjectAsync(dmeEditor, project.Name);
                    if (result.Flag == Errors.Ok)
                        successCount++;
                    else
                        failCount++;
                }

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Synchronized {successCount} projects successfully, {failCount} failed (async)";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, null, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error synchronizing projects asynchronously: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Rebuilds a project's folder structure from the file system.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to rebuild.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RebuildProjectStructure(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                var project = dmeEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                if (!Directory.Exists(project.Url))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Project folder {project.Url} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                // Clear existing folder structure
                project.Folders.Clear();

                // Rebuild folder structure
                var folder = FileOperationHelper.CreateFolderStructure(project.Url);
                project.Folders.Add(folder);

                // Update metadata
                project.LastModifiedDate = DateTime.Now;
                dmeEditor.ConfigEditor.SaveProjects();

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Rebuilt project structure for {projectName}";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error rebuilding project structure {projectName}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Asynchronously rebuilds a project's folder structure from the file system.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to rebuild.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> RebuildProjectStructureAsync(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                var project = dmeEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                if (!Directory.Exists(project.Url))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Project folder {project.Url} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                // Clear existing folder structure
                project.Folders.Clear();

                // Rebuild folder structure asynchronously
                var folder = await FileOperationHelper.CreateFolderStructureAsync(project.Url);
                project.Folders.Add(folder);

                // Update metadata
                project.LastModifiedDate = DateTime.Now;
                await Task.Run(() => dmeEditor.ConfigEditor.SaveProjects());

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Rebuilt project structure for {projectName} asynchronously";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error rebuilding project structure asynchronously {projectName}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }
    }
}