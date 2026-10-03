using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional native upsert capability. A failed update alone never implies a missing row.</summary>
    public interface IUpsertDataSource
    {
        IErrorsInfo UpsertEntity(string entityName, object record);
    }
}
