using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Pre-built trigger factory patterns ported from WinForms BeepDataBlockTriggerHelper.
    /// All methods work against ITriggerManager — no WinForms dependency.
    /// </summary>
    public static class TriggerLibrary
    {
        // ---------------------------------------------------------------
        // Reflection helpers (same logic as WinForms, now in BeepDM)
        // ---------------------------------------------------------------

        /// <summary>
        /// Reads a field or property value from a record using case-insensitive reflection.
        /// </summary>
        /// <remarks>
        /// Routes through <see cref="RecordPropertyAccessor"/> so the engine-wide typed
        /// <c>PropertyInfo</c> catalog handles the lookup. The previous direct
        /// <c>Type.GetProperty(...)</c> path re-issued a reflection call per read and
        /// bypassed the case-insensitive / throttle / log-diagnostic plumbing the rest
        /// of the engine uses.
        /// </remarks>
        public static object GetFieldValue(object record, string fieldName)
        {
            if (record == null || string.IsNullOrEmpty(fieldName)) return null;
            return RecordPropertyAccessor.TryGetValue(record, fieldName, out var value, logger: null)
                ? value
                : null;
        }

        /// <summary>
        /// Sets a field or property value on a record using case-insensitive reflection.
        /// </summary>
        /// <remarks>
        /// Routes through <see cref="RecordPropertyAccessor.TrySetValue"/>, which handles
        /// the <see cref="Convert.ChangeType(object, Type)"/> call internally and logs a
        /// throttled diagnostic on missing/read-only/type-mismatch. The previous inline
        /// <c>ConvertValue</c> helper (removed) duplicated the conversion logic and
        /// silently swallowed conversion failures.
        /// </remarks>
        public static bool SetFieldValue(object record, string fieldName, object value)
        {
            if (record == null || string.IsNullOrEmpty(fieldName)) return false;
            return RecordPropertyAccessor.TrySetValue(record, fieldName, value, logger: null);
        }

        // ---------------------------------------------------------------
        // Trigger statistics
        // ---------------------------------------------------------------

        /// <summary>
        /// Builds aggregate trigger statistics for a block from the registered block triggers.
        /// </summary>
        public static TriggerStatisticsInfo GetTriggerStatistics(string blockName, ITriggerManager triggers)
        {
            var all = triggers.GetBlockTriggers(blockName);

            return new TriggerStatisticsInfo
            {
                BlockName = blockName,
                TotalTriggers = all.Count,
                EnabledTriggers = all.Count(t => t.IsEnabled),
                DisabledTriggers = all.Count(t => !t.IsEnabled),
                TotalExecutions = all.Sum(t => t.ExecutionCount),
                TotalSuccesses = all.Sum(t => t.SuccessCount),
                TotalFailures = all.Sum(t => t.FailureCount),
                AverageExecutionMs = all.Any() ? all.Average(t => t.AverageExecutionTimeMs) : 0,
                TriggersByType = all.GroupBy(t => t.TriggerType.ToString())
                                    .ToDictionary(g => g.Key, g => g.Count()),
                MostExecutedTriggerName = all.OrderByDescending(t => t.ExecutionCount)
                                            .FirstOrDefault()?.TriggerName
            };
        }

        // ---------------------------------------------------------------
        // Scope filter helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns the form-scope triggers registered for a block.
        /// </summary>
        public static IReadOnlyList<TriggerDefinition> GetFormLevelTriggers(string blockName, ITriggerManager triggers)
            => triggers.GetBlockTriggers(blockName).Where(t => t.Scope == TriggerScope.Form).ToList();

        /// <summary>
        /// Returns the block-scope triggers registered for a block.
        /// </summary>
        public static IReadOnlyList<TriggerDefinition> GetBlockLevelTriggers(string blockName, ITriggerManager triggers)
            => triggers.GetBlockTriggers(blockName).Where(t => t.Scope == TriggerScope.Block).ToList();

        /// <summary>
        /// Returns the record-scope triggers registered for a block.
        /// </summary>
        public static IReadOnlyList<TriggerDefinition> GetRecordLevelTriggers(string blockName, ITriggerManager triggers)
            => triggers.GetBlockTriggers(blockName).Where(t => t.Scope == TriggerScope.Record).ToList();

        /// <summary>
        /// Returns the item-scope triggers registered for a block.
        /// </summary>
        public static IReadOnlyList<TriggerDefinition> GetItemLevelTriggers(string blockName, ITriggerManager triggers)
            => triggers.GetBlockTriggers(blockName).Where(t => t.Scope == TriggerScope.Item).ToList();

        // ---------------------------------------------------------------
        // Common trigger patterns
        // ---------------------------------------------------------------

        /// <summary>
        /// Register audit trail triggers (CreatedBy / CreatedDate on insert;
        /// ModifiedBy / ModifiedDate / Version on update).
        /// </summary>
        public static void RegisterAuditTriggers(
            string blockName,
            ITriggerManager triggers,
            string username = null)
        {
            username ??= Environment.UserName;

            // PRE-INSERT: set created + modified audit fields
            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_AuditInsert",
                TriggerType = TriggerType.PreInsert,
                Scope = TriggerScope.Record,
                BlockName = blockName,
                Priority = TriggerPriority.High,
                AsyncHandler = (ctx, ct) =>
                {
                    var record = ctx.CurrentRecord ?? ctx.NewRecord;
                    if (record != null)
                    {
                        var now = DateTime.UtcNow;
                        SetFieldValue(record, "CreatedDate", now);
                        SetFieldValue(record, "CreatedBy", username);
                        SetFieldValue(record, "ModifiedDate", now);
                        SetFieldValue(record, "ModifiedBy", username);
                    }
                    return Task.FromResult(TriggerResult.Success);
                }
            });

            // PRE-UPDATE: update modified audit fields + increment Version
            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_AuditUpdate",
                TriggerType = TriggerType.PreUpdate,
                Scope = TriggerScope.Record,
                BlockName = blockName,
                Priority = TriggerPriority.High,
                AsyncHandler = (ctx, ct) =>
                {
                    var record = ctx.CurrentRecord;
                    if (record != null)
                    {
                        SetFieldValue(record, "ModifiedDate", DateTime.UtcNow);
                        SetFieldValue(record, "ModifiedBy", username);
                        var ver = GetFieldValue(record, "Version");
                        if (ver is int v)
                            SetFieldValue(record, "Version", v + 1);
                    }
                    return Task.FromResult(TriggerResult.Success);
                }
            });
        }

        /// <summary>
        /// Register a WHEN-NEW-RECORD-INSTANCE trigger that sets default field values.
        /// </summary>
        public static void RegisterDefaultValueTrigger(
            string blockName,
            ITriggerManager triggers,
            Dictionary<string, object> defaultValues)
        {
            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_DefaultValues",
                TriggerType = TriggerType.WhenNewRecordInstance,
                Scope = TriggerScope.Record,
                BlockName = blockName,
                AsyncHandler = (ctx, ct) =>
                {
                    var record = ctx.CurrentRecord ?? ctx.NewRecord;
                    if (record != null)
                    {
                        foreach (var kvp in defaultValues)
                            SetFieldValue(record, kvp.Key, kvp.Value);
                    }
                    return Task.FromResult(TriggerResult.Success);
                }
            });
        }

        /// <summary>
        /// Register a computed field trigger that recalculates resultField
        /// whenever any of the sourceFields changes (WHEN-VALIDATE-ITEM)
        /// and also on POST-QUERY.
        /// </summary>
        public static void RegisterComputedFieldTrigger(
            string blockName,
            ITriggerManager triggers,
            string resultField,
            string[] sourceFields,
            Func<object, object> computation)
        {
            // WHEN-VALIDATE-ITEM: recompute on each source field change
            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_Computed_{resultField}_OnChange",
                TriggerType = TriggerType.WhenValidateItem,
                Scope = TriggerScope.Item,
                BlockName = blockName,
                AsyncHandler = (ctx, ct) =>
                {
                    if (sourceFields.Contains(ctx.ItemName, StringComparer.OrdinalIgnoreCase))
                    {
                        var record = ctx.CurrentRecord;
                        if (record != null)
                            SetFieldValue(record, resultField, computation(record));
                    }
                    return Task.FromResult(TriggerResult.Success);
                }
            });

            // POST-QUERY: recompute on data load
            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_Computed_{resultField}_PostQuery",
                TriggerType = TriggerType.PostQuery,
                Scope = TriggerScope.Block,
                BlockName = blockName,
                AsyncHandler = (ctx, ct) =>
                {
                    var record = ctx.CurrentRecord;
                    if (record != null)
                        SetFieldValue(record, resultField, computation(record));
                    return Task.FromResult(TriggerResult.Success);
                }
            });
        }

        /// <summary>
        /// Register the standard set of CRUD triggers (audit + post-query coordination
        /// + when-validate-record stub) in one call.
        /// </summary>
        public static void RegisterStandardCRUDTriggers(string blockName, ITriggerManager triggers,
            string username = null)
        {
            RegisterAuditTriggers(blockName, triggers, username);

            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_PostQueryCoordinate",
                TriggerType = TriggerType.PostQuery,
                Scope = TriggerScope.Block,
                BlockName = blockName,
                Handler = ctx => TriggerResult.Success
            });

            triggers.RegisterTrigger(new TriggerDefinition
            {
                TriggerName = $"{blockName}_ValidateRecord",
                TriggerType = TriggerType.WhenValidateRecord,
                Scope = TriggerScope.Record,
                BlockName = blockName,
                Handler = ctx => TriggerResult.Success
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // AutoNumber — fires on PreInsert, sets a numeric primary-key field
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="TriggerType.PreInsert"/> trigger that populates
        /// <paramref name="fieldName"/> with the next value from <paramref name="sequenceFn"/>
        /// before a new record is inserted.
        /// </summary>
        public static TriggerDefinition AutoNumberTrigger(
            string blockName,
            string fieldName,
            Func<long> sequenceFn)
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName required", nameof(blockName));
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("fieldName required", nameof(fieldName));
            if (sequenceFn == null) throw new ArgumentNullException(nameof(sequenceFn));

            return new TriggerDefinition
            {
                TriggerType   = TriggerType.PreInsert,
                Scope         = TriggerScope.Block,
                TriggerName   = $"auto_number_{blockName}_{fieldName}",
                BlockName     = blockName,
                LogExecution  = false,
                ContinueOnFailure = false,
                Handler = ctx =>
                {
                    if (ctx?.CurrentRecord == null) return TriggerResult.Success;
                    // Route through RecordPropertyAccessor: it handles the
                    // case-insensitive property lookup, the type conversion via
                    // Convert.ChangeType, the read-only check, and the throttled
                    // diagnostic log. The previous direct GetProperty path was
                    // case-sensitive (BindingFlags.IgnoreCase missing) and bypassed
                    // the shared metadata catalog.
                    var next = sequenceFn();
                    if (!RecordPropertyAccessor.TrySetValue(ctx.CurrentRecord, fieldName, next, logger: null))
                    {
                        ctx.ErrorMessage = $"AutoNumber: field '{fieldName}' is missing or read-only on {ctx.CurrentRecord.GetType().Name}.";
                        return TriggerResult.Failure;
                    }
                    return TriggerResult.Success;
                }
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // AuditStamp — fires on PreInsert and PreUpdate
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates two triggers (PreInsert + PreUpdate) that stamp audit fields on the record.
        /// Returns [0] = insert trigger, [1] = update trigger.
        /// </summary>
        public static TriggerDefinition[] AuditStampTriggers(
            string blockName,
            Func<string> getUserFn,
            string createdByField  = "CreatedBy",
            string createdAtField  = "CreatedAt",
            string modifiedByField = "ModifiedBy",
            string modifiedAtField = "ModifiedAt")
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName required", nameof(blockName));
            if (getUserFn == null) throw new ArgumentNullException(nameof(getUserFn));

            TriggerResult Stamp(TriggerContext ctx, bool isInsert)
            {
                if (ctx?.CurrentRecord == null) return TriggerResult.Success;
                var user = getUserFn();
                var now  = DateTime.Now;

                // Route the per-field set through RecordPropertyAccessor so the
                // lookup is case-insensitive, the type conversion is centralized,
                // and missing/read-only fields produce a throttled diagnostic
                // log instead of a silent no-op. The previous inline Set used a
                // case-sensitive GetProperty and an empty catch that swallowed
                // type-mismatch exceptions — meaning a record whose ModifiedAt
                // property was typed as long instead of DateTime would silently
                // not get stamped.
                void Set(string field, object value)
                {
                    if (string.IsNullOrEmpty(field)) return;
                    RecordPropertyAccessor.TrySetValue(ctx.CurrentRecord, field, value, logger: null);
                }

                if (isInsert)
                {
                    Set(createdByField, user);
                    Set(createdAtField, now);
                }
                Set(modifiedByField, user);
                Set(modifiedAtField, now);

                return TriggerResult.Success;
            }

            return new[]
            {
                new TriggerDefinition
                {
                    TriggerType = TriggerType.PreInsert,
                    Scope       = TriggerScope.Block,
                    TriggerName = $"audit_insert_{blockName}",
                    BlockName   = blockName,
                    Handler     = ctx => Stamp(ctx, isInsert: true)
                },
                new TriggerDefinition
                {
                    TriggerType = TriggerType.PreUpdate,
                    Scope       = TriggerScope.Block,
                    TriggerName = $"audit_update_{blockName}",
                    BlockName   = blockName,
                    Handler     = ctx => Stamp(ctx, isInsert: false)
                }
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // CascadeDelete — fires on PreDelete, records cascade info in context
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="TriggerType.PreDelete"/> trigger that stores cascade
        /// parameters in the <see cref="TriggerContext"/> for downstream handlers to act on.
        /// </summary>
        public static TriggerDefinition CascadeDeleteTrigger(
            string masterBlockName,
            string detailBlockName,
            string foreignKeyField,
            string primaryKeyField = "Id")
        {
            if (string.IsNullOrWhiteSpace(masterBlockName)) throw new ArgumentException("masterBlockName required", nameof(masterBlockName));
            if (string.IsNullOrWhiteSpace(detailBlockName)) throw new ArgumentException("detailBlockName required", nameof(detailBlockName));
            if (string.IsNullOrWhiteSpace(foreignKeyField)) throw new ArgumentException("foreignKeyField required", nameof(foreignKeyField));

            return new TriggerDefinition
            {
                TriggerType   = TriggerType.PreDelete,
                Scope         = TriggerScope.Block,
                TriggerName   = $"cascade_delete_{masterBlockName}_to_{detailBlockName}",
                BlockName     = masterBlockName,
                ContinueOnFailure = false,
                Handler = ctx =>
                {
                    if (ctx?.CurrentRecord == null) return TriggerResult.Success;
                    // Route the primary-key read through RecordPropertyAccessor so the
                    // lookup is case-insensitive (caller-supplied "Id" matches a
                    // property declared as "id" or "ID"). The previous
                    // Type.GetProperty(primaryKeyField) call had no IgnoreCase flag,
                    // so a record whose PK property was named differently from the
                    // cascade registration would silently fail to populate the
                    // CascadeDelete_PKValue parameter — and downstream handlers
                    // would then have no PK to filter the detail block on.
                    if (!RecordPropertyAccessor.TryGetValue(ctx.CurrentRecord, primaryKeyField, out var pkValue, logger: null))
                    {
                        // No PK on the record — nothing to cascade. Treat as no-op
                        // (matches the prior "return TriggerResult.Success" on a
                        // missing property), but do not invent a null PK.
                        return TriggerResult.Success;
                    }

                    ctx.Parameters["CascadeDelete_Master"]  = masterBlockName;
                    ctx.Parameters["CascadeDelete_Detail"]  = detailBlockName;
                    ctx.Parameters["CascadeDelete_FK"]      = foreignKeyField;
                    ctx.Parameters["CascadeDelete_PKValue"] = pkValue;

                    return TriggerResult.Success;
                }
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // FormatField — fires on WhenValidateItem to transform a field value
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="TriggerType.WhenValidateItem"/> trigger that applies
        /// <paramref name="formatter"/> to the current item value and writes it back.
        /// </summary>
        public static TriggerDefinition FormatFieldTrigger(
            string blockName,
            string itemName,
            Func<object, object> formatter)
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName required", nameof(blockName));
            if (string.IsNullOrWhiteSpace(itemName))  throw new ArgumentException("itemName required",  nameof(itemName));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            return new TriggerDefinition
            {
                TriggerType = TriggerType.WhenValidateItem,
                Scope       = TriggerScope.Item,
                TriggerName = $"format_{blockName}_{itemName}",
                BlockName   = blockName,
                ItemName    = itemName,
                Handler = ctx =>
                {
                    if (ctx?.CurrentRecord == null) return TriggerResult.Success;
                    // Route the read/write through RecordPropertyAccessor: the
                    // previous direct GetProperty had no IgnoreCase flag, so a
                    // trigger registered for "customerid" would not find a record
                    // property named "CustomerId". The accessor is also the
                    // single place that handles the read-only check, type
                    // conversion, and throttled diagnostic.
                    if (!RecordPropertyAccessor.TryGetValue(ctx.CurrentRecord, itemName, out var current, logger: null))
                    {
                        // No such property on the record — leave it alone. This
                        // matches the prior "if (prop == null) return Success"
                        // behavior, but with the case-insensitive catalog and
                        // throttled warning rather than a silent no-op.
                        return TriggerResult.Success;
                    }

                    object formatted;
                    try
                    {
                        formatted = formatter(current);
                    }
                    catch (Exception ex)
                    {
                        ctx.ErrorMessage = $"FormatField '{itemName}' formatter threw: {ex.Message}";
                        return TriggerResult.Failure;
                    }

                    if (!RecordPropertyAccessor.TrySetValue(ctx.CurrentRecord, itemName, formatted, logger: null))
                    {
                        ctx.ErrorMessage = $"FormatField '{itemName}' is read-only or has an incompatible value type.";
                        return TriggerResult.Failure;
                    }
                    return TriggerResult.Success;
                }
            };
        }
    }
}
