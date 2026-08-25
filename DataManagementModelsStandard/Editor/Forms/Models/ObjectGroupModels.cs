using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// One member of an Object Group — a reference to an already-existing
    /// object elsewhere on the form, not a copy of it.
    /// </summary>
    public class ObjectGroupMemberRef
    {
        /// <summary>What kind of object this member refers to: "Block", "Item", "RecordGroup", "ParameterList", "Alert", or "Editor".</summary>
        public string Kind { get; set; }

        /// <summary>
        /// The member's own identity path — a block name ("Ord"), an
        /// item's dotted path ("Ord.Qty"), or a form-level object's plain
        /// name ("SalesRegions").
        /// </summary>
        public string Path { get; set; }

        /// <summary>Human-readable label, captured at authoring time so the group's own row can display its contents without re-resolving every member.</summary>
        public string DisplayLabel { get; set; }
    }

    /// <summary>
    /// A named Object Group (Oracle Forms OBJECT_GROUP) — a design-time-only
    /// bundle of references to existing objects, for organization and future
    /// reuse. Unlike every other object this IDE authors, an Object Group has
    /// no engine counterpart and fires nothing at runtime: Oracle Forms itself
    /// gives it no CREATE/SHOW built-in, only a design-time "copy this group"
    /// action in the Object Navigator. Persisted as a comment-only marker in
    /// the Designer file (no executable call to emit), the same way the Menu
    /// Builder's model marker round-trips its tree.
    /// </summary>
    public class ObjectGroupDefinition
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public List<ObjectGroupMemberRef> Members { get; set; } = new();
    }
}
