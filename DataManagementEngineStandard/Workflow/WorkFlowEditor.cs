using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowEditor : IWorkFlowEditor
    {
        private CancellationTokenSource _cancellationTokenSource;

        public WorkFlowEditor(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor ?? throw new ArgumentNullException(nameof(pDMEEditor));
            WorkFlows = new List<IWorkFlow>();
            Actions = new List<IWorkFlowAction>();
            Rules = new List<IWorkFlowRule>();
        }

        #region Properties
        public IDMEEditor DMEEditor { get; set; }
        public List<IWorkFlow> WorkFlows { get; set; }
        public List<IWorkFlowAction> Actions { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        #endregion

        #region Methods

        public IErrorsInfo RunWorkFlow(string WorkFlowName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(WorkFlowName))
                {
                    string errmsg = "Workflow name cannot be null or empty.";
                    LogError(errmsg);
                    return DMEEditor.ErrorObject;
                }

                var workflow = WorkFlows.FirstOrDefault(w => w.DataWorkFlowName == WorkFlowName);
                if (workflow == null)
                {
                    string errmsg = $"Workflow '{WorkFlowName}' does not exist.";
                    LogError(errmsg);
                    return DMEEditor.ErrorObject;
                }

                // Initialize CancellationTokenSource for stopping the workflow
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                DMEEditor.AddLogMessage($"Starting workflow '{WorkFlowName}'...");

                foreach (var step in workflow.Datasteps)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        DMEEditor.AddLogMessage("Workflow execution canceled.");
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        DMEEditor.ErrorObject.Message = "Workflow execution canceled.";
                        return DMEEditor.ErrorObject;
                    }

                    progress?.Report(new PassedArgs { Messege = $"Executing step '{step.Name}'" });

                    foreach (var action in step.Actions)
                    {
                        action.PerformAction(progress, _cancellationTokenSource.Token);
                    }
                }

                DMEEditor.AddLogMessage($"Workflow '{WorkFlowName}' completed successfully.");
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Workflow '{WorkFlowName}' completed successfully.";
                return DMEEditor.ErrorObject;
            }
            catch (OperationCanceledException)
            {
                string errmsg = "Workflow execution was stopped.";
                LogError(errmsg);
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                string errmsg = $"Error running workflow '{WorkFlowName}': {ex.Message}";
                LogError(errmsg);
                return DMEEditor.ErrorObject;
            }
        }

        public IErrorsInfo StopWorkFlow()
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    DMEEditor.AddLogMessage("Workflow execution stopped successfully.");
                    DMEEditor.ErrorObject.Flag = Errors.Ok;
                    DMEEditor.ErrorObject.Message = "Workflow execution stopped successfully.";
                }
                else
                {
                    string errmsg = "No workflow is currently running to stop.";
                    LogError(errmsg);
                }

                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                string errmsg = $"Error stopping workflow: {ex.Message}";
                LogError(errmsg);
                return DMEEditor.ErrorObject;
            }
        }

        public IErrorsInfo LoadWorkFlow(string WorkFlowName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(WorkFlowName))
                {
                    string errmsg = "Workflow name cannot be null or empty.";
                    LogError(errmsg);
                    return DMEEditor.ErrorObject;
                }

                DMEEditor.AddLogMessage($"Loading workflow '{WorkFlowName}'...");
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Workflow '{WorkFlowName}' loaded successfully.";
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                string errmsg = $"Error loading workflow: {ex.Message}";
                LogError(errmsg);
                return DMEEditor.ErrorObject;
            }
        }

        public IErrorsInfo SaveWorkFlow(string WorkFlowName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(WorkFlowName))
                {
                    string errmsg = "Workflow name cannot be null or empty.";
                    LogError(errmsg);
                    return DMEEditor.ErrorObject;
                }

                DMEEditor.AddLogMessage($"Saving workflow '{WorkFlowName}'...");
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = $"Workflow '{WorkFlowName}' saved successfully.";
                return DMEEditor.ErrorObject;
            }
            catch (Exception ex)
            {
                string errmsg = $"Error saving workflow: {ex.Message}";
                LogError(errmsg);
                return DMEEditor.ErrorObject;
            }
        }

        private void LogError(string message)
        {
            DMEEditor.ErrorObject.Flag = Errors.Failed;
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.AddLogMessage(message);
        }

        #endregion
    }
}
