using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Populates block definitions with <c>EntityStructure</c> metadata from a live datasource.
/// Shared by WPF and WinForms. Implementations must not throw; surface failures via return value.
/// </summary>
public interface IFormsBootstrapper
{
    Task<bool> BootstrapAsync(IBeepFormsHost formsHost, CancellationToken cancellationToken = default);
}
