using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Cleanse Data Action", Name = "CleanseDataAction", addinType = AddinType.Class)]
    public class CleanseDataAction : IWorkFlowAction
    {
        public CleanseDataAction(IDMEEditor dmeEditor)
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
        public string ActionTypeName { get; set; } = "CleanseDataAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "CleanseDataAction";
        public IDMEEditor DMEEditor { get; }

        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        #endregion

        #region Perform Action
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return PerformAction(progress, token, null);
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Data Cleansing Action Started" };
            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                // Retrieve parameters
                string sourceEntity = GetParameterValue<string>("SourceEntity");
                string targetEntity = GetParameterValue<string>("TargetEntity") ?? sourceEntity;

                if (string.IsNullOrEmpty(sourceEntity) || actionToExecute == null)
                {
                    args.Messege = "Missing required parameters or cleansing function.";
                    DMEEditor.AddLogMessage("CleanseData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Fetch data
                var dataSource = DMEEditor.GetDataSource(sourceEntity);
                var data = dataSource?.GetEntity(sourceEntity, null);

                if (data == null)
                {
                    args.Messege = "Failed to retrieve data from source entity.";
                    DMEEditor.AddLogMessage("CleanseData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Apply cleansing logic
                var cleansedData = new List<object>();
                if (data is IEnumerable<object> dataList)
                {
                    foreach (var record in dataList)
                    {
                        // Prepare PassedArgs for the cleansing function
                        var cleanseArgs = new PassedArgs
                        {
                            ReturnData = record,
                            ParameterString1 = sourceEntity,
                            ParameterString2 = targetEntity
                        };

                        // Execute the cleansing function
                        PassedArgs cleansedArgs = (PassedArgs)actionToExecute.Invoke(cleanseArgs);

                        if (cleansedArgs?.ReturnData is object cleansedRecord && cleansedRecord != null)
                        {
                            cleansedData.Add(cleansedRecord);
                        }
                    }
                }

                // Store cleansed data
                var targetDataSource = DMEEditor.GetDataSource(targetEntity);
                if (targetDataSource != null)
                {
                    // Clear existing records in the target entity
                    var existingRecords = targetDataSource.GetEntity(targetEntity, null);
                    if (existingRecords is IEnumerable<object> recordList)
                    {
                        foreach (var record in recordList)
                        {
                            targetDataSource.DeleteEntity(targetEntity, record);
                        }
                    }

                    // Insert cleansed data into the target entity
                    foreach (var record in cleansedData)
                    {
                        targetDataSource.InsertEntity(targetEntity, record);
                    }
                }

                args.Messege = "Data cleansing completed successfully.";
                DMEEditor.AddLogMessage("CleanseData", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (Exception ex)
            {
                args.Messege = $"Error: {ex.Message}";
                DMEEditor.AddLogMessage("CleanseData", args.Messege, DateTime.Now, -1, null, Errors.Failed);
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
            return new PassedArgs { Messege = "Data Cleansing Action Stopped." };
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
