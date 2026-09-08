using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T>
    {
        /// <summary>
        /// Optional tenant boundary. When set, <b>every read this unit of work performs carries the
        /// discriminator in its filter, and every insert is stamped with it.</b>
        ///
        /// <para><b>Why it belongs here and not in the caller.</b> A multi-tenant application that
        /// keeps every tenant's rows in one table has to add that predicate to every single query.
        /// Leaving it to each call site means it is applied wherever someone remembered — which, in
        /// the application this was built for, measured as roughly two thirds of reads, with the
        /// remainder silently returning every tenant's data. Odoo solves the same problem the same
        /// way: its ORM appends the company domain below where feature code runs, so a developer
        /// cannot forget it. This is that hook.</para>
        ///
        /// <para>It is deliberately expressed as a field name and a value rather than a typed
        /// interface, so this layer needs no knowledge of the consuming application's entity base
        /// class.</para>
        /// </summary>
        public string? TenantFieldName { get; private set; }

        /// <summary>The tenant value applied to reads and stamped onto inserts. See <see cref="TenantFieldName"/>.</summary>
        public string? TenantId { get; private set; }

        /// <summary>True when this unit of work is confined to a single tenant.</summary>
        public bool IsTenantScoped => !string.IsNullOrWhiteSpace(TenantFieldName) && TenantId is not null;

        /// <summary>
        /// Whether the tenant predicate is added to reads, as opposed to only being stamped onto
        /// inserts.
        ///
        /// <para><b>Why the two can be separated.</b> Adopting the boundary in an application that has
        /// been running without one is a sequence, not a switch: rows written before the stamp existed
        /// carry no tenant, so turning filtering on first makes that history *disappear* rather than
        /// become correct. Stamping first is safe and immediate — it only affects new rows — and
        /// filtering follows once the old rows have been backfilled. This flag is what lets a caller
        /// take those two steps separately, per table, instead of all at once.</para>
        /// </summary>
        public bool TenantFiltersReads { get; private set; } = true;

        // Property lookup is per closed generic type and never changes, so it is resolved once.
        private static readonly Dictionary<string, PropertyInfo?> _tenantProperties = new();
        private static readonly object _tenantPropertyLock = new();

        /// <summary>
        /// Confines this unit of work to one tenant: reads gain <paramref name="fieldName"/> =
        /// <paramref name="tenantId"/>, and inserts are stamped with it.
        ///
        /// <para>An empty <paramref name="tenantId"/> is rejected rather than treated as "all
        /// tenants". A caller that does not know its tenant must not silently read across every one
        /// of them — that is the failure this exists to prevent, and it should surface where the
        /// scope was requested.</para>
        /// </summary>
        public UnitofWork<T> ScopeToTenant(string fieldName, string tenantId, bool filterReads = true)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("A tenant field name is required.", nameof(fieldName));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException(
                    "A tenant id is required. Scoping to an empty tenant would read and write across every tenant.",
                    nameof(tenantId));

            TenantFieldName = fieldName;
            TenantId = tenantId;
            TenantFiltersReads = filterReads;
            return this;
        }

        /// <summary>
        /// Returns the caller's filters with the tenant predicate added, or the filters unchanged when
        /// this unit of work is not scoped.
        ///
        /// <para>A filter the caller already supplied on the same field is left alone: it is either the
        /// same value, or a deliberate attempt to read another tenant that must fail loudly at the
        /// database rather than be silently rewritten into something that succeeds.</para>
        /// </summary>
        internal List<AppFilter> ApplyTenantFilter(List<AppFilter>? filters)
        {
            var result = filters is null ? new List<AppFilter>() : new List<AppFilter>(filters);
            if (!IsTenantScoped || !TenantFiltersReads) return result;

            var alreadyFiltered = result.Any(f =>
                string.Equals(f.FieldName, TenantFieldName, StringComparison.OrdinalIgnoreCase));
            if (alreadyFiltered) return result;

            result.Add(new AppFilter
            {
                FieldName = TenantFieldName!,
                Operator = "=",
                FilterValue = TenantId!
            });
            return result;
        }

        /// <summary>
        /// Stamps the tenant onto an entity being inserted, when this unit of work is scoped and the
        /// entity does not already carry one.
        ///
        /// <para>An existing non-empty value is never overwritten — an insert that deliberately names
        /// its tenant (a platform process acting for a specific one) stays as written. A blank one is
        /// filled, because a row saved with no tenant belongs to nobody: it is invisible to every
        /// scoped read and visible to every unscoped one, which is precisely how a missing filter and
        /// a missing stamp hide each other.</para>
        /// </summary>
        internal void StampTenant(T entity)
        {
            if (!IsTenantScoped || entity is null) return;

            var property = ResolveTenantProperty();
            if (property is null) return;

            var current = property.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(current)) return;

            property.SetValue(entity, TenantId);
        }

        /// <summary>
        /// True when this entity belongs to a different tenant than the one this unit of work is
        /// confined to — i.e. modifying or deleting it would reach across the boundary.
        ///
        /// <para>Filtering reads is only half of isolation. A caller that already holds an entity —
        /// fetched before the scope was applied, handed in by an API request, or carried over from
        /// another operation — can otherwise update or delete another tenant's row through a scoped
        /// unit of work, because nothing on the write path looks at the discriminator. Reading is the
        /// half people remember; this is the half that changes someone else's data.</para>
        ///
        /// <para>A blank tenant on the entity is <b>not</b> treated as foreign: those are rows written
        /// before stamping existed, and refusing to update them would make historical data
        /// uneditable rather than safe. They are the backfill's problem, not this check's.</para>
        /// </summary>
        internal bool BelongsToAnotherTenant(T entity)
        {
            if (!IsTenantScoped || entity is null) return false;

            var property = ResolveTenantProperty();
            if (property is null) return false;

            var value = property.GetValue(entity) as string;
            if (string.IsNullOrWhiteSpace(value)) return false;

            return !string.Equals(value, TenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The refusal message used when a write is rejected for crossing the boundary.</summary>
        internal string CrossTenantRefusal(string operation) =>
            $"{operation} was refused: the {typeof(T).Name} belongs to a different {TenantFieldName} " +
            $"than the one this operation is scoped to.";

        private PropertyInfo? ResolveTenantProperty()
        {
            var key = typeof(T).FullName + "|" + TenantFieldName;
            lock (_tenantPropertyLock)
            {
                if (_tenantProperties.TryGetValue(key, out var cached)) return cached;

                var property = typeof(T).GetProperty(
                    TenantFieldName!,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                // Only a writable string carries a tenant id. Anything else is a different field that
                // happens to share the name, and writing to it would corrupt data.
                if (property is not null && (property.PropertyType != typeof(string) || !property.CanWrite))
                    property = null;

                _tenantProperties[key] = property;
                return property;
            }
        }
    }
}
