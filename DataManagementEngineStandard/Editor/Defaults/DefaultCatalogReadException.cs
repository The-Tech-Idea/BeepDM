using System;

namespace TheTechIdea.Beep.Editor.Defaults
{
    /// <summary>A required defaults catalog could not be read or validated; no raw catalog details are exposed.</summary>
    public sealed class DefaultCatalogReadException : Exception
    {
        public DefaultCatalogReadException() : base("Required defaults catalog admission failed.") { }
    }
}
