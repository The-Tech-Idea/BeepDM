namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// Operator gate for GDPR-style purge calls. The purge service
    /// requires an <see cref="IPurgePolicy"/> implementation to authorize
    /// every removal so a misconfigured caller cannot accidentally erase
    /// audit history.
    /// </summary>
    /// <remarks>
    /// The default <see cref="ConfirmTokenPurgePolicy"/> matches the
    /// supplied confirmation token against the value configured on
    /// <c>BeepAuditOptions.PurgeConfirmationToken</c>. Hosts that need
    /// stricter governance (Active Directory group, signed Azure AD
    /// access token, etc.) implement their own policy and inject it into
    /// the DI container.
    /// </remarks>
    public interface IPurgePolicy
    {
        /// <summary>
        /// Returns <c>true</c> when the supplied
        /// <paramref name="confirmationToken"/> authorizes the purge.
        /// </summary>
        bool Authorize(string confirmationToken);

        /// <summary>
        /// Identifier of the operator persisted in the synthetic
        /// <c>Purge</c> audit event so the purge itself is auditable.
        /// </summary>
        string OperatorId { get; }
    }
}
