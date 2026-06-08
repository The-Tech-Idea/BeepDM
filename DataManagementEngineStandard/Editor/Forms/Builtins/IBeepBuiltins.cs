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

        // Messaging — Oracle Forms MESSAGE / ALERT built-ins
        /// <summary>Push a status-bar message at the requested severity.</summary>
        void PublishMessage(string message, int messageLevel, BeepBuiltinMessageSeverity severity);
        /// <summary>Clear the current status-bar message.</summary>
        void ClearMessage();
        /// <summary>Show a modal alert. Returns the button the user clicked (1, 2, or 3).</summary>
        Task<int> ShowAlertAsync(string title, string message, BeepBuiltinAlertStyle style, string button1, string? button2, string? button3, CancellationToken ct);

        // Trigger plumbing — BeepBuiltins raises synthetic triggers through these
        // host methods so the host can fan them out to its own TriggerExecuting /
        // TriggerExecuted event subscribers.
        void RaiseBuiltinTriggerExecuting(TriggerExecutingEventArgs args);
        void RaiseBuiltinTriggerExecuted(TriggerExecutedEventArgs args);

        // ── Multi-form (M4-RUN-003) ────────────────────────────────────────────
        // The four multi-form built-ins are routed through the
        // host's Application property. The engine-side surface is
        // deliberately thin — the WinForms host is responsible
        // for the actual orchestration (opening / closing forms,
        // managing :GLOBAL variables).
        object? MultiFormOpenForm(string formName);
        bool MultiFormCloseForm(string formName);
        bool MultiFormGoForm(string formName);
        void MultiFormSetGlobal(string name, object? value);
        object? MultiFormGetGlobal(string name);

        // ── Application / Form property bag (M4-RUN-016) ────────────────────
        // The host (a BeepApplication) owns an application-level
        // dictionary and a per-form dictionary. The built-ins
        // route through these two methods; the engine stays
        // UI-agnostic.
        void SetApplicationProperty(string name, object? value);
        object? GetApplicationProperty(string name);
        void SetFormProperty(string formName, string name, object? value);
        object? GetFormProperty(string formName, string name);

        // ── LIST_VALUES (M4-RUN-015) ──────────────────────────────────────────
        // The host returns the LOV's records as a list. Used by
        // the runtime's list-of-values integration; the engine
        // stays UI-agnostic.
        System.Collections.Generic.IReadOnlyList<object> ListLovRecords(
            string blockName, string fieldName);
    }

    /// <summary>
    /// Severity for the Oracle Forms <c>MESSAGE</c> built-in. Maps to the
    /// status-bar / toast styling.
    /// </summary>
    public enum BeepBuiltinMessageSeverity
    {
        Hint = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Style of the Oracle Forms <c>ALERT</c> built-in. Maps to a message-box
    /// icon and tone.
    /// </summary>
    public enum BeepBuiltinAlertStyle
    {
        Info = 0,
        Caution = 1,
        Stop = 2,
        Note = 3
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

        // ── Messaging — Oracle Forms MESSAGE / ALERT built-ins ─────────────────
        /// <summary>
        /// Push a status-bar message at the requested severity. Mirrors
        /// <c>MESSAGE(text, severity)</c> where severity 0 = hint, 5 = warning,
        /// 10 = error and 15 = stop.
        /// </summary>
        void Message(string text, int ack = 0, BeepBuiltinMessageSeverity severity = BeepBuiltinMessageSeverity.Info);
        void ClearMessage();
        Task<int> AlertAsync(
            string title,
            string message,
            BeepBuiltinAlertStyle style,
            string button1,
            string? button2 = null,
            string? button3 = null,
            CancellationToken ct = default);

        // ── Diagnostics ──────────────────────────────────────────────────────────
        IReadOnlyList<string> GetAvailableBuiltins();

        // ── Multi-form (M4-RUN-003) ────────────────────────────────────────────
        // The four multi-form built-ins are routed through the
        // host's IBeepFormsHost.Application property. The host
        // owns a single BeepApplication that tracks OpenForms and
        // GlobalVariables. The engine-side declaration is
        // deliberately thin — the actual orchestration is the
        // WinForms host's responsibility (the engine is UI-agnostic).
        bool OpenForm(string formName);
        bool CloseForm(string formName);
        bool GoForm(string formName);
        void SetGlobal(string name, object? value);
        object? GetGlobal(string name);

        // ── Extended built-ins (M4-RUN-015..017) ──────────────────────────────
        // POPUP_LOV / LIST_VALUES round out the LOV surface.
        // SET_APPLICATION_PROPERTY / GET_APPLICATION_PROPERTY
        // / SET_FORM_PROPERTY / GET_FORM_PROPERTY expose a
        // property bag on the application and the active form.
        // RAISE_FORM_TRIGGER_FAILURE surfaces a
        // BeepBuiltinException with the developer-supplied
        // failure code.
        object? PopupLov(string blockName, string fieldName, string? searchText = null);
        IReadOnlyList<object> ListValues(string blockName, string fieldName);

        void SetApplicationProperty(string name, object? value);
        object? GetApplicationProperty(string name);
        void SetFormProperty(string name, object? value);
        object? GetFormProperty(string name);

        void RaiseFormTriggerFailure(string failureCode, string message);
    }
}
