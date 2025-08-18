using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Composite
{
    public interface ICompositeLayerDataSource
    {
        string DatabaseType { get; }
        CompositeLayer LayerInfo { get; set; }
        ILocalDB LocalDB { get; set; }

        bool AddEntitytoLayer(EntityStructure entity);
        IErrorsInfo CreateLayer();
        bool DropDatabase();
        IErrorsInfo DropEntity(string EntityName);
        bool GetAllEntitiesFromDataView();
        int GetEntityIdx(string entityName);
        EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);
        EntityStructure GetEntityStructure(string EntityName, bool refresh = false);
    }
}