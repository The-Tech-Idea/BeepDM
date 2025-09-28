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
                var filePath = Path.Combine(_scriptPath, $"{script.id}.json");
                _dmeEditor.ConfigEditor.JsonLoader.Serialize(filePath, script);
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Saved script {script.id}.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Error saving script {script.id}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
            if (string.IsNullOrWhiteSpace(script.scriptSource))
            {
                _dmeEditor.ErrorObject.Flag = Errors.Failed;
                _dmeEditor.ErrorObject.Message = "Script ID is missing.";
                return _dmeEditor.ErrorObject;
            }

            if (script.ScriptDTL == null || !script.ScriptDTL.Any())
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
                foreach (var detail in script.ScriptDTL)
                {
                    if (token.IsCancellationRequested)
                        break;

                    // Execute script detail
                    await ExecuteScriptDetailAsync(detail, progress, token, customTransformation);
                }

                _dmeEditor.AddLogMessage("ETLScriptManager", $"Executed script {script.id} successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (OperationCanceledException)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Execution of script {script.id} was cancelled.", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Error executing script {script.id}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return _dmeEditor.ErrorObject;
        }

        private async Task ExecuteScriptDetailAsync(
            ETLScriptDet detail,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            Func<object, object> customTransformation)
        {
            var sourceDs = _dmeEditor.GetDataSource(detail.sourcedatasourcename);
            var destDs = _dmeEditor.GetDataSource(detail.destinationdatasourcename);

            if (sourceDs == null || destDs == null)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"DataSource not found for {detail.ID}.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            // Fetch source data
            var sourceData = await FetchSourceDataAsync(sourceDs, detail.sourceentityname, token);
            if (sourceData == null)
            {
                _dmeEditor.AddLogMessage("ETLScriptManager", $"Source data for entity {detail.sourceentityname} is null.", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            // Apply custom transformation, if provided
            var transformedData = customTransformation != null
                ? sourceData.Select(customTransformation).ToList()
                : sourceData;

            // Insert transformed data into destination
            await InsertDataAsync(destDs, detail.destinationentityname, transformedData, progress, token);
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
            var batches = data.Batch(100); // Batch size of 100 for efficiency
            int totalCount = data.Count();
            int processedCount = 0;

            foreach (var batch in batches)
            {
                if (token.IsCancellationRequested)
                    break;

                await Task.WhenAll(batch.Select(record => Task.Run(() => destDs.InsertEntity(destEntity, record), token)));

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
