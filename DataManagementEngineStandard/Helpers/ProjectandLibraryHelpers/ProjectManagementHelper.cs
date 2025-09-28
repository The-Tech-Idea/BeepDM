using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Helpers.ProjectandLibraryHelpers
{
    /// <summary>
    /// Main facade for project and library management operations, delegating to specialized helper classes.
    /// </summary>
    public static partial class ProjectManagementHelper
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
        /// Event raised when a project is updated.
        /// </summary>
        public static event Action<RootFolder> ProjectUpdated;

        /// <summary>
        /// Event raised when a project is removed.
        /// </summary>
        public static event Action<string> ProjectRemoved;

        /// <summary>
        /// Initializes the ProjectManagementHelper with the required editor.
        /// </summary>
        /// <param name="editor">The editor instance for configuration and logging.</param>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        public static void Initialize(IDMEEditor editor)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("ProjectManagementHelper is already initialized.");

                DMEEditor = editor ?? throw new ArgumentNullException(nameof(editor));
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Resets the ProjectManagementHelper, clearing the editor.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                DMEEditor = null;
                _isInitialized = false;
            }
        }

        #region Project Creation and Management (Delegated to ProjectCreationHelper)

        /// <summary>
        /// Adds a folder as a project using the specified folder path.
        /// </summary>
        /// <param name="foldername">The path of the folder to add as a project.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo AddFolder(string foldername)
        {
            EnsureInitialized();
            var result = ProjectCreationHelper.AddFolder(DMEEditor, foldername);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, System.IO.Path.GetFileName(foldername));
                ProjectCreated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Asynchronously adds a folder as a project using the specified folder path.
        /// </summary>
        /// <param name="foldername">The path of the folder to add as a project.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> AddFolderAsync(string foldername)
        {
            EnsureInitialized();
            var result = await ProjectCreationHelper.AddFolderAsync(DMEEditor, foldername);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, System.IO.Path.GetFileName(foldername));
                ProjectCreated?.Invoke(project);
            }
            return result;
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
            var result = ProjectCreationHelper.CreateProject(DMEEditor, folderpath, folderType);
            if (result.Item1.Flag == Errors.Ok && result.Item2 != null)
            {
                ProjectCreated?.Invoke(result.Item2);
            }
            return result;
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
            var result = await ProjectCreationHelper.CreateProjectAsync(DMEEditor, folderpath, folderType);
            if (result.Item1.Flag == Errors.Ok && result.Item2 != null)
            {
                ProjectCreated?.Invoke(result.Item2);
            }
            return result;
        }

        /// <summary>
        /// Creates a new project with custom configuration and metadata.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="folderpath">The folder path for the project.</param>
        /// <param name="description">The project description.</param>
        /// <param name="author">The project author.</param>
        /// <param name="version">The project version.</param>
        /// <param name="folderType">The type of project folder.</param>
        /// <returns>A tuple containing error information and the created project folder.</returns>
        public static Tuple<IErrorsInfo, RootFolder> CreateProjectWithMetadata(
            string projectName, 
            string folderpath, 
            string description = null, 
            string author = null, 
            string version = "1.0.0",
            ProjectFolderType folderType = ProjectFolderType.Files)
        {
            EnsureInitialized();
            var result = ProjectCreationHelper.CreateProjectWithMetadata(DMEEditor, projectName, folderpath, description, author, version, folderType);
            if (result.Item1.Flag == Errors.Ok && result.Item2 != null)
            {
                ProjectCreated?.Invoke(result.Item2);
            }
            return result;
        }

        #endregion

        #region Project Retrieval and Filtering (Delegated to ProjectRetrievalHelper)

        /// <summary>
        /// Retrieves a list of projects filtered by folder type.
        /// </summary>
        /// <param name="folderType">The type of project folder to filter (e.g., Files, Project).</param>
        /// <returns>A list of projects matching the specified folder type.</returns>
        public static List<RootFolder> GetProjects(ProjectFolderType folderType)
        {
            EnsureInitialized();
            return ProjectRetrievalHelper.GetProjects(DMEEditor, folderType);
        }

        /// <summary>
        /// Retrieves all projects, optionally filtered by active status.
        /// </summary>
        /// <param name="onlyActive">If true, returns only active projects.</param>
        /// <returns>A list of projects.</returns>
        public static List<RootFolder> GetAllProjects(bool onlyActive = false)
        {
            EnsureInitialized();
            return ProjectRetrievalHelper.GetAllProjects(DMEEditor, onlyActive);
        }

        /// <summary>
        /// Gets a project by name.
        /// </summary>
        /// <param name="projectName">The name of the project to retrieve.</param>
        /// <returns>The project if found, otherwise null.</returns>
        public static RootFolder GetProjectByName(string projectName)
        {
            EnsureInitialized();
            return ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
        }

        /// <summary>
        /// Gets projects by author.
        /// </summary>
        /// <param name="author">The author name to filter by.</param>
        /// <returns>A list of projects by the specified author.</returns>
        public static List<RootFolder> GetProjectsByAuthor(string author)
        {
            EnsureInitialized();
            return ProjectRetrievalHelper.GetProjectsByAuthor(DMEEditor, author);
        }

        #endregion

        #region Project Metadata Management (Delegated to ProjectMetadataHelper)

        /// <summary>
        /// Updates metadata for a specified project.
        /// </summary>
        /// <param name="projectName">The name of the project to update.</param>
        /// <param name="updateAction">The action to update the project's metadata.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectMetadata(string projectName, Action<RootFolder> updateAction)
        {
            EnsureInitialized();
            var result = ProjectMetadataHelper.UpdateProjectMetadata(DMEEditor, projectName, updateAction);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Updates the description of a project.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="description">The new description.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectDescription(string projectName, string description)
        {
            EnsureInitialized();
            var result = ProjectMetadataHelper.UpdateProjectDescription(DMEEditor, projectName, description);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Updates the version of a project.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="version">The new version.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo UpdateProjectVersion(string projectName, string version)
        {
            EnsureInitialized();
            var result = ProjectMetadataHelper.UpdateProjectVersion(DMEEditor, projectName, version);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Sets the active status of a project.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="isActive">The active status to set.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo SetProjectActiveStatus(string projectName, bool isActive)
        {
            EnsureInitialized();
            var result = ProjectMetadataHelper.SetProjectActiveStatus(DMEEditor, projectName, isActive);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        #endregion

        #region Project Refresh and Synchronization (Delegated to ProjectSynchronizationHelper)

        /// <summary>
        /// Refreshes the metadata for a specified project by re-scanning its folder for new files.
        /// </summary>
        /// <param name="projectName">The name of the project to refresh.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RefreshProject(string projectName)
        {
            EnsureInitialized();
            var result = ProjectSynchronizationHelper.RefreshProject(DMEEditor, projectName);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Asynchronously refreshes the metadata for a specified project by re-scanning its folder for new files.
        /// </summary>
        /// <param name="projectName">The name of the project to refresh.</param>
        /// <returns>A task that returns error information indicating success or failure.</returns>
        public static async Task<IErrorsInfo> RefreshProjectAsync(string projectName)
        {
            EnsureInitialized();
            var result = await ProjectSynchronizationHelper.RefreshProjectAsync(DMEEditor, projectName);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Synchronizes all projects by refreshing their file listings.
        /// </summary>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo SynchronizeAllProjects()
        {
            EnsureInitialized();
            return ProjectSynchronizationHelper.SynchronizeAllProjects(DMEEditor);
        }

        #endregion

        #region Project Lifecycle Management (Delegated to ProjectLifecycleHelper)

        /// <summary>
        /// Removes a project from the configuration.
        /// </summary>
        /// <param name="projectName">The name of the project to remove.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo RemoveProject(string projectName)
        {
            EnsureInitialized();
            var result = ProjectLifecycleHelper.RemoveProject(DMEEditor, projectName);
            if (result.Flag == Errors.Ok)
            {
                ProjectRemoved?.Invoke(projectName);
            }
            return result;
        }

        /// <summary>
        /// Archives a project by marking it as inactive.
        /// </summary>
        /// <param name="projectName">The name of the project to archive.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ArchiveProject(string projectName)
        {
            EnsureInitialized();
            var result = ProjectLifecycleHelper.ArchiveProject(DMEEditor, projectName);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Activates an archived project.
        /// </summary>
        /// <param name="projectName">The name of the project to activate.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ActivateProject(string projectName)
        {
            EnsureInitialized();
            var result = ProjectLifecycleHelper.ActivateProject(DMEEditor, projectName);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, projectName);
                ProjectUpdated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Duplicates a project with a new name.
        /// </summary>
        /// <param name="sourceProjectName">The name of the project to duplicate.</param>
        /// <param name="newProjectName">The name for the new project.</param>
        /// <param name="newFolderPath">The folder path for the new project.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo DuplicateProject(string sourceProjectName, string newProjectName, string newFolderPath)
        {
            EnsureInitialized();
            var result = ProjectLifecycleHelper.DuplicateProject(DMEEditor, sourceProjectName, newProjectName, newFolderPath);
            if (result.Flag == Errors.Ok)
            {
                var project = ProjectRetrievalHelper.GetProjectByName(DMEEditor, newProjectName);
                ProjectCreated?.Invoke(project);
            }
            return result;
        }

        /// <summary>
        /// Validates a project path for existence, write permissions, and valid files.
        /// </summary>
        /// <param name="path">The folder path to validate.</param>
        /// <returns>Error information indicating success or failure.</returns>
        public static IErrorsInfo ValidateProjectPath(string path)
        {
            EnsureInitialized();
            return ProjectLifecycleHelper.ValidateProjectPath(DMEEditor, path);
        }

        /// <summary>
        /// Validates that a project name is unique.
        /// </summary>
        /// <param name="projectName">The project name to validate.</param>
        /// <returns>True if the name is unique, false otherwise.</returns>
        public static bool IsProjectNameUnique(string projectName)
        {
            EnsureInitialized();
            return ProjectLifecycleHelper.IsProjectNameUnique(DMEEditor, projectName);
        }

        #endregion

        #region Private Methods

        private static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (!_isInitialized || DMEEditor == null)
                    throw new InvalidOperationException("ProjectManagementHelper must be initialized with a valid DMEEditor.");
            }
        }

        #endregion
    }
}