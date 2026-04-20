namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Source of the HMAC secret used to seal the audit hash chain.
    /// Kept abstract so different hosts can plug in their own secret
    /// store: an environment variable on Linux, DPAPI on Windows, the
    /// macOS Keychain, Azure Key Vault, etc.
    /// </summary>
    /// <remarks>
    /// Implementations must never write the secret to disk and must
    /// never include it in any log envelope. The default
    /// <see cref="EnvironmentKeyMaterialProvider"/> reads from a
    /// configurable environment variable so the operator can rotate
    /// without redeploying. Tests should use
    /// <see cref="StaticKeyMaterialProvider"/> with a fixed string.
    /// </remarks>
    public interface IKeyMaterialProvider
    {
        /// <summary>Returns the HMAC secret bytes for chain hashing.</summary>
        byte[] GetHmacKey();
    }
}
