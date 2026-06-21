using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Scanner contract for discovering forms and their blocks/items/triggers
/// from source files. Implementations: WinForms (.Designer.cs) and WPF (.xaml).
/// </summary>
public interface IFormScanner
{
    /// <summary>Returns "winforms" or "wpf".</summary>
    string PlatformId { get; }

    /// <summary>Scans the solution for all forms of this platform.</summary>
    Task<List<ScannedFormInfo>> ScanSolutionAsync(CancellationToken cancellationToken);

    /// <summary>Parses a single form file.</summary>
    Task<ScannedFormInfo?> ParseFormFileAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>Parses a designer file (.Designer.cs or .xaml).</summary>
    Task<ScannedBlockInfo> ParseDesignerFileAsync(string designerPath, CancellationToken cancellationToken);
}
