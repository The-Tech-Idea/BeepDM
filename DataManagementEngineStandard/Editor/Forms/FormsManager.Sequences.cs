using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Sequence and default-value built-ins partial class.
    /// Provides Oracle Forms :SEQUENCE.NEXTVAL, DEFAULT_VALUE, COPY, and group-populate patterns.
    /// </summary>
    public partial class FormsManager
    {
        // Per-field default factories: key = "blockName|itemName"
        private readonly Dictionary<string, Func<object>> _fieldDefaults =
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
            _fieldDefaults[$"{blockName}|{itemName}"] = defaultFactory
                ?? throw new ArgumentNullException(nameof(defaultFactory));
        }

        /// <summary>
        /// Remove a previously registered field default.
        /// </summary>
        public void ClearItemDefault(string blockName, string itemName)
            => _fieldDefaults.Remove($"{blockName}|{itemName}");

        /// <summary>
        /// Apply all registered defaults for a block to the supplied record object.
        /// Called internally from CreateNewRecord after the record is constructed.
        /// </summary>
        public void ApplyItemDefaults(string blockName, object record)
        {
            if (record == null) return;
            var type = record.GetType();

            foreach (var kv in _fieldDefaults)
            {
                var parts = kv.Key.Split('|');
                if (parts.Length != 2 || !parts[0].Equals(blockName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var fieldName = parts[1];
                try
                {
                    var value = kv.Value();
                    var prop = type.GetProperty(fieldName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    prop?.SetValue(record, Convert.ChangeType(value, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    LogError($"Error applying default for field '{fieldName}' in block '{blockName}'", ex, blockName);
                }
            }
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
            SetFieldValue(dstRecord, destField, value);
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
            var type = record.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try { result[prop.Name] = prop.GetValue(record); }
                catch { /* skip unreadable props */ }
            }
            return result;
        }

        #endregion
    }
}
