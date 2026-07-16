using System;
using TheTechIdea.Beep.Editor.Forms.Hosts;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

/// <summary>
/// Syncs <see cref="BeepViewState"/> from <see cref="IUnitofWorksManager"/>.
/// Shared by WPF and WinForms. No platform-specific code.
/// </summary>
public sealed class ViewStateSyncer
{
    private IUnitofWorksManager? _engine;

    public void Attach(IUnitofWorksManager? engine) => _engine = engine;

    public void Sync(BeepViewState viewState)
    {
        viewState.IsDirty = _engine?.IsDirty ?? false;
        viewState.StatusText = _engine?.Status ?? string.Empty;
        viewState.ActiveBlockName = _engine?.CurrentBlockName;
        SyncRecordPosition(viewState);
        if (TryGetCurrentMessage(viewState.ActiveBlockName, out var msg, out var sev))
        {
            viewState.CurrentMessage = msg;
            viewState.MessageSeverity = sev;
        }
    }

    public void SyncBlock(IBlockView blockView) => blockView.SyncFromManager();

    private void SyncRecordPosition(BeepViewState viewState)
    {
        viewState.RecordPositionText = string.Empty;
        string? blockName = viewState.ActiveBlockName;
        if (_engine == null || string.IsNullOrWhiteSpace(blockName) || !_engine.BlockExists(blockName))
            return;
        try
        {
            var uow = _engine.GetBlock(blockName)?.UnitOfWork;
            if (uow?.CurrentItem == null) { viewState.RecordPositionText = "0 records"; return; }
            try
            {
                dynamic units = uow.Units;
                int currentIdx = 0;
                int count = 0;
                try { currentIdx = (int)(units.CurrentIndex); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ViewStateSyncer.SyncRecordPosition: CurrentIndex read ignored: {ex}"); }
                try { count = units.Count; }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ViewStateSyncer.SyncRecordPosition: Count read ignored: {ex}"); }
                viewState.RecordPositionText = $"{currentIdx + 1}/{count}";
            }
            catch { viewState.RecordPositionText = "1/?"; }
        }
        catch { viewState.RecordPositionText = string.Empty; }
    }

    public bool TryGetCurrentMessage(string? activeBlockName, out string message, out BeepMessageSeverity severity)
    {
        message = string.Empty;
        severity = BeepMessageSeverity.None;
        if (_engine == null) return false;
        string blockName = !string.IsNullOrWhiteSpace(activeBlockName) ? activeBlockName : _engine.CurrentBlockName ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(blockName))
        {
            message = _engine.Messages.GetCurrentMessage(blockName);
            if (!string.IsNullOrWhiteSpace(message))
            {
                severity = MessageClassifier.MapMessageLevel(_engine.Messages.GetCurrentMessageLevel(blockName));
                return true;
            }
        }
        return false;
    }
}
