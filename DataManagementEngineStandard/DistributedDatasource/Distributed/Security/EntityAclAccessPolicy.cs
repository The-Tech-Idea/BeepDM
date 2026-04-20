using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Security
{
    /// <summary>
    /// Simple per-entity ACL policy. For each entity the caller
    /// registers the set of principals allowed to perform Read,
    /// Write, and DDL operations respectively. A request is
    /// allowed when the principal appears in the matching set,
    /// the wildcard <c>"*"</c> is present, or the caller opted
    /// into a <see cref="DefaultAllow"/> fallback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ACL is thread-safe and supports runtime edits via
    /// <see cref="Allow"/> and <see cref="Revoke"/>; inspection is
    /// lock-free against reads. Unknown entities fall back to
    /// <see cref="DefaultAllow"/> which defaults to <c>false</c>
    /// (deny-by-default) to keep mis-configurations loud during
    /// development.
    /// </para>
    /// </remarks>
    public sealed class EntityAclAccessPolicy : IDistributedAccessPolicy
    {
        private const string Wildcard = "*";

        private readonly ConcurrentDictionary<Key, HashSet<string>> _acl
            = new ConcurrentDictionary<Key, HashSet<string>>();

        /// <summary>Gets or sets the fallback result for unmapped entity/access pairs.</summary>
        public bool DefaultAllow { get; set; }

        /// <summary>Creates a new policy.</summary>
        /// <param name="defaultAllow">
        /// When <c>true</c> unknown entities are allowed; defaults
        /// to <c>false</c>.
        /// </param>
        public EntityAclAccessPolicy(bool defaultAllow = false)
        {
            DefaultAllow = defaultAllow;
        }

        /// <summary>Allows <paramref name="principal"/> to perform <paramref name="accessKind"/> on <paramref name="entityName"/>.</summary>
        public EntityAclAccessPolicy Allow(
            string                 entityName,
            DistributedAccessKind  accessKind,
            string                 principal)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("Entity name required.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(principal))  throw new ArgumentException("Principal required.",  nameof(principal));

            var key = new Key(entityName, accessKind);
            var set = _acl.GetOrAdd(key, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            lock (set) set.Add(principal);
            return this;
        }

        /// <summary>Grants a principal every access kind on an entity.</summary>
        public EntityAclAccessPolicy AllowAll(string entityName, string principal)
        {
            Allow(entityName, DistributedAccessKind.Read,  principal);
            Allow(entityName, DistributedAccessKind.Write, principal);
            Allow(entityName, DistributedAccessKind.Ddl,   principal);
            return this;
        }

        /// <summary>Revokes a principal on one access kind.</summary>
        public EntityAclAccessPolicy Revoke(
            string                 entityName,
            DistributedAccessKind  accessKind,
            string                 principal)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return this;
            if (string.IsNullOrWhiteSpace(principal))  return this;

            var key = new Key(entityName, accessKind);
            if (_acl.TryGetValue(key, out var set))
            {
                lock (set) set.Remove(principal);
            }
            return this;
        }

        /// <inheritdoc/>
        public bool IsAllowed(string entityName, DistributedAccessKind accessKind, string principal)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return DefaultAllow;

            var key = new Key(entityName, accessKind);
            if (!_acl.TryGetValue(key, out var set)) return DefaultAllow;

            lock (set)
            {
                if (set.Contains(Wildcard)) return true;
                if (!string.IsNullOrWhiteSpace(principal) && set.Contains(principal)) return true;
            }
            return false;
        }

        private readonly struct Key : IEquatable<Key>
        {
            public Key(string entityName, DistributedAccessKind kind)
            {
                EntityName = entityName ?? string.Empty;
                Kind       = kind;
            }

            public string                 EntityName { get; }
            public DistributedAccessKind  Kind       { get; }

            public bool Equals(Key other)
                => Kind == other.Kind
                && string.Equals(EntityName, other.EntityName, StringComparison.OrdinalIgnoreCase);

            public override bool Equals(object obj) => obj is Key k && Equals(k);

            public override int GetHashCode()
                => StringComparer.OrdinalIgnoreCase.GetHashCode(EntityName ?? string.Empty) * 17
                 ^ (int)Kind;
        }
    }
}
