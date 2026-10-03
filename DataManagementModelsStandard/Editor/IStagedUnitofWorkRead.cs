using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional read capability. Preparation does not replace live records.</summary>
    public interface IStagedUnitofWorkRead
    {
        bool SupportsStagedRead { get; }
        Task<IUnitofWorkReadStage> PrepareReadAsync(List<AppFilter> filters,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Owns an unpublished candidate and read admission until disposed. No live collection is exposed.
    /// Publication is single-use; rejection/disposal before publication preserves the prior records.
    /// </summary>
    public interface IUnitofWorkReadStage : IDisposable
    {
        bool IsPublished { get; }
        IReadOnlyList<Exception> NotificationFailures { get; }
        /// <summary>
        /// The authorizer must synchronously invoke the supplied owned-memory action exactly once on its calling thread,
        /// or throw without invoking it. It may gate that action on policy/registration identity.
        /// Do not retain the action or run observers/providers inside its publication monitors.
        /// After publication, observer failures are reported separately, never as an unpublished read.
        /// </summary>
        void Publish(Action<Action> authorizePublication);
    }
}
