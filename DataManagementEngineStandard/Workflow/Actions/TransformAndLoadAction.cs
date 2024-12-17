using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Transform and Load Action", Name = "TransformAndLoadAction", addinType = AddinType.Class)]
    public class TransformAndLoadAction : IWorkFlowAction
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public TransformAndLoadAction(IDMEEditor dmeEditor)
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
        public string ActionTypeName { get; set; } = "TransformAndLoadAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; } = false;
        public bool IsRunning { get; set; } = false;
        public string ClassName { get; set; }
        public string Name { get; set; } = "TransformAndLoadAction";
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        public IDMEEditor DMEEditor { get; }
        #endregion

        #region Perform Action
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return PerformAction(progress, token, null); // Default to no custom function
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Transform and Load Action Started", ParameterInt1 = 0 };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                // Custom Function Execution
                if (actionToExecute != null)
                {
                    return actionToExecute.Invoke();
                }

                // Extract required parameters
                var sourceDataSource = GetParameterValue<string>("SourceDataSource");
                var transformationSteps = GetParameterValue<List<Func<object, object>>>("TransformationSteps");
                var targetEntity = GetParameterValue<string>("TargetTable");

                if (string.IsNullOrEmpty(sourceDataSource) || string.IsNullOrEmpty(targetEntity) || transformationSteps == null)
                {
                    args.Messege = "Invalid Parameters: SourceDataSource, TransformationSteps, or TargetTable missing.";
                    return args;
                }

                // Load source data
                SendProgress(progress, "Loading Source Data...");
                var sourceData = LoadSourceData(sourceDataSource, token);
                if (sourceData == null)
                {
                    args.Messege = "Failed to load source data.";
                    return args;
                }

                // Apply transformations
                SendProgress(progress, "Applying Transformations...");
                var transformedData = ApplyTransformations(sourceData, transformationSteps, token);

                // Load into target entity
                SendProgress(progress, $"Loading Data into Target: {targetEntity}...");
                var destinationDataSource = DMEEditor.GetDataSource(sourceDataSource);
                destinationDataSource?.InsertEntity(targetEntity, transformedData);

                args.Messege = "Transform and Load Completed Successfully.";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "Action Canceled.";
            }
            catch (Exception ex)
            {
                args.Messege = $"Error during TransformAndLoadAction: {ex.Message}";
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
            cancellationTokenSource.Cancel();
            return new PassedArgs { Messege = "Transform and Load Action Stopped." };
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

        private object LoadSourceData(string sourceDataSource, CancellationToken token)
        {
            var dataSource = DMEEditor.GetDataSource(sourceDataSource);
            if (dataSource == null)
            {
                DMEEditor.AddLogMessage("Beep", $"Source data source '{sourceDataSource}' not found.", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            // Assuming you load all data (adjust as needed for filters/paging)
            return dataSource.GetEntity(dataSource.EntitiesNames.First(), null);
        }

        private List<object> ApplyTransformations(object sourceData, List<Func<object, object>> transformationSteps, CancellationToken token)
        {
            var transformedData = new List<object>();
            foreach (var record in (IEnumerable<object>)sourceData)
            {
                if (token.IsCancellationRequested) break;

                var transformedRecord = record;
                foreach (var step in transformationSteps)
                {
                    transformedRecord = step.Invoke(transformedRecord);
                }
                transformedData.Add(transformedRecord);
            }
            return transformedData;
        }

        private void SendProgress(IProgress<PassedArgs> progress, string message)
        {
            progress?.Report(new PassedArgs { Messege = message });
        }
        #endregion
    }
}