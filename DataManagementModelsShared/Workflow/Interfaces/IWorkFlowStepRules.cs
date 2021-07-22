namespace TheTechIdea.Beep
{
    public interface IWorkFlowStepRules
    {
        int Id { get; set; }
        string Rule { get; set; }
        string RuleDescription { get; set; }
        string RuleName { get; set; }
    }
}