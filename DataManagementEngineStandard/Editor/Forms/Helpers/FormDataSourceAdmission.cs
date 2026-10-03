using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    internal sealed class FormDataSourceAdmission : IDisposable
    {
        private sealed class Slot { internal FormDataSourceAdmission Owner; }
        private static readonly ConditionalWeakTable<IDataSource, Slot> Slots = new();
        private readonly Slot _slot;
        private Func<bool> _releaseCondition;
        private int _disposed;

        private FormDataSourceAdmission(Slot slot) { _slot = slot; }

        internal static FormDataSourceAdmission Enter(IDataSource source)
        {
            var slot = Slots.GetValue(source, _ => new Slot());
            while (true)
            {
                var current = Volatile.Read(ref slot.Owner);
                if (current != null)
                {
                    if (current.TryRelease()) continue;
                    throw new InvalidOperationException("Another form commit or unreconciled transaction owns this datasource.");
                }
                var lease = new FormDataSourceAdmission(slot);
                if (Interlocked.CompareExchange(ref slot.Owner, lease, null) == null) return lease;
            }
        }

        internal void HoldUntil(Func<bool> reconciled) { _releaseCondition = reconciled; }

        private bool TryRelease()
        {
            if (Volatile.Read(ref _disposed) == 0) return false;
            try
            {
                if (_releaseCondition != null && !_releaseCondition()) return false;
            }
            catch { return false; }
            Interlocked.CompareExchange(ref _slot.Owner, null, this);
            return true;
        }

        public void Dispose()
        {
            Volatile.Write(ref _disposed, 1);
            TryRelease();
        }
    }
}
