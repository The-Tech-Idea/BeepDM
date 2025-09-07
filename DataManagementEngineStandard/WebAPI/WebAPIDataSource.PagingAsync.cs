using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Pagination and Async Operations partial class for WebAPIDataSource
    /// Contains IDataSource methods for paged results and asynchronous operations
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region IDataSource Pagination and Async Methods

        /// <summary>Retrieves an entity based on the provided name and filters with paging</summary>
        public virtual PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                this.Logger?.WriteLog($"Getting paged entity '{EntityName}' - Page {pageNumber}, Size {pageSize}");
                
                // Get the full result first using the base GetEntity method
                var fullResult = GetEntity(EntityName, filter);
                
                // Calculate paging
                var totalCount = fullResult?.Count ?? 0;
                var startIndex = (pageNumber - 1) * pageSize;
                
                // Create paged result
                var pagedResult = new PagedResult
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
                
                // Extract the page data
                if (fullResult != null && startIndex < totalCount)
                {
                    var pageData = new List<object>();
                    var endIndex = Math.Min(startIndex + pageSize, totalCount);
                    
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        pageData.Add(fullResult[i]);
                    }
                    
                    pagedResult.Data = pageData;
                }
                else
                {
                    pagedResult.Data = new List<object>();
                }
                
                return pagedResult;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting paged entity: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new PagedResult 
                { 
                    Data = new List<object>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }
        }

        /// <summary>Asynchronously retrieves a scalar value based on the provided query</summary>
        public virtual async Task<double> GetScalarAsync(string query)
        {
            try
            {
                this.Logger?.WriteLog($"Getting scalar async: {query}");
                
                // For Web API, this would typically be a specific endpoint call
                // Try to get data and extract first numeric value
                var result = await GetEntityAsync(query, null);
                if (result != null && result.Count > 0)
                {
                    var firstRow = result[0];
                    if (firstRow != null)
                    {
                        // Try to find first numeric property
                        var properties = firstRow.GetType().GetProperties();
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(firstRow);
                            if (double.TryParse(value?.ToString(), out double numericValue))
                            {
                                return numericValue;
                            }
                        }
                    }
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting scalar async: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return 0;
            }
        }

        #endregion
    }
}
