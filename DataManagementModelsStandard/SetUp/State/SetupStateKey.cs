using System;
using System.Text;

namespace TheTechIdea.Beep.SetUp.State
{
    /// <summary>
    /// Identity of a stored <see cref="SetupState"/> record.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WizardId"/> is part of the identity on purpose: the old store keyed only off a
    /// bare file path, so two wizards pointed at the same <c>StateFilePath</c> silently overwrote
    /// each other's checkpoints. <see cref="AppId"/> is the hook Phase 7 needs to run setup per app
    /// without reshaping the store.
    /// </para>
    /// </remarks>
    public sealed class SetupStateKey : IEquatable<SetupStateKey>
    {
        public SetupStateKey(string wizardId, string environment = null, string appId = null)
        {
            if (string.IsNullOrWhiteSpace(wizardId))
                throw new ArgumentException("wizardId is required.", nameof(wizardId));

            WizardId = wizardId;
            Environment = string.IsNullOrWhiteSpace(environment) ? "Development" : environment;
            AppId = string.IsNullOrWhiteSpace(appId) ? null : appId;
        }

        public string WizardId { get; }
        public string Environment { get; }

        /// <summary>Optional application scope (Phase 7); null for a single-app setup.</summary>
        public string AppId { get; }

        /// <summary>
        /// Stable, filesystem- and url-safe token identifying this key. Used as a local file name
        /// and a remote resource id, so it must not vary across runs for the same logical key.
        /// </summary>
        public string ToToken()
        {
            var sb = new StringBuilder();
            if (AppId != null) { AppendSafe(sb, AppId); sb.Append('.'); }
            AppendSafe(sb, Environment);
            sb.Append('.');
            AppendSafe(sb, WizardId);
            return sb.ToString();
        }

        private static void AppendSafe(StringBuilder sb, string value)
        {
            foreach (var c in value)
                sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-');
        }

        public bool Equals(SetupStateKey other)
            => other != null
               && string.Equals(WizardId, other.WizardId, StringComparison.Ordinal)
               && string.Equals(Environment, other.Environment, StringComparison.Ordinal)
               && string.Equals(AppId, other.AppId, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SetupStateKey);

        public override int GetHashCode()
            => HashCode.Combine(WizardId, Environment, AppId);

        public override string ToString() => ToToken();
    }
}
