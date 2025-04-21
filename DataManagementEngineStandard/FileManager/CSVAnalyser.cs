// Create a new file: ../BeepDM/DataManagementEngineStandard/FileManager/CSVAnalyser.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Analyzes CSV files for structure and optimization opportunities
    /// </summary>
    public class CSVAnalyser
    {
        private readonly IDMEEditor _dmeEditor;

        public CSVAnalyser(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor;
        }

        /// <summary>
        /// Analyzes a CSV file and returns detailed information and suggestions
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="delimiter">CSV delimiter character</param>
        /// <returns>CSV analysis results</returns>
        public CSVAnalysisResult AnalyzeCSVFile(string filePath, char delimiter = ',')
        {
            var result = new CSVAnalysisResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add("CSV file does not exist");
                    return result;
                }

                // Count rows and check for issues
                int lineCount = 0;
                int maxColumns = 0;
                int minColumns = int.MaxValue;
                bool hasInconsistentColumnCount = false;
                int largeTextFieldCount = 0;
                bool hasQuotingIssues = false;
                Dictionary<string, ColumnStats> columnStats = new Dictionary<string, ColumnStats>();

                using (var parser = new CsvTextFieldParser(filePath))
                {
                    parser.SetDelimiter(delimiter);

                    // Read header
                    string[] headers = parser.ReadFields();
                    if (headers == null)
                    {
                        result.Errors.Add("CSV file is empty or has formatting issues");
                        return result;
                    }

                    result.ColumnCount = headers.Length;
                    maxColumns = minColumns = headers.Length;

                    // Initialize column stats
                    for (int i = 0; i < headers.Length; i++)
                    {
                        columnStats[headers[i]] = new ColumnStats
                        {
                            Index = i,
                            MinLength = int.MaxValue,
                            MaxLength = 0,
                            NullCount = 0,
                            UniqueValues = new HashSet<string>()
                        };
                    }

                    // Read data rows
                    while (!parser.EndOfData)
                    {
                        try
                        {
                            lineCount++;
                            string[] fields = parser.ReadFields();

                            if (fields == null)
                                continue;

                            // Check column count consistency
                            if (fields.Length != headers.Length)
                            {
                                hasInconsistentColumnCount = true;
                                maxColumns = Math.Max(maxColumns, fields.Length);
                                minColumns = Math.Min(minColumns, fields.Length);
                            }

                            // Collect stats for each column
                            for (int i = 0; i < Math.Min(fields.Length, headers.Length); i++)
                            {
                                string value = fields[i];
                                string header = headers[i];

                                // Update stats
                                if (string.IsNullOrEmpty(value))
                                {
                                    columnStats[header].NullCount++;
                                }
                                else
                                {
                                    columnStats[header].MinLength = Math.Min(columnStats[header].MinLength, value.Length);
                                    columnStats[header].MaxLength = Math.Max(columnStats[header].MaxLength, value.Length);

                                    // Count large text fields
                                    if (value.Length > 1000)
                                        largeTextFieldCount++;

                                    // Check for quoting issues
                                    if ((value.Contains(delimiter) && !value.StartsWith("\"")) ||
                                        (value.Contains("\"") && !value.StartsWith("\"") && !value.EndsWith("\"")))
                                    {
                                        hasQuotingIssues = true;
                                    }

                                    // Track unique values (limited to avoid memory issues)
                                    if (columnStats[header].UniqueValues.Count < 100)
                                        columnStats[header].UniqueValues.Add(value);
                                }

                                // Detect data type
                                if (columnStats[header].DataType == DataType.Unknown)
                                {
                                    columnStats[header].DataType = DetectDataType(value);
                                }
                                else if (!string.IsNullOrEmpty(value))
                                {
                                    // Check if current value matches detected type
                                    DataType valueType = DetectDataType(value);
                                    if (valueType != columnStats[header].DataType)
                                    {
                                        // Handle type changes
                                        if (columnStats[header].DataType == DataType.Integer && valueType == DataType.Decimal)
                                        {
                                            columnStats[header].DataType = DataType.Decimal; // Upgrade int to decimal
                                        }
                                        else if (columnStats[header].DataType != DataType.String)
                                        {
                                            columnStats[header].DataType = DataType.String; // Default to string for mixed types
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Error parsing line {lineCount}: {ex.Message}");
                        }
                    }
                }

                result.RowCount = lineCount;
                result.EstimatedSizeKB = new FileInfo(filePath).Length / 1024;
                result.HasInconsistentColumnCount = hasInconsistentColumnCount;
                result.MaxColumns = maxColumns;
                result.MinColumns = minColumns;
                result.LargeTextFieldCount = largeTextFieldCount;
                result.HasQuotingIssues = hasQuotingIssues;

                // Generate suggestions
                if (hasInconsistentColumnCount)
                {
                    result.Suggestions.Add($"File has inconsistent column counts (min: {minColumns}, max: {maxColumns}). Consider fixing the source data.");
                }

                if (hasQuotingIssues)
                {
                    result.Suggestions.Add("File has potential quoting issues. Consider preprocessing the file or using a more robust CSV parser.");
                }

                if (largeTextFieldCount > 0)
                {
                    result.Suggestions.Add($"File has {largeTextFieldCount} large text fields (>1000 chars). Consider using a more suitable format for large text.");
                }

                // Analyze columns for optimization opportunities
                foreach (var col in columnStats)
                {
                    // Fields with high null percentage
                    double nullPercentage = lineCount > 0 ? (double)col.Value.NullCount / lineCount * 100 : 0;
                    if (nullPercentage > 80)
                    {
                        result.Suggestions.Add($"Column '{col.Key}' is mostly null ({nullPercentage:F1}%). Consider removing this column.");
                    }

                    // Fields with few unique values that could be enums
                    double uniquenessRatio = lineCount > 0 ? (double)col.Value.UniqueValues.Count / (lineCount - col.Value.NullCount) : 0;
                    if (col.Value.UniqueValues.Count > 1 && col.Value.UniqueValues.Count <= 10 && uniquenessRatio < 0.1)
                    {
                        result.Suggestions.Add($"Column '{col.Key}' has only {col.Value.UniqueValues.Count} unique values. Consider using an enumeration for this field.");
                    }

                    result.ColumnAnalysis.Add(col.Key, new ColumnAnalysis
                    {
                        Index = col.Value.Index,
                        DataType = col.Value.DataType,
                        NullPercentage = nullPercentage,
                        MinLength = col.Value.MinLength == int.MaxValue ? 0 : col.Value.MinLength,
                        MaxLength = col.Value.MaxLength,
                        UniqueValueCount = col.Value.UniqueValues.Count >= 100 ? "100+" : col.Value.UniqueValues.Count.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Analysis failed: {ex.Message}");
            }

            return result;
        }

        private DataType DetectDataType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return DataType.Unknown;

            // Try Boolean
            if (bool.TryParse(value, out _))
                return DataType.Boolean;

            // Try Date
            if (DateTime.TryParse(value, out _))
                return DataType.DateTime;

            // Try Decimal
            if (decimal.TryParse(value, out _))
            {
                // Check if it's actually an integer
                if (int.TryParse(value, out _))
                    return DataType.Integer;
                else
                    return DataType.Decimal;
            }

            // Default to string
            return DataType.String;
        }
    }

    /// <summary>
    /// Results of CSV file analysis
    /// </summary>
    public class CSVAnalysisResult
    {
        /// <summary>
        /// Number of rows in the CSV file
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Number of columns in the CSV file
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// File size in kilobytes
        /// </summary>
        public long EstimatedSizeKB { get; set; }

        /// <summary>
        /// Whether file has rows with inconsistent column counts
        /// </summary>
        public bool HasInconsistentColumnCount { get; set; }

        /// <summary>
        /// Maximum number of columns in any row
        /// </summary>
        public int MaxColumns { get; set; }

        /// <summary>
        /// Minimum number of columns in any row
        /// </summary>
        public int MinColumns { get; set; }

        /// <summary>
        /// Count of very large text fields
        /// </summary>
        public int LargeTextFieldCount { get; set; }

        /// <summary>
        /// Whether file has quoting issues
        /// </summary>
        public bool HasQuotingIssues { get; set; }

        /// <summary>
        /// List of errors encountered during analysis
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// List of suggestions for optimization
        /// </summary>
        public List<string> Suggestions { get; set; } = new List<string>();

        /// <summary>
        /// Analysis results by column
        /// </summary>
        public Dictionary<string, ColumnAnalysis> ColumnAnalysis { get; set; } = new Dictionary<string, ColumnAnalysis>();
    }

    /// <summary>
    /// Analysis data for a single column
    /// </summary>
    public class ColumnAnalysis
    {
        /// <summary>
        /// Column's position in the CSV file
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Detected data type
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Percentage of rows where this column is null
        /// </summary>
        public double NullPercentage { get; set; }

        /// <summary>
        /// Minimum value length
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Maximum value length
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Number of unique values (capped at 100)
        /// </summary>
        public string UniqueValueCount { get; set; }
    }

    /// <summary>
    /// Column statistics used during analysis
    /// </summary>
    internal class ColumnStats
    {
        public int Index { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public int NullCount { get; set; }
        public DataType DataType { get; set; } = DataType.Unknown;
        public HashSet<string> UniqueValues { get; set; }
    }

    /// <summary>
    /// Data types detected in CSV columns
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// Unknown data type
        /// </summary>
        Unknown,

        /// <summary>
        /// String data type
        /// </summary>
        String,

        /// <summary>
        /// Integer data type
        /// </summary>
        Integer,

        /// <summary>
        /// Decimal data type
        /// </summary>
        Decimal,

        /// <summary>
        /// Boolean data type
        /// </summary>
        Boolean,

        /// <summary>
        /// DateTime data type
        /// </summary>
        DateTime
    }
}
