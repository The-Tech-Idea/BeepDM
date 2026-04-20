namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// One per-column delta captured for an <see cref="AuditEvent"/>
    /// representing a Create/Update/Delete operation. Producers should
    /// only emit the columns that actually changed (not the whole row)
    /// to keep audit storage bounded.
    /// </summary>
    /// <remarks>
    /// Both <see cref="OldValue"/> and <see cref="NewValue"/> are stored
    /// as <see cref="object"/> so the canonical JSON serializer can keep
    /// the source type intact (numeric vs string) — this matters for
    /// chain hashing because <c>"42"</c> and <c>42</c> must not collide.
    /// </remarks>
    public sealed class AuditFieldChange
    {
        /// <summary>Column / field name being changed.</summary>
        public string Field { get; set; }

        /// <summary>Value before the change. May be <c>null</c>.</summary>
        public object OldValue { get; set; }

        /// <summary>Value after the change. May be <c>null</c>.</summary>
        public object NewValue { get; set; }

        /// <summary>Default constructor for serializers and object initializers.</summary>
        public AuditFieldChange()
        {
        }

        /// <summary>Convenience constructor.</summary>
        public AuditFieldChange(string field, object oldValue, object newValue)
        {
            Field = field;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
