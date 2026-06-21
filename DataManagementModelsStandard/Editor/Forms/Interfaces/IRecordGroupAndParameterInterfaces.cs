using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Manages named in-memory record groups (RECORD_GROUP / RECORDGROUP_FROM_QUERY equivalents).
    /// Record groups are populated from queries and used for LOVs, combo boxes, and find dialogs.
    /// </summary>
    public interface IRecordGroupRegistry
    {
        void CreateRecordGroup(string name, string dataSourceName, string entityName, List<AppFilter> filters = null);

        Task<bool> PopulateRecordGroupAsync(string name, CancellationToken ct = default);

        RecordGroup GetRecordGroup(string name);

        IReadOnlyList<RecordGroup> GetAllRecordGroups();

        bool RemoveRecordGroup(string name);

        void ClearAllRecordGroups();

        bool RecordGroupExists(string name);
    }

    /// <summary>
    /// Manages named parameter lists (PARAMETER_LIST equivalents).
    /// Named collections of key-value pairs that can be passed between forms or to the engine.
    /// </summary>
    public interface IParameterListManager
    {
        ParameterList CreateParameterList(string name);
        bool DestroyParameterList(string name);
        void AddParameter(string listName, string paramName, object value);
        object GetParameter(string listName, string paramName);
        T GetParameter<T>(string listName, string paramName);
        bool RemoveParameter(string listName, string paramName);
        bool HasParameter(string listName, string paramName);
        ParameterList GetParameterList(string name);
        IReadOnlyList<ParameterList> GetAllParameterLists();
        bool ParameterListExists(string name);
        void ClearParameterList(string name);
    }
}
