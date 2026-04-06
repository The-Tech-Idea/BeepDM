using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Block property built-ins partial class.
    /// Exposes Oracle Forms SET_BLOCK_PROPERTY / GET_BLOCK_PROPERTY semantics.
    /// </summary>
    public partial class FormsManager
    {
        #region SetBlockProperty / GetBlockProperty

        /// <summary>
        /// Set an Oracle Forms-style block property.
        /// Corresponds to Oracle Forms SET_BLOCK_PROPERTY built-in.
        /// </summary>
        public void SetBlockProperty(string blockName, BlockProperty property, object value)
        {
            _blockPropertyManager.SetBlockProperty(blockName, property, value);
            LogOperation($"SetBlockProperty({blockName}, {property}, {value})", blockName);
        }

        /// <summary>
        /// Get an Oracle Forms-style block property.
        /// Corresponds to Oracle Forms GET_BLOCK_PROPERTY built-in.
        /// </summary>
        public object GetBlockProperty(string blockName, BlockProperty property)
            => _blockPropertyManager.GetBlockProperty(blockName, property);

        /// <summary>
        /// Typed convenience overload for GetBlockProperty.
        /// </summary>
        public T GetBlockProperty<T>(string blockName, BlockProperty property)
            => _blockPropertyManager.GetBlockProperty<T>(blockName, property);

        /// <summary>
        /// Allow or disallow INSERT for a block.
        /// Shorthand for SetBlockProperty(blockName, BlockProperty.InsertAllowed, value).
        /// </summary>
        public void SetInsertAllowed(string blockName, bool value)
            => SetBlockProperty(blockName, BlockProperty.InsertAllowed, value);

        /// <summary>
        /// Allow or disallow UPDATE for a block.
        /// </summary>
        public void SetUpdateAllowed(string blockName, bool value)
            => SetBlockProperty(blockName, BlockProperty.UpdateAllowed, value);

        /// <summary>
        /// Allow or disallow DELETE for a block.
        /// </summary>
        public void SetDeleteAllowed(string blockName, bool value)
            => SetBlockProperty(blockName, BlockProperty.DeleteAllowed, value);

        /// <summary>
        /// Allow or disallow QUERY for a block.
        /// </summary>
        public void SetQueryAllowed(string blockName, bool value)
            => SetBlockProperty(blockName, BlockProperty.QueryAllowed, value);

        /// <summary>
        /// Set a default WHERE clause appended to every query on this block.
        /// Corresponds to Oracle Forms SET_BLOCK_PROPERTY(block, DEFAULT_WHERE, clause).
        /// </summary>
        public void SetDefaultWhere(string blockName, string whereClause)
            => SetBlockProperty(blockName, BlockProperty.DefaultWhere, whereClause);

        /// <summary>
        /// Set a default ORDER BY clause appended to every query on this block.
        /// </summary>
        public void SetOrderBy(string blockName, string orderByClause)
            => SetBlockProperty(blockName, BlockProperty.OrderBy, orderByClause);

        #endregion
    }
}
