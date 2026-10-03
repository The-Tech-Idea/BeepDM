using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Helpers
{
    // The public DataSources list remains a compatibility view. External mutation
    // must be serialized by its caller; framework lifecycle paths use this registry.
    internal static class EditorDataSourceRegistry
    {
        private sealed class Entry
        {
            internal string Name;
            internal string Guid;
            internal IDataSource Source;
            internal readonly object OperationGate = new();
            internal readonly TaskCompletionSource<IDataSource> Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal bool Released;
            internal int OperationDepth;
            internal bool ReleaseRequested;
        }

        private sealed class State
        {
            internal readonly object Gate = new();
            internal readonly Dictionary<string, Entry> Entries = new(StringComparer.OrdinalIgnoreCase);
            internal bool Stopped;
        }

        private sealed class Ownership
        {
            internal readonly WeakReference<IDMEEditor> Editor;
            internal readonly Entry Entry;
            internal Ownership(IDMEEditor editor, Entry entry) { Editor = new(editor); Entry = entry; }
        }

        private sealed record CreationScope(Entry Entry, CreationScope Parent);
        private static readonly AsyncLocal<CreationScope> Creating = new();

        private static readonly ConditionalWeakTable<IDMEEditor, State> States = new();
        private static readonly ConditionalWeakTable<IDataSource, Ownership> Owners = new();

        private static string Key(string name, string guid) =>
            string.IsNullOrEmpty(guid) ? "N:" + name : "G:" + guid;

        private static State For(IDMEEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);
            return States.GetValue(editor, _ => new State());
        }

        private static Entry FindEntry(State state, string identifier, bool useGuid) =>
            state.Entries.Values.FirstOrDefault(entry => string.Equals(
                useGuid ? entry.Guid : entry.Name, identifier, StringComparison.OrdinalIgnoreCase));

        private static Entry Adopt(IDMEEditor editor, State state, IDataSource source)
        {
            if (Owners.TryGetValue(source, out var owner) &&
                (!owner.Editor.TryGetTarget(out var original) || !ReferenceEquals(original, editor) || owner.Entry.Released))
                throw new InvalidOperationException("A datasource cannot be shared with another editor or reused after release.");
            var entry = state.Entries.Values.FirstOrDefault(candidate => ReferenceEquals(candidate.Source, source));
            if (entry != null) return entry;
            entry = new Entry { Name = source.DatasourceName, Guid = source.GuidID, Source = source };
            var key = Key(entry.Name, entry.Guid);
            while (state.Entries.ContainsKey(key)) key += ":duplicate";
            state.Entries[key] = entry;
            Owners.GetValue(source, _ => new Ownership(editor, entry));
            entry.Ready.TrySetResult(source);
            return entry;
        }

        private static Entry FindOwned(IDMEEditor editor, State state, string identifier, bool useGuid)
        {
            var entry = FindEntry(state, identifier, useGuid);
            if (entry != null) return entry;
            var source = editor.DataSources?.FirstOrDefault(candidate => candidate != null && string.Equals(
                useGuid ? candidate.GuidID : candidate.DatasourceName, identifier, StringComparison.OrdinalIgnoreCase));
            return source == null ? null : Adopt(editor, state, source);
        }

        internal static IDataSource Find(IDMEEditor editor, string identifier, bool useGuid = false)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            var state = For(editor);
            lock (state.Gate)
            {
                ObjectDisposedException.ThrowIf(state.Stopped, editor);
                return FindOwned(editor, state, identifier, useGuid)?.Source;
            }
        }

        internal static Task<IDataSource> GetOrCreateAsync(IDMEEditor editor, ConnectionProperties connection,
            Func<Task<IDataSource>> factory)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(factory);
            if (string.IsNullOrEmpty(connection.ConnectionName))
                throw new ArgumentException("A datasource requires a connection name.", nameof(connection));
            var state = For(editor);
            Entry entry;
            var key = Key(connection.ConnectionName, connection.GuidID);
            lock (state.Gate)
            {
                ObjectDisposedException.ThrowIf(state.Stopped, editor);
                var existing = FindOwned(editor, state, connection.ConnectionName, false);
                if (existing != null) return AwaitCreation(existing);
                if (state.Entries.TryGetValue(key, out entry)) return AwaitCreation(entry);
                entry = new Entry { Name = connection.ConnectionName, Guid = connection.GuidID };
                state.Entries.Add(key, entry);
            }
            // Never hold a registry lock while invoking plugin constructors or awaiting them.
            _ = CompleteCreationAsync(editor, state, key, entry, factory);
            return entry.Ready.Task;
        }

        private static Task<IDataSource> AwaitCreation(Entry entry)
        {
            for (var context = Creating.Value; context != null; context = context.Parent)
                if (ReferenceEquals(context.Entry, entry))
                    throw new InvalidOperationException("A datasource constructor cannot recursively wait for its own lifecycle entry.");
            return entry.Ready.Task;
        }

        private static async Task CompleteCreationAsync(IDMEEditor editor, State state, string key,
            Entry entry, Func<Task<IDataSource>> factory)
        {
            IDataSource source = null;
            var previous = Creating.Value;
            Creating.Value = new CreationScope(entry, previous);
            try
            {
                source = await factory().ConfigureAwait(false);
                lock (state.Gate)
                {
                    if (!state.Stopped && state.Entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                    {
                        if (source == null) state.Entries.Remove(key);
                        else
                        {
                            if (Owners.TryGetValue(source, out var owner) &&
                                (!owner.Editor.TryGetTarget(out var original) || !ReferenceEquals(original, editor) || !ReferenceEquals(owner.Entry, entry)))
                            {
                                source = null;
                                throw new InvalidOperationException("The factory returned a datasource already owned by a different lifecycle entry.");
                            }
                            entry.Source = source;
                            entry.Name = source.DatasourceName;
                            entry.Guid = source.GuidID;
                            editor.DataSources ??= new List<IDataSource>();
                            if (!editor.DataSources.Any(candidate => ReferenceEquals(candidate, source))) editor.DataSources.Add(source);
                            Owners.GetValue(source, _ => new Ownership(editor, entry));
                        }
                        entry.Ready.TrySetResult(source);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Report(editor, "Datasource creation failed", ex);
                lock (state.Gate)
                {
                    if (state.Entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry)) state.Entries.Remove(key);
                }
            }
            finally { Creating.Value = previous; }
            // Removal/disposal invalidates the generation even when a constructor cannot be cancelled.
            Release(editor, entry, source);
            entry.Ready.TrySetResult(null);
        }

        internal static bool Close(IDMEEditor editor, IDataSource source)
            => Execute(editor, source, value => value.Dataconnection?.CloseConn() == ConnectionState.Closed, false);

        internal static ConnectionState Open(IDMEEditor editor, IDataSource source)
            => Execute(editor, source, value => value.Openconnection(), ConnectionState.Broken);

        private static T Execute<T>(IDMEEditor editor, IDataSource source, Func<IDataSource, T> operation, T unavailable)
        {
            if (source == null) return unavailable;
            var state = For(editor);
            Entry entry;
            lock (state.Gate)
            {
                if (state.Stopped) return unavailable;
                if (Owners.TryGetValue(source, out var owner))
                {
                    if (!owner.Editor.TryGetTarget(out var original) || !ReferenceEquals(original, editor) ||
                        !state.Entries.Values.Contains(owner.Entry)) return unavailable;
                    entry = owner.Entry;
                }
                else entry = Adopt(editor, state, source);
            }
            lock (entry.OperationGate)
            {
                lock (state.Gate)
                    if (state.Stopped || entry.Released || !state.Entries.Values.Contains(entry)) return unavailable;
                entry.OperationDepth++;
                try { return operation(source); }
                finally
                {
                    entry.OperationDepth--;
                    if (entry.OperationDepth == 0 && entry.ReleaseRequested) Release(editor, entry, source);
                }
            }
        }

        internal static bool Remove(IDMEEditor editor, string identifier, bool useGuid, Action<IDataSource> onRemoved = null)
            => Remove(editor, identifier, useGuid, onRemoved, null);

        private static bool Remove(IDMEEditor editor, string identifier, bool useGuid, Action<IDataSource> onRemoved, IDataSource expected)
        {
            if (string.IsNullOrEmpty(identifier)) return false;
            var state = For(editor);
            Entry entry;
            lock (state.Gate)
            {
                if (state.Stopped) return false;
                entry = FindOwned(editor, state, identifier, useGuid);
                if (entry == null) return false;
                if (expected != null && !ReferenceEquals(entry.Source, expected)) return false;
                foreach (var key in state.Entries.Where(pair => ReferenceEquals(pair.Value, entry)).Select(pair => pair.Key).ToArray()) state.Entries.Remove(key);
                editor.DataSources?.RemoveAll(candidate => ReferenceEquals(candidate, entry.Source));
                entry.Ready.TrySetResult(null);
            }
            lock (entry.OperationGate)
            {
                var success = true;
                try { if (entry.Source != null) onRemoved?.Invoke(entry.Source); }
                finally { success = Release(editor, entry, entry.Source); }
                return success;
            }
        }

        internal static List<IDataSource> Snapshot(IDMEEditor editor)
        {
            var state = For(editor);
            lock (state.Gate)
            {
                ObjectDisposedException.ThrowIf(state.Stopped, editor);
                return editor.DataSources?.Where(source => source != null).Distinct(ReferenceEqualityComparer.Instance).Cast<IDataSource>().ToList()
                    ?? new List<IDataSource>();
            }
        }

        internal static void Stop(IDMEEditor editor)
        {
            var state = For(editor);
            Entry[] entries;
            lock (state.Gate)
            {
                if (state.Stopped) return;
                foreach (var source in editor.DataSources?.ToArray() ?? Array.Empty<IDataSource>())
                {
                    if (source == null) continue;
                    if (state.Entries.Values.Any(entry => ReferenceEquals(entry.Source, source))) continue;
                    if (Owners.TryGetValue(source, out var owner) &&
                        (!owner.Editor.TryGetTarget(out var original) || !ReferenceEquals(original, editor) || owner.Entry.Released))
                    {
                        Report(editor, "Invalid datasource ownership in legacy list", new InvalidOperationException("Source is foreign or already released."));
                        continue;
                    }
                    // Shutdown must not depend on provider name/GUID getters still working.
                    var entry = new Entry { Source = source };
                    state.Entries.Add("shutdown:" + Guid.NewGuid().ToString("N"), entry);
                    Owners.GetValue(source, _ => new Ownership(editor, entry));
                }
                state.Stopped = true;
                entries = state.Entries.Values.Distinct().ToArray();
                state.Entries.Clear();
                editor.DataSources?.Clear();
                foreach (var entry in entries) entry.Ready.TrySetResult(null);
            }
            foreach (var entry in entries) Release(editor, entry, entry.Source);
        }

        internal static bool ReleaseOwned(IDataSource source)
        {
            if (source == null || !Owners.TryGetValue(source, out var owner) || !owner.Editor.TryGetTarget(out var editor)) return false;
            var state = For(editor);
            lock (state.Gate)
                if (state.Stopped) return true;
            Remove(editor, source.DatasourceName, false, null, source);
            return true;
        }

        private static bool Release(IDMEEditor editor, Entry entry, IDataSource source)
        {
            if (source == null) return true;
            lock (entry.OperationGate)
            {
                if (entry.Released) return true;
                // A provider can synchronously request removal from an open/close
                // callback. Do not dispose it while its own method is on the stack.
                if (entry.OperationDepth != 0) { entry.ReleaseRequested = true; return true; }
                entry.Released = true;
                var success = true;
                try
                {
                    if (source.ConnectionStatus == ConnectionState.Open && source.Closeconnection() != ConnectionState.Closed) success = false;
                }
                catch (Exception ex) { success = false; Report(editor, "Datasource close during cleanup failed", ex); }
                try { source.Dispose(); }
                catch (Exception ex) { success = false; Report(editor, "Datasource disposal failed", ex); }
                return success;
            }
        }

        internal static void Report(IDMEEditor editor, string context, Exception error)
        {
            try { editor?.Logger?.WriteLog($"{context}: {error.Message}"); }
            catch (Exception logError)
            {
                // Cleanup must proceed even if the host logger is already disposed.
                System.Diagnostics.Debug.WriteLine($"{context}: {error}; logging failed: {logError}");
            }
        }
    }
}
