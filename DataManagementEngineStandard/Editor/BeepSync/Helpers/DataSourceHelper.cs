using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for datasource operations used by sync validation/orchestration.
    /// Delegates lifecycle and error handling to shared engine helpers.
    /// </summary>
    public class DataSourceHelper : TheTechIdea.Beep.Editor.BeepSync.Interfaces.IDataSourceHelper
    {
        private readonly IDMEEditor _editor;

        public DataSourceHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public IDataSource GetDataSource(string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return null;

            return ErrorHandlingHelper.ExecuteWithErrorHandling(
                () =>
                {
                    var ds = _editor.GetDataSource(dataSourceName);
                    if (ds == null)
                    {
                        _editor.AddLogMessage("BeepSync", $"Data source '{dataSourceName}' not found", DateTime.Now, -1, "", Errors.Failed);
                    }
                    return ds;
                },
                $"GetDataSource:{dataSourceName}",
                _editor,
                defaultValue: null);
        }

        public bool ValidateDataSourceConnection(string dataSourceName)
        {
            var ds = GetDataSource(dataSourceName);
            if (ds == null)
                return false;

            var state = DataSourceLifecycleHelper
                .OpenWithRetryAsync(ds, 3)
                .GetAwaiter()
                .GetResult();

            return state == System.Data.ConnectionState.Open;
        }

        public Task<object> GetEntityDataAsync(string dataSourceName, string entityName, List<AppFilter> filters = null)
        {
            return ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var ds = GetDataSource(dataSourceName);
                    if (ds == null)
                        return null;

                    return await Task.Run<object>(() => ds.GetEntity(entityName, filters)?.ToList());
                },
                $"GetEntityDataAsync:{dataSourceName}.{entityName}",
                _editor,
                defaultValue: null);
        }

        public Task<IErrorsInfo> InsertEntityAsync(string dataSourceName, string entityName, object entity)
        {
            return ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var ds = GetDataSource(dataSourceName);
                    if (ds == null)
                        return new ErrorsInfo { Flag = Errors.Failed, Message = $"Data source '{dataSourceName}' not found" };

                    return await Task.Run(() => ds.InsertEntity(entityName, entity));
                },
                $"InsertEntityAsync:{dataSourceName}.{entityName}",
                _editor,
                defaultValue: new ErrorsInfo { Flag = Errors.Failed, Message = "Insert failed" });
        }

        public Task<IErrorsInfo> UpdateEntityAsync(string dataSourceName, string entityName, object entity)
        {
            return ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var ds = GetDataSource(dataSourceName);
                    if (ds == null)
                        return new ErrorsInfo { Flag = Errors.Failed, Message = $"Data source '{dataSourceName}' not found" };

                    return await Task.Run(() => ds.UpdateEntity(entityName, entity));
                },
                $"UpdateEntityAsync:{dataSourceName}.{entityName}",
                _editor,
                defaultValue: new ErrorsInfo { Flag = Errors.Failed, Message = "Update failed" });
        }

        public Task<bool> EntityExistsAsync(string dataSourceName, string entityName, List<AppFilter> filters)
        {
            return ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var ds = GetDataSource(dataSourceName);
                    if (ds == null)
                        return false;

                    var result = await Task.Run(() => ds.GetEntity(entityName, filters));
                    return result != null && result.Any();
                },
                $"EntityExistsAsync:{dataSourceName}.{entityName}",
                _editor,
                defaultValue: false);
        }
    }
}
