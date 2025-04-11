using TheTechIdea.Beep.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Xml.Linq;
using System.Text.Json;
using System.Data;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Helper class that provides methods for file operations and management.
    /// </summary>
    public static class FileHelper
    {
        #region File Extension Management

        /// <summary>
        /// Retrieves supported file extensions for data drivers.
        /// </summary>
        public static string GetFileExtensions(IDMEEditor DMEEditor)
        {
            List<ConnectionDriversConfig> drivers = DMEEditor.ConfigEditor.DataDriversClasses
                .Where(p => p.extensionstoHandle != null && !string.IsNullOrEmpty(p.classHandler))
                .ToList();

            string retval = null;

            if (drivers != null)
            {
                IEnumerable<string> extensionsList = drivers.Select(p => p.extensionstoHandle);
                string extString = string.Join(",", extensionsList);
                List<string> extensions = extString.Split(',').Distinct().ToList();

                foreach (string item in extensions)
                {
                    retval += $"{item} files(*.{item})|*.{item}|";
                }
            }

            retval += "All files(*.*)|*.*";
            return retval;
        }

        /// <summary>
        /// Retrieves a list of file-based data sources.
        /// </summary>
        public static List<ConnectionDriversConfig> GetFileDataSources(IDMEEditor DMEEditor)
        {
            return DMEEditor.ConfigEditor.DataDriversClasses
                .Where(p => p.extensionstoHandle != null)
                .ToList();
        }

        /// <summary>
        /// Gets all files with supported extensions in a directory
        /// </summary>
        /// <param name="DMEEditor">The DME editor instance</param>
        /// <param name="directoryPath">Directory path to search</param>
        /// <param name="searchSubdirectories">Whether to search subdirectories</param>
        /// <returns>List of file paths with supported extensions</returns>
        public static List<string> GetSupportedFilesInDirectory(IDMEEditor DMEEditor, string directoryPath, bool searchSubdirectories = false)
        {
            if (!Directory.Exists(directoryPath))
            {
                DMEEditor.AddLogMessage("Error", $"Directory does not exist: {directoryPath}", DateTime.Now, 0, null, Errors.Failed);
                return new List<string>();
            }

            List<string> supportedFiles = new List<string>();
            List<ConnectionDriversConfig> dataSources = GetFileDataSources(DMEEditor);

            HashSet<string> extensions = new HashSet<string>(
                dataSources
                    .Where(p => !string.IsNullOrEmpty(p.extensionstoHandle))
                    .SelectMany(p => p.extensionstoHandle.Split(','))
                    .Select(ext => ext.Trim().ToLowerInvariant())
            );

            SearchOption searchOption = searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                foreach (string extension in extensions)
                {
                    string[] files = Directory.GetFiles(directoryPath, $"*.{extension}", searchOption);
                    supportedFiles.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error searching for files: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return supportedFiles;
        }

        /// <summary>
        /// Checks if a specific file extension is supported.
        /// </summary>
        public static ConnectionDriversConfig ExtensionExists(IDMEEditor DMEEditor, string ext)
        {
            var drivers = DMEEditor.ConfigEditor.DataDriversClasses
                .Where(p => p.extensionstoHandle != null)
                .ToList();

            foreach (var driver in drivers)
            {
                var extensions = driver.extensionstoHandle.Split(',').Select(e => e.Trim().ToLowerInvariant());
                if (extensions.Contains(ext.ToLowerInvariant()))
                {
                    return driver;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the MIME type for a file based on its extension
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>MIME type as string</returns>
        public static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" or ".htm" => "text/html",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        #endregion

        #region File Information and Validation

        /// <summary>
        /// Checks if a file exists in the configuration.
        /// </summary>
        public static ConnectionProperties FileExists(IDMEEditor DMEEditor, string fileAndPath)
        {
            try
            {
                string filename = Path.GetFileName(fileAndPath);
                string filepath = Path.GetDirectoryName(fileAndPath);

                return DMEEditor.ConfigEditor.DataConnections
                    .Where(p => p.Category == DatasourceCategory.FILE &&
                                p.FileName == filename &&
                                p.FilePath == filepath)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not find file: {fileAndPath}. Error: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Checks if a file is accessible for read/write operations
        /// </summary>
        public static bool IsFileAccessible(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates the structure of a CSV file against expected column count
        /// </summary>
        public static bool ValidateCsvStructure(string filePath, int expectedColumnCount)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                        return false;

                    int columnCount = headerLine.Split(',').Length;
                    return columnCount == expectedColumnCount;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the size of a file in bytes
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            if (File.Exists(filePath))
                return new FileInfo(filePath).Length;
            return -1;
        }

        /// <summary>
        /// Gets the creation date of a file
        /// </summary>
        public static DateTime? GetFileCreationDate(string filePath)
        {
            if (File.Exists(filePath))
                return File.GetCreationTime(filePath);
            return null;
        }

        /// <summary>
        /// Gets the last modification date of a file
        /// </summary>
        public static DateTime? GetFileModificationDate(string filePath)
        {
            if (File.Exists(filePath))
                return File.GetLastWriteTime(filePath);
            return null;
        }

        /// <summary>
        /// Computes the MD5 hash of a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>MD5 hash as a string</returns>
        public static string ComputeFileHash(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error computing file hash: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Compares two files to check if their contents are identical
        /// </summary>
        /// <param name="filePath1">Path to the first file</param>
        /// <param name="filePath2">Path to the second file</param>
        /// <returns>True if files are identical, false otherwise</returns>
        public static bool CompareFiles(string filePath1, string filePath2)
        {
            try
            {
                if (GetFileSize(filePath1) != GetFileSize(filePath2))
                    return false;

                string hash1 = ComputeFileHash(filePath1);
                string hash2 = ComputeFileHash(filePath2);

                return hash1 == hash2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error comparing files: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Loads files and returns their connection properties.
        /// </summary>
        /// <param name="DMEEditor">The DME editor instance</param>
        /// <param name="filenames">List of file paths to load</param>
        /// <returns>List of ConnectionProperties for the loaded files</returns>
        public static List<ConnectionProperties> LoadFiles(IDMEEditor DMEEditor, List<string> filenames)
        {
            try
            {
                if (filenames == null || filenames.Count == 0)
                {
                    DMEEditor.AddLogMessage("Warning", "No files provided to load", DateTime.Now, 0, null, Errors.Failed);
                    return new List<ConnectionProperties>();
                }

                List<ConnectionProperties> connections = new List<ConnectionProperties>();

                foreach (string filepath in filenames)
                {
                    if (!File.Exists(filepath))
                    {
                        DMEEditor.AddLogMessage("Warning", $"File not found: {filepath}", DateTime.Now, 0, null, Errors.Failed);
                        continue;
                    }

                    FileInfo fileInfo = new FileInfo(filepath);
                    string fileExtension = fileInfo.Extension.TrimStart('.').ToLower();

                    // Check if this file extension is supported
                    ConnectionDriversConfig driver = ExtensionExists(DMEEditor, fileExtension);
                    if (driver == null)
                    {
                        DMEEditor.AddLogMessage("Warning", $"File extension not supported: {fileExtension}", DateTime.Now, 0, null, Errors.Failed);
                        continue;
                    }

                    // Create connection properties for this file
                    ConnectionProperties connection = new ConnectionProperties
                    {
                        ConnectionName = fileInfo.Name,
                        FilePath = fileInfo.DirectoryName,
                        FileName = fileInfo.Name,
                        IsFile = true,
                        Category = DatasourceCategory.FILE,
                        DatabaseType = GetDataSourceTypeFromExtension(fileExtension),
                        DriverName = driver.PackageName,
                        DriverVersion = driver.version,
                        ID = Guid.NewGuid().GetHashCode()
                    };

                    DMEEditor.AddLogMessage("Success", $"Loaded file: {filepath}", DateTime.Now, 0, null, Errors.Ok);
                    connections.Add(connection);
                }

                return connections;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not load files. Error: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
        public static ConnectionProperties LoadFile(IDMEEditor DMEEditor, string filepath)
        {
            try
            {
                if (string.IsNullOrEmpty(filepath))
                {
                    DMEEditor.AddLogMessage("Warning", "No files provided to load", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }

               

                    if (!File.Exists(filepath))
                    {
                        DMEEditor.AddLogMessage("Warning", $"File not found: {filepath}", DateTime.Now, 0, null, Errors.Failed);
                      return null;
                    }

                    FileInfo fileInfo = new FileInfo(filepath);
                    string fileExtension = fileInfo.Extension.TrimStart('.').ToLower();

                    // Check if this file extension is supported
                    ConnectionDriversConfig driver = ExtensionExists(DMEEditor, fileExtension);
                    if (driver == null)
                    {
                        DMEEditor.AddLogMessage("Warning", $"File extension not supported: {fileExtension}", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                    }

                    // Create connection properties for this file
                    ConnectionProperties connection = new ConnectionProperties
                    {
                        ConnectionName = fileInfo.Name,
                        FilePath = fileInfo.DirectoryName,
                        FileName = fileInfo.Name,
                        IsFile = true,
                        Category = DatasourceCategory.FILE,
                        DatabaseType = GetDataSourceTypeFromExtension(fileExtension),
                        DriverName = driver.PackageName,
                        DriverVersion = driver.version,
                        ID = Guid.NewGuid().GetHashCode()
                    };

                    DMEEditor.AddLogMessage("Success", $"Loaded file: {filepath}", DateTime.Now, 0, null, Errors.Ok);
                    
                

                return connection;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not load files. Error: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Gets the appropriate DataSourceType based on file extension
        /// </summary>
        /// <param name="extension">File extension (without dot)</param>
        /// <returns>The appropriate DataSourceType for the extension</returns>
        private static DataSourceType GetDataSourceTypeFromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return DataSourceType.FlatFile;

            return extension.ToLower() switch
            {
                "csv" => DataSourceType.CSV,
                "json" => DataSourceType.Json,
                "xml" => DataSourceType.XML,
                "txt" => DataSourceType.Text,
                "xls" or "xlsx" => DataSourceType.Xls,
                "pdf" => DataSourceType.PDF,
                "doc" or "docx" => DataSourceType.Doc,
                "yaml" or "yml" => DataSourceType.YAML,
                "ini" => DataSourceType.INI,
                "md" => DataSourceType.Markdown,
                "log" => DataSourceType.Log,
                _ => DataSourceType.FlatFile
            };
        }


        /// <summary>
        /// Updates the file structure by adding or removing a column.
        /// </summary>
        public static bool UpdateFileStructure(IDMEEditor DMEEditor, EntityStructure updatedEntity, string filePath, bool addColumn = true)
        {
            try
            {
                string tempFile = Path.GetTempFileName();
                int columnToRemoveIndex = -1;

                using (var reader = new StreamReader(filePath))
                using (var writer = new StreamWriter(tempFile))
                {
                    // Step 1: Update Headers
                    string headerLine = reader.ReadLine();
                    var headers = headerLine.Split(',');

                    if (!addColumn) // Remove Column
                    {
                        var columnToRemove = updatedEntity.Fields.Select(f => f.fieldname).ToArray();
                        columnToRemoveIndex = Array.FindIndex(headers, h => columnToRemove.Contains(h));
                        writer.WriteLine(string.Join(",", headers.Where((h, i) => i != columnToRemoveIndex)));
                    }
                    else // Add Column
                    {
                        var newColumn = updatedEntity.Fields.Last().fieldname;
                        writer.WriteLine(headerLine + $",{newColumn}");
                    }

                    // Step 2: Process Rows
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var values = line.Split(',');

                        if (!addColumn) // Remove Column
                        {
                            var updatedValues = values.Where((v, i) => i != columnToRemoveIndex).ToArray();
                            writer.WriteLine(string.Join(",", updatedValues));
                        }
                        else // Add Column
                        {
                            writer.WriteLine(line + ","); // Append empty value for new column
                        }
                    }
                }

                // Replace original file with updated file
                File.Copy(tempFile, filePath, true);
                File.Delete(tempFile);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error updating file structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Adds a column to a CSV file with a default value.
        /// </summary>
        public static bool AddColumnToFile(string filePath, string columnName, string defaultValue = "")
        {
            try
            {
                string tempFile = Path.GetTempFileName();

                using (var reader = new StreamReader(filePath))
                using (var writer = new StreamWriter(tempFile))
                {
                    string headerLine = reader.ReadLine();
                    writer.WriteLine(headerLine + $",{columnName}");

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        writer.WriteLine(line + $",{defaultValue}");
                    }
                }

                File.Copy(tempFile, filePath, true);
                File.Delete(tempFile);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding column to file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of a file
        /// </summary>
        public static string BackupFile(string filePath)
        {
            string backupPath = $"{filePath}_{DateTime.Now:yyyyMMddHHmmss}.bak";
            try
            {
                File.Copy(filePath, backupPath, true);
                return backupPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error backing up file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Restores a file from a backup
        /// </summary>
        public static bool RestoreFromBackup(string backupFilePath, string originalFilePath)
        {
            try
            {
                File.Copy(backupFilePath, originalFilePath, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a file if it's empty
        /// </summary>
        public static bool DeleteIfEmpty(string filePath)
        {
            try
            {
                if (new FileInfo(filePath).Length == 0)
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleans temporary files from a directory
        /// </summary>
        public static void CleanTemporaryFiles(string directoryPath)
        {
            try
            {
                var tempFiles = Directory.GetFiles(directoryPath, "*.tmp");
                foreach (var tempFile in tempFiles)
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning temporary files: {ex.Message}");
            }
        }

        /// <summary>
        /// Compresses a file into a ZIP archive
        /// </summary>
        /// <param name="filePath">Path to the file to compress</param>
        /// <param name="zipFilePath">Path for the output ZIP file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool CompressFile(string filePath, string zipFilePath)
        {
            try
            {
                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Create))
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    string fileName = Path.GetFileName(filePath);
                    var entry = archive.CreateEntry(fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var entryStream = entry.Open())
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts a file from a ZIP archive
        /// </summary>
        /// <param name="zipFilePath">Path to the ZIP file</param>
        /// <param name="extractPath">Path to extract files to</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ExtractZipFile(string zipFilePath, string extractPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting ZIP file: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region File Content Operations

        /// <summary>
        /// Counts the number of rows in a text file
        /// </summary>
        public static int CountRows(string filePath)
        {
            int rowCount = 0;
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    while (reader.ReadLine() != null)
                        rowCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting rows: {ex.Message}");
            }
            return rowCount;
        }

        /// <summary>
        /// Appends content to a file
        /// </summary>
        public static void AppendContentToFile(string filePath, string content)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error appending content to file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the last N lines from a file
        /// </summary>
        public static List<string> ReadLastNLines(string filePath, int lineCount)
        {
            var lines = new List<string>();
            try
            {
                var allLines = File.ReadAllLines(filePath);
                lines = allLines.Skip(Math.Max(0, allLines.Length - lineCount)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading last {lineCount} lines: {ex.Message}");
            }
            return lines;
        }

        /// <summary>
        /// Converts a file's encoding
        /// </summary>
        public static void ConvertFileEncoding(string filePath, Encoding sourceEncoding, Encoding targetEncoding)
        {
            try
            {
                string content = File.ReadAllText(filePath, sourceEncoding);
                File.WriteAllText(filePath, content, targetEncoding);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting file encoding: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a CSV file to a JSON string
        /// </summary>
        public static string ConvertCsvToJson(string csvFilePath)
        {
            try
            {
                var lines = File.ReadAllLines(csvFilePath);
                if (lines.Length == 0)
                    return "[]";

                var headers = lines[0].Split(',');

                var jsonData = lines.Skip(1)
                                    .Select(line => line.Split(','))
                                    .Select(values => headers.Zip(values, (header, value) => new { header, value })
                                                             .ToDictionary(x => x.header, x => x.value));

                return System.Text.Json.JsonSerializer.Serialize(jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting CSV to JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts a JSON file to CSV format
        /// </summary>
        /// <param name="jsonFilePath">Path to the JSON file</param>
        /// <param name="outputCsvPath">Path to save the CSV file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ConvertJsonToCsv(string jsonFilePath, string outputCsvPath)
        {
            try
            {
                // Read the JSON file
                string jsonContent = File.ReadAllText(jsonFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent, options);

                if (jsonData == null || !jsonData.Any())
                    return false;

                // Extract column headers (from the first object)
                var headers = jsonData[0].Keys.ToArray();

                // Prepare CSV content
                var csvLines = new List<string>();
                csvLines.Add(string.Join(",", headers));

                // Add data rows
                foreach (var item in jsonData)
                {
                    var values = headers.Select(header =>
                        item.ContainsKey(header) ? item[header]?.ToString() ?? "" : "").ToArray();
                    csvLines.Add(string.Join(",", values));
                }

                // Write to file
                File.WriteAllLines(outputCsvPath, csvLines);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting JSON to CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parses a JSON file to a generic object structure
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="jsonFilePath">Path to the JSON file</param>
        /// <returns>Deserialized object or default(T) if failed</returns>
        public static T ParseJsonFile<T>(string jsonFilePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                return JsonSerializer.Deserialize<T>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON file: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Parses an XML file to an XDocument
        /// </summary>
        /// <param name="xmlFilePath">Path to the XML file</param>
        /// <returns>XDocument or null if failed</returns>
        public static XDocument ParseXmlFile(string xmlFilePath)
        {
            try
            {
                return XDocument.Load(xmlFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing XML file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Queries a CSV file for specific data
        /// </summary>
        /// <param name="csvFilePath">Path to the CSV file</param>
        /// <param name="columnName">Column to filter on</param>
        /// <param name="filterValue">Value to filter for</param>
        /// <returns>List of matching rows as string arrays</returns>
        public static List<string[]> QueryCsvFile(string csvFilePath, string columnName, string filterValue)
        {
            try
            {
                var result = new List<string[]>();
                var lines = File.ReadAllLines(csvFilePath);

                if (lines.Length == 0)
                    return result;

                var headers = lines[0].Split(',');
                int columnIndex = Array.IndexOf(headers, columnName);

                if (columnIndex == -1)
                    return result;

                foreach (var line in lines.Skip(1))
                {
                    var values = line.Split(',');
                    if (values.Length > columnIndex && values[columnIndex] == filterValue)
                    {
                        result.Add(values);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying CSV file: {ex.Message}");
                return new List<string[]>();
            }
        }

        #endregion

        #region Async File Operations

        /// <summary>
        /// Asynchronously reads all text from a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File content as string</returns>
        public static async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file asynchronously: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously writes text to a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="content">Content to write</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> WriteFileAsync(string filePath, string content)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false))
                {
                    await writer.WriteAsync(content);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file asynchronously: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asynchronously copies a file
        /// </summary>
        /// <param name="sourceFile">Source file path</param>
        /// <param name="destinationFile">Destination file path</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> CopyFileAsync(string sourceFile, string destinationFile, int bufferSize = 4096)
        {
            try
            {
                using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
                using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying file asynchronously: {ex.Message}");
                return false;
            }
        }

        #endregion
        #region File Manipulation and Conversion

        /// <summary>
        /// Converts a DataTable to a CSV file
        /// </summary>
        /// <param name="dataTable">DataTable to convert</param>
        /// <param name="filePath">Path to save the CSV file</param>
        /// <param name="includeHeaders">Whether to include column headers</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool DataTableToCsvFile(DataTable dataTable, string filePath, bool includeHeaders = true)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                // Add headers if requested
                if (includeHeaders)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        sb.Append(EscapeCsvField(dataTable.Columns[i].ColumnName));
                        if (i < dataTable.Columns.Count - 1)
                            sb.Append(',');
                    }
                    sb.AppendLine();
                }

                // Add rows
                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        sb.Append(EscapeCsvField(row[i]?.ToString() ?? string.Empty));
                        if (i < dataTable.Columns.Count - 1)
                            sb.Append(',');
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(filePath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting DataTable to CSV file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts a list to a CSV file
        /// </summary>
        /// <param name="list">List to convert</param>
        /// <param name="filePath">Path to save the CSV file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ListToCsvFile<T>(IList<T> list, string filePath)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return false;

                var properties = typeof(T).GetProperties();
                if (properties.Length == 0)
                    return false;

                StringBuilder sb = new StringBuilder();

                // Add headers (property names)
                for (int i = 0; i < properties.Length; i++)
                {
                    sb.Append(EscapeCsvField(properties[i].Name));
                    if (i < properties.Length - 1)
                        sb.Append(',');
                }
                sb.AppendLine();

                // Add rows
                foreach (var item in list)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var value = properties[i].GetValue(item);
                        sb.Append(EscapeCsvField(value?.ToString() ?? string.Empty));
                        if (i < properties.Length - 1)
                            sb.Append(',');
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(filePath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting List to CSV file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Escapes a field value for CSV format
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            bool requiresQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n");

            if (requiresQuoting)
            {
                // Double up any quotes and wrap in quotes
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        /// <summary>
        /// Creates a DataTable from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="hasHeader">Whether the CSV file has headers</param>
        /// <returns>DataTable with the CSV data</returns>
        public static DataTable CreateDataTableFromCsvFile(string filePath, bool hasHeader = true)
        {
            DataTable dataTable = new DataTable();
            try
            {
                if (!File.Exists(filePath))
                    return dataTable;

                using (var reader = new StreamReader(filePath))
                {
                    // Process header if present
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                        return dataTable;

                    string[] headers = SplitCsvLine(headerLine);

                    if (hasHeader)
                    {
                        foreach (string header in headers)
                        {
                            dataTable.Columns.Add(header.Trim());
                        }
                    }
                    else
                    {
                        // Create default column names
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dataTable.Columns.Add($"Column{i + 1}");
                        }

                        // Add first row data
                        dataTable.Rows.Add(headers);
                    }

                    // Process remaining rows
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;

                        string[] fields = SplitCsvLine(line);
                        dataTable.Rows.Add(fields);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating DataTable from CSV file: {ex.Message}");
            }
            return dataTable;
        }

        /// <summary>
        /// Splits a CSV line into individual fields, handling quoted fields correctly
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder field = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    field.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            // Add the last field
            result.Add(field.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Downloads a file from a URL to a local path
        /// </summary>
        /// <param name="url">URL to download from</param>
        /// <param name="downloadFileName">Name of the file to save</param>
        /// <param name="downloadFilePath">Path to save the file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool DownloadFile(string url, string downloadFileName, string downloadFilePath)
        {
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(downloadFilePath))
                {
                    Directory.CreateDirectory(downloadFilePath);
                }

                string fullPath = Path.Combine(downloadFilePath, downloadFileName);

                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(url, fullPath);
                }

                return File.Exists(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads a file asynchronously from a URL to a local path
        /// </summary>
        /// <param name="url">URL to download from</param>
        /// <param name="downloadFileName">Name of the file to save</param>
        /// <param name="downloadFilePath">Path to save the file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> DownloadFileAsync(string url, string downloadFileName, string downloadFilePath)
        {
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(downloadFilePath))
                {
                    Directory.CreateDirectory(downloadFilePath);
                }

                string fullPath = Path.Combine(downloadFilePath, downloadFileName);

                using (var client = new System.Net.WebClient())
                {
                    await client.DownloadFileTaskAsync(new Uri(url), fullPath);
                }

                return File.Exists(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file asynchronously: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region File Validation and Information

        /// <summary>
        /// Checks if a file is valid (exists and not locked)
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file is valid, false otherwise</returns>
        public static bool IsFileValid(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If we can open the file, it's not locked and is valid
                    return true;
                }
            }
            catch
            {
                // File is locked or inaccessible
                return false;
            }
        }

        /// <summary>
        /// Creates a FileInfo object for the given file path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>FileInfo object or null if file doesn't exist</returns>
        public static FileInfo GetFileInfo(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    return new FileInfo(filePath);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a connection properties object from a file path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>ConnectionProperties object</returns>
        public static ConnectionProperties CreateFileDataConnection(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var fileInfo = new FileInfo(filePath);

            ConnectionProperties connectionProperties = new ConnectionProperties
            {
                ConnectionName = fileInfo.Name,
                FilePath = fileInfo.DirectoryName,
                FileName = fileInfo.Name,
                IsFile = true,
                Category = DatasourceCategory.FILE,
                DatabaseType = GetDataSourceTypeFromExtension(fileInfo.Extension)
            };

            return connectionProperties;
        }

        /// <summary>
        /// Creates a list of connection properties from an array of file paths
        /// </summary>
        /// <param name="filePaths">Array of file paths</param>
        /// <returns>List of ConnectionProperties objects</returns>
        public static List<ConnectionProperties> CreateFileConnections(string[] filePaths)
        {
            List<ConnectionProperties> connections = new List<ConnectionProperties>();

            if (filePaths == null || filePaths.Length == 0)
                return connections;

            foreach (string filePath in filePaths)
            {
                ConnectionProperties connection = CreateFileDataConnection(filePath);
                if (connection != null)
                {
                    connections.Add(connection);
                }
            }

            return connections;
        }

       

        /// <summary>
        /// Creates a string for use in file dialogs listing all supported extensions
        /// </summary>
        /// <param name="extensions">Optional list of specific extensions to include</param>
        /// <returns>File dialog filter string</returns>
        public static string CreateFileExtensionString(string extensions = null)
        {
            if (!string.IsNullOrEmpty(extensions))
            {
                string[] exts = extensions.Split(',');
                StringBuilder sb = new StringBuilder();

                foreach (string ext in exts)
                {
                    string trimmedExt = ext.Trim();
                    if (!string.IsNullOrEmpty(trimmedExt))
                    {
                        sb.Append($"{trimmedExt.ToUpperInvariant()} Files (*.{trimmedExt})|*.{trimmedExt}|");
                    }
                }

                // Add all files option
                sb.Append("All Files (*.*)|*.*");
                return sb.ToString();
            }

            // Default set of common file types
            return "CSV Files (*.csv)|*.csv|" +
                   "JSON Files (*.json)|*.json|" +
                   "XML Files (*.xml)|*.xml|" +
                   "Text Files (*.txt)|*.txt|" +
                   "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|" +
                   "All Files (*.*)|*.*";
        }

        #endregion

        #region Directory Operations

        /// <summary>
        /// Ensures a directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>True if the directory exists or was created, false otherwise</returns>
        public static bool EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring directory exists: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the size of a directory in bytes
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <param name="includeSubdirectories">Whether to include subdirectories in the calculation</param>
        /// <returns>Size of the directory in bytes</returns>
        public static long GetDirectorySize(string directoryPath, bool includeSubdirectories = true)
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            long size = 0;
            SearchOption searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

                foreach (FileInfo file in directoryInfo.GetFiles("*", searchOption))
                {
                    size += file.Length;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating directory size: {ex.Message}");
            }

            return size;
        }

        /// <summary>
        /// Copies a directory and its contents to another location
        /// </summary>
        /// <param name="sourceDir">Source directory path</param>
        /// <param name="destinationDir">Destination directory path</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool CopyDirectory(string sourceDir, string destinationDir, bool overwrite = false)
        {
            try
            {
                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Get all files in the source directory
                string[] files = Directory.GetFiles(sourceDir);

                // Copy each file
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationDir, fileName);
                    File.Copy(file, destFile, overwrite);
                }

                // Get all subdirectories
                string[] subDirs = Directory.GetDirectories(sourceDir);

                // Recursively copy each subdirectory
                foreach (string subDir in subDirs)
                {
                    string subDirName = Path.GetFileName(subDir);
                    string destSubDir = Path.Combine(destinationDir, subDirName);
                    CopyDirectory(subDir, destSubDir, overwrite);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying directory: {ex.Message}");
                return false;
            }
        }

        #endregion

    }
}
