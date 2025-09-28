# ProjectandLibraryHelpers Documentation

## Overview

The `ProjectandLibraryHelpers` namespace contains specialized helper classes for project and library management operations within the Beep Data Management Engine. This provides comprehensive project lifecycle management with enhanced metadata support.

## Structure

### ProjectManagementHelper.cs

The `ProjectManagementHelper` class handles all project-related operations including:

#### Project Creation and Management
- **AddFolder(string foldername)** - Adds a folder as a project
- **AddFolderAsync(string foldername)** - Asynchronously adds a folder as a project
- **CreateProject(string folderpath, ProjectFolderType folderType)** - Creates a new project
- **CreateProjectAsync(...)** - Asynchronous project creation
- **CreateProjectWithMetadata(...)** - Creates project with custom metadata

#### Project Retrieval and Filtering
- **GetProjects(ProjectFolderType folderType)** - Gets projects by folder type
- **GetAllProjects(bool onlyActive)** - Gets all projects with optional filtering
- **GetProjectByName(string projectName)** - Gets project by name
- **GetProjectsByAuthor(string author)** - Gets projects by author

#### Project Metadata Management
- **UpdateProjectMetadata(string projectName, Action<RootFolder> updateAction)** - Updates project metadata
- **UpdateProjectDescription(string projectName, string description)** - Updates project description
- **UpdateProjectVersion(string projectName, string version)** - Updates project version
- **SetProjectActiveStatus(string projectName, bool isActive)** - Sets project active status

#### Project Refresh and Synchronization
- **RefreshProject(string projectName)** - Refreshes project by scanning for new files
- **RefreshProjectAsync(string projectName)** - Asynchronous project refresh
- **SynchronizeAllProjects()** - Synchronizes all active projects

#### Project Lifecycle Management
- **RemoveProject(string projectName)** - Removes a project from configuration
- **ArchiveProject(string projectName)** - Archives project (marks as inactive)
- **ActivateProject(string projectName)** - Activates an archived project
- **DuplicateProject(...)** - Duplicates project with new name and path

#### Project Validation
- **ValidateProjectPath(string path)** - Validates project path
- **IsProjectNameUnique(string projectName)** - Checks if project name is unique

## Key Features

### ?? **Comprehensive Project Management**
- Full project lifecycle support
- Rich metadata management
- Version control integration
- Author tracking and management

### ?? **Advanced Filtering and Querying**
- Filter by project type, author, status
- Search and retrieval capabilities
- Active/inactive project management
- Type-based project organization

### ? **Asynchronous Operations**
- Non-blocking project operations
- Async file scanning and refresh
- Concurrent project management

### ?? **Event-Driven Architecture**
- Project creation events
- Project update notifications
- Project removal events
- Real-time status updates

### ?? **Thread Safety**
- Thread-safe operations
- Concurrent access protection
- Atomic metadata updates

### ?? **Comprehensive Logging**
- Detailed operation tracking
- Error reporting and recovery
- Success/failure notifications

## Usage Examples

### Project Creation

```csharp
// Initialize the helper
ProjectManagementHelper.Initialize(dmeEditor);

// Create simple project
var result = ProjectManagementHelper.CreateProject(@"C:\MyProject");

// Create project with metadata
var result = ProjectManagementHelper.CreateProjectWithMetadata(
    "MyAwesomeProject",
    @"C:\MyProject", 
    "A project for data analysis",
    "John Doe",
    "1.0.0",
    ProjectFolderType.Project
);

// Add folder as project asynchronously
var result = await ProjectManagementHelper.AddFolderAsync(@"C:\DataFolder");
```

### Project Retrieval

```csharp
// Get all active projects
var activeProjects = ProjectManagementHelper.GetAllProjects(onlyActive: true);

// Get projects by type
var fileProjects = ProjectManagementHelper.GetProjects(ProjectFolderType.Files);
var libraryProjects = ProjectManagementHelper.GetProjects(ProjectFolderType.Library);

// Get specific project
var project = ProjectManagementHelper.GetProjectByName("MyAwesomeProject");

// Get projects by author
var johnProjects = ProjectManagementHelper.GetProjectsByAuthor("John Doe");
```

### Project Metadata Management

```csharp
// Update project description
var result = ProjectManagementHelper.UpdateProjectDescription(
    "MyProject", 
    "Updated description for my project"
);

// Update project version
var result = ProjectManagementHelper.UpdateProjectVersion("MyProject", "2.0.0");

// Custom metadata update
var result = ProjectManagementHelper.UpdateProjectMetadata("MyProject", project => 
{
    project.Tags = "analysis,data,ml";
    project.IsPrivate = true;
    project.ModificationAuthor = "Jane Smith";
});

// Set project status
var result = ProjectManagementHelper.SetProjectActiveStatus("MyProject", false);
```

### Project Refresh and Synchronization

```csharp
// Refresh single project
var result = ProjectManagementHelper.RefreshProject("MyProject");

// Refresh project asynchronously
var result = await ProjectManagementHelper.RefreshProjectAsync("MyProject");

// Synchronize all projects
var result = ProjectManagementHelper.SynchronizeAllProjects();
```

### Project Lifecycle Management

```csharp
// Archive project
var result = ProjectManagementHelper.ArchiveProject("OldProject");

// Activate archived project
var result = ProjectManagementHelper.ActivateProject("OldProject");

// Duplicate project
var result = ProjectManagementHelper.DuplicateProject(
    "SourceProject",
    "NewProject",
    @"C:\NewProjectPath"
);

// Remove project completely
var result = ProjectManagementHelper.RemoveProject("UnwantedProject");
```

### Project Validation

```csharp
// Validate project path
var result = ProjectManagementHelper.ValidateProjectPath(@"C:\ProjectPath");
if (result.Flag == Errors.Ok)
{
    Console.WriteLine("Path is valid for project creation");
}

// Check if project name is unique
bool isUnique = ProjectManagementHelper.IsProjectNameUnique("NewProjectName");
if (!isUnique)
{
    Console.WriteLine("Project name already exists");
}
```

## Event Handling

```csharp
// Subscribe to project events
ProjectManagementHelper.ProjectCreated += (project) => 
{
    Console.WriteLine($"Project created: {project.Name}");
};

ProjectManagementHelper.ProjectUpdated += (project) => 
{
    Console.WriteLine($"Project updated: {project.Name}");
};

ProjectManagementHelper.ProjectRemoved += (projectName) => 
{
    Console.WriteLine($"Project removed: {projectName}");
};
```

## Project Types

### ProjectFolderType Enum
- **Files** - File-based projects for data analysis
- **Project** - Full project with multiple components
- **Library** - Reusable library components

Each type provides different functionality and organization patterns.

## Error Handling

All operations return comprehensive error information:

```csharp
var result = ProjectManagementHelper.CreateProject(@"C:\InvalidPath");
if (result.Item1.Flag == Errors.Failed)
{
    Console.WriteLine($"Project creation failed: {result.Item1.Message}");
    if (result.Item1.Ex != null)
    {
        Console.WriteLine($"Exception: {result.Item1.Ex.Message}");
    }
}
else
{
    Console.WriteLine($"Project created successfully: {result.Item2.Name}");
}
```

## Configuration Integration

The helper integrates with:
- **IDMEEditor** for configuration access
- **ConfigEditor** for project persistence
- **FileOperationHelper** for file structure management
- **Category management** for project organization

## Performance Optimizations

### Efficient Operations
- Lazy loading of project metadata
- Asynchronous file scanning
- Batch operations for multiple projects
- Caching of frequently accessed data

### Best Practices
- Use async methods for I/O operations
- Filter projects at retrieval time
- Batch metadata updates
- Use events for UI updates

## Thread Safety

All operations are thread-safe:
- Atomic metadata updates
- Concurrent project access
- Thread-safe initialization
- Protected shared resources

## Migration and Compatibility

The helper maintains compatibility with:
- Original FileConnectionHelper interface
- Existing project configurations
- Legacy project metadata
- Current event handling patterns

## Integration with File Operations

The helper works seamlessly with `FileOperationHelper`:
- Delegates file structure creation
- Uses file validation services
- Integrates folder scanning operations
- Maintains consistent error handling

## Future Enhancements

Planned improvements:
- Project templates and scaffolding
- Advanced search and filtering
- Project dependencies management
- Version control integration
- Collaborative project features
- Project analytics and reporting
- Cloud project synchronization