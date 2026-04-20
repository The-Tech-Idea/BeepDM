namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// How a redactor transforms a matched secret or PII fragment.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><see cref="Mask"/> replaces the value with the
    ///   redactor's <c>ReplacementToken</c> (defaults to <c>***</c>). Cheapest
    ///   and most readable for log streams.</description></item>
    ///   <item><description><see cref="Hash"/> replaces the value with a
    ///   SHA-256 hex digest of the value mixed with
    ///   <see cref="RedactionContext.HashSalt"/>. Used by audit so analysts
    ///   can join records by stable hash without learning the raw value.</description></item>
    ///   <item><description><see cref="Drop"/> removes the value entirely
    ///   (sets the matched property to <c>null</c> or strips the matched
    ///   substring from the message). Use only when zero leakage is required.</description></item>
    /// </list>
    /// </remarks>
    public enum RedactionMode
    {
        /// <summary>Replace the value with a static replacement token.</summary>
        Mask = 0,

        /// <summary>Replace the value with a salted SHA-256 hex digest.</summary>
        Hash = 1,

        /// <summary>Strip the value entirely.</summary>
        Drop = 2
    }
}
