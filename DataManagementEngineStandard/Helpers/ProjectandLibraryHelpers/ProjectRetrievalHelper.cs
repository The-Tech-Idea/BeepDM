using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Helpers.ProjectandLibraryHelpers
{
    /// <summary>
    /// Helper class for project retrieval and filtering operations.
    /// </summary>
    public static class ProjectRetrievalHelper
    {
        /// <summary>
        /// Retrieves a list of projects filtered by folder type.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="folderType">The type of project folder to filter (e.g., Files, Project).</param>
        /// <returns>A list of projects matching the specified folder type.</returns>
        public static List<RootFolder> GetProjects(IDMEEditor dmeEditor, ProjectFolderType folderType)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                {
                    dmeEditor.AddLogMessage("Info", "No projects configured", DateTime.Now, 0, null, Errors.Ok);
                    return new List<RootFolder>();
                }

                var projects = dmeEditor.ConfigEditor.Projects.Where(p => p.FolderType == folderType).ToList();
                dmeEditor.AddLogMessage("Success", $"Retrieved {projects.Count} projects of type {folderType}", DateTime.Now, 0, null, Errors.Ok);
                return projects;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error retrieving projects: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Retrieves all projects, optionally filtered by active status.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="onlyActive">If true, returns only active projects.</param>
        /// <returns>A list of projects.</returns>
        public static List<RootFolder> GetAllProjects(IDMEEditor dmeEditor, bool onlyActive = false)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                {
                    dmeEditor.AddLogMessage("Info", "No projects configured", DateTime.Now, 0, null, Errors.Ok);
                    return new List<RootFolder>();
                }

                var projects = onlyActive
                    ? dmeEditor.ConfigEditor.Projects.Where(p => p.IsActive).ToList()
                    : dmeEditor.ConfigEditor.Projects.ToList();
                dmeEditor.AddLogMessage("Success", $"Retrieved {projects.Count} projects (onlyActive: {onlyActive})", DateTime.Now, 0, null, Errors.Ok);
                return projects;
            }
            catch (Exception ex)
            {
                dmeEditor.ErrorObject.Flag = Errors.Failed;
                dmeEditor.ErrorObject.Message = $"Error retrieving projects: {ex.Message}";
                dmeEditor.AddLogMessage("Error", dmeEditor.ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Gets a project by name.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="projectName">The name of the project to retrieve.</param>
        /// <returns>The project if found, otherwise null.</returns>
        public static RootFolder GetProjectByName(IDMEEditor dmeEditor, string projectName)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(projectName))
                return null;

            try
            {
                return dmeEditor.ConfigEditor.Projects?.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error getting project {projectName}: {ex.Message}", DateTime.Now, -1, projectName, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Gets projects by author.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="author">The author name to filter by.</param>
        /// <returns>A list of projects by the specified author.</returns>
        public static List<RootFolder> GetProjectsByAuthor(IDMEEditor dmeEditor, string author)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(author))
                return new List<RootFolder>();

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                    return new List<RootFolder>();

                return dmeEditor.ConfigEditor.Projects
                    .Where(p => !string.IsNullOrEmpty(p.Author) && p.Author.Equals(author, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error getting projects by author {author}: {ex.Message}", DateTime.Now, -1, author, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Gets projects by version.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="version">The version to filter by.</param>
        /// <returns>A list of projects with the specified version.</returns>
        public static List<RootFolder> GetProjectsByVersion(IDMEEditor dmeEditor, string version)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(version))
                return new List<RootFolder>();

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                    return new List<RootFolder>();

                return dmeEditor.ConfigEditor.Projects
                    .Where(p => !string.IsNullOrEmpty(p.Version) && p.Version.Equals(version, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error getting projects by version {version}: {ex.Message}", DateTime.Now, -1, version, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Gets projects by tags.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="tag">The tag to filter by.</param>
        /// <returns>A list of projects containing the specified tag.</returns>
        public static List<RootFolder> GetProjectsByTag(IDMEEditor dmeEditor, string tag)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(tag))
                return new List<RootFolder>();

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                    return new List<RootFolder>();

                return dmeEditor.ConfigEditor.Projects
                    .Where(p => !string.IsNullOrEmpty(p.Tags) && p.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error getting projects by tag {tag}: {ex.Message}", DateTime.Now, -1, tag, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Gets projects modified within a date range.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="startDate">The start date for the range.</param>
        /// <param name="endDate">The end date for the range.</param>
        /// <returns>A list of projects modified within the specified date range.</returns>
        public static List<RootFolder> GetProjectsByDateRange(IDMEEditor dmeEditor, DateTime startDate, DateTime endDate)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                    return new List<RootFolder>();

                return dmeEditor.ConfigEditor.Projects
                    .Where(p => p.LastModifiedDate >= startDate && p.LastModifiedDate <= endDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error getting projects by date range: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new List<RootFolder>();
            }
        }

        /// <summary>
        /// Searches projects by name pattern.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance.</param>
        /// <param name="pattern">The search pattern (supports wildcards).</param>
        /// <returns>A list of projects matching the search pattern.</returns>
        public static List<RootFolder> SearchProjectsByName(IDMEEditor dmeEditor, string pattern)
        {
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));

            if (string.IsNullOrEmpty(pattern))
                return new List<RootFolder>();

            try
            {
                if (dmeEditor.ConfigEditor.Projects == null)
                    return new List<RootFolder>();

                // Convert wildcard pattern to regex if needed
                var regexPattern = pattern.Replace("*", ".*").Replace("?", ".");
                var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                return dmeEditor.ConfigEditor.Projects
                    .Where(p => !string.IsNullOrEmpty(p.Name) && regex.IsMatch(p.Name))
                    .ToList();
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage("Error", $"Error searching projects by name pattern {pattern}: {ex.Message}", DateTime.Now, -1, pattern, Errors.Failed);
                return new List<RootFolder>();
            }
        }
    }
}