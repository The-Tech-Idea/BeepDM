using System;

namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// Default <see cref="IPurgePolicy"/>. Authorizes the purge when the
    /// caller supplies the same confirmation token that the operator
    /// configured on <c>BeepAuditOptions.PurgeConfirmationToken</c>.
    /// </summary>
    /// <remarks>
    /// The check uses an ordinal comparison; tokens are intended to be
    /// long random strings managed via the same secret store as the
    /// HMAC chain key. When no token is configured the policy denies
    /// every call so an oblivious caller cannot purge by accident.
    /// </remarks>
    public sealed class ConfirmTokenPurgePolicy : IPurgePolicy
    {
        private readonly string _expectedToken;
        private readonly string _operatorId;

        /// <summary>Creates a policy keyed by <paramref name="expectedToken"/>.</summary>
        public ConfirmTokenPurgePolicy(string expectedToken, string operatorId = null)
        {
            _expectedToken = expectedToken;
            _operatorId = string.IsNullOrEmpty(operatorId) ? "purge-operator" : operatorId;
        }

        /// <inheritdoc />
        public string OperatorId => _operatorId;

        /// <inheritdoc />
        public bool Authorize(string confirmationToken)
        {
            if (string.IsNullOrEmpty(_expectedToken))
            {
                return false;
            }
            if (string.IsNullOrEmpty(confirmationToken))
            {
                return false;
            }
            return string.Equals(_expectedToken, confirmationToken, StringComparison.Ordinal);
        }
    }
}
