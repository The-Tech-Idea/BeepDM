namespace TheTechIdea.Beep.Distributed.Security
{
    /// <summary>
    /// Optional per-entity access policy evaluated by the
    /// distribution tier before any executor call. Implementations
    /// return <c>true</c> to allow, <c>false</c> to deny; denied
    /// calls surface as <see cref="DistributedSecurityException"/>
    /// at the boundary so no shard is ever touched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keep implementations allocation-light: the policy is
    /// consulted on every read/write/DDL hop. Anything more
    /// expensive than a dictionary lookup should be cached by the
    /// implementation itself.
    /// </para>
    /// <para>
    /// <paramref name="principal"/> is the caller identity when
    /// available, otherwise an empty string. Callers may layer a
    /// richer authentication context on top by supplying a
    /// policy that reads from <c>Thread.CurrentPrincipal</c> or a
    /// scoped accessor.
    /// </para>
    /// </remarks>
    public interface IDistributedAccessPolicy
    {
        /// <summary>Evaluates one access check.</summary>
        bool IsAllowed(
            string                 entityName,
            DistributedAccessKind  accessKind,
            string                 principal);
    }

    /// <summary>
    /// Default no-op policy that allows every call. Use as the
    /// baseline when security is enforced at a higher layer.
    /// </summary>
    public sealed class AllowAllAccessPolicy : IDistributedAccessPolicy
    {
        /// <summary>Shared singleton.</summary>
        public static readonly AllowAllAccessPolicy Instance = new AllowAllAccessPolicy();

        private AllowAllAccessPolicy() { }

        /// <inheritdoc/>
        public bool IsAllowed(string entityName, DistributedAccessKind accessKind, string principal) => true;
    }
}
