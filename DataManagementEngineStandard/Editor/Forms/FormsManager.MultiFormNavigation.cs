using System;
using System.Collections.Generic;
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
        public async Task<bool> CallFormAsync(
            string formName,
            Dictionary<string, object> parameters = null,
            FormCallMode mode = FormCallMode.Modal)
        {
            if (string.IsNullOrWhiteSpace(formName)) return false;
            if (_formRegistry == null || !_formRegistry.FormExists(formName)) return false;

            // Push call stack entry
            var entry = new FormCallStackEntry
            {
                FormName = formName,
                CallerFormName = _currentFormName,
                CallMode = mode,
                Parameters = parameters ?? new Dictionary<string, object>()
            };
            _callStack.Push(entry);

            // Suspend this form
            if (mode == FormCallMode.Modal)
                Status = "Suspended";

            // Pass parameters to target form
            var targetForm = _formRegistry.GetForm(formName);
            if (targetForm is FormsManager targetFm && parameters != null)
                foreach (var kv in parameters)
                    targetFm._formParameters[kv.Key] = kv.Value;

            // Fire WhenNewFormInstance on the target
            if (targetForm is FormsManager targetFm2)
                await targetFm2._triggerManager.FireFormTriggerAsync(
                    TriggerType.WhenNewFormInstance,
                    formName,
                    TriggerContext.ForForm(TriggerType.WhenNewFormInstance, formName, _dmeEditor));

            _formRegistry.SetActiveForm(formName);
            LogOperation($"CallForm: '{formName}' (mode={mode})", null);
            return true;
        }

        /// <summary>
        /// Open a form independently (both forms run concurrently).
        /// Equivalent to Oracle Forms OPEN_FORM.
        /// </summary>
        public Task<bool> OpenFormAsync(
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
        public Task<bool> ReturnToCallerAsync(object returnData = null)
        {
            if (_callStack.Count == 0) return Task.FromResult(false);

            var entry = _callStack.Pop();

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
}
