using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// Per-app governance backed by a local JSON store (members, approval tickets,
/// audit entries). Production deployments should swap the JSON sink for the
/// engine's <c>IBeepAudit</c> + a durable ticket store — the contract stays the same.
/// </summary>
internal sealed class AppGovernanceWorkflow : IAppGovernanceWorkflow
{
    private readonly IDMEEditor _editor;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly object _lock = new();

    public AppGovernanceWorkflow(IDMEEditor editor) => _editor = editor;

    public Task<StudioResult<IReadOnlyList<AppRoleAssignment>>> ListMembersAsync(string appId, CancellationToken ct = default)
        => Task.FromResult(Read(appId, s => StudioResult<IReadOnlyList<AppRoleAssignment>>.Ok(s.Members)));

    public Task<StudioResult<AppRoleAssignment>> AssignRoleAsync(string appId, string userId, AppMemberRole role, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Invalid<AppRoleAssignment>("userId is required.");
        return Task.FromResult(Write(appId, s =>
        {
            s.Members.RemoveAll(m => m.UserId == userId);
            var assign = new AppRoleAssignment { AppId = appId, UserId = userId, Role = role };
            s.Members.Add(assign);
            return StudioResult<AppRoleAssignment>.Ok(assign);
        }));
    }

    public Task<StudioResult<bool>> RevokeRoleAsync(string appId, string userId, CancellationToken ct = default)
        => Task.FromResult(Write(appId, s => { s.Members.RemoveAll(m => m.UserId == userId); return StudioResult<bool>.Ok(true); }));

    public Task<StudioResult<bool>> CanUserAsync(string appId, string userId, AppMemberRole required, CancellationToken ct = default)
    {
        return Task.FromResult(Read(appId, s =>
        {
            var m = s.Members.FirstOrDefault(x => x.UserId == userId);
            var has = m != null && m.Role >= required;
            return StudioResult<bool>.Ok(has);
        }));
    }

    public Task<StudioResult<ApprovalTicket>> RequestApprovalAsync(string appId, string envId, string action, string requestedBy, string? reason = null, CancellationToken ct = default)
    {
        return Task.FromResult(Write(appId, s =>
        {
            var env = _editor.AppRegistry?.GetApp(appId)?.GetEnvironment(envId);
            var required = env?.RequiresApproval == true || env?.IsProduction == true ? 2 : 1;
            var ticket = new ApprovalTicket { AppId = appId, EnvId = envId, Action = action, RequestedBy = requestedBy, Reason = reason, RequiredApprovals = required };
            s.Tickets.Add(ticket);
            return StudioResult<ApprovalTicket>.Ok(ticket);
        }));
    }

    public Task<StudioResult<ApprovalTicket>> DecideAsync(string appId, string ticketId, bool approved, string decidedBy, string? comment = null, CancellationToken ct = default)
    {
        return Task.FromResult(WriteAcross(appId, ticketId: null, fn: s =>
        {
            var t = s.Tickets.FirstOrDefault(x => x.Id == ticketId);
            if (t == null) return StudioResult<ApprovalTicket>.Fail(StudioErrorCode.NotFound, $"Ticket '{ticketId}' not found.");
            if (t.State != ApprovalState.Open) return StudioResult<ApprovalTicket>.Fail(StudioErrorCode.InvalidArgument, $"Ticket already {t.State}.");
            if (approved)
            {
                if (!t.Approvers.Contains(decidedBy)) t.Approvers.Add(decidedBy);
                if (t.Approvers.Count >= t.RequiredApprovals) { t.State = ApprovalState.Approved; t.DecidedAt = DateTimeOffset.UtcNow; t.DecidedBy = decidedBy; t.Comment = comment; }
            }
            else { t.State = ApprovalState.Denied; t.DecidedAt = DateTimeOffset.UtcNow; t.DecidedBy = decidedBy; t.Comment = comment; }
            return StudioResult<ApprovalTicket>.Ok(t);
        }));
    }

    public Task<StudioResult<IReadOnlyList<ApprovalTicket>>> ListApprovalsAsync(string appId, string? envId = null, CancellationToken ct = default)
        => Task.FromResult(Read(appId, s => StudioResult<IReadOnlyList<ApprovalTicket>>.Ok(s.Tickets.Where(t => envId == null || t.EnvId == envId).ToList())));

    public Task<StudioResult<PolicyDecision>> EvaluateAsync(string appId, string envId, string action, string userId, CancellationToken ct = default)
    {
        return Task.FromResult(Read(appId, s =>
        {
            var d = new PolicyDecision();
            var env = _editor.AppRegistry?.GetApp(appId)?.GetEnvironment(envId);
            var member = s.Members.FirstOrDefault(x => x.UserId == userId);
            if (member == null) { d.Reasons.Add("User is not a member of the app."); return StudioResult<PolicyDecision>.Ok(d); }
            var needsOperator = env is { IsProduction: true } or { RequiresApproval: true };
            if (member.Role < AppMemberRole.Operator && needsOperator) d.Reasons.Add("Operator role required for protected environments.");
            var open = s.Tickets.FirstOrDefault(t => t.EnvId == envId && t.Action == action && t.State == ApprovalState.Open);
            if (needsOperator) { if (open == null) d.Reasons.Add("An open approval ticket is required."); else d.OpenTicketId = open.Id; }
            d.Allowed = d.Reasons.Count == 0;
            return StudioResult<PolicyDecision>.Ok(d);
        }));
    }

    public Task<StudioResult<int>> RecordAuditAsync(string appId, string envId, string action, string userId, string? detail = null, CancellationToken ct = default)
    {
        return Task.FromResult(Write(appId, s =>
        {
            var entry = new AppAuditEntry { Sequence = s.NextAuditSeq++, AppId = appId, EnvId = envId, Action = action, UserId = userId, Detail = detail, At = DateTimeOffset.UtcNow };
            s.Audit.Add(entry);
            return StudioResult<int>.Ok(s.Audit.Count);
        }));
    }

    public Task<StudioResult<IReadOnlyList<AppAuditEntry>>> QueryAuditAsync(string appId, string? envId = null, int skip = 0, int take = 100, CancellationToken ct = default)
        => Task.FromResult(Read(appId, s => StudioResult<IReadOnlyList<AppAuditEntry>>.Ok(s.Audit.Where(a => envId == null || a.EnvId == envId).Skip(skip).Take(take).ToList())));

    // ── JSON store ─────────────────────────────────────────────────────────
    private sealed class Store { public List<AppRoleAssignment> Members { get; set; } = new(); public List<ApprovalTicket> Tickets { get; set; } = new(); public List<AppAuditEntry> Audit { get; set; } = new(); public long NextAuditSeq { get; set; } = 1; }

    private StudioResult<T> Read<T>(string appId, Func<Store, StudioResult<T>> fn)
    { lock (_lock) { try { return fn(Load(appId)); } catch (Exception ex) { return StudioResult<T>.Fail(StudioErrorCode.HostNotSupported, ex.Message); } } }

    private StudioResult<T> Write<T>(string appId, Func<Store, StudioResult<T>> fn)
    { lock (_lock) { try { var s = Load(appId); var r = fn(s); Save(appId, s); return r; } catch (Exception ex) { return StudioResult<T>.Fail(StudioErrorCode.HostNotSupported, ex.Message); } } }

    private StudioResult<T> WriteAcross<T>(string? appId, string? ticketId, Func<Store, StudioResult<T>> fn)
    {
        lock (_lock)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(appId))
                {
                    var s = Load(appId);
                    var r = fn(s);
                    Save(appId, s);
                    return r;
                }
                // AppId not provided: search all stores for the ticket (legacy path for cross-app lookups)
                foreach (var file in Directory.GetFiles(Root(), "*.json"))
                {
                    var id = Path.GetFileNameWithoutExtension(file);
                    var s = Load(id);
                    var r = fn(s);
                    Save(id, s);
                    if (r.IsSuccess || r.Error.Code != StudioErrorCode.NotFound) return r;
                }
                return fn(new Store());
            }
            catch (Exception ex) { return StudioResult<T>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
        }
    }

    private Store Load(string appId) { var p = Path.Combine(Root(), appId + ".json"); return File.Exists(p) ? JsonSerializer.Deserialize<Store>(File.ReadAllText(p), Json) ?? new() : new(); }
    private void Save(string appId, Store s) { Directory.CreateDirectory(Root()); File.WriteAllText(Path.Combine(Root(), appId + ".json"), JsonSerializer.Serialize(s, Json)); }
    private string Root() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeepDM", "Studio", "governance");

    private static Task<StudioResult<T>> Invalid<T>(string msg) => Task.FromResult(StudioResult<T>.Fail(StudioErrorCode.InvalidArgument, msg));
}
