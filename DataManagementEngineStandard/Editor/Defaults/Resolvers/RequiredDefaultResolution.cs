using System;
using System.Threading;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    // Scoped to synchronous required resolution, not a process-wide diagnostic switch.
    internal sealed class RequiredDefaultResolution : IDisposable
    {
        private static readonly AsyncLocal<RequiredDefaultResolution> Slot = new();
        private readonly RequiredDefaultResolution _previous;
        internal static RequiredDefaultResolution Current => Slot.Value;
        internal CancellationToken Token { get; }
        internal RequiredResolverRegistry Registry { get; }
        internal bool Failed { get; private set; }
        internal int Depth { get; }

        internal RequiredDefaultResolution(CancellationToken token, RequiredResolverRegistry registry)
        {
            _previous = Slot.Value;
            Token = token;
            Registry = registry;
            Depth = (_previous?.Depth ?? 0) + 1;
            Slot.Value = this;
        }

        internal static bool Report(bool failure)
        {
            if (Current == null) return false;
            if (failure) Current.Failed = true;
            return true;
        }

        internal static bool HasValidEnvelope(string expression)
        {
            int depth = 0;
            char quote = '\0';
            bool closedCall = false;
            foreach (var ch in expression)
            {
                if (quote != '\0')
                {
                    if (ch == quote) quote = '\0';
                    continue;
                }
                if (closedCall && !char.IsWhiteSpace(ch)) return false;
                if (ch is '\'' or '"') { quote = ch; continue; }
                if (ch == '(') { if (++depth > 32) return false; }
                else if (ch == ')')
                {
                    if (--depth < 0) return false;
                    if (depth == 0) closedCall = true;
                }
            }
            return depth == 0 && quote == '\0';
        }

        public void Dispose()
        {
            if (Failed && _previous != null) _previous.Failed = true;
            Slot.Value = _previous;
        }
    }
}
