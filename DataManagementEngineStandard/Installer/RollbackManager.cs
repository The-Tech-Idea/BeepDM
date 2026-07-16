using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Transactional rollback manager. Each install operation registers a revert action.
    /// On failure, actions are executed in reverse order to undo the installation.
    /// </summary>
    public class RollbackManager
    {
        private readonly Stack<RollbackAction> _actions = new();
        private readonly InstallLogger? _logger;

        public int ActionCount => _actions.Count;

        public RollbackManager(InstallLogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>Register a file that was copied (revert: delete it).</summary>
        public void RegisterFileCreated(string filePath)
        {
            _actions.Push(new RollbackAction
            {
                Description = $"Delete file: {filePath}",
                Execute = () =>
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
            });
        }

        /// <summary>Register a directory that was created (revert: delete if empty).</summary>
        public void RegisterDirectoryCreated(string dirPath)
        {
            _actions.Push(new RollbackAction
            {
                Description = $"Remove directory: {dirPath}",
                Execute = () =>
                {
                    try
                    {
                        if (Directory.Exists(dirPath) && !Directory.EnumerateFileSystemEntries(dirPath).Any())
                            Directory.Delete(dirPath, recursive: false);
                    }
                    catch (Exception ex) { _logger?.Warn("Rollback", $"RegisterDirectoryCreated revert: could not remove '{dirPath}': {ex.Message}"); }
                }
            });
        }

        /// <summary>Register a registry value that was written (revert: delete value).</summary>
        public void RegisterRegistryWrite(string keyPath, string valueName)
        {
            _actions.Push(new RollbackAction
            {
                Description = $"Remove registry value: {keyPath}\\{valueName}",
                Execute = () =>
                {
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true);
                        key?.DeleteValue(valueName, throwOnMissingValue: false);
                    }
                    catch (Exception ex) { _logger?.Warn("Rollback", $"RegisterRegistryWrite revert: could not remove '{keyPath}\\{valueName}': {ex.Message}"); }
                }
            });
        }

        /// <summary>Register a shortcut that was created (revert: delete .lnk).</summary>
        public void RegisterShortcutCreated(string linkPath)
        {
            _actions.Push(new RollbackAction
            {
                Description = $"Remove shortcut: {linkPath}",
                Execute = () =>
                {
                    if (File.Exists(linkPath)) File.Delete(linkPath);
                }
            });
        }

        /// <summary>Executes all revert actions in reverse order. Returns number of actions rolled back.</summary>
        public int Rollback()
        {
            int count = 0;
            _logger?.Info("Rollback", $"Starting rollback of {_actions.Count} actions...");

            while (_actions.Count > 0)
            {
                var action = _actions.Pop();
                try
                {
                    _logger?.Debug("Rollback", action.Description);
                    action.Execute();
                    count++;
                }
                catch (Exception ex)
                {
                    _logger?.Error("Rollback", $"Failed: {action.Description} — {ex.Message}");
                }
            }

            _logger?.Info("Rollback", $"Rollback complete. {count} actions reverted.");
            return count;
        }

        /// <summary>Clear all pending actions (call on successful completion).</summary>
        public void Commit()
        {
            _logger?.Info("Rollback", $"Install committed. Discarding {_actions.Count} rollback actions.");
            _actions.Clear();
        }

        private struct RollbackAction
        {
            public string Description;
            public Action Execute;
        }
    }
}
