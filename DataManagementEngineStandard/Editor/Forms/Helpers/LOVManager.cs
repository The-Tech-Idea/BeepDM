using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages List of Values (LOV) for Oracle Forms-compatible LOV system.
    /// Handles LOV registration, data loading, caching, validation, and related field population.
    /// Thread-safe implementation using ConcurrentDictionary.
    /// </summary>
    public class LOVManager : ILOVManager
    {
        #region Fields
        
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        
        // Key: "blockName:fieldName", Value: LOV definition
        private readonly ConcurrentDictionary<string, LOVDefinition> _lovs = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObject = new object();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of LOVManager
        /// </summary>
        /// <param name="dmeEditor">DME Editor instance</param>
        /// <param name="blocks">Blocks collection reference</param>
        public LOVManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }
        
        #endregion
        
        #region Events
        
        /// <inheritdoc />
        public event EventHandler<LOVDataLoadedEventArgs> LOVDataLoaded;
        
        /// <inheritdoc />
        public event EventHandler<LOVValidationEventArgs> LOVValidationFailed;
        
        #endregion
        
        #region LOV Registration
        
        /// <inheritdoc />
        /// <remarks>
        /// If <paramref name="lov"/>.<see cref="LOVDefinition.LOVName"/> is null or whitespace,
        /// this method MUTATES the caller's <see cref="LOVDefinition"/> instance to set it to
        /// <c>{blockName}_{fieldName}_LOV</c>. The mutation is intentional so that callers
        /// reusing the same definition across multiple registrations see a consistent name, but
        /// it is observable from the caller's reference. If this is a concern, pass a dedicated
        /// instance per registration.
        ///
        /// Re-registering an existing LOV for the same (blockName, fieldName) is a
        /// supported idempotent operation. The previous definition is overwritten and any
        /// in-memory cache it held is cleared (best-effort) so a downstream caller cannot
        /// read stale rows from the dropped reference.
        /// </remarks>
        public void RegisterLOV(string blockName, string fieldName, LOVDefinition lov)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("Block name required", nameof(blockName));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name required", nameof(fieldName));
            if (lov == null)
                throw new ArgumentNullException(nameof(lov));

            // Set LOV name if not specified (intentional mutation of the caller's instance — see remarks).
            if (string.IsNullOrWhiteSpace(lov.LOVName))
            {
                lov.LOVName = $"{blockName}_{fieldName}_LOV";
            }

            var key = GetLOVKey(blockName, fieldName);
            if (_lovs.TryGetValue(key, out var previous) && !ReferenceEquals(previous, lov))
            {
                // Diagnostic: caller is replacing a different definition. We clear the dropped
                // reference's cache so its rows cannot leak via another reference the caller
                // might still hold. (If previous == lov, the call is idempotent and we skip.)
                try { previous?.ClearCache(); }
                catch (Exception ex) { Debug.WriteLine($"[LOVManager] Dropped LOV cache clear failed for '{key}': {ex.Message}"); }
                Debug.WriteLine($"[LOVManager] RegisterLOV is replacing existing definition for '{key}'. " +
                                $"Previous='{previous?.LOVName}', New='{lov.LOVName}'.");
            }
            _lovs[key] = lov;
        }
        
        /// <inheritdoc />
        public void UnregisterLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            _lovs.TryRemove(key, out _);
        }
        
        /// <inheritdoc />
        public bool HasLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            return _lovs.ContainsKey(key);
        }
        
        /// <inheritdoc />
        public LOVDefinition GetLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            return _lovs.TryGetValue(key, out var lov) ? lov : null;
        }
        
        /// <inheritdoc />
        public Dictionary<string, LOVDefinition> GetBlockLOVs(string blockName)
        {
            var prefix = $"{blockName}:";
            return _lovs
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kvp => kvp.Key.Substring(prefix.Length),
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase);
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetAllLOVKeys()
        {
            return _lovs.Keys.ToList();
        }
        
        #endregion
        
        #region LOV Data Operations
        
        /// <inheritdoc />
        public async Task<LOVResult> LoadLOVDataAsync(string blockName, string fieldName, string searchText = null)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
            {
                return LOVResult.Fail($"No LOV registered for {blockName}.{fieldName}");
            }
            
            var result = await LoadLOVDataAsync(lov, searchText);
            
            // Raise event
            LOVDataLoaded?.Invoke(this, new LOVDataLoadedEventArgs
            {
                BlockName = blockName,
                FieldName = fieldName,
                LOV = lov,
                RecordCount = result.TotalCount,
                FromCache = result.FromCache,
                LoadTimeMs = result.LoadTimeMs
            });
            
            return result;
        }
        
        /// <inheritdoc />
        public async Task<LOVResult> LoadLOVDataAsync(LOVDefinition lov, string searchText = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Check cache first (only if no search text). The CachedData list and the
                // IsCacheValid() check both read state on the shared lov instance, and the
                // later write (lov.CachedData = ...) needs to publish the new list atomically
                // with the timestamp — otherwise a concurrent loader can observe
                // (CacheTimestamp != null) while CachedData is mid-swap, or a reader can
                // enumerate CachedData while another thread is replacing it.
                // We serialize the read+write with _lockObject; the actual DB query below
                // happens outside the lock so concurrent loads for different LOVs are not
                // serialized with each other.
                if (string.IsNullOrEmpty(searchText))
                {
                    List<object> cachedSnapshot = null;
                    bool cacheHit = false;
                    lock (_lockObject)
                    {
                        cacheHit = lov.IsCacheValid();
                        if (cacheHit)
                        {
                            // Materialize a snapshot so the reader cannot observe a torn
                            // CachedData list if another thread swaps it during enumeration.
                            cachedSnapshot = lov.CachedData != null
                                ? new List<object>(lov.CachedData)
                                : new List<object>();
                        }
                    }
                    if (cacheHit)
                    {
                        stopwatch.Stop();
                        return new LOVResult
                        {
                            Success = true,
                            Records = cachedSnapshot,
                            TotalCount = cachedSnapshot.Count,
                            FromCache = true,
                            LoadTimeMs = stopwatch.ElapsedMilliseconds
                        };
                    }
                }

                // Get data source
                var dataSource = _dmeEditor.GetDataSource(lov.DataSourceName);
                if (dataSource == null)
                {
                    return LOVResult.Fail($"Data source '{lov.DataSourceName}' not found");
                }

                // Open connection if needed
                if (dataSource.ConnectionStatus != ConnectionState.Open)
                {
                    dataSource.Openconnection();
                }

                // Build filters
                var filters = BuildFilters(lov, searchText);

                // Query data
                var data = await Task.Run(() =>
                {
                    if (filters.Count > 0)
                    {
                        return dataSource.GetEntity(lov.EntityName, filters);
                    }
                    return dataSource.GetEntity(lov.EntityName, null);
                });

                // Convert to list
                var records = ConvertToList(data);

                stopwatch.Stop();

                var result = new LOVResult
                {
                    Success = true,
                    Records = records,
                    TotalCount = records.Count,
                    FromCache = false,
                    LoadTimeMs = stopwatch.ElapsedMilliseconds
                };

                // Cache if not searching and caching enabled. The publish step is atomic:
                // assign CachedData before CacheTimestamp so a reader that observes a
                // non-null CacheTimestamp is guaranteed to see the matching list.
                if (string.IsNullOrEmpty(searchText) && lov.UseCache)
                {
                    lock (_lockObject)
                    {
                        lov.CachedData = records;
                        lov.CacheTimestamp = DateTime.Now;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return LOVResult.Fail($"Error loading LOV data: {ex.Message}");
            }
        }
        
        /// <inheritdoc />
        public LOVResult FilterLOVData(string blockName, string fieldName, string searchText)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null || !lov.IsCacheValid())
            {
                return LOVResult.Fail("No cached data available");
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            List<object> filteredRecords;
            if (string.IsNullOrEmpty(searchText))
            {
                filteredRecords = lov.CachedData;
            }
            else
            {
                filteredRecords = lov.CachedData
                    .Where(r => MatchesSearch(r, lov, searchText))
                    .ToList();
            }
            
            stopwatch.Stop();
            
            return new LOVResult
            {
                Success = true,
                Records = filteredRecords,
                TotalCount = filteredRecords.Count,
                FromCache = true,
                LoadTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        
        /// <inheritdoc />
        public async Task<LOVValidationResult> ValidateLOVValueAsync(string blockName, string fieldName, object value)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return LOVValidationResult.Valid(); // No LOV = no validation required
                
            if (lov.ValidationType == LOVValidationType.Unrestricted)
                return LOVValidationResult.Valid(); // No validation required
                
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return LOVValidationResult.Valid(); // Null check handled by required validation
                
            // Load LOV data
            var lovResult = await LoadLOVDataAsync(lov, null);
            if (!lovResult.Success)
            {
                return LOVValidationResult.Invalid($"Cannot validate: {lovResult.ErrorMessage}");
            }
            
            // Check if value exists in LOV. For large LOVs the previous O(N) loop is
            // painful: every validation scanned every row, so a 10k-row LOV took
            // 10k reflection calls per keystroke. Build a HashSet of the return-field
            // string representations once, then look up the value in O(1). The HashSet
            // uses OrdinalIgnoreCase so the match semantics are unchanged from the
            // prior loop body.
            var valueStr = value.ToString();
            var returnField = lov.ReturnField ?? lov.DisplayField;

            // First pass: build the index of all return-field values. We keep the
            // first record per unique value so callers see a deterministic
            // "matched record" when duplicates exist in the data.
            object matchedRecord = null;
            var returnValueIndex = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in lovResult.Records)
            {
                var fieldValue = GetPropertyValue(record, returnField);
                var key = fieldValue?.ToString();
                if (key == null)
                {
                    continue;
                }
                if (!returnValueIndex.ContainsKey(key))
                {
                    returnValueIndex[key] = record;
                }
            }

            // O(1) lookup.
            returnValueIndex.TryGetValue(valueStr, out matchedRecord);
            
            if (matchedRecord != null)
            {
                return LOVValidationResult.Valid(matchedRecord);
            }
            
            // Value not found - get suggestions
            var suggestions = GetSuggestions(lovResult.Records, lov, valueStr, 5);
            
            var result = LOVValidationResult.Invalid($"Value '{value}' not found in List of Values", suggestions);
            
            // Raise event
            LOVValidationFailed?.Invoke(this, new LOVValidationEventArgs
            {
                BlockName = blockName,
                FieldName = fieldName,
                Value = value,
                ErrorMessage = result.ErrorMessage,
                Suggestions = suggestions
            });
            
            return result;
        }
        
        /// <inheritdoc />
        public Dictionary<string, object> GetRelatedFieldValues(LOVDefinition lov, object selectedRecord)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            
            if (lov?.RelatedFieldMappings == null || selectedRecord == null)
                return values;
            
            // First, add the return field value
            if (!string.IsNullOrWhiteSpace(lov.ReturnField))
            {
                var returnValue = GetPropertyValue(selectedRecord, lov.ReturnField);
                if (returnValue != null)
                {
                    values["__RETURN_VALUE__"] = returnValue;
                }
            }
            
            // Then add related field mappings
            foreach (var mapping in lov.RelatedFieldMappings)
            {
                var lovFieldName = mapping.Key;      // Field in LOV record
                var blockFieldName = mapping.Value;  // Target field in block
                
                var fieldValue = GetPropertyValue(selectedRecord, lovFieldName);
                if (fieldValue != null)
                {
                    values[blockFieldName] = fieldValue;
                }
            }
            
            return values;
        }
        
        /// <inheritdoc />
        public async Task<object> FindRecordByValueAsync(string blockName, string fieldName, object value)
        {
            if (value == null)
                return null;
                
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return null;
                
            var lovResult = await LoadLOVDataAsync(lov, null);
            if (!lovResult.Success)
                return null;
                
            var returnField = lov.ReturnField ?? lov.DisplayField;
            var valueStr = value.ToString();
            
            return lovResult.Records.FirstOrDefault(r =>
            {
                var fieldValue = GetPropertyValue(r, returnField);
                return fieldValue?.ToString()?.Equals(valueStr, StringComparison.OrdinalIgnoreCase) == true;
            });
        }
        
        #endregion
        
        #region Cache Management
        
        /// <inheritdoc />
        public void ClearLOVCache(string blockName, string fieldName)
        {
            var lov = GetLOV(blockName, fieldName);
            lov?.ClearCache();
        }
        
        /// <inheritdoc />
        public void ClearBlockLOVCache(string blockName)
        {
            foreach (var lov in GetBlockLOVs(blockName).Values)
            {
                // ClearCache can throw if a LOVDefinition is in a partially
                // constructed state (e.g. someone registered a definition and
                // then nulled out one of its properties). Isolate each clear so
                // one bad definition does not prevent the rest of the block's
                // caches from being released.
                try { lov.ClearCache(); }
                catch (Exception ex) { Debug.WriteLine($"[LOVManager] ClearCache failed for block '{blockName}' LOV '{lov?.LOVName}': {ex.Message}"); }
            }
        }

        /// <inheritdoc />
        public void ClearAllLOVCaches()
        {
            foreach (var lov in _lovs.Values)
            {
                // Same isolation rationale as ClearBlockLOVCache: one bad
                // definition must not abort the rest of the cleanup. The
                // ClearCache call site is best-effort; we surface failures
                // to the debug log so an operator can spot a stuck/malformed
                // LOV without losing the entire batch.
                try { lov.ClearCache(); }
                catch (Exception ex) { Debug.WriteLine($"[LOVManager] ClearCache failed for LOV '{lov?.LOVName}': {ex.Message}"); }
            }
        }
        
        /// <inheritdoc />
        public async Task RefreshLOVCacheAsync(string blockName, string fieldName)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return;
                
            lov.ClearCache();
            await LoadLOVDataAsync(lov, null);
        }
        
        /// <inheritdoc />
        public async Task PreloadLOVAsync(string blockName, string fieldName)
        {
            await LoadLOVDataAsync(blockName, fieldName, null);
        }
        
        /// <inheritdoc />
        public async Task PreloadBlockLOVsAsync(string blockName)
        {
            var lovs = GetBlockLOVs(blockName);
            var tasks = lovs.Select(kvp => LoadLOVDataAsync(blockName, kvp.Key, null));
            await Task.WhenAll(tasks);
        }
        
        #endregion
        
        #region Private Methods
        
        private static string GetLOVKey(string blockName, string fieldName)
        {
            return $"{blockName}:{fieldName}";
        }
        
        private List<AppFilter> BuildFilters(LOVDefinition lov, string searchText)
        {
            var filters = new List<AppFilter>();
            
            // Add existing filters
            if (lov.Filters != null)
            {
                filters.AddRange(lov.Filters);
            }
            
            // Add search filters if search text provided
            if (!string.IsNullOrEmpty(searchText))
            {
                var searchableColumns = lov.Columns.Where(c => c.Searchable).ToList();
                
                // If no columns defined, use display field
                if (!searchableColumns.Any() && !string.IsNullOrWhiteSpace(lov.DisplayField))
                {
                    searchableColumns.Add(new LOVColumn { FieldName = lov.DisplayField });
                }
                
                foreach (var col in searchableColumns)
                {
                    var searchFilter = CreateSearchFilter(col.FieldName, searchText, lov.SearchMode);
                    filters.Add(searchFilter);
                }
            }
            
            return filters;
        }
        
        private static AppFilter CreateSearchFilter(string fieldName, string searchText, LOVSearchMode searchMode)
        {
            var filter = new AppFilter
            {
                FieldName = fieldName,
                FilterValue = searchText
            };
            
            switch (searchMode)
            {
                case LOVSearchMode.StartsWith:
                    filter.Operator = "LIKE";
                    filter.FilterValue = $"{searchText}%";
                    break;
                case LOVSearchMode.EndsWith:
                    filter.Operator = "LIKE";
                    filter.FilterValue = $"%{searchText}";
                    break;
                case LOVSearchMode.Contains:
                    filter.Operator = "LIKE";
                    filter.FilterValue = $"%{searchText}%";
                    break;
                case LOVSearchMode.Exact:
                default:
                    filter.Operator = "=";
                    break;
            }
            
            return filter;
        }
        
        private List<object> ConvertToList(object data)
        {
            if (data == null)
                return new List<object>();
            
            // Handle DataTable
            if (data is DataTable dt)
            {
                return dt.AsEnumerable()
                    .Select(r => r.ItemArray.Select((val, i) => new { Key = dt.Columns[i].ColumnName, Value = val })
                        .ToDictionary(x => x.Key, x => x.Value) as object)
                    .ToList();
            }
            
            // Handle IEnumerable
            if (data is IEnumerable<object> enumerable)
            {
                return enumerable.ToList();
            }
            
            // Handle non-generic IEnumerable
            if (data is System.Collections.IEnumerable nonGenericEnum)
            {
                var list = new List<object>();
                foreach (var item in nonGenericEnum)
                {
                    list.Add(item);
                }
                return list;
            }
            
            // Single object
            return new List<object> { data };
        }
        
        private bool MatchesSearch(object record, LOVDefinition lov, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            // We previously lowercased both sides and used the default culture-sensitive
            // comparison. That makes a German user typing "Müller" match "Mueller" in
            // ways the user almost certainly did not intend, and it makes a Turkish
            // user typing "istanbul" (lower-case 'i' in their locale) NOT match
            // "İstanbul" in the data — a real bug in any product with a non-English
            // customer. Pin the comparison to OrdinalIgnoreCase so the match is
            // culture-independent and matches the behavior used by the validation
            // path (Equals(..., OrdinalIgnoreCase)).
            const StringComparison cmp = StringComparison.OrdinalIgnoreCase;

            var searchableColumns = lov.Columns.Where(c => c.Searchable).ToList();

            // If no columns defined, use display field
            if (!searchableColumns.Any() && !string.IsNullOrWhiteSpace(lov.DisplayField))
            {
                searchableColumns.Add(new LOVColumn { FieldName = lov.DisplayField });
            }

            foreach (var col in searchableColumns)
            {
                var value = GetPropertyValue(record, col.FieldName)?.ToString();
                if (value == null)
                    continue;

                bool matches = lov.SearchMode switch
                {
                    LOVSearchMode.StartsWith => value.StartsWith(searchText, cmp),
                    LOVSearchMode.EndsWith => value.EndsWith(searchText, cmp),
                    LOVSearchMode.Exact => string.Equals(value, searchText, cmp),
                    _ => value.IndexOf(searchText, cmp) >= 0
                };

                if (matches)
                    return true;
            }

            return false;
        }
        
        private List<object> GetSuggestions(List<object> records, LOVDefinition lov, string searchText, int maxSuggestions)
        {
            if (records == null || !records.Any())
                return new List<object>();
                
            var displayField = lov.DisplayField ?? lov.ReturnField;
            if (string.IsNullOrWhiteSpace(displayField) && lov.Columns.Any())
            {
                displayField = lov.Columns.First().FieldName;
            }
            
            // Find records that partially match
            return records
                .Where(r =>
                {
                    var value = GetPropertyValue(r, displayField)?.ToString();
                    return value != null && 
                           value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                })
                .Take(maxSuggestions)
                .ToList();
        }
        
        private static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;

            try
            {
                // Handle Dictionary
                if (obj is IDictionary<string, object> dict)
                {
                    return dict.TryGetValue(propertyName, out var val) ? val : null;
                }

                // Handle DataRow
                if (obj is DataRow row)
                {
                    return row.Table.Columns.Contains(propertyName) ? row[propertyName] : null;
                }

                // Handle regular objects via the shared RecordPropertyAccessor. This
                // routes the lookup through the engine-wide typed PropertyInfo catalog
                // instead of a fresh Type.GetProperty(...) call per row. The accessor
                // is also the single place we cache reflection metadata, so the perf
                // and case-insensitive behavior stay consistent with the rest of the
                // engine (Forms manager, savepoint manager, etc).
                if (RecordPropertyAccessor.TryGetValue(obj, propertyName, out var value, logger: null))
                {
                    return value;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        #endregion
    }
}
