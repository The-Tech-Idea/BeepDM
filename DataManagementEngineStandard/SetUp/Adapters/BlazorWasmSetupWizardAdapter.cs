using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Base adapter for Blazor WebAssembly applications.
    ///
    /// This class compiles without Blazor references and persists <see cref="SetupState"/>
    /// to a provided key/value store so interrupted setup runs can resume after a page reload.
    ///
    /// In your Blazor WASM project, subclass this and inject <c>IJSRuntime</c> to use
    /// <c>localStorage</c>:
    ///
    /// <code>
    /// public class MyWasmSetupAdapter : BlazorWasmSetupWizardAdapter
    /// {
    ///     private readonly IJSRuntime _js;
    ///     public MyWasmSetupAdapter(IJSRuntime js) => _js = js;
    ///
    ///     protected override async Task&lt;string&gt; LoadStateAsync(string key)
    ///         => await _js.InvokeAsync&lt;string&gt;("localStorage.getItem", key);
    ///
    ///     protected override async Task SaveStateAsync(string key, string json)
    ///         => await _js.InvokeVoidAsync("localStorage.setItem", key, json);
    /// }
    /// </code>
    /// </summary>
    public class BlazorWasmSetupWizardAdapter : SetupWizardAdapterBase
    {
        /// <summary>Key used to persist <see cref="SetupState"/> in the backing store.</summary>
        public string StateStorageKey { get; set; } = "beep-setup-state";

        /// <summary>Resume: restore persisted state if available.</summary>
        protected override async Task OnRunStartingAsync(ISetupWizard wizard, SetupContext context)
        {
            var stateJson = await LoadStateAsync(StateStorageKey).ConfigureAwait(false);
            if (string.IsNullOrEmpty(stateJson)) return;

            try
            {
                var savedState = JsonSerializer.Deserialize<SetupState>(stateJson);
                if (savedState != null) context.State = savedState;
            }
            catch { /* corrupt cache — start fresh */ }
        }

        /// <summary>Routes progress to this adapter's <see cref="OnProgress"/> extension point.</summary>
        protected override void ReportProgress(ISetupWizard wizard, SetupContext context, PassedArgs args)
            => OnProgress(args);

        /// <inheritdoc/>
        protected override async Task OnCancelledAsync(SetupContext context)
        {
            // Save partial state so a future Resume() can continue from here.
            await PersistStateAsync(context).ConfigureAwait(false);
            OnProgress(new PassedArgs { ParameterInt1 = 0, Messege = "Setup wizard cancelled." });
        }

        /// <inheritdoc/>
        protected override async Task OnFailedAsync(Exception ex, SetupContext context)
        {
            await PersistStateAsync(context).ConfigureAwait(false);
            OnProgress(new PassedArgs { ParameterInt1 = 0, Messege = $"Setup wizard failed: {ex.Message}" });
        }

        /// <inheritdoc/>
        protected override async Task OnCompletedAsync(SetupReport report, SetupContext context)
        {
            // Persist updated state for potential resume.
            await PersistStateAsync(context).ConfigureAwait(false);
            OnComplete(report);
        }

        private async Task PersistStateAsync(SetupContext context)
        {
            if (context?.State == null) return;
            var json = JsonSerializer.Serialize(context.State);
            await SaveStateAsync(StateStorageKey, json).ConfigureAwait(false);
        }

        // ── Extension points ─────────────────────────────────────────────────

        /// <summary>Override to load persisted state JSON from the platform store (e.g. localStorage).</summary>
        protected virtual Task<string> LoadStateAsync(string key) =>
            Task.FromResult<string>(null);

        /// <summary>Override to save state JSON to the platform store (e.g. localStorage).</summary>
        protected virtual Task SaveStateAsync(string key, string json) =>
            Task.CompletedTask;

        /// <summary>Override to react when the wizard finishes (e.g. navigate to a results page).</summary>
        protected virtual void OnComplete(SetupReport report) { }

        /// <summary>Override to surface progress updates to the component (e.g. call StateHasChanged).</summary>
        protected virtual void OnProgress(PassedArgs args) { }
    }
}
