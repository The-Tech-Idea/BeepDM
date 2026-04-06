using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Implements IBlockPropertyManager — maps BlockProperty enum values to/from
    /// fields on DataBlockInfo so FormsManager can call Oracle Forms-style
    /// SET_BLOCK_PROPERTY / GET_BLOCK_PROPERTY built-ins.
    /// </summary>
    public class BlockPropertyManager : IBlockPropertyManager
    {
        private readonly IDictionary<string, DataBlockInfo> _blocks;

        public BlockPropertyManager(IDictionary<string, DataBlockInfo> blocks)
        {
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }

        /// <inheritdoc/>
        public void SetBlockProperty(string blockName, BlockProperty property, object value)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            if (!_blocks.TryGetValue(blockName, out var info)) return;

            switch (property)
            {
                case BlockProperty.InsertAllowed:
                    info.InsertAllowed = Convert.ToBoolean(value);
                    break;
                case BlockProperty.UpdateAllowed:
                    info.UpdateAllowed = Convert.ToBoolean(value);
                    break;
                case BlockProperty.DeleteAllowed:
                    info.DeleteAllowed = Convert.ToBoolean(value);
                    break;
                case BlockProperty.QueryAllowed:
                    info.QueryAllowed = Convert.ToBoolean(value);
                    break;
                case BlockProperty.DefaultWhere:
                    info.DefaultWhereClause = value?.ToString() ?? string.Empty;
                    break;
                case BlockProperty.OrderBy:
                    info.DefaultOrderByClause = value?.ToString() ?? string.Empty;
                    break;
                case BlockProperty.CurrentRecordIndex:
                    // Navigation index is managed by the navigation layer; log as extended property
                    info.ExtendedProperties[property.ToString()] = Convert.ToInt32(value);
                    break;
                default:
                    // Store in extended properties for unknown / UI-only properties
                    info.ExtendedProperties[property.ToString()] = value;
                    break;
            }
        }

        /// <inheritdoc/>
        public object GetBlockProperty(string blockName, BlockProperty property)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return null;
            if (!_blocks.TryGetValue(blockName, out var info)) return null;

            return property switch
            {
                BlockProperty.InsertAllowed     => (object)info.InsertAllowed,
                BlockProperty.UpdateAllowed     => info.UpdateAllowed,
                BlockProperty.DeleteAllowed     => info.DeleteAllowed,
                BlockProperty.QueryAllowed      => info.QueryAllowed,
                BlockProperty.DefaultWhere      => info.DefaultWhereClause,
                BlockProperty.OrderBy           => info.DefaultOrderByClause,
                BlockProperty.Status            => info.Mode.ToString(),
                BlockProperty.CurrentRecordIndex => info.UnitOfWork?.Units?.CurrentIndex ?? 0,
                _ => info.ExtendedProperties.TryGetValue(property.ToString(), out var ext) ? ext : null
            };
        }

        /// <inheritdoc/>
        public T GetBlockProperty<T>(string blockName, BlockProperty property)
        {
            var raw = GetBlockProperty(blockName, property);
            if (raw == null) return default;
            try { return (T)Convert.ChangeType(raw, typeof(T)); }
            catch { return default; }
        }
    }
}
