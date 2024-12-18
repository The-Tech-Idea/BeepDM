using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.ConfigUtil;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Merge Files Action", Name = "MergeFilesAction", addinType = AddinType.Class)]
    public class MergeFilesAction : IWorkFlowAction
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public MergeFilesAction(IDMEEditor dmeEditor)
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
        public string ActionTypeName { get; set; } = "MergeFilesAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "MergeFilesAction";
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        public IDMEEditor DMEEditor { get; }
        #endregion

        #region Perform Action
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return PerformAction(progress, token, null);
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Merge Files Action Started" };
            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                string sourceFiles = GetParameterValue<string>("SourceFiles"); // Comma-separated file paths
                string targetFile = GetParameterValue<string>("TargetFile");
                string mergeType = GetParameterValue<string>("MergeType"); // Append, Combine

                if (string.IsNullOrEmpty(sourceFiles) || string.IsNullOrEmpty(targetFile))
                {
                    args.Messege = "SourceFiles or TargetFile parameter is missing.";
                    DMEEditor.AddLogMessage("MergeFiles", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                var fileList = sourceFiles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList();

                if (!fileList.All(File.Exists))
                {
                    args.Messege = "One or more source files do not exist.";
                    DMEEditor.AddLogMessage("MergeFiles", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                switch (mergeType?.ToLower())
                {
                    case "append":
                        MergeFilesByAppending(fileList, targetFile);
                        break;

                    case "combine":
                        MergeFilesByCombining(fileList, targetFile);
                        break;

                    default:
                        args.Messege = "Invalid MergeType. Supported types: Append, Combine.";
                        DMEEditor.AddLogMessage("MergeFiles", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                        return args;
                }

                args.Messege = $"Files merged successfully into {targetFile}.";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "Merge Files Action Canceled.";
            }
            catch (Exception ex)
            {
                args.Messege = $"Error Merging Files: {ex.Message}";
                DMEEditor.AddLogMessage("MergeFiles", args.Messege, DateTime.Now, -1, null, Errors.Failed);
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
            _cancellationTokenSource.Cancel();
            return new PassedArgs { Messege = "Merge Files Action Stopped." };
        }
        #endregion

        #region Merge Methods
        private void MergeFilesByAppending(List<string> sourceFiles, string targetFile)
        {
            using (var targetStream = new StreamWriter(targetFile, false))
            {
                foreach (var file in sourceFiles)
                {
                    using (var sourceStream = new StreamReader(file))
                    {
                        targetStream.Write(sourceStream.ReadToEnd());
                    }
                }
            }

            DMEEditor.AddLogMessage("MergeFiles", $"Appended files into {targetFile}", DateTime.Now, -1, null, Errors.Ok);
        }

        private void MergeFilesByCombining(List<string> sourceFiles, string targetFile)
        {
            var combinedRecords = new List<object>();

            foreach (var file in sourceFiles)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (extension == ".json")
                {
                    var records = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(File.ReadAllText(file));
                    combinedRecords.AddRange(records);
                }
                else if (extension == ".csv")
                {
                    var lines = File.ReadAllLines(file).Skip(1); // Skip headers
                    combinedRecords.AddRange(lines);
                }
            }

            // Write to target file (default: JSON format)
            File.WriteAllText(targetFile, JsonConvert.SerializeObject(combinedRecords, Formatting.Indented));
            DMEEditor.AddLogMessage("MergeFiles", $"Combined files into {targetFile}", DateTime.Now, -1, null, Errors.Ok);
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
        #endregion
    }
}
