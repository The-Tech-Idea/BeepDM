using System;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional operation-local error/diagnostic routing. Dispose in the entering flow
    /// after physical work drains; nested operations must restore their parent.</summary>
    public interface IScopedEditorErrors
    {
        IDisposable BeginOperationErrors(IErrorsInfo outcome);
    }
}
