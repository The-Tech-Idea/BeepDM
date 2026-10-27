using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Tools;

/// <summary>Releases each owned loader extension once, retaining independent cleanup failures for the owner to report.</summary>
internal static class LoaderExtensionLifetime
{
    internal static IReadOnlyList<Exception> Release(IList<ILoaderExtention> extensions)
    {
        var failures=new List<Exception>();
        if(extensions==null)return failures;
        var released=new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach(var extension in extensions.ToArray())
        {
            if(extension is not IDisposable disposable||!released.Add(extension))continue;
            try{disposable.Dispose();}
            catch(Exception ex){failures.Add(ex);}
        }
        extensions.Clear();return failures;
    }
}
