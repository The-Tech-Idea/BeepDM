namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Defines the record locking mode for a data block.
    /// </summary>
    public enum LockMode
    {
        /// <summary>No locking — lock requests succeed without storing state</summary>
        None,

        /// <summary>Automatic locking — records locked automatically on edit when LockOnEdit is true</summary>
        Automatic,

        /// <summary>Manual locking — caller must explicitly lock/unlock records</summary>
        Manual,

        /// <summary>Immediate locking — records locked as soon as the block is navigated to</summary>
        Immediate
    }
}
