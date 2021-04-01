using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine
{
    public interface IDataWorkFlowStep
    {
       
        int Seq { get; set; }
        string Description { get; set; }
        string ID { get; set; }
        string StepName { get; set; }
        string ActionName { get; set; }
        List<WorkFlowStepRules> Rules { get; set; }
        string PrevStep { get; set; }
        List<string> NextStep { get; set; }
        List<PassedArgs> InParameters { get; set; }
        List<PassedArgs> OutParameters { get; set; }
        string Mapping { get; set; }
        Boolean Finish { get; set; }

    }
    public class IDataWorkFlowEventArgs :PassedArgs
    {

    }
  
    public class WorkFlowStepRules : IWorkFlowStepRules
    {
        public WorkFlowStepRules()
        {

        }
        public int Id { get; set; }
        public string RuleName { get; set; }
        public string RuleDescription { get; set; }
        public string Rule { get; set; }
    }
    public enum EnumParameterType
    {
        Table,Query,Value,DataSource
    }


}