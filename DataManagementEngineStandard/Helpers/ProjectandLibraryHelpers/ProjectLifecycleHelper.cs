using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Helpers.ProjectandLibraryHelpers
{
    /// <summary>
    /// Helper class for project lifecycle management operations including removal, archiving, and validation.
    /// </summary>
    public static class ProjectLifecycleHelper
    {
        /// <summary>
        /// Removes a project from the configuration.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to remove.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RemoveProject(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = "No projects configured";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                var project = dmeEditor.ConfigEditor.Projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Could not find project {projectName}";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                dmeEditor.ConfigEditor.Projects.Remove(project);
                dmeEditor.ConfigEditor.SaveProjects();

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Removed project {projectName}";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error removing project {projectName}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Archives a project by marking it as inactive.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to archive.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ArchiveProject(IDMEEditor dmeEditor, string projectName)
        {
            return ProjectMetadataHelper.UpdateProjectMetadata(dmeEditor, projectName, p => p.IsActive = false);
        }

        /// <summary>
        /// Activates an archived project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to activate.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ActivateProject(IDMEEditor dmeEditor, string projectName)
        {
            return ProjectMetadataHelper.UpdateProjectMetadata(dmeEditor, projectName, p => p.IsActive = true);
        }

        /// <summary>
        /// Duplicates a project with a new name.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="sourceProjectName">The name of the project to duplicate.</param>
        /// <param name="newProjectName">The name for the new project.</param>
        /// <param name="newFolderPath">The folder path for the new project.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo DuplicateProject(IDMEEditor dmeEditor, string sourceProjectName, string newProjectName, string newFolderPath)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            try
            {
                var sourceProject = ProjectRetrievalHelper.GetProjectByName(dmeEditor, sourceProjectName);
                if (sourceProject == null)
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Source project {sourceProjectName} not found";
                    return dmeEditor.ErrorObject;
                }

                var result = ProjectCreationHelper.CreateProjectWithMetadata(
                    dmeEditor,
                    newProjectName,
                    newFolderPath,
                    sourceProject.Description,
                    sourceProject.Author,
                    sourceProject.Version,
                    sourceProject.FolderType);

                if (result.Item1.Flag == Errors.Ok)
                {
                    result.Item2.Tags = sourceProject.Tags;
                    result.Item2.Icon = sourceProject.Icon;
                    dmeEditor.ConfigEditor.SaveProjects();
                }

                return result.Item1;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error duplicating project: {ex.Message}";
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Validates a project path for existence, write permissions, and valid files.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="path">The folder path to validate.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ValidateProjectPath(IDMEEditor dmeEditor, string path)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(path))
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = "Project path cannot be null or empty";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return dmeEditor.ErrorObject;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Project path {path} does not exist";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                // Test write permissions
                string testFile = Path.Combine(path, "test.temp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                // Check for valid files
                var validFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => FileandFolderHelpers.FileOperationHelper.IsFileValid(f))
                    .ToList();
                    
                if (!validFiles.Any())
                {
                    dmeEditor.ErrorObject.Flag = Errors.Failed;
                    dmeEditor.ErrorObject.Message = $"Project path {path} contains no valid files";
                    dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                    return dmeEditor.ErrorObject;
                }

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Project path {path} is valid with {validFiles.Count} valid files";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, path, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Invalid project path {path}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, path, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Validates that a project name is unique.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The project name to validate.</param>
        /// <returns>True if the name is unique, false otherwise.</returns>
        public static bool IsProjectNameUnique(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(projectName))
                return false;

            try
            {
                return dmeEditor.ConfigEditor.Projects?.Any(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase)) != true;
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error checking project name uniqueness: {ex.Message}", DateTime.Now, -1, projectName, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Renames a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="oldProjectName">The current name of the project.</param>
        /// <param name="newProjectName">The new name for the project.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RenameProject(IDMEEditor dmeEditor, string oldProjectName, string newProjectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(oldProjectName) || string.IsNullOrEmpty(newProjectName))
                throw new ArgumentException("Project names cannot be null or empty");

            // Check if new name is unique
            if (!IsProjectNameUnique(dmeEditor, newProjectName))
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Project name {newProjectName} already exists";
                return dmeEditor.ErrorObject;
            }

            return ProjectMetadataHelper.UpdateProjectMetadata(dmeEditor, oldProjectName, p => p.Name = newProjectName);
        }

        /// <summary>
        /// Gets project statistics including file count, size, etc.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <returns>A dictionary containing project statistics.</returns>
        public static Dictionary<string, object> GetProjectStatistics(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            var stats = new Dictionary<string, object>();
            
            try
            {
                var project = ProjectRetrievalHelper.GetProjectByName(dmeEditor, projectName);
                if (project == null)
                {
                    stats["Error"] = $"Project {projectName} not found";
                    return stats;
                }

                if (!Directory.Exists(project.Url))
                {
                    stats["Error"] = $"Project folder {project.Url} does not exist";
                    return stats;
                }

                var files = Directory.GetFiles(project.Url, "*.*", SearchOption.AllDirectories);
                var validFiles = files.Where(f => FileandFolderHelpers.FileOperationHelper.IsFileValid(f)).ToArray();
                
                stats["TotalFiles"] = files.Length;
                stats["ValidFiles"] = validFiles.Length;
                stats["TotalSize"] = files.Sum(f => new FileInfo(f).Length);
                stats["ValidFileSize"] = validFiles.Sum(f => new FileInfo(f).Length);
                stats["FolderCount"] = Directory.GetDirectories(project.Url, "*", SearchOption.AllDirectories).Length;
                stats["CreatedDate"] = project.AddedDate;
                stats["LastModified"] = project.LastModifiedDate;
                stats["IsActive"] = project.IsActive;
                stats["Author"] = project.Author;
                stats["Version"] = project.Version;
            }
            catch (Exception ex)
            {
                stats["Error"] = ex.Message;
                dmeEditor.AddLogMessage("Error", $"Error getting project statistics for {projectName}: {ex.Message}", DateTime.Now, -1, projectName, Errors.Failed);
            }

            return stats;
        }

        /// <summary>
        /// Exports project metadata to a dictionary.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <returns>A dictionary containing project metadata.</returns>
        public static Dictionary<string, object> ExportProjectMetadata(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            var metadata = new Dictionary<string, object>();
            
            try
            {
                var project = ProjectRetrievalHelper.GetProjectByName(dmeEditor, projectName);
                if (project == null)
                {
                    metadata["Error"] = $"Project {projectName} not found";
                    return metadata;
                }

                metadata["Name"] = project.Name;
                metadata["Description"] = project.Description;
                metadata["Author"] = project.Author;
                metadata["Version"] = project.Version;
                metadata["Tags"] = project.Tags;
                metadata["Icon"] = project.Icon;
                metadata["Url"] = project.Url;
                metadata["FolderType"] = project.FolderType.ToString();
                metadata["IsActive"] = project.IsActive;
                metadata["IsPrivate"] = project.IsPrivate;
                metadata["AddedDate"] = project.AddedDate;
                metadata["LastModifiedDate"] = project.LastModifiedDate;
                metadata["GuidID"] = project.GuidID;
            }
            catch (Exception ex)
            {
                metadata["Error"] = ex.Message;
                dmeEditor.AddLogMessage("Error", $"Error exporting project metadata for {projectName}: {ex.Message}", DateTime.Now, -1, projectName, Errors.Failed);
            }

            return metadata;
        }
    }
}