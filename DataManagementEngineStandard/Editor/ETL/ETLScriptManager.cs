using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Exensions;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Editor.ETL
{
    public partial class ETLScriptManager
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly string _scriptPath;

        public ETLScriptManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _scriptPath = Path.Combine(_dmeEditor.ConfigEditor.ExePath, "Scripts");

            if (!Directory.Exists(_scriptPath))
            {
                Directory.CreateDirectory(_scriptPath);
            }
        }

        #region "Script Management"

        public List<ETLScriptHDR> LoadScripts()
        {
            var scripts = new List<ETLScriptHDR>();
            try
            {
                foreach (var file in Directory.GetFiles(_scriptPath, "*.json"))
                {
                    var script = _dmeEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(file);
                    if (script != null)
                        scripts.Add(script);
                }

                _dmeEditor.AddLogMessage("ETLScriptManager", $"Loaded {scripts.Count} scripts.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Error loading scripts: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return scripts;
        }

        public IErrorsInfo SaveScript(ETLScriptHDR script)
        {
            try
            {
                if (script == null)
                {
                    _dmeEditor.AddLogMessage("ETLScriptManager", "Cannot save null script.", DateTime.Now, -1, null, Errors.Failed);
                    return _dmeEditor.ErrorObject;
                }

                var filePath = Path.Combine(_scriptPath, $"{script.Id}.json");
                _dmeEditor.ConfigEditor.JsonLoader.Serialize(filePath, script);
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Saved script {script.Id}.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                var scriptId = script == null ? "unknown" : script.Id.ToString();
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Error saving script {scriptId}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return _dmeEditor.ErrorObject;
        }

        public IErrorsInfo DeleteScript(string scriptId)
        {
            try
            {
                var filePath = Path.Combine(_scriptPath, $"{scriptId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _dmeEditor.AddLogMessage("ETLScriptManager", $"Deleted script {scriptId}.", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    _dmeEditor.AddLogMessage("ETLScriptManager", $"Script {scriptId} not found.", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Error deleting script {scriptId}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return _dmeEditor.ErrorObject;
        }

        public IErrorsInfo UpdateScript(ETLScriptHDR updatedScript)
        {
            return SaveScript(updatedScript);
        }

        public IErrorsInfo ValidateScript(ETLScriptHDR script)
        {
            if (script == null)
            {
                _dmeEditor.ErrorObject.Flag = Errors.Failed;
                _dmeEditor.ErrorObject.Message = "Script is missing.";
                return _dmeEditor.ErrorObject;
            }

            if (string.IsNullOrWhiteSpace(script.ScriptSource))
            {
                _dmeEditor.ErrorObject.Flag = Errors.Failed;
                _dmeEditor.ErrorObject.Message = "Script source is missing.";
                return _dmeEditor.ErrorObject;
            }

            if (script.ScriptDetails == null || !script.ScriptDetails.Any())
            {
                _dmeEditor.ErrorObject.Flag = Errors.Failed;
                _dmeEditor.ErrorObject.Message = "Script details are missing.";
                return _dmeEditor.ErrorObject;
            }

            _dmeEditor.ErrorObject.Flag = Errors.Ok;
            return _dmeEditor.ErrorObject;
        }

        #endregion

        #region "Script Execution"

        public async Task<IErrorsInfo> ExecuteScriptAsync(
            ETLScriptHDR script,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            Func<object, object> customTransformation = null)
        {
            try
            {
                if (script == null)
                {
                    _dmeEditor.AddLogMessage("ETLScriptManager", "Cannot execute null script.", DateTime.Now, -1, null, Errors.Failed);
                    return _dmeEditor.ErrorObject;
                }

                if (script.ScriptDetails == null)
                {
                    _dmeEditor.AddLogMessage("ETLScriptManager", $"Script {script.Id} has no details to execute.", DateTime.Now, -1, null, Errors.Failed);
                    return _dmeEditor.ErrorObject;
                }

                foreach (var detail in script.ScriptDetails)
                {
                    if (token.IsCancellationRequested)
                        break;

                    // Execute script detail
                    await ExecuteScriptDetailAsync(detail, progress, token, customTransformation);
                }

                _dmeEditor.AddLogMessage("ETLScriptManager", $"Executed script {script.Id} successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (OperationCanceledException)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Execution of script {script?.Id} was cancelled.", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Error executing script {script?.Id}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return _dmeEditor.ErrorObject;
        }

        private async Task ExecuteScriptDetailAsync(
            ETLScriptDet detail,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            Func<object, object> customTransformation)
        {
            if (detail == null)
                return;

            if (string.IsNullOrWhiteSpace(detail.SourceDataSourceName) || string.IsNullOrWhiteSpace(detail.DestinationDataSourceName))
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Script detail {detail.Id} is missing datasource names.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            var sourceDs = _dmeEditor.GetDataSource(detail.SourceDataSourceName);
            var destDs = _dmeEditor.GetDataSource(detail.DestinationDataSourceName);

            if (sourceDs == null || destDs == null)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"DataSource not found for {detail.Id}.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            // Fetch source data
            if (string.IsNullOrWhiteSpace(detail.SourceEntityName))
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Script detail {detail.Id} is missing source entity name.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            if (string.IsNullOrWhiteSpace(detail.DestinationEntityName))
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Script detail {detail.Id} is missing destination entity name.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            var sourceData = await FetchSourceDataAsync(sourceDs, detail.SourceEntityName, token);
            if (sourceData == null)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Source data for entity {detail.SourceEntityName} is null.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            // Apply custom transformation, if provided
            var transformedData = customTransformation != null
                ? sourceData.Select(customTransformation).ToList()
                : sourceData;

            // Insert transformed data into destination
            await InsertDataAsync(destDs, detail.DestinationEntityName, transformedData, progress, token);
        }

        private async Task<IEnumerable<object>> FetchSourceDataAsync(IDataSource sourceDs, string srcEntity, CancellationToken token)
        {
            return await Task.Run(() => sourceDs.GetEntity(srcEntity, null), token) as IEnumerable<object>;
        }

        private async Task InsertDataAsync(
            IDataSource destDs,
            string destEntity,
            IEnumerable<object> data,
            IProgress<PassedArgs> progress,
            CancellationToken token)
        {
            var dataList = data as IList<object> ?? data.ToList();
            var batches = dataList.Batch(100); // Batch size of 100 for efficiency
            int totalCount = dataList.Count;
            int processedCount = 0;

            foreach (var batch in batches)
            {
                if (token.IsCancellationRequested)
                    break;

                foreach (var record in batch)
                {
                    if (token.IsCancellationRequested)
                        break;

                    destDs.InsertEntity(destEntity, record);
                }

                processedCount += batch.Count();

                progress?.Report(new PassedArgs
                {
                    Messege = $"Inserted {processedCount}/{totalCount} records into {destEntity}",
                    ParameterInt1 = processedCount,
                    ParameterInt2 = totalCount
                });
            }

            _dmeEditor.AddLogMessage("ETLScriptManager", $"Successfully inserted {processedCount}/{totalCount} records into {destEntity}.", DateTime.Now, -1, null, Errors.Ok);
        }

        #endregion
    }
}
