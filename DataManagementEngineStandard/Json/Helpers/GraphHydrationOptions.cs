using System;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal sealed class GraphHydrationOptions
    {
        public int Depth { get; set; } = 1;
        public bool IncludeParentReference { get; set; } = true;
        public bool IncludeAncestorChain { get; set; } = false;
        public Func<string, bool> IncludeEntityNamePredicate { get; set; } = _ => true;
        public string ParentReferenceKey { get; set; } = "__parent";
        public string AncestorsKey { get; set; } = "__ancestors";
    }
}