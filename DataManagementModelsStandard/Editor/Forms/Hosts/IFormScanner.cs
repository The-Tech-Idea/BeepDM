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

    /// <summary>
    /// Parses a designer file (.Designer.cs or .xaml) and returns its PRIMARY
    /// block. A designer file can declare several blocks; this returns the first.
    /// Use <see cref="ParseDesignerBlocksAsync"/> when every block matters.
    /// </summary>
    Task<ScannedBlockInfo> ParseDesignerFileAsync(string designerPath, CancellationToken cancellationToken);

    /// <summary>
    /// Parses a designer file (.Designer.cs or .xaml) and returns EVERY block it
    /// declares, in declaration order. Empty when the file declares none.
    /// </summary>
    /// <remarks>
    /// <see cref="ParseDesignerFileAsync"/> returns only the first block, which
    /// silently loses the rest of a multi-block designer file when a caller reaches
    /// for a designer directly rather than through <see cref="ParseFormFileAsync"/>
    /// (plan §3.3). This is the lossless entry point.
    /// </remarks>
    Task<IReadOnlyList<ScannedBlockInfo>> ParseDesignerBlocksAsync(string designerPath, CancellationToken cancellationToken);
}
