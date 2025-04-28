using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.FileManager
{

         /// <summary>
        /// Interface for CSV data reader to provide streaming access to CSV files
        /// </summary>
        public interface ICSVDataReader : IDisposable
        {
            /// <summary>
            /// Advances the reader to the next record
            /// </summary>
            /// <returns>True if there are more records, false if at the end</returns>
            bool Read();

            /// <summary>
            /// Gets the value at the specified column ordinal
            /// </summary>
            /// <param name="i">The zero-based column ordinal</param>
            /// <returns>The value as an object</returns>
            object GetValue(int i);

            /// <summary>
            /// Gets the value for the specified column name
            /// </summary>
            /// <param name="columnName">The column name</param>
            /// <returns>The value as an object</returns>
            object GetValue(string columnName);

            /// <summary>
            /// Determines if the column contains null value
            /// </summary>
            /// <param name="i">The zero-based column ordinal</param>
            /// <returns>True if null, false otherwise</returns>
            bool IsDBNull(int i);

            /// <summary>
            /// Determines if the column contains null value
            /// </summary>
            /// <param name="columnName">The column name</param>
            /// <returns>True if null, false otherwise</returns>
            bool IsDBNull(string columnName);

            /// <summary>
            /// Gets the number of columns in the current row
            /// </summary>
            int FieldCount { get; }

            /// <summary>
            /// Gets the name of the column at the specified ordinal
            /// </summary>
            /// <param name="i">The zero-based column ordinal</param>
            /// <returns>The column name</returns>
            string GetName(int i);

            /// <summary>
            /// Gets the ordinal of the specified column name
            /// </summary>
            /// <param name="name">The column name</param>
            /// <returns>The zero-based column ordinal</returns>
            int GetOrdinal(string name);
        }
    

    /// <summary>
    /// Implementation of CSV data reader for streaming large files
    /// </summary>
    internal class CSVDataReader : ICSVDataReader
    {
        private StreamReader _reader;
        private CsvTextFieldParser _parser;
        private readonly EntityStructure _entityStructure;
        private readonly List<int> _columnIndexes;
        private readonly List<string> _columnNames;
        private string[] _currentRow;
        private bool _hasReadHeader = false;

        public int FieldCount => _columnIndexes?.Count ?? 0;

        public CSVDataReader(string filePath, char delimiter, EntityStructure entityStructure, List<string> columnNames = null)
        {
            _entityStructure = entityStructure;
            _reader = new StreamReader(filePath);
            _parser = new CsvTextFieldParser(_reader);
            _parser.SetDelimiter(delimiter);

            // always create the index list, even if no filter
            _columnIndexes = new List<int>();

            if (columnNames != null && columnNames.Count > 0)
            {
                _columnNames = columnNames;
            }
        }

        public bool Read()
        {
            if (!_hasReadHeader)
            {
                // 1) read header row
                var headers = _parser.ReadFields();
                _hasReadHeader = true;

                // 2) clear any old indexes
                _columnIndexes.Clear();

                if (_columnNames != null && _columnNames.Count > 0)
                {
                    // only the named columns
                    foreach (var name in _columnNames)
                    {
                        int idx = Array.IndexOf(headers, name);
                        if (idx >= 0) _columnIndexes.Add(idx);
                    }
                }
                else
                {
                    // no filter → include every column
                    _columnIndexes.AddRange(Enumerable.Range(0, headers.Length));
                }
            }

            // 3) now read the next data row
            _currentRow = _parser.ReadFields();
            return _currentRow != null;
        }

        public object GetValue(int i)
        {
            if (_currentRow == null)
                throw new InvalidOperationException("No current row");

            int actualIndex = _columnIndexes != null ? _columnIndexes[i] : i;

            if (actualIndex >= _currentRow.Length)
                return null;

            string value = _currentRow[actualIndex];

            // Get field type from entity structure
            string fieldName = GetName(i);
            var field = _entityStructure.Fields.FirstOrDefault(f => f.fieldname == fieldName);
            if (field != null && !string.IsNullOrEmpty(field.fieldtype))
            {
                try
                {
                    Type fieldType = Type.GetType(field.fieldtype);
                    if (fieldType != null 
                        && !string.IsNullOrEmpty(value) 
                        && !string.IsNullOrWhiteSpace(value))
                    {
                        return  CSVTypeMapper.ConvertValue(value, fieldType);

                    }
                }
                catch
                {
                    // Return as string if conversion fails
                }
            }

            return value;
        }

        public object GetValue(string columnName)
        {
            return GetValue(GetOrdinal(columnName));
        }

        public bool IsDBNull(int i)
        {
            if (_currentRow == null)
                throw new InvalidOperationException("No current row");

            int actualIndex = _columnIndexes != null ? _columnIndexes[i] : i;

            return actualIndex >= _currentRow.Length || string.IsNullOrEmpty(_currentRow[actualIndex]);
        }

        public bool IsDBNull(string columnName)
        {
            return IsDBNull(GetOrdinal(columnName));
        }

        public string GetName(int i)
        {
            if (_columnNames != null && i < _columnNames.Count)
                return _columnNames[i];

            if (_entityStructure != null && i < _entityStructure.Fields.Count)
                return _entityStructure.Fields[i].fieldname;

            return $"Column{i}";
        }

        public int GetOrdinal(string name)
        {
            if (_columnNames != null)
                return _columnNames.IndexOf(name);

            for (int i = 0; i < _entityStructure.Fields.Count; i++)
            {
                if (_entityStructure.Fields[i].fieldname == name)
                    return i;
            }

            throw new ArgumentOutOfRangeException(nameof(name), $"Column '{name}' not found");
        }

        public void Dispose()
        {
            _parser?.Dispose();
            _reader?.Dispose();
        }
    }
}
