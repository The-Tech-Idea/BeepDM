using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional dirty-state helper contract preserving one outcome per requested block.</summary>
    public interface ICoordinatedBlockSave
    {
        Task<IReadOnlyList<SaveResult>> SaveDirtyBlocksWithResultsAsync(List<string> blockNames,
            Func<DataBlockInfo, Task<IErrorsInfo>> writer = null);
    }
}
