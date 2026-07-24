using System.Collections.Generic;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// The content-addressed payload manifest (<c>_payload-manifest.json</c>) — the same shape the
    /// installer's solid packager writes: every file path mapped to the SHA-256 of its content
    /// (<see cref="PayloadEntry.Blob"/>). Because identical content shares a blob, a delta update
    /// diffs blob hashes rather than files, so a moved or duplicated file costs no download.
    /// </summary>
    public sealed class PayloadManifest
    {
        public bool Solid { get; set; } = true;
        public List<PayloadEntry> Entries { get; set; } = new();
    }

    /// <summary>One installed file: its relative path, the blob (SHA-256) holding its content, and size.</summary>
    public sealed class PayloadEntry
    {
        public string Path { get; set; } = "";
        public string Blob { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>A unique blob a delta update must download, with its size for byte accounting.</summary>
    public sealed class BlobFetch
    {
        public string Hash { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>
    /// The output of <c>DeltaPlanner</c>: exactly which blobs to download, which files to write or
    /// delete to converge the install on the new manifest, and the byte cost versus a full install.
    /// Pure data — computing it touches no disk and no network.
    /// </summary>
    public sealed class DeltaPlan
    {
        /// <summary>True when there is no usable local manifest (fresh install, or a forced-full below minSupportedVersion) — every blob is fetched.</summary>
        public bool IsFullInstall { get; set; }

        /// <summary>Unique blobs not already present locally.</summary>
        public List<BlobFetch> BlobsToFetch { get; set; } = new();

        /// <summary>Files whose path is new or whose content (blob) changed.</summary>
        public List<PayloadEntry> FilesToWrite { get; set; } = new();

        /// <summary>Installed files absent from the new manifest — removed to converge.</summary>
        public List<string> FilesToDelete { get; set; } = new();

        /// <summary>Total bytes the delta actually downloads (sum of <see cref="BlobsToFetch"/>).</summary>
        public long DownloadBytes { get; set; }

        /// <summary>Total unique-blob bytes a full install would download — the baseline the delta is measured against.</summary>
        public long FullBytes { get; set; }

        /// <summary>Bytes saved versus taking the full install (never negative).</summary>
        public long SavingsBytes => System.Math.Max(0, FullBytes - DownloadBytes);
    }
}
