# FileandFolderHelpers Documentation

## Overview

The `FileandFolderHelpers` namespace contains specialized helper classes for file and folder management operations within the Beep Data Management Engine. This refactored structure provides better organization and separation of concerns.

## Structure

### FileOperationHelper.cs

The `FileOperationHelper` class handles all file-related operations including:

#### File Loading Operations
- **LoadFile(string filePath)** - Loads a single file and creates connection properties
- **LoadFileAsync(string filePath)** - Asynchronously loads a single file
- **LoadFiles(List<string> filenames)** - Loads multiple files from a list of paths
- **LoadFiles(string[] filenames)** - Loads multiple files from an array of paths
- **LoadFilesAsync(...)** - Asynchronous versions of file loading operations

#### File Management Operations
- **AddFile(ConnectionProperties file)** - Adds a file as a data connection
- **AddFileAsync(ConnectionProperties file)** - Asynchronously adds a file connection
- **AddFiles(List<ConnectionProperties> files)** - Adds multiple file connections
- **AddFilesAsync(...)** - Asynchronous versions of file addition operations

#### File Validation
- **IsFileValid(string filename)** - Checks if a file has a supported extension
- **GetSupportedFileExtensions()** - Gets list of supported file extensions
- **ValidateFilePath(string filePath)** - Validates file path for existence and readability

#### Folder Structure Operations
- **CreateFolderStructure(string path)** - Creates folder structure with files and subfolders
- **CreateFolderStructureAsync(string path)** - Asynchronous folder structure creation

#### Category Folder Operations
- **GetCategoryForFile(string fileName, string rootName)** - Finds category for a file
- **AddCategoryFolder(...)** - Adds a new category folder
- **RemoveCategoryFolder(...)** - Removes a category folder
- **MoveFileToCategory(...)** - Moves file between categories

## Key Features

### ?? **Separation of Concerns**
- File operations are isolated from project management
- Each helper focuses on specific functionality
- Clear responsibility boundaries

### ? **Asynchronous Support**
- All major operations have async versions
- Non-blocking file I/O operations
- Better performance for large file operations

### ?? **Thread Safety**
- Thread-safe initialization and operations
- Proper locking mechanisms
- Concurrent access protection

### ?? **Comprehensive Logging**
- Detailed operation logging
- Error tracking and reporting
- Success/failure status reporting

### ? **Robust Validation**
- File existence checking
- Extension validation
- Permission verification
- Path validation

## Usage Examples

### Basic File Operations

```csharp
// Initialize the helper
FileOperationHelper.Initialize(dmeEditor);

// Load a single file
var connectionProps = FileOperationHelper.LoadFile(@"C:\Data\myfile.csv");

// Load multiple files asynchronously
var files = new List<string> { "file1.csv", "file2.json", "file3.xml" };
var connections = await FileOperationHelper.LoadFilesAsync(files);

// Validate file
bool isValid = FileOperationHelper.IsFileValid("myfile.csv");

// Get supported extensions
var extensions = FileOperationHelper.GetSupportedFileExtensions();
```

### Folder Structure Operations

```csharp
// Create folder structure
var folder = FileOperationHelper.CreateFolderStructure(@"C:\MyProject");

// Create folder structure asynchronously
var folder = await FileOperationHelper.CreateFolderStructureAsync(@"C:\MyProject");
```

### Category Management

```csharp
// Add category folder
var result = FileOperationHelper.AddCategoryFolder("CSVFiles", "FILE", 
    new List<string> { "data1.csv", "data2.csv" });

// Move file between categories
var result = FileOperationHelper.MoveFileToCategory("data.csv", "Unsorted", "CSVFiles", "FILE");

// Get category for file
string category = FileOperationHelper.GetCategoryForFile("data.csv", "FILE");
```

### File Addition and Management

```csharp
// Add single file
var result = FileOperationHelper.AddFile(connectionProperties);

// Add multiple files asynchronously
var result = await FileOperationHelper.AddFilesAsync(connectionsList);
```

## Error Handling

All operations return `IErrorsInfo` objects that contain:
- **Flag**: Success/failure status
- **Message**: Descriptive error or success message
- **Exception**: Original exception if applicable

```csharp
var result = FileOperationHelper.AddFile(connectionProps);
if (result.Flag == Errors.Ok)
{
    Console.WriteLine($"Success: {result.Message}");
}
else
{
    Console.WriteLine($"Error: {result.Message}");
}
```

## Configuration Requirements

The helper requires:
1. **IDMEEditor** instance for configuration and logging
2. **Data files folder** configured in the system
3. **Connection drivers** with extension mappings
4. **Category folders** configuration (optional)

## Thread Safety

The `FileOperationHelper` is thread-safe:
- Uses locking for initialization
- Atomic operations for file management
- Safe for concurrent access from multiple threads

## Performance Considerations

### Optimizations
- Asynchronous operations for I/O operations
- Efficient file enumeration using `Directory.EnumerateFiles`
- Batch operations for multiple files
- Lazy loading of configurations

### Best Practices
- Use async methods for large file operations
- Batch multiple file operations when possible
- Cache supported extensions list
- Use appropriate file filters to reduce processing

## Integration with Beep Engine

The helper integrates seamlessly with:
- **DMEEditor** for configuration and logging
- **ConfigEditor** for data connections and categories
- **Utilfunction** for file data connection creation
- **Connection drivers** for file type validation
- **Error reporting** system for comprehensive logging

## Migration from Original FileConnectionHelper

The new structure maintains backward compatibility through the facade pattern:
- All original method signatures preserved
- Same initialization process
- Same event handling
- Transparent delegation to specialized helpers

## Future Enhancements

Planned improvements include:
- File watching and automatic refresh
- Advanced file filtering and searching
- File metadata extraction
- Cloud storage integration
- Performance monitoring and metrics