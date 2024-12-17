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

namespace TheTechIdea.Beep.Helpers
{
    public static class FileHelper
    {
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
        /// Loads files and returns their connection properties.
        /// </summary>
        public static List<ConnectionProperties> LoadFiles(IDMEEditor DMEEditor, List<string> filenames)
        {
            try
            {
                return DMEEditor.Utilfunction.LoadFiles(filenames.ToArray());
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not load files. Error: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
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
                var extensions = driver.extensionstoHandle.Split(',').Distinct();
                if (extensions.Contains(ext))
                {
                    return driver;
                }
            }

            return null;
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
        public static long GetFileSize(string filePath)
        {
            if (File.Exists(filePath))
                return new FileInfo(filePath).Length;
            return -1;
        }

        public static DateTime? GetFileCreationDate(string filePath)
        {
            if (File.Exists(filePath))
                return File.GetCreationTime(filePath);
            return null;
        }
        public static DateTime? GetFileModificationDate(string filePath)
        {
            if (File.Exists(filePath))
                return File.GetLastWriteTime(filePath);
            return null;
        }
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
        public static List<string> ReadLastNLines(string filePath, int lineCount)
        {
            var lines = new List<string>();
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    var allLines = File.ReadAllLines(filePath);
                    lines = allLines.Skip(Math.Max(0, allLines.Length - lineCount)).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading last {lineCount} lines: {ex.Message}");
            }
            return lines;
        }
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
        public static string ConvertCsvToJson(string csvFilePath)
        {
            try
            {
                var lines = File.ReadAllLines(csvFilePath);
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

    }

}
