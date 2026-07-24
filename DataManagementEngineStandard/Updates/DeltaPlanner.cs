using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// Computes a file-level delta from the installed payload manifest to a new one — pure, so it
    /// is fully unit-testable and touches neither disk nor network. The content-addressed store
    /// does the hard part: a file is downloaded only when its <em>blob</em> (SHA-256) is not
    /// already present locally, so a rename or a duplicate costs zero bytes.
    /// </summary>
    public static class DeltaPlanner
    {
        /// <summary>
        /// Plans the update from <paramref name="local"/> (the installed manifest, or null for a
        /// fresh / forced-full install) to <paramref name="remote"/> (the new release manifest).
        /// </summary>
        public static DeltaPlan ComputePlan(PayloadManifest remote, PayloadManifest? local)
        {
            ArgumentNullException.ThrowIfNull(remote);

            // Unique remote blobs (a blob shared by N files is downloaded once) and the full baseline.
            var remoteBlobs = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in remote.Entries)
                remoteBlobs[e.Blob] = e.Size;
            var fullBytes = remoteBlobs.Values.Sum();

            // No local manifest → full install: fetch every unique blob, write every file.
            if (local == null || local.Entries.Count == 0)
            {
                var fetchAll = remoteBlobs.Select(kv => new BlobFetch { Hash = kv.Key, Size = kv.Value }).ToList();
                return new DeltaPlan
                {
                    IsFullInstall = true,
                    BlobsToFetch = fetchAll,
                    FilesToWrite = remote.Entries.ToList(),
                    FilesToDelete = new List<string>(),
                    DownloadBytes = fetchAll.Sum(b => b.Size),
                    FullBytes = fullBytes
                };
            }

            var localByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var localBlobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in local.Entries)
            {
                localByPath[e.Path] = e.Blob;
                localBlobs.Add(e.Blob);
            }

            // Files to write: new path, or same path whose content changed.
            var filesToWrite = remote.Entries
                .Where(re => !localByPath.TryGetValue(re.Path, out var lb) || !string.Equals(lb, re.Blob, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Files to delete: installed paths the new manifest no longer contains.
            var remotePaths = new HashSet<string>(remote.Entries.Select(e => e.Path), StringComparer.OrdinalIgnoreCase);
            var filesToDelete = localByPath.Keys.Where(p => !remotePaths.Contains(p)).ToList();

            // Blobs to fetch: unique remote blobs whose content is not already on disk anywhere.
            var blobsToFetch = remoteBlobs
                .Where(kv => !localBlobs.Contains(kv.Key))
                .Select(kv => new BlobFetch { Hash = kv.Key, Size = kv.Value })
                .ToList();

            return new DeltaPlan
            {
                IsFullInstall = false,
                BlobsToFetch = blobsToFetch,
                FilesToWrite = filesToWrite,
                FilesToDelete = filesToDelete,
                DownloadBytes = blobsToFetch.Sum(b => b.Size),
                FullBytes = fullBytes
            };
        }
    }
}
