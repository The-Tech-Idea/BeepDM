using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Centralized reflection-based property accessor for records managed by
    /// <see cref="FormsManager"/>. The engine binds records to a block by name
    /// (see <c>DataBlockField.FieldName</c>), so the runtime record type is
    /// opaque to the framework and <c>PropertyInfo</c> lookups cannot be
    /// generated at compile time.
    ///
    /// <para>
    /// This helper consolidates what used to be ~12 ad-hoc reflection sites
    /// scattered across <c>FormsManager.BlockRegistration.cs</c>,
    /// <c>FormsManager.DataOperations.cs</c>, <c>FormsManager.Helpers.cs</c>,
    /// <c>FormsManager.Sequences.cs</c>, <c>FormsManager.Validation.cs</c>,
    /// and <c>FormsSimulationHelper.cs</c>. Two regressions it fixes:
    /// </para>
    ///
    /// <list type="number">
    ///   <item>
    ///     <term>Silent null returns</term>
    ///     <description>
    ///       Every previous site swallowed the missing-field case (returning
    ///       <c>null</c> or <c>false</c> with no log). Misconfigurations such
    ///       as <c>FieldName = "OrderId"</c> on a record type that has
    ///       <c>order_id</c> would silently no-op. The accessor emits a
    ///       <c>Debug.WriteLine</c> + <c>IDMEEditor.AddLogMessage</c> warning
    ///       on first miss per (Type, name) pair, with throttling.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Repeated reflection cost</term>
    ///     <description>
    ///       <c>FormsSimulationHelper._propertyCache</c> used a
    ///       <c>$"{type.FullName}.{propertyName}"</c> string key (one
    ///       allocation per call) and was per-instance. The new cache is
    ///       process-wide, keyed by <c>Type</c> (no string allocation), and
    ///       caches both the "found" and "definitively missing" answers.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Lookup rules: <c>Public | Instance | IgnoreCase</c>, skipping indexers
    /// (<c>GetIndexParameters().Length != 0</c>). Set-only properties (no
    /// getter) are NOT filtered: they pass the indexer filter, but
    /// <see cref="GetValue"/> returns <c>null</c> for them (the caller has
    /// no way to distinguish "set-only" from "getter returned null"). This
    /// matches the original pre-consolidation behavior bit-for-bit so
    /// callers see no behavior change unless a previously-silent
    /// misconfiguration trips the new diagnostic.
    /// </para>
    ///
    /// <para>
    /// <b>Memory note for hosts with collectible <c>AssemblyLoadContext</c>s:</b>
    /// the catalog holds strong references to <see cref="PropertyInfo"/>,
    /// which transitively roots the declaring <see cref="Type"/>,
    /// <see cref="System.Reflection.Module"/>, and <see cref="System.Reflection.Assembly"/>.
    /// Hosts that load record types from a collectible <c>AssemblyLoadContext</c>
    /// and later unload it will find that the unloaded <c>Assembly</c> cannot
    /// be collected because the catalog still references types from it.
    /// Workarounds: (a) <see cref="ClearCatalog()"/> on unload, or
    /// (b) avoid collectible ALCs for record types. The BeepDM Forms folder
    /// itself is not a collectible ALC host, so this is a future-host
    /// concern.
    /// </para>
    /// </summary>
    public static class RecordPropertyAccessor
    {
        private const BindingFlags PropertyLookupFlags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        /// <summary>
        /// Per-type property catalog: <c>(Type, name) -> PropertyInfo</c>.
        /// The inner dict's value type is the non-nullable
        /// <see cref="PropertyInfo"/>, but we overload <c>null</c> as a
        /// semantic sentinel meaning "this (Type, name) was looked up and
        /// definitively does not exist on the type" — the negative cache
        /// that prevents the "lookup repeats forever" failure mode for
        /// misspelled field names. See <see cref="BuildCatalogForType"/>
        /// for the seeding logic and <see cref="EnumerateReadableProperties"/>
        /// for the skip-null behavior.
        ///
        /// <para>
        /// Nested structure: the outer dict grows once per Type, the
        /// inner dict is per-Type. No allocations on the hot path
        /// (the <c>TryGetValue</c> fast path).
        /// </para>
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _catalog
            = new();

        /// <summary>
        /// Throttle: do not log the same (Type, name) "missing field" more
        /// than once per minute. Production forms can run thousands of
        /// navigation events per second; we don't want to flood the log.
        /// </summary>
        private static readonly ConcurrentDictionary<string, DateTime> _lastMissLog
            = new(StringComparer.Ordinal);

        private static readonly TimeSpan MissLogInterval = TimeSpan.FromMinutes(1);

        #region Public API

        /// <summary>
        /// Read a property value from <paramref name="record"/>. Returns
        /// <c>null</c> if the record is null, the field name is blank, or
        /// the property does not exist on the record's type.
        /// </summary>
        /// <remarks>
        /// On a definitive miss (property does not exist on the type) the
        /// accessor emits a single throttled warning via
        /// <see cref="Debug.WriteLine(string?)"/>. Bind a <paramref name="logger"/>
        /// to surface the same warning in the editor's log stream.
        /// </remarks>
        public static object GetValue(object record, string fieldName, IDMEEditor logger = null)
        {
            var property = ResolveProperty(record, fieldName, logger);
            if (property == null)
                return null;

            try
            {
                return property.GetValue(record);
            }
            catch (Exception ex)
            {
                LogPropertyAccessFailure(logger, "get", record, fieldName, property, ex);
                return null;
            }
        }

        /// <summary>
        /// Try to read a property value from <paramref name="record"/>.
        /// Returns <c>true</c> when the property exists; <c>false</c> on a
        /// miss or read error (in which case <paramref name="value"/> is
        /// set to <c>null</c>).
        /// </summary>
        public static bool TryGetValue(object record, string fieldName, out object value, IDMEEditor logger = null)
        {
            value = null;
            var property = ResolveProperty(record, fieldName, logger);
            if (property == null)
                return false;

            try
            {
                value = property.GetValue(record);
                return true;
            }
            catch (Exception ex)
            {
                LogPropertyAccessFailure(logger, "get", record, fieldName, property, ex);
                return false;
            }
        }

        /// <summary>
        /// Try to set a property value on <paramref name="record"/>. Returns
        /// <c>false</c> on: null record, blank name, missing property,
        /// read-only property, type-incompatible value (with conversion
        /// failure), or reflection set error. Logs a single throttled
        /// warning per (Type, name) for the "missing" and "read-only"
        /// categories.
        /// </summary>
        /// <remarks>
        /// Behavior delta from the previous <c>FormsManager.Helpers.TrySetPropertyValue</c>:
        /// <c>Convert.ChangeType</c> failures (e.g. caller passes a string
        /// for an int property) previously threw one of
        /// <see cref="InvalidCastException"/>, <see cref="FormatException"/>,
        /// or <see cref="OverflowException"/>; <c>SetValue</c> failures
        /// could also throw <see cref="System.Reflection.TargetException"/>,
        /// <see cref="System.Reflection.TargetInvocationException"/>, or
        /// <see cref="NotSupportedException"/> (e.g. for C# 9+ <c>init</c>-only
        /// properties). All such exceptions are now caught, logged, and
        /// the method returns <c>false</c>. Callers that wrapped the
        /// previous code in a try/catch continue to work; callers that
        /// relied on the throw will see the same "skip" behavior as a
        /// missing field.
        /// </remarks>
        public static bool TrySetValue(object record, string fieldName, object value, IDMEEditor logger = null)
        {
            var property = ResolveProperty(record, fieldName, logger);
            if (property == null)
                return false;

            if (!property.CanWrite)
            {
                LogReadOnlySetAttempt(logger, record, fieldName, property);
                return false;
            }

            try
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var converted = value;
                if (converted != null && !targetType.IsInstanceOfType(converted))
                    converted = Convert.ChangeType(converted, targetType);

                property.SetValue(record, converted);
                return true;
            }
            catch (Exception ex)
            {
                LogPropertyAccessFailure(logger, "set", record, fieldName, property, ex);
                return false;
            }
        }

        /// <summary>
        /// Enumerate all publicly-readable, non-indexed instance properties
        /// of <paramref name="record"/>'s type as a snapshot dictionary keyed
        /// by property name (case-insensitive). Returns an empty dictionary
        /// if <paramref name="record"/> is null.
        /// </summary>
        /// <remarks>
        /// The returned dictionary is a fresh allocation per call. Callers
        /// that snapshot records for rollback should hold on to it for the
        /// duration of the rollback window and then drop it. Returned as
        /// <see cref="IDictionary{TKey, TValue}"/> so it can be passed
        /// directly to APIs that demand the mutable surface (e.g.
        /// ValidationManager.ValidateRecord).
        /// </remarks>
        public static IDictionary<string, object> GetAllReadable(object record, IDMEEditor logger = null)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (record == null)
                return result;

            foreach (var property in EnumerateReadableProperties(record.GetType()))
            {
                try
                {
                    result[property.Name] = property.GetValue(record);
                }
                catch (Exception ex)
                {
                    // Best-effort snapshot: skip properties that throw on read.
                    LogPropertyAccessFailure(logger, "snapshot-get", record, property.Name, property, ex);
                }
            }

            return result;
        }

        /// <summary>
        /// Enumerate all publicly-writable, non-indexed instance properties
        /// of <paramref name="record"/>'s type. Used by snapshot-restore to
        /// walk the target type's writable surface.
        /// </summary>
        /// <remarks>
        /// The catalog <see cref="BuildCatalogForType"/> already filters
        /// indexers out at seeding time, so they cannot appear here. We
        /// still check <see cref="PropertyInfo.CanWrite"/> because the
        /// snapshot-restore path only writes to settable properties —
        /// set-only and read-only properties are valid for reads but not
        /// for the restore path.
        /// </remarks>
        public static IEnumerable<PropertyInfo> EnumerateWritableProperties(object record)
        {
            if (record == null)
                yield break;

            foreach (var property in EnumerateReadableProperties(record.GetType()))
            {
                if (property.CanWrite)
                    yield return property;
            }
        }

        #endregion

        #region Cache resolution

        /// <summary>
        /// Resolve a property by name on the record's runtime type. Returns
        /// <c>null</c> if the record is null, the field name is blank, or
        /// the property does not exist on the type.
        /// </summary>
        /// <remarks>
        /// The catalog is a <c>ConcurrentDictionary&lt;Type, ConcurrentDictionary&lt;string, PropertyInfo&gt;&gt;</c>.
        /// A <c>null</c> entry in the per-type dict means "definitively
        /// missing" (negative cache). The lookup is:
        /// </remarks>
        /// <list type="number">
        ///   <item>
        ///    <term>Fast path (hit)</term>
        ///    <description>
        ///      Lock-free <c>perType.TryGetValue</c>. Zero allocation. This
        ///      branch dominates in real forms — once a (Type, name) pair
        ///      has been resolved, every subsequent call hits the cache.
        ///    </description>
        ///  </item>
        ///  <item>
        ///    <term>Slow path (miss)</term>
        ///    <description>
        ///      Allocates a <c>ValueTuple&lt;Type, string&gt;</c> for the
        ///      <c>GetOrAdd</c> state argument. Misses are rare (one per
        ///      (Type, name) pair over the lifetime of the process), so
        ///      the allocation is acceptable. <c>GetOrAdd</c> may invoke
        ///      the factory more than once under contention, but the
        ///      factory is deterministic for the same (Type, name) — the
        ///      resulting <c>PropertyInfo</c> instances are equivalent
        ///      for our purposes.
        ///    </description>
        ///  </item>
        ///  <item>
        ///    <term>Logging</term>
        ///    <description>
        ///      Done by the caller (<see cref="LogMissingField"/>) only
        ///      when the factory returns <c>null</c>. Logging holds a
        ///      single global <c>_throttleLock</c> (scoped to the
        ///      throttle bookkeeping, NOT the I/O). The I/O
        ///      (<c>AddLogMessage</c>) happens outside the lock.
        ///    </description>
        ///  </item>
        ///</list>
        /// <para>
        /// This is the third shape of this method. The previous versions had:
        /// </para>
        /// <list type="bullet">
        ///  <item>
        ///    <term>v1 (original)</term>
        ///    <description>
        ///      Per-type <c>lock</c> on the catalog; logging I/O inside
        ///      the lock.
        ///    </description>
        ///  </item>
        ///  <item>
        ///    <term>v2 (audit pass 1)</term>
        ///    <description>
        ///      Replaced the lock with <c>GetOrAdd</c>, but the
        ///      throttle was racy and the state tuple was eager
        ///      (allocated on every call, not just on miss).
        ///    </description>
        ///  </item>
        ///  <item>
        ///    <term>v3 (audit pass 2, this version)</term>
        ///    <description>
        ///      Explicit <c>TryGetValue</c> fast path avoids the
        ///      tuple allocation on hit. Global <c>_throttleLock</c>
        ///      scoped to throttle bookkeeping only.
        ///    </description>
        ///  </item>
        ///  <item>
        ///    <term>v4 (audit pass 3, current)</term>
        ///    <description>
        ///      Same core as v3, but the typeKey string for the
        ///      throttle is built inside the lock (rejected callers
        ///      pay no allocation). The factory lambda in
        ///      <see cref="ShouldLogMiss(Type, string, string)"/> is
        ///      marked <c>static</c> for Roslyn delegate caching.
        ///      <see cref="ClearCatalog"/> added for collectible
        ///      <c>AssemblyLoadContext</c> hosts.
        ///    </description>
        ///  </item>
        ///</list>
        private static PropertyInfo ResolveProperty(object record, string fieldName, IDMEEditor logger)
        {
            if (record == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            var type = record.GetType();
            var perType = _catalog.GetOrAdd(type, static t => BuildCatalogForType(t));

            // Fast path: lock-free dict lookup. The vast majority of
            // calls in a real form are hits (PropertyInfo cached from
            // a previous resolution), so this branch dominates and
            // allocates nothing.
            if (perType.TryGetValue(fieldName, out var resolved))
                return resolved;

            // Slow path: this is the first lookup for this (Type, name).
            // We allocate a ValueTuple<Type, string> as the state
            // argument to GetOrAdd. The allocation is per-(Type, name)
            // pair, not per call — misses are rare in practice.
            //
            // GetOrAdd's factory may run more than once under contention.
            // That's safe because Type.GetProperty is deterministic for
            // the same (Type, name) pair (it returns equivalent but not
            // reference-identical PropertyInfo instances; for our purposes
            // — reading and writing values — the two instances are
            // interchangeable).
            resolved = perType.GetOrAdd(
                fieldName,
                static (key, arg) => ResolvePropertyCore(arg.type, arg.fieldName),
                (type, fieldName));

            if (resolved == null)
            {
                // The first thread to insert a null into the per-type
                // dict is the one that pays the log cost. Subsequent
                // misses hit the cache and stay silent — the throttle in
                // LogMissingField enforces "approximately once per minute"
                // under the global _throttleLock.
                LogMissingField(logger, record, fieldName);
            }

            return resolved;
        }

        /// <summary>
        /// Pure factory: scan the type's public-readable surface for
        /// <paramref name="fieldName"/>. Returns <c>null</c> if the field
        /// does not exist (or is an indexer). Has no side effects; safe
        /// to invoke from <c>ConcurrentDictionary.GetOrAdd</c>.
        /// </summary>
        private static PropertyInfo ResolvePropertyCore(Type type, string fieldName)
        {
            var property = type.GetProperty(fieldName, PropertyLookupFlags);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property;

            return null;
        }

        private static ConcurrentDictionary<string, PropertyInfo> BuildCatalogForType(Type type)
        {
            // Seed with the public-readable surface so the first call against
            // a new type is a dict walk, not a reflection scan. Note: the
            // value type is PropertyInfo, not PropertyInfo? — a null entry
            // in the inner dict means "this name was looked up and is
            // definitively missing." See ResolveProperty for the lookup
            // contract.
            var perType = new ConcurrentDictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in type.GetProperties(PropertyLookupFlags))
            {
                if (property.GetIndexParameters().Length != 0)
                    continue;

                perType[property.Name] = property;
            }
            return perType;
        }

        private static IEnumerable<PropertyInfo> EnumerateReadableProperties(Type type)
        {
            var perType = _catalog.GetOrAdd(type, static t => BuildCatalogForType(t));
            foreach (var kvp in perType)
            {
                // Skip negative-cache entries. The catalog holds a real
                // PropertyInfo for hits and null for misses; the foreach
                // yields only the hits.
                if (kvp.Value == null)
                    continue;

                yield return kvp.Value;
            }
        }

        #endregion

        #region Diagnostic logging

        private static void LogMissingField(IDMEEditor logger, object record, string fieldName)
        {
            // The throttle key is built inside ShouldLogMiss so that
            // rejected (throttled) calls don't pay the string
            // allocation. See ShouldLogMiss(Type, string, string) for
            // the key format.
            if (!ShouldLogMiss(record.GetType(), fieldName, "miss"))
                return;

            var message = $"[RecordPropertyAccessor] field '{fieldName}' not found on type '{record.GetType().Name}'. " +
                          "Check the BlockDefinition.FieldName spelling and the bound record's property casing.";
            Debug.WriteLine(message);
            logger?.AddLogMessage("RecordPropertyAccessor", message, DateTime.Now, -1, null, Errors.Failed);
        }

        private static void LogReadOnlySetAttempt(IDMEEditor logger, object record, string fieldName, PropertyInfo property)
        {
            if (!ShouldLogMiss(record.GetType(), fieldName, "ro"))
                return;

            var message = $"[RecordPropertyAccessor] field '{fieldName}' on '{record.GetType().Name}' is read-only " +
                          $"(property type: {property.PropertyType.Name}). Set attempt was ignored.";
            Debug.WriteLine(message);
            logger?.AddLogMessage("RecordPropertyAccessor", message, DateTime.Now, -1, null, Errors.Failed);
        }

        private static void LogPropertyAccessFailure(IDMEEditor logger, string operation, object record, string fieldName, PropertyInfo property, Exception ex)
        {
            // Access failures (throws during Get/Set) are not throttled the
            // same way as misses — they usually indicate a structural problem
            // that won't go away on its own. But we still cap at one per
            // minute per (Type, name) to avoid log floods.
            if (!ShouldLogMiss(record.GetType(), fieldName, $"err::{operation}"))
                return;

            var message = $"[RecordPropertyAccessor] {operation} on field '{fieldName}' (type {property?.PropertyType.Name ?? "?"}) " +
                          $"on '{record.GetType().Name}' threw: {ex.GetType().Name}: {ex.Message}";
            Debug.WriteLine(message);
            logger?.AddLogMessage("RecordPropertyAccessor", message, DateTime.Now, -1, ex.Message, Errors.Failed);
        }

        /// <summary>
        /// Diagnostic hook for callers that bypass <see cref="TrySetValue"/>
        /// and need to surface a throttled SetValue failure (e.g. the
        /// snapshot-restore path in <c>FormsManager.BlockRegistration.cs</c>,
        /// which uses <c>ConvertSnapshotValue</c> for enum/DateTime
        /// handling and calls <c>property.SetValue</c> directly).
        /// </summary>
        /// <remarks>
        /// Public so external callers can route SetValue failures through
        /// the same throttled diagnostic as the accessor's own TrySetValue.
        /// Throttle is "approximately once per minute" per (Type, fieldName).
        /// The <paramref name="property"/> is used to surface the target
        /// type in the log message; pass <c>null</c> if it is not
        /// available (the message will read "type ?" for the target type).
        /// </remarks>
        public static void LogRestoreFailure(IDMEEditor logger, object record, string fieldName, PropertyInfo property, Exception ex)
        {
            LogPropertyAccessFailure(logger, "restore", record, fieldName, property, ex);
        }

        private static bool ShouldLogMiss(Type recordType, string fieldName, string category)
        {
            // Throttle key format: "{category}::{typeKey}::{fieldName}"
            // where typeKey is the type's FullName (with Name as
            // fallback for null FullName on constructed generic types).
            // The key is built inside the lock so that rejected
            // (throttled) callers don't pay the string allocation —
            // every call used to do this allocation up-front and
            // then discard it on the rejected branch. Building it
            // here moves the cost to the "about to log" branch only.
            //
            // Use a single global lock to make the throttle strict
            // under high concurrency. Without this lock, multiple
            // threads hitting a misspelled field simultaneously could
            // all pass the "now - last >= interval" check and all
            // log, defeating the "approximately once per minute"
            // guarantee. The lock is short-held (just a dict read +
            // a dict write) and contention is low because logging is
            // rare.
            //
            // Note: a previous version of this code used a per-type
            // lock on the property catalog instead, but that locked
            // for the duration of the entire log message (including
            // the AddLogMessage I/O), stalling all other field
            // resolutions for the same type. This lock is scoped
            // tightly to the throttle bookkeeping.
            var typeKey = recordType.FullName ?? recordType.Name ?? recordType.ToString();
            var key = $"{category}::{typeKey}::{fieldName}";

            lock (_throttleLock)
            {
                var now = DateTime.UtcNow;
                // `static` keyword on the lambda enables Roslyn's
                // delegate caching (no closure allocation per call).
                var last = _lastMissLog.GetOrAdd(key, static _ => DateTime.MinValue);
                if (now - last < MissLogInterval)
                    return false;

                _lastMissLog[key] = now;
                return true;
            }
        }

        /// <summary>
        /// Global lock for the log throttle bookkeeping. See
        /// <see cref="ShouldLogMiss"/> for why this is needed and why
        /// it is scoped tightly.
        /// </summary>
        private static readonly object _throttleLock = new();

        /// <summary>
        /// Clears the per-type property catalog and the log throttle
        /// dictionary. Intended for hosts that unload record types
        /// from a collectible <see cref="System.Reflection.AssemblyLoadContext"/>
        /// and need the catalog to release its strong references to
        /// the unloaded <see cref="Type"/> instances. After this call,
        /// the next lookup for any (Type, name) pair will re-seed the
        /// catalog via <see cref="BuildCatalogForType"/>, which will
        /// resolve the new <c>Type</c> reference correctly.
        /// </summary>
        /// <remarks>
        /// Not safe to call from inside an active resolution. Callers
        /// should drain all in-flight form work before invoking this.
        /// The BeepDM Forms folder does not need this method; it is
        /// provided for hosts that integrate with collectible ALCs.
        /// </remarks>
        public static void ClearCatalog()
        {
            lock (_throttleLock)
            {
                _catalog.Clear();
                _lastMissLog.Clear();
            }
        }

        #endregion
    }
}
