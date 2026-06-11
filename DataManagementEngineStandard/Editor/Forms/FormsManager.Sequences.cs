using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Sequence and default-value built-ins partial class.
    /// Provides Oracle Forms :SEQUENCE.NEXTVAL, DEFAULT_VALUE, COPY, and group-populate patterns.
    /// </summary>
    public partial class FormsManager
    {
        // B2 (audit pass 3, 2026-06): re-indexed from
        //   Dictionary<string, Func<object>>  (key = "blockName|itemName")
        // to
        //   Dictionary<string, Dictionary<string, Func<object>>>
        //   (outer key = block name, inner key = item name)
        // for two reasons:
        //   1. ApplyItemDefaults no longer iterates the entire
        //      registry to find the entries for one block; it
        //      indexes directly by block name.
        //   2. The compound-string key was a latent bug: a field
        //      name containing a '|' character would split
        //      incorrectly on Split('|') and the field would
        //      be silently skipped (B3). The nested dict makes
        //      the field name the inner key directly, with no
        //      delimiter involved.
        // Outer key uses StringComparer.OrdinalIgnoreCase to
        // match the engine's block-name handling; same for the
        // inner key. B2/B3 combined.
        //
        // Note: this is a Dictionary<,>, not ConcurrentDictionary.
        // The class is not documented as thread-safe (see N2 in
        // the audit). If the host needs concurrent access, swap
        // for ConcurrentDictionary and add a lock around the
        // inner-dict mutations.
        private readonly Dictionary<string, Dictionary<string, Func<object>>> _fieldDefaultsByBlock =
            new(StringComparer.OrdinalIgnoreCase);

        #region Sequence Built-ins

        /// <summary>
        /// Return the next value from a named sequence.
        /// Corresponds to Oracle Forms :SEQUENCE.sequencename.NEXTVAL.
        /// The sequence is created automatically on first use.
        /// </summary>
        public long GetNextSequence(string sequenceName)
            => _sequenceProvider.GetNextSequence(sequenceName);

        /// <summary>
        /// Peek at the next value without consuming it.
        /// </summary>
        public long PeekNextSequence(string sequenceName)
            => _sequenceProvider.PeekNextSequence(sequenceName);

        /// <summary>
        /// Reset a sequence to a given starting value.
        /// </summary>
        public void ResetSequence(string sequenceName, long startValue = 1)
            => _sequenceProvider.ResetSequence(sequenceName, startValue);

        /// <summary>
        /// Create a named sequence with custom start and increment.
        /// </summary>
        public void CreateSequence(string sequenceName, long startValue = 1, long incrementBy = 1)
            => _sequenceProvider.CreateSequence(sequenceName, startValue, incrementBy);

        #endregion

        #region Default Value Built-ins

        /// <summary>
        /// Register a factory that supplies the default value for a field when a new record is created.
        /// Corresponds to Oracle Forms DEFAULT_VALUE built-in / block item default.
        /// </summary>
        public void SetItemDefault(string blockName, string itemName, Func<object> defaultFactory)
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentNullException(nameof(blockName));
            if (string.IsNullOrWhiteSpace(itemName)) throw new ArgumentNullException(nameof(itemName));
            if (defaultFactory == null) throw new ArgumentNullException(nameof(defaultFactory));

            // B2: index by block name; the inner dict is created
            // on first SetItemDefault for that block.
            var perBlock = GetOrCreatePerBlockDict(blockName);
            perBlock[itemName] = defaultFactory;
        }

        /// <summary>
        /// Remove a previously registered field default.
        /// </summary>
        public void ClearItemDefault(string blockName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(itemName))
                return;

            if (_fieldDefaultsByBlock.TryGetValue(blockName, out var perBlock))
            {
                perBlock.Remove(itemName);
                if (perBlock.Count == 0)
                    _fieldDefaultsByBlock.Remove(blockName);
            }
        }

        /// <summary>
        /// Apply all registered defaults for a block to the supplied record object.
        /// Called internally from CreateNewRecord after the record is constructed.
        /// </summary>
        public void ApplyItemDefaults(string blockName, object record)
        {
            if (record == null) return;

            // B2: index directly by block name — no full
            // registry walk. B1: removed the unused
            // `var type = record.GetType();` from the previous
            // version (audit pass 3, 2026-06).
            if (!_fieldDefaultsByBlock.TryGetValue(blockName, out var perBlock))
                return;

            // Snapshot the keys so a SetItemDefault inside a
            // factory callback (which can happen — e.g. one
            // default registers another) doesn't invalidate the
            // enumerator.
            var keys = perBlock.Keys.ToArray();
            foreach (var fieldName in keys)
            {
                if (!perBlock.TryGetValue(fieldName, out var factory))
                    continue;

                try
                {
                    var value = factory();
                    // Read through RecordPropertyAccessor: cached lookup
                    // + throttled diagnostic on miss. Replaces the
                    // per-call Type.GetProperty reflection that ran
                    // on every default apply.
                    //
                    // Behavior delta from the pre-consolidation code:
                    // a missing field used to silently no-op (the
                    // `prop?.SetValue(...)` short-circuited on null
                    // prop). Now TrySetValue returns false and we log
                    // a loud error. A type-conversion failure
                    // (e.g. default for an int field returned a
                    // string) used to throw InvalidCastException; now
                    // it's caught inside TrySetValue and surfaces as
                    // a "could not set field" log line, which is the
                    // same observable behavior for the catch below.
                    if (!RecordPropertyAccessor.TrySetValue(record, fieldName, value, _dmeEditor))
                    {
                        LogError(
                            $"ApplyItemDefaults: could not set field '{fieldName}' on record type '{record.GetType().Name}' in block '{blockName}'",
                            null, blockName);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error applying default for field '{fieldName}' in block '{blockName}'", ex, blockName);
                }
            }
        }

        private Dictionary<string, Func<object>> GetOrCreatePerBlockDict(string blockName)
        {
            if (!_fieldDefaultsByBlock.TryGetValue(blockName, out var perBlock))
            {
                perBlock = new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase);
                _fieldDefaultsByBlock[blockName] = perBlock;
            }
            return perBlock;
        }

        #endregion

        #region Copy Field Value

        /// <summary>
        /// Copy the current value of a field in one block to a field in another block.
        /// Corresponds to Oracle Forms COPY built-in.
        /// </summary>
        public void CopyFieldValue(string sourceBlock, string sourceField,
            string destBlock, string destField)
        {
            var srcInfo = GetBlock(sourceBlock);
            var dstInfo = GetBlock(destBlock);
            if (srcInfo?.UnitOfWork?.Units?.Current == null || dstInfo?.UnitOfWork?.Units?.Current == null)
            {
                LogError($"CopyFieldValue: one or both blocks not found or have no current record",
                    null, sourceBlock);
                return;
            }

            var srcRecord = srcInfo.UnitOfWork.Units.Current;
            var dstRecord = dstInfo.UnitOfWork.Units.Current;
            var value = GetFieldValue(srcRecord, sourceField);
            // B5 (audit pass 3, 2026-06): the previous version
            // discarded the bool return from SetFieldValue. A
            // failed set (e.g. destination field is read-only or
            // doesn't exist) silently kept going. Now we log a
            // diagnostic so the user has a chance to notice.
            if (!SetFieldValue(dstRecord, destField, value))
            {
                LogError(
                    $"CopyFieldValue: failed to set field '{destField}' on destination record in block '{destBlock}' " +
                    $"(source value was from field '{sourceField}' in block '{sourceBlock}')",
                    null, destBlock);
            }
        }

        #endregion

        #region Populate Group from Block

        /// <summary>
        /// Copy all field values from the current record of a block into a flat
        /// dictionary (group) keyed by field name.
        /// Roughly corresponds to Oracle Forms POPULATE_GROUP built-in.
        /// </summary>
        public Dictionary<string, object> PopulateGroupFromBlock(string blockName)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var blockInfo = GetBlock(blockName);
            if (blockInfo?.UnitOfWork?.Units?.Current == null)
                return result;

            var record = blockInfo.UnitOfWork.Units.Current;
            // Use the cached PropertyInfo catalog via
            // RecordPropertyAccessor. Note: the previous implementation
            // caught and silently swallowed GetValue exceptions; the
            // accessor does the same but also emits a throttled
            // diagnostic for the first failure per (Type, name).
            var snapshot = RecordPropertyAccessor.GetAllReadable(record, _dmeEditor);
            foreach (var kvp in snapshot)
                result[kvp.Key] = kvp.Value;
            return result;
        }

        #endregion
    }
}
