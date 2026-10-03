namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional opaque identity for the current read buffer, not its row contents.</summary>
    public interface IUnitofWorkReadBufferIdentity
    {
        bool SupportsReadBufferIdentity { get; }
        /// <summary>True when local read scope forbids trusting a preloaded buffer without a managed receipt.</summary>
        bool RequiresReadAuthorization { get; }
        bool TryGetReadBufferIdentity(out object identity);
    }

    /// <summary>
    /// Optional stage receipt. The stable opaque identity is available before publication;
    /// successful publication installs it on the owning UoW before any observers run.
    /// It must not expose candidate rows. Replacing/clearing the buffer invalidates it.
    /// </summary>
    public interface IUnitofWorkReadBufferStage
    {
        object PreparedBufferIdentity { get; }
    }
}
