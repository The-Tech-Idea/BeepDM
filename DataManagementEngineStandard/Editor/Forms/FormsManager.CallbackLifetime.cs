using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        private readonly object _callbackGate = new();
        private readonly AsyncLocal<int> _callbackDepth = new();
        private readonly TaskCompletionSource _disposeCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource _callbacksDrained;
        private int _pendingCallbacks;

        /// <summary>Manager-owned callbacks, registration, managed reads/LOV, local paging and synchronous validation, including awaited work.</summary>
        public int PendingCallbackCount { get { lock (_callbackGate) return _pendingCallbacks; } }

        private CallbackLease TryEnterCallback()
        {
            lock (_callbackGate)
            {
                if (_disposed) return null;
                if (_pendingCallbacks++ == 0)
                    _callbacksDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            _callbackDepth.Value++;
            return new CallbackLease(this);
        }

        private sealed class CallbackLease : IDisposable
        {
            private FormsManager _owner;
            internal CallbackLease(FormsManager owner) => _owner = owner;
            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                if (owner == null) return;
                owner._callbackDepth.Value--;
                TaskCompletionSource drained = null;
                lock (owner._callbackGate)
                    if (--owner._pendingCallbacks == 0) drained = owner._callbacksDrained;
                drained?.TrySetResult();
            }
        }

        /// <summary>Waits for the current callback set. After Dispose, callback admission remains closed.</summary>
        public Task WaitForPendingCallbacksAsync(CancellationToken cancellationToken = default)
        {
            if (_callbackDepth.Value > 0)
                throw new InvalidOperationException("A Forms callback cannot await its own drain. Dispose synchronously and drain from the host.");
            lock (_callbackGate)
                return _pendingCallbacks == 0 ? Task.CompletedTask : _callbacksDrained.Task.WaitAsync(cancellationToken);
        }

        /// <summary>Closes synchronously, then awaits teardown and admitted manager work.</summary>
        public ValueTask DisposeAsync()
        {
            Dispose();
            if (_callbackDepth.Value > 0)
                throw new InvalidOperationException("A Forms callback cannot await its own disposal. Drain from the host after it returns.");
            return new ValueTask(FinishDisposalAsync());
        }

        private async Task FinishDisposalAsync()
        {
            await _disposeCompleted.Task.ConfigureAwait(false);
            await WaitForPendingCallbacksAsync().ConfigureAwait(false);
            await _readResourcesRetired.ConfigureAwait(false);
        }
    }
}
