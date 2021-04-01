namespace TheTechIdea.DataManagment_Engine
{
    public interface IWorkFlowStepRules
    {
        int Id { get; set; }
        string Rule { get; set; }
        string RuleDescription { get; set; }
        string RuleName { get; set; }
    }
}