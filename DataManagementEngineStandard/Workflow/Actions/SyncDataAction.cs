using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;


namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Sync Data Action", Name = "SyncDataAction", addinType = AddinType.Class)]
    public class SyncDataAction : IWorkFlowAction
    {
        public SyncDataAction(IDMEEditor dmeEditor)
        {
            editor = dmeEditor;
            Id = Guid.NewGuid().ToString();
            NextAction = new List<IWorkFlowAction>();
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            _dataSyncManager = new DataSyncManager(editor);
        }

        #region Properties
        private readonly DataSyncManager _dataSyncManager;
        private CancellationTokenSource _cancellationTokenSource;

        public IWorkFlowAction PrevAction { get; set; }
        public List<IWorkFlowAction> NextAction { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }

        private IDMEEditor editor;

        public string Id { get; set; }
        public string ActionTypeName { get; set; } = "SyncDataAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; } = nameof(SyncDataAction);
        public string Name { get; set; } = "Sync Data Action";
        #endregion

        #region Events
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        #endregion

        #region PerformAction
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            return PerformAction(progress, _cancellationTokenSource.Token, null);
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> customAction)
        {
            var args = new PassedArgs { Messege = "Sync Data Action Started", ParameterInt1 = 0 };
            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;
                progress?.Report(args);

                // Extract parameters for DataSyncManager
                var schemaID = InParameters.FirstOrDefault(p => p.ParameterString1 == "SchemaID")?.ParameterString1;

                if (string.IsNullOrEmpty(schemaID))
                {
                    throw new ArgumentException("SchemaID parameter is missing.");
                }

                // Perform synchronization using DataSyncManager
                var schema = _dataSyncManager.SyncSchemas.FirstOrDefault(s => s.ID == schemaID);
                if (schema == null)
                {
                    throw new ArgumentException($"No sync schema found for ID: {schemaID}");
                }

                _dataSyncManager.SyncData(schema, token, (Progress<PassedArgs>)progress);

                args.Messege = "Data Synchronization Completed";
                args.ParameterInt1 = 100;
                progress?.Report(args);

                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "Synchronization Canceled";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Canceled", ActionName = Name });
            }
            catch (Exception ex)
            {
                args.Messege = $"Error during Sync: {ex.Message}";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = $"Action Failed: {ex.Message}", ActionName = Name });
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
            if (_cancellationTokenSource != null && !IsFinish)
            {
                _cancellationTokenSource.Cancel();
                return new PassedArgs { Messege = "Sync Action Stopped" };
            }
            return new PassedArgs { Messege = "No action is running." };
        }
        #endregion
    }
}
