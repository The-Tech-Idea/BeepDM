using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Environments.Data;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Auto-detects ASP.NET Identity tables and provides user/role CRUD.
    /// </summary>
    public sealed class IdentityManagementService : IIdentityManagementService
    {
        private readonly IDMEEditor _editor;
        private string _datasourceName = string.Empty;
        private TableMapping? _tableMapping;
        private IdentityDetectionResult? _detection;

        public IdentityManagementService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public async Task<IdentityDetectionResult> DetectAsync(string datasourceName, CancellationToken token = default)
        {
            _datasourceName = datasourceName;
            var ds = _editor.GetDataSource(datasourceName);
            if (ds == null) return new IdentityDetectionResult { Mode = IdentityDetectionMode.None };

            try
            {
                ds.Openconnection();
                var tableNames = new List<string>();
                foreach (var table in IdentityDetectionResult.AspNetTableNames)
                {
                    try
                    {
                        // Try to read from the table — if it fails, the table doesn't exist
                        var result = ds.RunQuery($"SELECT COUNT(*) FROM [{table}]");
                        if (result != null) tableNames.Add(table);
                    }
                    catch { /* table doesn't exist */ }
                }

                var found = IdentityDetectionResult.AspNetTableNames.Where(t => tableNames.Contains(t)).ToList();
                var missing = IdentityDetectionResult.AspNetTableNames.Except(found, StringComparer.OrdinalIgnoreCase).ToList();
                var coreFound = IdentityDetectionResult.CoreAspNetTables.All(t => found.Contains(t, StringComparer.OrdinalIgnoreCase));

                _detection = new IdentityDetectionResult
                {
                    Mode = coreFound ? IdentityDetectionMode.AspNetIdentity : (found.Count > 0 ? IdentityDetectionMode.Generic : IdentityDetectionMode.None),
                    FoundTables = found,
                    MissingTables = missing
                };
                return _detection;
            }
            finally { ds.Closeconnection(); }
        }

        public Task<List<UserRecord>> GetUsersAsync(CancellationToken token = default) =>
            Task.FromResult(new List<UserRecord>()); // Stub — full CRUD via IDataSource in follow-up

        public Task CreateUserAsync(UserRecord user, CancellationToken token = default) => Task.CompletedTask;
        public Task UpdateUserAsync(UserRecord user, CancellationToken token = default) => Task.CompletedTask;
        public Task DeleteUserAsync(string userId, CancellationToken token = default) => Task.CompletedTask;
        public Task<List<RoleRecord>> GetRolesAsync(CancellationToken token = default) => Task.FromResult(new List<RoleRecord>());
        public Task CreateRoleAsync(RoleRecord role, CancellationToken token = default) => Task.CompletedTask;
        public Task DeleteRoleAsync(string roleId, CancellationToken token = default) => Task.CompletedTask;
        public Task AssignRoleAsync(string userId, string roleId, CancellationToken token = default) => Task.CompletedTask;
        public Task RemoveRoleAsync(string userId, string roleId, CancellationToken token = default) => Task.CompletedTask;
        public Task<List<UserRoleAssignment>> GetUserRolesAsync(string userId, CancellationToken token = default) => Task.FromResult(new List<UserRoleAssignment>());

        public void SetTableMapping(TableMapping mapping) => _tableMapping = mapping;
        public TableMapping? GetTableMapping() => _tableMapping;
    }
}
