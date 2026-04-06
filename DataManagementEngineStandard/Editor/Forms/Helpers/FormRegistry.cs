using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Thread-safe registry for all active FormsManager instances.
    /// Shared singleton — pass a single instance to all FormsManager constructors
    /// so they can discover and call each other.
    /// </summary>
    public class FormRegistry : IFormRegistry
    {
        private readonly ConcurrentDictionary<string, IUnitofWorksManager> _forms
            = new ConcurrentDictionary<string, IUnitofWorksManager>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, object> _globals
            = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private volatile string _activeFormName;

        /// <inheritdoc/>
        public string ActiveFormName => _activeFormName;

        /// <inheritdoc/>
        public event EventHandler<FormLifecycleEventArgs> FormLifecycleChanged;

        /// <inheritdoc/>
        public void RegisterForm(string formName, IUnitofWorksManager form)
        {
            if (string.IsNullOrWhiteSpace(formName)) throw new ArgumentNullException(nameof(formName));
            if (form == null) throw new ArgumentNullException(nameof(form));
            _forms[formName] = form;
            RaiseLifecycle(formName, FormLifecycleEvent.Registered);
        }

        /// <inheritdoc/>
        public bool UnregisterForm(string formName)
        {
            if (!_forms.TryRemove(formName, out _)) return false;
            if (string.Equals(_activeFormName, formName, StringComparison.OrdinalIgnoreCase))
                _activeFormName = null;
            RaiseLifecycle(formName, FormLifecycleEvent.Unregistered);
            return true;
        }

        /// <inheritdoc/>
        public IUnitofWorksManager GetForm(string formName)
        {
            _forms.TryGetValue(formName, out var form);
            return form;
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetActiveFormNames() => new List<string>(_forms.Keys);

        /// <inheritdoc/>
        public bool FormExists(string formName) => _forms.ContainsKey(formName ?? string.Empty);

        /// <inheritdoc/>
        public void SetActiveForm(string formName)
        {
            var old = _activeFormName;
            _activeFormName = formName;
            if (old != null && !string.Equals(old, formName, StringComparison.OrdinalIgnoreCase))
                RaiseLifecycle(old, FormLifecycleEvent.Deactivated);
            if (formName != null)
                RaiseLifecycle(formName, FormLifecycleEvent.Activated);
        }

        /// <inheritdoc/>
        public void SetGlobal(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _globals[name] = value;
        }

        /// <inheritdoc/>
        public object GetGlobal(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            _globals.TryGetValue(name, out var v);
            return v;
        }

        /// <inheritdoc/>
        public bool GlobalExists(string name)
            => !string.IsNullOrWhiteSpace(name) && _globals.ContainsKey(name);

        private void RaiseLifecycle(string formName, FormLifecycleEvent evt)
            => FormLifecycleChanged?.Invoke(this, new FormLifecycleEventArgs { FormName = formName, Event = evt });
    }
}
