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
    public class BlazorWasmSetupWizardAdapter : ISetupWizardAdapter
    {
        /// <summary>Key used to persist <see cref="SetupState"/> in the backing store.</summary>
        public string StateStorageKey { get; set; } = "beep-setup-state";

        /// <inheritdoc/>
        public async Task<SetupReport> RunAsync(
            ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            // Resume: restore persisted state if available
            var stateJson = await LoadStateAsync(StateStorageKey);
            if (!string.IsNullOrEmpty(stateJson))
            {
                try
                {
                    var savedState = JsonSerializer.Deserialize<SetupState>(stateJson);
                    if (savedState != null) context.State = savedState;
                }
                catch { /* corrupt cache — start fresh */ }
            }

            var progress = new Progress<PassedArgs>(args => OnProgress(args));

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Save partial state so a future Resume() can continue from here
                if (context.State != null)
                {
                    var cancelJson = JsonSerializer.Serialize(context.State);
                    await SaveStateAsync(StateStorageKey, cancelJson);
                }
                OnProgress(new PassedArgs { ParameterInt1 = 0, Messege = "Setup wizard cancelled." });
                throw;
            }

            var report = wizard.GetReport();

            // Persist updated state for potential resume
            if (context.State != null)
            {
                var json = JsonSerializer.Serialize(context.State);
                await SaveStateAsync(StateStorageKey, json);
            }

            OnComplete(report);
            return report;
        }

        /// <inheritdoc/>
        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps) { }

        /// <inheritdoc/>
        public void ShowProgress(string stepId, int percentComplete, string message) { }

        /// <inheritdoc/>
        public void ShowResult(SetupReport report) { }

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
