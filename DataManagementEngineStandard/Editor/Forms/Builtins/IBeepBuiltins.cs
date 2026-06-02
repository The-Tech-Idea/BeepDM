using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Builtins
{
    /// <summary>
    /// Minimal host abstraction required by <see cref="IBeepBuiltins"/>.
    /// Defined in the BeepDM Editor assembly so the built-ins contract has no
    /// dependency on the WinForms or any other presentation project. The
    /// concrete WinForms host (<c>IBeepFormsHost</c> in
    /// <c>Beep.Winform.Data.Integrated.Controls</c>) implements this interface
    /// by delegating each member to its existing proxy methods.
    /// </summary>
    public interface IBuiltinHost
    {
        string? ActiveBlockName { get; }
        string? ActiveItemName { get; }

        bool TrySetActiveBlock(string blockName);
        bool TrySetActiveItem(string blockName, string itemName);

        bool IsBlockRegistered(string blockName);
        bool IsItemRegistered(string blockName, string itemName);
        IReadOnlyList<string> GetRegisteredBlockNames();
        IReadOnlyList<string> GetRegisteredItemNames(string blockName);

        DataBlockInfo? GetBlockInfo(string blockName);
        IUnitofWork? GetBlockUnitOfWork(string blockName);
        object? GetCurrentBlockItem(string blockName);

        bool IsBlockQueryAllowed(string blockName);
        void SetBlockCurrentRecordIndex(string blockName, int index);
        int GetBlockRecordCount(string blockName);

        DataBlockMode GetBlockMode(string blockName);
        void SetBlockMode(string blockName, DataBlockMode mode);

        // LOV
        bool HasLov(string blockName, string fieldName);
        LOVDefinition? GetLov(string blockName, string fieldName);
        Task<LOVResult> ShowLovAsync(string blockName, string fieldName, string? searchText, CancellationToken ct);

        // Item / block property bag
        bool TryGetItemProperty(string blockName, string itemName, string property, out object? value);
        bool TrySetItemProperty(string blockName, string itemName, string property, object? value);
        bool TryGetBlockProperty(string blockName, string property, out object? value);
        bool TrySetBlockProperty(string blockName, string property, object? value);

        // Mutation
        Task<bool> SaveBlockAsync(string blockName, CancellationToken ct);
        Task<bool> RollbackBlockAsync(string blockName, CancellationToken ct);
        Task<bool> InsertBlockRecordAsync(string blockName, CancellationToken ct);
        Task<bool> DeleteBlockCurrentRecordAsync(string blockName, CancellationToken ct);
        Task<bool> ExecuteQueryAsync(string blockName, CancellationToken ct);
        Task<bool> ClearBlockAsync(string blockName, CancellationToken ct);
        Task<bool> ClearRecordAsync(string blockName, CancellationToken ct);

        // Validation
        RecordValidationResult? ValidateBlockRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing);

        // Trigger plumbing — BeepBuiltins raises synthetic triggers through these
        // host methods so the host can fan them out to its own TriggerExecuting /
        // TriggerExecuted event subscribers.
        void RaiseBuiltinTriggerExecuting(TriggerExecutingEventArgs args);
        void RaiseBuiltinTriggerExecuted(TriggerExecutedEventArgs args);
    }

    /// <summary>
    /// Oracle Forms-compatible built-in procedures exposed by the BeepForms host.
    /// Each method mirrors a named built-in (GO_BLOCK, NEXT_RECORD, SHOW_LOV, …) and
    /// raises <c>TriggerExecuting</c> / <c>TriggerExecuted</c> on the host so handlers
    /// can be attached via the standard trigger system.
    /// <para>
    /// Returning <c>false</c> from any method indicates the operation could not be
    /// completed. Failures that should surface to the user raise
    /// <see cref="BeepBuiltinException"/> with a Forms-style error code
    /// (for example <c>FRM-41003</c>).
    /// </para>
    /// </summary>
    public interface IBeepBuiltins
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        string? CurrentBlock { get; }
        string? CurrentItem { get; }
        IBuiltinHost Host { get; }

        // ── Block navigation ─────────────────────────────────────────────────────
        bool GoBlock(string blockName);
        bool NextBlock();
        bool PreviousBlock();
        bool FirstBlock();
        bool LastBlock();

        // ── Record navigation ────────────────────────────────────────────────────
        bool FirstRecord();
        bool LastRecord();
        bool NextRecord();
        bool PreviousRecord();
        bool GoRecord(int oneBased);

        // ── Item navigation ──────────────────────────────────────────────────────
        bool GoItem(string itemName);
        bool NextItem();
        bool PreviousItem();

        // ── Item / block properties ──────────────────────────────────────────────
        bool SetItemProperty(string itemName, string property, object? value);
        object? GetItemProperty(string itemName, string property);
        bool SetBlockProperty(string blockName, string property, object? value);
        object? GetBlockProperty(string blockName, string property);

        // ── LOV ──────────────────────────────────────────────────────────────────
        bool ShowLov(string blockName, string fieldName);
        bool ShowLov(string blockName, string fieldName, out object? selectedValue);

        // ── Transaction / form lifecycle ─────────────────────────────────────────
        bool Commit();
        Task<bool> CommitAsync(CancellationToken ct = default);
        bool Rollback();
        Task<bool> RollbackAsync(CancellationToken ct = default);
        bool Post();
        Task<bool> PostAsync(CancellationToken ct = default);

        // ── Query mode ───────────────────────────────────────────────────────────
        bool EnterQuery();
        bool ExecuteQuery();
        bool ExitQuery();
        Task<bool> ExecuteQueryAsync(CancellationToken ct = default);

        // ── Clear / reset ────────────────────────────────────────────────────────
        bool ClearBlock();
        bool ClearForm();
        bool ClearRecord();

        // ── Mode introspection / control ─────────────────────────────────────────
        DataBlockMode GetBlockMode(string blockName);
        bool SetBlockMode(string blockName, DataBlockMode mode);

        // ── Diagnostics ──────────────────────────────────────────────────────────
        IReadOnlyList<string> GetAvailableBuiltins();
    }
}
