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
    [Addin(Caption = "Archive Old Data", Name = "ArchiveOldDataAction", addinType = AddinType.Class)]
    public class ArchiveOldDataAction : IWorkFlowAction
    {
        public ArchiveOldDataAction(IDMEEditor dmeEditor)
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
        public string ActionTypeName { get; set; } = "ArchiveOldDataAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "ArchiveOldDataAction";
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
            var args = new PassedArgs { Messege = "Archive Old Data Action Started" };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                // Retrieve parameters
                string sourceEntity = GetParameterValue<string>("SourceEntity");
                string archiveFilePath = GetParameterValue<string>("ArchiveFilePath");
                string archiveFormat = GetParameterValue<string>("ArchiveFormat")?.ToLower();
                int retentionPeriod = GetParameterValue<int>("RetentionPeriod");

                if (string.IsNullOrEmpty(sourceEntity) || string.IsNullOrEmpty(archiveFilePath) || string.IsNullOrEmpty(archiveFormat))
                {
                    args.Messege = "Missing required parameters.";
                    DMEEditor.AddLogMessage("ArchiveOldData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Fetch the data
                var dataSource = DMEEditor.GetDataSource(sourceEntity);
                var data = dataSource?.GetEntity(sourceEntity, null);

                if (data == null)
                {
                    args.Messege = "Failed to retrieve data from source entity.";
                    DMEEditor.AddLogMessage("ArchiveOldData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Filter old data based on RetentionPeriod
                var filteredData = FilterOldData(data, retentionPeriod);

                // Archive the data
                switch (archiveFormat)
                {
                    case "csv":
                        ExportToCsv(filteredData, archiveFilePath);
                        break;
                    case "json":
                        ExportToJson(filteredData, archiveFilePath);
                        break;
                    case "xml":
                        ExportToXml(filteredData, archiveFilePath);
                        break;
                    default:
                        args.Messege = "Unsupported archive format.";
                        DMEEditor.AddLogMessage("ArchiveOldData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                        return args;
                }

                args.Messege = "Data archived successfully.";
                DMEEditor.AddLogMessage("ArchiveOldData", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (Exception ex)
            {
                args.Messege = $"Error: {ex.Message}";
                DMEEditor.AddLogMessage("ArchiveOldData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
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
            return new PassedArgs { Messege = "Archive Old Data Action Stopped" };
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

        private IEnumerable<object> FilterOldData(object data, int retentionPeriod)
        {
            if (data is IEnumerable<object> list)
            {
                return list.Where(record =>
                {
                    var dateField = record.GetType().GetProperty("DateField")?.GetValue(record) as DateTime?;
                    return dateField.HasValue && dateField.Value < DateTime.Now.AddDays(-retentionPeriod);
                }).ToList();
            }

            return Enumerable.Empty<object>();
        }

        private void ExportToCsv(object data, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            if (data is IEnumerable<object> list && list.Any())
            {
                var headers = string.Join(",", list.First().GetType().GetProperties().Select(p => p.Name));
                writer.WriteLine(headers);

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
