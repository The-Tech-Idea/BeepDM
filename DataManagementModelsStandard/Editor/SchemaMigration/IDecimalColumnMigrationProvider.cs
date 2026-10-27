using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>Optional native capability. Locks and validates the exact existing decimal shape,
    /// then widens without reducing integer/fractional capacity or changing nullability.
    /// Unsupported providers must not emulate this with an unlocked ALTER.</summary>
    public interface IDecimalColumnMigrationProvider
    {
        string DecimalMigrationTargetIdentity { get; }
        IErrorsInfo WidenDecimalColumn(string entityName, EntityField expectedColumn, EntityField desiredColumn, string expectedTargetIdentity);
    }
}
