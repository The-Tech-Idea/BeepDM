namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Classifies the kind of structural change carried by an
    /// <see cref="AlterEntityChange"/>. Phase 12 v1 ships the five
    /// most common alter kinds; new kinds must be appended so that
    /// persisted plans remain forward compatible.
    /// </summary>
    public enum AlterEntityChangeKind
    {
        /// <summary>Add a new column to the entity.</summary>
        AddColumn = 0,

        /// <summary>Drop an existing column from the entity.</summary>
        DropColumn = 1,

        /// <summary>Alter an existing column (type / nullability / length).</summary>
        AlterColumn = 2,

        /// <summary>Create a new secondary index on the entity.</summary>
        AddIndex = 3,

        /// <summary>Drop an existing secondary index from the entity.</summary>
        DropIndex = 4
    }
}
