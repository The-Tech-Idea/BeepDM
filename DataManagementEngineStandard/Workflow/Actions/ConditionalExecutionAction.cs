using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Conditional Execution Action", Name = "ConditionalExecutionAction", addinType = AddinType.Class)]
    public class ConditionalExecutionAction : IWorkFlowAction
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ConditionalExecutionAction(IDMEEditor dmeEditor)
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
        public string ActionTypeName { get; set; } = "ConditionalExecutionAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "ConditionalExecutionAction";
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

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Conditional Execution Action Started" };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                // Retrieve condition and actions directly from IPassedArgs
                var conditionExpression = InParameters?.FirstOrDefault(p => p.ParameterName == "ConditionExpression")?.ParameterString1;

                Action trueAction = InParameters?.FirstOrDefault(p => p.ParameterName == "TrueAction")?.TrueAction;
                Action falseAction = InParameters?.FirstOrDefault(p => p.ParameterName == "FalseAction")?.FalseAction;

                if (string.IsNullOrEmpty(conditionExpression))
                {
                    args.Messege = "ConditionExpression is missing.";
                    DMEEditor.AddLogMessage("ConditionalExecution", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Evaluate the condition
                bool conditionResult = EvaluateCondition(conditionExpression);
                DMEEditor.AddLogMessage("ConditionalExecution", $"Condition Evaluated: {conditionExpression} -> {conditionResult}", DateTime.Now, -1, null, Errors.Ok);

                // Execute based on condition result
                if (conditionResult)
                {
                    trueAction?.Invoke();
                    args.Messege = "Executed TrueAction.";
                }
                else
                {
                    falseAction?.Invoke();
                    args.Messege = "Executed FalseAction.";
                }

                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "Conditional Execution Canceled.";
            }
            catch (Exception ex)
            {
                args.Messege = $"Error: {ex.Message}";
                DMEEditor.AddLogMessage("ConditionalExecution", args.Messege, DateTime.Now, -1, null, Errors.Failed);
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
            return new PassedArgs { Messege = "Conditional Execution Stopped" };
        }
        #endregion

        #region Helper Methods
        private bool EvaluateCondition(string expression)
        {
            try
            {
                // Example condition evaluation logic: Supports simple True/False expressions
                if (expression.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (expression.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Additional simple numeric comparison logic
                if (expression.Contains(">") || expression.Contains("<") || expression.Contains("=="))
                {
                    var evaluator = new ExpressionEvaluator();
                    return evaluator.Evaluate(expression);
                }

                return false;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ConditionalExecution", $"Condition Evaluation Error: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }
        #endregion
    }

    /// <summary>
    /// A simple evaluator for logical expressions.
    /// </summary>
    public class ExpressionEvaluator
    {
        public bool Evaluate(string expression)
        {
            // Simulates simple numeric comparisons: "5 > 3", "10 < 20", "5 == 5"
            if (expression.Contains(">"))
            {
                var parts = expression.Split('>');
                return Convert.ToDouble(parts[0].Trim()) > Convert.ToDouble(parts[1].Trim());
            }
            else if (expression.Contains("<"))
            {
                var parts = expression.Split('<');
                return Convert.ToDouble(parts[0].Trim()) < Convert.ToDouble(parts[1].Trim());
            }
            else if (expression.Contains("=="))
            {
                var parts = expression.Split("==");
                return parts[0].Trim() == parts[1].Trim();
            }

            return false;
        }
    }
}
