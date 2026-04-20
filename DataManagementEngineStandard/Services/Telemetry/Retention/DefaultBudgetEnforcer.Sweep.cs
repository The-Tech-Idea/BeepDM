using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Sweep half of <see cref="DefaultBudgetEnforcer"/>. Applies, in
    /// order, the age cap, the file-count cap, and the byte budget for
    /// each registered scope.
    /// </summary>
    public sealed partial class DefaultBudgetEnforcer
    {
        /// <inheritdoc />
        public Task<BudgetSweepResult> EnforceAsync(string directory, CancellationToken cancellationToken = default)
        {
            EnforcerScope scope = FindScope(directory);
            if (scope is null)
            {
                BudgetSweepResult missing = new BudgetSweepResult
                {
                    Directory = directory,
                    SweptUtc = DateTime.UtcNow,
                    LastError = "No scope registered for directory."
                };
                return Task.FromResult(missing);
            }
            return Task.FromResult(EnforceScope(scope, cancellationToken));
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<BudgetSweepResult>> EnforceAllAsync(CancellationToken cancellationToken = default)
        {
            List<BudgetSweepResult> results = new List<BudgetSweepResult>(_scopes.Count);
            foreach (EnforcerScope scope in _scopes.Values)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                results.Add(EnforceScope(scope, cancellationToken));
            }
            return Task.FromResult((IReadOnlyList<BudgetSweepResult>)results);
        }

        private BudgetSweepResult EnforceScope(EnforcerScope scope, CancellationToken cancellationToken)
        {
            BudgetSweepResult result = new BudgetSweepResult
            {
                ScopeName = scope.ResolveName(),
                Directory = scope.Directory,
                ActionTaken = BudgetBreachAction.EmitOnly,
                SweptUtc = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(scope.Directory) || !Directory.Exists(scope.Directory))
            {
                result.LastError = "Directory missing.";
                RaiseSwept(result);
                return result;
            }

            string pattern = string.IsNullOrWhiteSpace(scope.FilePattern) ? "*" : scope.FilePattern;

            List<FileInfo> files;
            try
            {
                files = new DirectoryInfo(scope.Directory)
                    .EnumerateFiles(pattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f.LastWriteTimeUtc)
                    .ToList();
            }
            catch (Exception ex)
            {
                result.LastError = ex.Message;
                RaiseSwept(result);
                return result;
            }

            result.TotalBytesBefore = SumBytes(files);

            int deleted = 0;
            deleted += DeleteByAge(files, scope.Retention, cancellationToken);
            deleted += DeleteByCount(files, scope.Retention, cancellationToken);

            long currentBytes = SumBytes(files);
            if (scope.Budget is { MaxTotalBytes: > 0 } budget && currentBytes > budget.MaxTotalBytes)
            {
                result.BudgetBreachTriggered = true;
                result.ActionTaken = budget.OnBreach;

                switch (budget.OnBreach)
                {
                    case BudgetBreachAction.DeleteOldest:
                        deleted += DeleteUntilUnderBudget(files, budget.MaxTotalBytes, cancellationToken);
                        currentBytes = SumBytes(files);
                        SetBlocked(scope.Directory, false);
                        break;

                    case BudgetBreachAction.BlockNewWrites:
                        SetBlocked(scope.Directory, true);
                        result.BlockingNewWrites = true;
                        break;

                    case BudgetBreachAction.EmitOnly:
                    default:
                        SetBlocked(scope.Directory, false);
                        break;
                }
            }
            else
            {
                SetBlocked(scope.Directory, false);
            }

            result.FilesDeleted = deleted;
            result.TotalBytesAfter = currentBytes;
            scope.LastSweptUtc = result.SweptUtc;

            RaiseSwept(result);
            return result;
        }

        private static int DeleteByAge(List<FileInfo> files, RetentionPolicy retention, CancellationToken cancellationToken)
        {
            if (retention is null || retention.MaxAgeDays <= 0 || files.Count == 0)
            {
                return 0;
            }

            DateTime cutoffUtc = DateTime.UtcNow.AddDays(-retention.MaxAgeDays);
            int deleted = 0;

            for (int i = files.Count - 1; i >= 0; i--)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                FileInfo fi = files[i];
                if (fi.LastWriteTimeUtc < cutoffUtc && TryDelete(fi))
                {
                    files.RemoveAt(i);
                    deleted++;
                }
            }
            return deleted;
        }

        private static int DeleteByCount(List<FileInfo> files, RetentionPolicy retention, CancellationToken cancellationToken)
        {
            if (retention is null || retention.MaxFiles <= 0 || files.Count <= retention.MaxFiles)
            {
                return 0;
            }

            int excess = files.Count - retention.MaxFiles;
            int deleted = 0;
            int idx = 0;

            while (excess > 0 && idx < files.Count)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                FileInfo fi = files[idx];
                if (TryDelete(fi))
                {
                    files.RemoveAt(idx);
                    deleted++;
                    excess--;
                }
                else
                {
                    idx++;
                }
            }
            return deleted;
        }

        private static int DeleteUntilUnderBudget(List<FileInfo> files, long maxTotalBytes, CancellationToken cancellationToken)
        {
            if (files.Count == 0 || maxTotalBytes <= 0)
            {
                return 0;
            }

            long total = SumBytes(files);
            int deleted = 0;
            int idx = 0;

            while (total > maxTotalBytes && idx < files.Count)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                FileInfo fi = files[idx];
                long bytes = fi.Length;
                if (TryDelete(fi))
                {
                    files.RemoveAt(idx);
                    deleted++;
                    total -= bytes;
                }
                else
                {
                    idx++;
                }
            }
            return deleted;
        }

        private static long SumBytes(IEnumerable<FileInfo> files)
        {
            long total = 0;
            foreach (FileInfo fi in files)
            {
                try
                {
                    total += fi.Length;
                }
                catch
                {
                    // file may have been deleted under us; ignore
                }
            }
            return total;
        }

        private static bool TryDelete(FileInfo fi)
        {
            try
            {
                using (FileStream probe = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // Probe held the exclusive lock; safe to delete after
                    // we release it. This guards against deleting a file
                    // currently being appended to by the sink.
                }
                fi.Delete();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private void SetBlocked(string directory, bool blocked)
        {
            string key = NormalizeKey(directory);
            if (blocked)
            {
                _blocked[key] = true;
            }
            else
            {
                _blocked.TryRemove(key, out _);
            }
        }
    }
}
