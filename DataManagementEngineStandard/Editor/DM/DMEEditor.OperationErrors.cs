#nullable enable
using System;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep
{
    public partial class DMEEditor : IScopedEditorErrors
    {
        private readonly AsyncLocal<EditorErrorScope?> _operationErrors = new();
        private IErrorsInfo _sharedErrors = new ErrorsInfo();
        public IDisposable BeginOperationErrors(IErrorsInfo outcome)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            var scope = new EditorErrorScope(this, _operationErrors.Value, outcome);
            _operationErrors.Value = scope;
            return scope;
        }
        private sealed class EditorErrorScope : IDisposable
        {
            private readonly DMEEditor _editor;
            private readonly EditorErrorScope? _parent;
            private readonly AsyncLocal<bool> _completedInFlow = new();
            public IErrorsInfo Outcome { get; }
            public EditorErrorScope(DMEEditor editor, EditorErrorScope? parent, IErrorsInfo outcome)
            { _editor = editor; _parent = parent; Outcome = outcome; }
            public void Dispose()
            {
                if (_completedInFlow.Value) return;
                var current = _editor._operationErrors.Value;
                var found = current;
                while (found != null && !ReferenceEquals(found, this)) found = found._parent;
                if (found == null) throw new InvalidOperationException("Editor error scope does not belong to this execution flow.");
                // Separate datasources can finish in a different order from their begin calls.
                // Completion is flow-local: a child cannot dispose its parent's context state.
                _completedInFlow.Value = true;
                if (ReferenceEquals(current, this))
                {
                    var parent = _parent;
                    while (parent != null && parent._completedInFlow.Value) parent = parent._parent;
                    _editor._operationErrors.Value = parent;
                }
            }
        }
    }
}
