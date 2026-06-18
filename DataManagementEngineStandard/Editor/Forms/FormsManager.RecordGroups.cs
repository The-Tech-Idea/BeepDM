using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager : IRecordGroupRegistry, IParameterListManager
    {
        #region Record Groups

        private readonly ConcurrentDictionary<string, RecordGroup> _recordGroups = new(StringComparer.OrdinalIgnoreCase);

        public void CreateRecordGroup(string name, string dataSourceName, string entityName, List<AppFilter> filters = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var rg = new RecordGroup(name, dataSourceName, entityName, filters);
            _recordGroups[name] = rg;
        }

        public async Task<bool> PopulateRecordGroupAsync(string name, CancellationToken ct = default)
        {
            if (!_recordGroups.TryGetValue(name, out var rg))
                return false;

            try
            {
                var ds = _dmeEditor.GetDataSource(rg.DataSourceName);
                if (ds == null)
                {
                    LogError($"PopulateRecordGroup: datasource '{rg.DataSourceName}' not found for group '{name}'", null, null);
                    return false;
                }

                var es = ds.GetEntityStructure(rg.EntityName, false);
                if (es == null)
                {
                    LogError($"PopulateRecordGroup: entity '{rg.EntityName}' not found in datasource '{rg.DataSourceName}'", null, null);
                    return false;
                }

                rg.ColumnNames = es.Fields.Select(f => f.FieldName).ToList();

                // Record group population requires IUnitofWork creation through
                // the engine's data source infrastructure. This is a deferred feature
                // pending a dedicated IDataSource.CreateUnitOfWork API on the engine.
                rg.IsPopulated = false;
                return false;
            }
            catch (Exception ex)
            {
                LogError($"PopulateRecordGroup '{name}' failed", ex, null);
                return false;
            }
        }

        public RecordGroup GetRecordGroup(string name) =>
            _recordGroups.TryGetValue(name, out var rg) ? rg : null;

        public IReadOnlyList<RecordGroup> GetAllRecordGroups() =>
            _recordGroups.Values.ToList().AsReadOnly();

        public bool RemoveRecordGroup(string name) =>
            _recordGroups.TryRemove(name, out _);

        public void ClearAllRecordGroups() =>
            _recordGroups.Clear();

        public bool RecordGroupExists(string name) =>
            !string.IsNullOrWhiteSpace(name) && _recordGroups.ContainsKey(name);

        #endregion

        #region Parameter Lists

        private readonly ConcurrentDictionary<string, ParameterList> _parameterLists = new(StringComparer.OrdinalIgnoreCase);

        public ParameterList CreateParameterList(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var pl = new ParameterList(name);
            _parameterLists[name] = pl;
            return pl;
        }

        public bool DestroyParameterList(string name) =>
            _parameterLists.TryRemove(name, out _);

        public void AddParameter(string listName, string paramName, object value)
        {
            var pl = GetOrCreateList(listName);
            pl.AddParameter(paramName, value);
        }

        public object GetParameter(string listName, string paramName)
        {
            return _parameterLists.TryGetValue(listName, out var pl) ? pl.GetParameter(paramName) : null;
        }

        public T GetParameter<T>(string listName, string paramName)
        {
            return _parameterLists.TryGetValue(listName, out var pl) ? pl.GetParameter<T>(paramName) : default;
        }

        public bool RemoveParameter(string listName, string paramName)
        {
            return _parameterLists.TryGetValue(listName, out var pl) && pl.RemoveParameter(paramName);
        }

        public bool HasParameter(string listName, string paramName)
        {
            return _parameterLists.TryGetValue(listName, out var pl) && pl.HasParameter(paramName);
        }

        public ParameterList GetParameterList(string name) =>
            _parameterLists.TryGetValue(name, out var pl) ? pl : null;

        public IReadOnlyList<ParameterList> GetAllParameterLists() =>
            _parameterLists.Values.ToList().AsReadOnly();

        public bool ParameterListExists(string name) =>
            !string.IsNullOrWhiteSpace(name) && _parameterLists.ContainsKey(name);

        public void ClearParameterList(string listName)
        {
            if (_parameterLists.TryGetValue(listName, out var pl))
                pl.Clear();
        }

        private ParameterList GetOrCreateList(string name)
        {
            return _parameterLists.GetOrAdd(name, _ => new ParameterList(name));
        }

        #endregion

        #region Client Info

        private ClientInfo _clientInfo;

        /// <summary>Gets or sets the client session metadata for this FormsManager instance.</summary>
        public ClientInfo ClientInfo
        {
            get => _clientInfo ??= new ClientInfo();
            set => _clientInfo = value;
        }

        /// <summary>Sets user-defined client metadata (Oracle: DBMS_APPLICATION_INFO.SET_CLIENT_INFO).</summary>
        public void SetClientInfo(string clientInfo)
        {
            ClientInfo.ClientInfoText = clientInfo;
            ClientInfo.LastModified = DateTime.UtcNow;
        }

        /// <summary>Sets the application module and action (Oracle: DBMS_APPLICATION_INFO.SET_MODULE + SET_ACTION).</summary>
        public void SetClientModule(string moduleName, string action)
        {
            ClientInfo.ModuleName = moduleName;
            ClientInfo.Action = action;
            ClientInfo.LastModified = DateTime.UtcNow;
        }

        /// <summary>Sets the client application action within the current module.</summary>
        public void SetClientAction(string action)
        {
            ClientInfo.Action = action;
            ClientInfo.LastModified = DateTime.UtcNow;
        }

        /// <summary>Sets the client hostname (Oracle: CLIENT_HOST).</summary>
        public void SetClientHost(string hostName)
        {
            ClientInfo.ClientHost = hostName;
            ClientInfo.LastModified = DateTime.UtcNow;
        }

        /// <summary>Sets the client IP address.</summary>
        public void SetClientIpAddress(string ipAddress)
        {
            ClientInfo.ClientIpAddress = ipAddress;
            ClientInfo.LastModified = DateTime.UtcNow;
        }

        /// <summary>Gets the combined client info string for the current session.</summary>
        public string GetClientInfo() => ClientInfo.ClientInfoText;

        /// <summary>Gets the current module name.</summary>
        public string GetClientModule() => ClientInfo.ModuleName;

        /// <summary>Gets the current action name.</summary>
        public string GetClientAction() => ClientInfo.Action;

        /// <summary>Gets the client hostname.</summary>
        public string GetClientHost() => ClientInfo.ClientHost ?? Environment.MachineName;

        /// <summary>Gets the client IP address when determinable.</summary>
        public string GetClientIpAddress() => ClientInfo.ClientIpAddress;

        #endregion
    }
}
