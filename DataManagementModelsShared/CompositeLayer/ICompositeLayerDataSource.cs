using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.CompositeLayer
{
    public interface ICompositeLayerDataSource
    {
        string DatabaseType { get; }
        IDataViewDataSource DataViewSource { get; set; }
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