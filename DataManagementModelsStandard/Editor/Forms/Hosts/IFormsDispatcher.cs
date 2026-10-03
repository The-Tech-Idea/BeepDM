using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>Optional adapter-owned UI affinity. No platform dispatcher type is required.</summary>
public interface IFormsDispatcher
{
    bool CheckAccess();
    /// <summary>
    /// Executes once on the UI context, or acknowledges cancellation without executing.
    /// The task must not complete while an action can still execute. Already running actions
    /// finish before acknowledgement. Dispatch in submission order, not fire-and-forget.
    /// </summary>
    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);
}
