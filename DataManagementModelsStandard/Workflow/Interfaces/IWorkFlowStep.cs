using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowStep
    {
        event EventHandler<WorkFlowEventArgs> WorkFlowStepStarted;
        event EventHandler<WorkFlowEventArgs> WorkFlowStepEnded;
        event EventHandler<WorkFlowEventArgs> WorkFlowStepRunning;

        int Seq { get; set; }
        string Description { get; set; }
        string ID { get; set; }
        string Name { get; set; }
        List<IWorkFlowRule> Rules { get; set; }
        IWorkFlowStep PrevStep { get; set; }
        List<IWorkFlowStep> NextStep { get; set; }
        List<IWorkFlowAction> Actions { get; set; }
        List<IPassedArgs> InParameters { get; set; }
        List<IPassedArgs> OutParameters { get; set; }
        string StepType { get; set; }
        string Code { get; set; }
        bool IsFinish { get; set; }
        bool IsRunning { get; set; }

    }
    public class WorkFlowEventArgs :PassedArgs
    {
        public IWorkFlowStep FlowStep { get; set; }
        public IWorkFlowAction FlowAction { get; set; }
        public IWorkFlowRule FlowRule { get; set; }
        public bool Cancel { get; set; }=false;

    }
  
  
    public enum EnumParameterType
    {
        Table,Query,Value,DataSource
    }


}