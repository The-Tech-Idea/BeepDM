using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Provides pause/resume capability for long installations.
    /// Integrates with SetupCheckpointStore for persistent state.
    /// </summary>
    public class PauseResumeManager
    {
        private readonly string _statePath;
        private readonly object _lock = new();
        private volatile bool _paused;
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        public bool IsPaused => _paused;
        public InstallProgressState? CurrentState { get; private set; }

        public PauseResumeManager(string? statePath = null)
        {
            _statePath = statePath ?? Path.Combine(Path.GetTempPath(), "beep_install_state.json");
        }

        /// <summary>Pauses the installation at the current stage.</summary>
        public void Pause(string currentStepId, string stepName, int percentComplete)
        {
            lock (_lock)
            {
                _paused = true;
                _pauseEvent.Reset();

                CurrentState = new InstallProgressState
                {
                    PausedAt = DateTimeOffset.UtcNow,
                    LastStepId = currentStepId,
                    LastStepName = stepName,
                    PercentComplete = percentComplete
                };

                PersistState();
            }
        }

        /// <summary>Resumes the installation.</summary>
        public void Resume()
        {
            lock (_lock)
            {
                _paused = false;
                _pauseEvent.Set();
                CurrentState = null;
            }
        }

        /// <summary>Blocks until resume is called. Returns false if cancelled.</summary>
        public bool WaitWhilePaused(CancellationToken token)
        {
            try { _pauseEvent.Wait(token); return true; }
            catch (OperationCanceledException) { return false; }
        }

        /// <summary>Loads a previously saved state. Returns null if no state exists.</summary>
        public static InstallProgressState? LoadState(string? statePath = null)
        {
            var path = statePath ?? Path.Combine(Path.GetTempPath(), "beep_install_state.json");
            if (!File.Exists(path)) return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<InstallProgressState>(json);
            }
            catch { return null; }
        }

        /// <summary>Deletes the saved state file (call on successful completion).</summary>
        public void ClearState()
        {
            try { if (File.Exists(_statePath)) File.Delete(_statePath); } catch { }
        }

        private void PersistState()
        {
            try
            {
                File.WriteAllText(_statePath, JsonSerializer.Serialize(CurrentState));
            }
            catch { }
        }
    }

    public class InstallProgressState
    {
        public DateTimeOffset PausedAt { get; set; }
        public string LastStepId { get; set; } = "";
        public string LastStepName { get; set; } = "";
        public int PercentComplete { get; set; }
        public int CompletedStepCount { get; set; }
        public int TotalStepCount { get; set; }
        public long BytesCopied { get; set; }
        public long TotalBytes { get; set; }
    }
}
