using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp.Steps
{
    public interface IDriverProvisionStep : ISetupStep { }
    public interface IConnectionConfigStep : ISetupStep { }
    public interface ISchemaSetupStep : ISetupStep { }
    public interface IDefaultsSetupStep : ISetupStep { }
    public interface ISeedingStep : ISetupStep { }
    public interface IDataImportStep : ISetupStep { }
}
