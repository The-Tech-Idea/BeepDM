using System;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>A named, typed parameter that can be passed into a workflow run.</summary>
    public class WorkFlowParameter
    {
        public string  Name         { get; set; } = string.Empty;
        public string  Description  { get; set; } = string.Empty;
        /// <summary>CLR type name, e.g. "System.String", "System.Int32".</summary>
        public string  TypeName     { get; set; } = "System.String";
        public object? DefaultValue { get; set; }
        public bool    Required     { get; set; } = false;
    }
}
