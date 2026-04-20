namespace TheTechIdea.Beep.Distributed.Security
{
    /// <summary>
    /// Classification of an operation checked by
    /// <see cref="IDistributedAccessPolicy"/>. Kept coarse so an
    /// ACL can be authored per entity without listing individual
    /// methods.
    /// </summary>
    public enum DistributedAccessKind
    {
        /// <summary>Read (GetEntity / scatter read / query).</summary>
        Read  = 0,
        /// <summary>Write (Insert / Update / Delete, replicated or sharded).</summary>
        Write = 1,
        /// <summary>Schema (CreateEntity / AlterEntity / DropEntity).</summary>
        Ddl   = 2,
    }
}
