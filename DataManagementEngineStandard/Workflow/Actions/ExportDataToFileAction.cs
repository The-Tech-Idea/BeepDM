using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Xml.Serialization;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Export Data to File", Name = "ExportDataToFileAction", addinType = AddinType.Class)]
    public class ExportDataToFileAction : IWorkFlowAction
    {
        public ExportDataToFileAction(IDMEEditor dmeEditor)
        {
            DMEEditor = dmeEditor;
            Id = Guid.NewGuid().ToString();
            NextAction = new List<IWorkFlowAction>();
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            Rules = new List<IWorkFlowRule>();
        }

        #region Properties
        public IWorkFlowAction PrevAction { get; set; }
        public List<IWorkFlowAction> NextAction { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public string Id { get; set; }
        public string ActionTypeName { get; set; } = "ExportDataToFileAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "ExportDataToFileAction";
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        public IDMEEditor DMEEditor { get; }
        #endregion

        #region PerformAction
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return PerformAction(progress, token, null);
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Export Data to File Action Started" };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                // Retrieve parameters
                string sourceEntity = GetParameterValue<string>("SourceEntity");
                string outputFilePath = GetParameterValue<string>("OutputFilePath");
                string fileFormat = GetParameterValue<string>("FileFormat")?.ToLower();
                bool includeHeaders = GetParameterValue<bool>("IncludeHeaders");

                if (string.IsNullOrEmpty(sourceEntity) || string.IsNullOrEmpty(outputFilePath) || string.IsNullOrEmpty(fileFormat))
                {
                    args.Messege = "Missing required parameters.";
                    DMEEditor.AddLogMessage("ExportDataToFile", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Fetch the data
                var dataSource = DMEEditor.GetDataSource(sourceEntity);
                var data = dataSource?.GetEntity(sourceEntity, null);

                if (data == null)
                {
                    args.Messege = "Failed to retrieve data from source entity.";
                    DMEEditor.AddLogMessage("ExportDataToFile", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Export the data
                switch (fileFormat)
                {
                    case "csv":
                        ExportToCsv(data, outputFilePath, includeHeaders);
                        break;
                    case "json":
                        ExportToJson(data, outputFilePath);
                        break;
                    case "xml":
                        ExportToXml(data, outputFilePath);
                        break;
                    default:
                        args.Messege = "Unsupported file format.";
                        DMEEditor.AddLogMessage("ExportDataToFile", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                        return args;
                }

                args.Messege = "Data export completed successfully.";
                DMEEditor.AddLogMessage("ExportDataToFile", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (Exception ex)
            {
                args.Messege = $"Error: {ex.Message}";
                DMEEditor.AddLogMessage("ExportDataToFile", args.Messege, DateTime.Now, -1, null, Errors.Failed);
            }
            finally
            {
                IsRunning = false;
                IsFinish = true;
            }

            return args;
        }

        public PassedArgs StopAction()
        {
            return new PassedArgs { Messege = "Export Data to File Action Stopped" };
        }
        #endregion

        #region Helper Methods
        private T GetParameterValue<T>(string parameterName)
        {
            foreach (var param in InParameters)
            {
                if (param.ParameterString1 == parameterName && param.ReturnData is T value)
                    return value;
            }
            return default;
        }

        private void ExportToCsv(object data, string filePath, bool includeHeaders)
        {
            using var writer = new StreamWriter(filePath);
            if (data is IEnumerable<object> list)
            {
                if (includeHeaders && list.Any())
                {
                    var headers = string.Join(",", list.First().GetType().GetProperties().Select(p => p.Name));
                    writer.WriteLine(headers);
                }

                foreach (var record in list)
                {
                    var line = string.Join(",", record.GetType().GetProperties().Select(p => p.GetValue(record)?.ToString()));
                    writer.WriteLine(line);
                }
            }
        }

        private void ExportToJson(object data, string filePath)
        {
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, json);
        }

        private void ExportToXml(object data, string filePath)
        {
            var serializer = new XmlSerializer(data.GetType());
            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, data);
        }
        #endregion
    }
}
