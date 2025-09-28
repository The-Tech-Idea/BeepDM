using System;
using System.Linq;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Helpers.ProjectandLibraryHelpers
{
    /// <summary>
    /// Helper class for project metadata management operations.
    /// </summary>
    public static class ProjectMetadataHelper
    {
        /// <summary>
        /// Updates metadata for a specified project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to update.</param>
        /// <param name="updateAction">The action to update the project's metadata.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectMetadata(IDMEEditor dmeEditor, string projectName, Action<RootFolder> updateAction)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException("Project name cannot be null or empty");
            if (updateAction == null)
                throw new ArgumentNullException(nameof(updateAction));

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

                updateAction(project);
                project.LastModifiedDate = DateTime.Now;
                dmeEditor.ConfigEditor.SaveProjects();

                dmeEditor.ErrorObject.Flag = Errors.Ok;
                dmeEditor.ErrorObject.Message = $"Updated metadata for project {projectName}";
                dmeEditor.AddLogMessage("Success", dmeEditor.ErrorObject.Message, DateTime.Now, 0, projectName, Errors.Ok);
                return dmeEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error updating project metadata {projectName}: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, projectName, Errors.Failed);
                return dmeEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Updates the description of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="description">The new description.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectDescription(IDMEEditor dmeEditor, string projectName, string description)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.Description = description);
        }

        /// <summary>
        /// Updates the version of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="version">The new version.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectVersion(IDMEEditor dmeEditor, string projectName, string version)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.Version = version);
        }

        /// <summary>
        /// Updates the author of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="author">The new author.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectAuthor(IDMEEditor dmeEditor, string projectName, string author)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.Author = author);
        }

        /// <summary>
        /// Updates the tags of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="tags">The new tags.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectTags(IDMEEditor dmeEditor, string projectName, string tags)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.Tags = tags);
        }

        /// <summary>
        /// Updates the icon of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="icon">The new icon path or name.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectIcon(IDMEEditor dmeEditor, string projectName, string icon)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.Icon = icon);
        }

        /// <summary>
        /// Sets the active status of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="isActive">The active status to set.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo SetProjectActiveStatus(IDMEEditor dmeEditor, string projectName, bool isActive)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.IsActive = isActive);
        }

        /// <summary>
        /// Sets the private status of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="isPrivate">The private status to set.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo SetProjectPrivateStatus(IDMEEditor dmeEditor, string projectName, bool isPrivate)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.IsPrivate = isPrivate);
        }

        /// <summary>
        /// Updates the folder type of a project.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="folderType">The new folder type.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectFolderType(IDMEEditor dmeEditor, string projectName, ProjectFolderType folderType)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p => p.FolderType = folderType);
        }

        /// <summary>
        /// Updates multiple project properties in a single operation.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="description">The new description (optional).</param>
        /// <param name="version">The new version (optional).</param>
        /// <param name="author">The new author (optional).</param>
        /// <param name="tags">The new tags (optional).</param>
        /// <param name="icon">The new icon (optional).</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectProperties(IDMEEditor dmeEditor, string projectName, 
            string description = null, string version = null, string author = null, 
            string tags = null, string icon = null)
        {
            return UpdateProjectMetadata(dmeEditor, projectName, p =>
            {
                if (description != null) p.Description = description;
                if (version != null) p.Version = version;
                if (author != null) p.Author = author;
                if (tags != null) p.Tags = tags;
                if (icon != null) p.Icon = icon;
            });
        }

        /// <summary>
        /// Adds a tag to a project's existing tags.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="tag">The tag to add.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddProjectTag(IDMEEditor dmeEditor, string projectName, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                throw new ArgumentException("Tag cannot be null or empty");

            return UpdateProjectMetadata(dmeEditor, projectName, p =>
            {
                if (string.IsNullOrEmpty(p.Tags))
                {
                    p.Tags = tag;
                }
                else if (!p.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase))
                {
                    p.Tags = $"{p.Tags},{tag}";
                }
            });
        }

        /// <summary>
        /// Removes a tag from a project's existing tags.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="tag">The tag to remove.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RemoveProjectTag(IDMEEditor dmeEditor, string projectName, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                throw new ArgumentException("Tag cannot be null or empty");

            return UpdateProjectMetadata(dmeEditor, projectName, p =>
            {
                if (!string.IsNullOrEmpty(p.Tags))
                {
                    var tags = p.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Where(t => !t.Trim().Equals(tag, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    p.Tags = string.Join(",", tags);
                }
            });
        }
    }
}