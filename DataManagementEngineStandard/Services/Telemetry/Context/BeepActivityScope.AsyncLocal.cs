using System.Collections.Generic;
using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Async-local stack backing for <see cref="BeepActivityScope"/>.
    /// Kept in its own partial so the public <see cref="BeepActivityScope.Begin"/>
    /// surface is not littered with stack-management plumbing.
    /// </summary>
    /// <remarks>
    /// We store a <see cref="Stack{T}"/> reference per async-local slot rather
    /// than pushing/popping a primitive value directly because
    /// <see cref="AsyncLocal{T}"/> only flows the reference itself — mutating
    /// the stack does not trigger <see cref="AsyncLocal{T}.ValueChangedHandler"/>
    /// callbacks but does propagate visibly to peers on the same logical
    /// context. Pop logic tolerates mismatched/out-of-order disposal so a
    /// faulty caller can never corrupt the stack for the rest of the host.
    /// </remarks>
    public static partial class BeepActivityScope
    {
        private static readonly AsyncLocal<Stack<BeepActivity>> Stack = new AsyncLocal<Stack<BeepActivity>>();

        private static void Push(BeepActivity activity)
        {
            Stack<BeepActivity> stack = Stack.Value;
            if (stack is null)
            {
                stack = new Stack<BeepActivity>();
                Stack.Value = stack;
            }
            stack.Push(activity);
        }

        private static BeepActivity Peek()
        {
            Stack<BeepActivity> stack = Stack.Value;
            if (stack is null || stack.Count == 0)
            {
                return null;
            }
            return stack.Peek();
        }

        private static void Pop(BeepActivity expected)
        {
            Stack<BeepActivity> stack = Stack.Value;
            if (stack is null || stack.Count == 0)
            {
                return;
            }
            if (!ReferenceEquals(stack.Peek(), expected))
            {
                // Out-of-order Dispose. Walk the stack and remove the matching
                // frame defensively; this only happens when a caller stores the
                // IDisposable and disposes it after a child scope is opened.
                var buffer = new Stack<BeepActivity>(stack.Count);
                bool removed = false;
                while (stack.Count > 0)
                {
                    BeepActivity frame = stack.Pop();
                    if (!removed && ReferenceEquals(frame, expected))
                    {
                        removed = true;
                        continue;
                    }
                    buffer.Push(frame);
                }
                while (buffer.Count > 0)
                {
                    stack.Push(buffer.Pop());
                }
                return;
            }
            stack.Pop();
        }

        /// <summary>
        /// Test/diagnostic helper that resets the current async-local stack.
        /// Production code never calls this.
        /// </summary>
        internal static void ResetCurrentForTests()
        {
            Stack.Value = null;
        }
    }
}
