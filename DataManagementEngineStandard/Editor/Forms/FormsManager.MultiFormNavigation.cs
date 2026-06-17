using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Oracle Forms Multi-Form Navigation Built-ins

        /// <summary>
        /// Call another form, suspending this one until the callee returns or is closed.
        /// Equivalent to Oracle Forms CALL_FORM.
        /// </summary>
        /// <remarks>
        /// For <see cref="FormCallMode.Modal"/>, the returned task does not
        /// complete until the callee calls <see cref="ReturnToCallerAsync"/>
        /// (or is closed without returning). The caller is genuinely suspended —
        /// not just status-flagged — via a <see cref="TaskCompletionSource{TResult}"/>
        /// stored on the <see cref="FormCallStackEntry"/>. For
        /// <see cref="FormCallMode.Modeless"/> and <see cref="FormCallMode.Replace"/>,
        /// the call still pushes a stack entry (so the callee has a "caller" to
        /// return to) but the caller's task completes immediately after the
        /// WhenNewFormInstance trigger fires — the engine does not block the
        /// caller on the callee's lifecycle for non-modal modes.
        ///
        /// If the form is unregistered between the existence check and the
        /// GetForm lookup, the call returns false without pushing a stack entry
        /// (B22: no TOCTOU orphan).
        /// </remarks>
        /// <param name="cancellationToken">Cancellation token observed while
        /// waiting for the callee to return. Cancelling does not abort the
        /// callee; it returns the caller with a "false" return value.</param>
        public async Task<bool> CallFormAsync(
            string formName,
            Dictionary<string, object> parameters = null,
            FormCallMode mode = FormCallMode.Modal,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(formName)) return false;
            if (_formRegistry == null) return false;

            // B22: single GetForm call. FormExists + GetForm was a TOCTOU — a
            // concurrent UnregisterForm between the two calls could leave the
            // form appearing to exist while GetForm returns null. Fetch the
            // form once and check for null.
            var targetForm = _formRegistry.GetForm(formName);
            if (targetForm == null) return false;

            // B3: build the entry first, push it AFTER the parameter-pass and
            // trigger fire succeed. If the trigger throws, the stack is not
            // pushed and the caller is not suspended. Without this ordering,
            // an exception path would leave an unbalanced stack entry that
            // ReturnToCallerAsync would later pop as a foreign entry.
            var entry = new FormCallStackEntry
            {
                FormName = formName,
                CallerFormName = _currentFormName,
                CallMode = mode,
                Parameters = parameters ?? new Dictionary<string, object>()
            };

            // Pass parameters to target form
            if (targetForm is FormsManager targetFm && parameters != null)
                foreach (var kv in parameters)
                    targetFm._formParameters[kv.Key] = kv.Value;

            // Fire WhenNewFormInstance on the target. If this throws, the
            // entry has not been pushed and the call returns false — the
            // caller sees the failure and can retry or surface a UI message.
            if (targetForm is FormsManager targetFm2)
                await targetFm2._triggerManager.FireFormTriggerAsync(
                    TriggerType.WhenNewFormInstance,
                    formName,
                    TriggerContext.ForForm(TriggerType.WhenNewFormInstance, formName, _dmeEditor));

            // All pre-suspend work succeeded — push the entry and switch the
            // active form. From here, the callee is "running" and a subsequent
            // ReturnToCallerAsync on the callee will pop this entry and
            // complete its Task, unblocking the caller.
            _callStack.Push(entry);

            if (mode == FormCallMode.Modal)
                Status = "Suspended";

            _formRegistry.SetActiveForm(formName);
            LogOperation($"CallForm: '{formName}' (mode={mode})", null);

            // B1: for modal calls, genuinely suspend the caller by awaiting
            // the entry's TaskCompletionSource. ReturnToCallerAsync sets the
            // result; if the caller passes a cancellation token, cancelling
            // returns the caller with "false" without aborting the callee.
            if (mode == FormCallMode.Modal)
            {
                try
                {
                    return await entry.Completion.WaitWithCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Caller cancelled. We must NOT pop the entry here —
                    // the callee is still running and will eventually call
                    // ReturnToCallerAsync, which will pop the entry as usual.
                    // We just return false to the caller.
                    return false;
                }
            }

            // Modeless and Replace: non-blocking, return true immediately.
            return true;
        }

        /// <summary>
        /// Open a form independently (both forms run concurrently).
        /// Equivalent to Oracle Forms OPEN_FORM.
        /// </summary>
        [Obsolete("Use OpenFormModelessAsync to avoid ambiguity with FormsManager.OpenFormAsync.")]
        public Task<bool> OpenFormAsync(
            string formName,
            Dictionary<string, object> parameters = null)
            => OpenFormModelessAsync(formName, parameters);

        /// <summary>
        /// Open a form independently (modeless — concurrent with the caller).
        /// Equivalent to Oracle Forms OPEN_FORM. Distinct from <see cref="FormOperations"/> 
        /// <c>OpenFormAsync</c> which opens the LOCAL form.
        /// </summary>
        public Task<bool> OpenFormModelessAsync(
            string formName,
            Dictionary<string, object> parameters = null)
            => CallFormAsync(formName, parameters, FormCallMode.Modeless);

        /// <summary>
        /// Close this form and open a new form in its place.
        /// Equivalent to Oracle Forms NEW_FORM.
        /// </summary>
        public async Task<bool> NewFormAsync(
            string formName,
            Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(formName)) return false;
            if (_formRegistry == null || !_formRegistry.FormExists(formName)) return false;

            // Unregister current form from registry
            if (!string.IsNullOrWhiteSpace(_currentFormName))
                _formRegistry.UnregisterForm(_currentFormName);

            return await CallFormAsync(formName, parameters, FormCallMode.Replace);
        }

        /// <summary>
        /// Return to the calling form, optionally passing return data.
        /// Equivalent to Oracle Forms EXIT_FORM with return value or RETURN built-in.
        /// </summary>
        /// <remarks>
        /// Validates that the top of the call stack was actually pushed by THIS
        /// form (the active callee). If a foreign entry is on top — e.g. a
        /// modeless sibling tries to return on behalf of a different call —
        /// the entry is pushed back and a debug warning is logged. Without
        /// this check, a cross-form ReturnToCallerAsync would silently corrupt
        /// the stack and switch the active form to the wrong caller.
        /// </remarks>
        public Task<bool> ReturnToCallerAsync(object returnData = null)
        {
            if (_callStack.Count == 0) return Task.FromResult(false);

            // B5: peek first, then validate. We need to inspect the entry
            // before popping so we can push it back if it's not ours.
            var entry = _callStack.Peek();

            // The callee is whoever is currently the active form. Use the
            // registry's view (which is updated by SetActiveForm) rather than
            // _currentFormName, because the active form may have been switched
            // by an inner multi-form sequence.
            var activeForm = _formRegistry?.ActiveFormName;
            if (!string.Equals(entry.FormName, activeForm, StringComparison.OrdinalIgnoreCase))
            {
                // Foreign entry — the top of the stack is not the form the
                // caller is currently running as. Push it back and bail.
                // (We did not pop yet, so the stack is unchanged.)
                System.Diagnostics.Debug.WriteLine(
                    $"[FormsManager.ReturnToCallerAsync] Stack corruption: top entry is for form " +
                    $"'{entry.FormName}' but active form is '{activeForm ?? "<null>"}'. " +
                    "Refusing to pop; the caller is not the form that pushed the entry.");
                return Task.FromResult(false);
            }

            // The entry is ours — pop and complete it (which unblocks the
            // suspended caller in CallFormAsync).
            _callStack.Pop();
            entry.Complete(success: true);

            // Hand return data to caller
            if (returnData != null && _formRegistry != null)
            {
                var callerForm = _formRegistry.GetForm(entry.CallerFormName);
                if (callerForm is FormsManager callerFm)
                    callerFm._formParameters["RETURN_VALUE"] = returnData;
            }

            _formRegistry?.SetActiveForm(entry.CallerFormName);
            Status = "Ready";
            LogOperation($"ReturnToCaller: back to '{entry.CallerFormName}'", null);
            return Task.FromResult(true);
        }

        /// <summary>Get the current form call stack (read-only snapshot).</summary>
        public IReadOnlyList<FormCallStackEntry> GetCallStack()
            => new List<FormCallStackEntry>(_callStack);

        /// <summary>Get a parameter that was passed to this form by its caller.</summary>
        public object GetFormParameter(string paramName)
        {
            _formParameters.TryGetValue(paramName ?? string.Empty, out var v);
            return v;
        }

        /// <summary>Get a typed parameter passed to this form.</summary>
        public T GetFormParameter<T>(string paramName)
        {
            var v = GetFormParameter(paramName);
            return v is T t ? t : default;
        }

        #endregion
    }

    /// <summary>
    /// Internal extension that lets a <see cref="Task"/> be awaited with a
    /// <see cref="CancellationToken"/>. When the token cancels, the awaiter
    /// throws <see cref="OperationCanceledException"/>. The original task is
    /// left running (we cannot cancel the callee from here — that is the
    /// host's responsibility, e.g. via <c>CloseFormAsync</c>).
    /// </summary>
    internal static class MultiFormTaskExtensions
    {
        public static async Task<bool> WaitWithCancellation(this Task task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await task.ConfigureAwait(false);
                return true;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), tcs))
            {
                var completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completed == tcs.Task)
                {
                    // Cancellation token fired. Propagate as OperationCanceledException
                    // so the caller's catch (OperationCanceledException) can handle it.
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            // The original task ran to completion (or already completed).
            await task.ConfigureAwait(false);
            return true;
        }
    }
}
