using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Pagination"
        public void SetPageSize(int pageSize)
        {
            if (pageSize <= 0)
                throw new ArgumentException("Page size must be greater than zero.");

            PageSize = pageSize;
            CurrentPage = 1;
            ApplyPaging();
        }

        public void GoToPage(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > TotalPages)
                throw new ArgumentOutOfRangeException("Invalid page number.");

            CurrentPage = pageNumber;
            ApplyPaging();
        }

        private void ApplyPaging()
        {
            _isPagingActive = true;
            // BUG 3 fix: Page from the filtered+sorted working set, not raw originalList
            var source = _currentWorkingSet ?? originalList;
            var pagedItems = source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            ResetItems(pagedItems);
            ResetBindings();
        }

        #endregion "Pagination"
    }
}
