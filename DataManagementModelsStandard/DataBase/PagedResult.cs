using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    // Helper class to return paginated data with metadata
    public class PagedResult
    {
        public object Data { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public PagedResult() { }

        public PagedResult(object data, int pageNumber, int pageSize, int totalRecords)
        {
            Data = data;
            PageNumber = Math.Max(1, pageNumber);
            PageSize = pageSize;
            TotalRecords = totalRecords;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0;
            HasPreviousPage = pageNumber > 1;
            HasNextPage = pageNumber * pageSize < totalRecords;
        }
    }

}
