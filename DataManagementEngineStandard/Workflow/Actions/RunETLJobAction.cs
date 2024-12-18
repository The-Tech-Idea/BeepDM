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
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Run ETL Job Action", Name = "RunETLJobAction", addinType = AddinType.Class)]
    public class RunETLJobAction : IWorkFlowAction
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ETLDataCopier _dataCopier;

        public RunETLJobAction(IDMEEditor dmeEditor)
        {
            DMEEditor = dmeEditor;
            Id = Guid.NewGuid().ToString();
            NextAction = new List<IWorkFlowAction>();
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            Rules = new List<IWorkFlowRule>();
            _dataCopier = new ETLDataCopier(dmeEditor);
        }

        #region Properties
        public IWorkFlowAction PrevAction { get; set; }
        public List<IWorkFlowAction> NextAction { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public string Id { get; set; }
        public string ActionTypeName { get; set; } = "RunETLJobAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "RunETLJobAction";
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
            var args = new PassedArgs { Messege = "ETL Job Started" };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                var sourceDsName = GetParameterValue<string>("SourceDataSource");
                var targetDsName = GetParameterValue<string>("TargetDataSource");
                var sourceEntity = GetParameterValue<string>("SourceEntity");
                var targetEntity = GetParameterValue<string>("TargetEntity");
                var mappingRules = GetParameterValue<EntityDataMap_DTL>("MappingRules");

                var sourceDs = DMEEditor.GetDataSource(sourceDsName);
                var targetDs = DMEEditor.GetDataSource(targetDsName);

                if (sourceDs == null || targetDs == null)
                {
                    args.Messege = "Invalid DataSource Names";
                    DMEEditor.AddLogMessage("ETL", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                var progressReporter = new Progress<PassedArgs>(report =>
                {
                    progress?.Report(report);
                });

                // Use ETLDataCopier to execute the ETL job
                _dataCopier.CopyEntityDataAsync(
                    sourceDs,
                    targetDs,
                    sourceEntity,
                    targetEntity,
                    progressReporter,
                    token,
                    mappingRules
                ).Wait(token);

                args.Messege = "ETL Job Completed Successfully.";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "ETL Job Canceled.";
            }
            catch (Exception ex)
            {
                args.Messege = $"Error during ETL Job: {ex.Message}";
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
            return new PassedArgs { Messege = "ETL Job Stopped." };
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
