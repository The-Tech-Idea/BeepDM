using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// Validates a <see cref="SetupDefinition"/> without executing it.
    /// </summary>
    /// <remarks>
    /// Runs with no <c>IDMEEditor</c> and no datasource, so a CI job can gate a pull request on a
    /// definition's structure before it touches a database.
    /// </remarks>
    public interface ISetupDefinitionValidator
    {
        IErrorsInfo Validate(SetupDefinition definition);
    }
}
