using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowStep
    {
        event EventHandler<IWorkFlowEventArgs> WorkFlowStepStarted;
        event EventHandler<IWorkFlowEventArgs> WorkFlowStepEnded;
        event EventHandler<IWorkFlowEventArgs> WorkFlowStepRunning;

        int Seq { get; set; }
        string Description { get; set; }
        string ID { get; set; }
        string StepName { get; set; }
        List<IWorkFlowRule> Rules { get; set; }
        IWorkFlowStep PrevStep { get; set; }
        List<IWorkFlowStep> NextStep { get; set; }
        List<IPassedArgs> InParameters { get; set; }
        List<IPassedArgs> OutParameters { get; set; }
        Boolean IsFinish { get; set; }
        Boolean IsRunning { get; set; }

        }
    public class IWorkFlowEventArgs :PassedArgs
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