# BeepDM App Updates (`TheTechIdea.Beep.Updates`)

Self-update for any Beep-based app: check a **static feed**, apply **hash-verified delta (or full)
app updates side-by-side**, roll back, and update **feed-pinned NuGet modules** — without a
dedicated update server. The capability lives in BeepDM (decision **D12**), so an app gets it by
registering one service, regardless of how it was deployed. Beep.Installer only *publishes* the
feed (`/PUBLISHFEED`) and *provisions* `update-settings.json` at build time.

## Register

```csharp
services.AddBeepAppUpdates();                       // reads update-settings.json beside the app
// or
services.AddBeepAppUpdates(new UpdateSettings {
    FeedUrl = "https://downloads.example.com/myapp/feed.json",
    Channel = "stable",
    Mode    = UpdateMode.Optional,                  // Required blocks the app until applied
    CurrentVersion = "1.2.0",
    InstallRoot = @"C:\Program Files\MyApp",        // where app-<ver>\ live and `current` points
});
```

## Check and apply

```csharp
var updates = provider.GetRequiredService<IAppUpdateService>();

var check = await updates.CheckAsync();
if (check.AppUpdateAvailable)
    await updates.ApplyAppUpdateAsync(check, progress);   // delta when possible, full otherwise
if (check.StaleModules.Count > 0)
    await updates.ApplyModuleUpdatesAsync(check);         // feed-pinned NuGet modules
// something went wrong after a launch?
await updates.RollbackAsync();                            // flip `current` back to the previous version
```

`check` is a data snapshot: `AppUpdateAvailable`, `RequiresFullInstall` (installed version is
below `minSupportedVersion`), `StaleModules`, and `AnyUpdateAvailable`. Subscribe to
`IAppUpdateService.UpdateAvailable` to drive an Optional-mode notification.

## The feed (`feed.json`)

```json
{
  "product": "MyApp", "channel": "stable",
  "latest": {
    "version": "1.2.3", "minSupportedVersion": "1.1.0",
    "full":  { "url": "1.2.3/Setup-MyApp-1.2.3.exe", "sha256": "…" },
    "delta": { "manifestUrl": "1.2.3/_payload-manifest.json", "blobBaseUrl": "1.2.3/_blobs/" }
  },
  "modules": [
    { "id": "TheTechIdea.Beep.PostgresDriver", "version": "2.4.1", "sha256": "…", "required": false }
  ]
}
```

Every artifact is SHA-256-verified before use (D11: TLS + per-artifact hash for v1). **A version is
immutable** — never republish in place; new content ⇒ new version (the P0 stale-cache lesson).

## How it works

- **`UpdateFeedClient`** — async fetch/parse of the feed and payload manifest; downloads verified
  by hash. Malformed input raises a named `UpdateFeedException` (never a silent null).
- **`DeltaPlanner`** — pure diff of the new manifest against the installed one, over **blob
  hashes** not files, so a rename or duplicate downloads nothing. Emits blobs-to-fetch,
  files-to-write/delete, and download-vs-full bytes.
- **`SideBySideApplier`** — stages into `app-<ver>\`, verifies every blob, then flips the
  `current` junction (`IDirectoryLink`). The running version is never written to, so there is no
  locked-file problem and an interrupted apply is harmless. `Rollback`/`RetireOldVersions` manage
  the version folders.
- **`ModuleUpdater`** — governs the existing NuGet `UpdateService` (via `IModulePackageService`):
  the feed's pinned version wins over "latest", a required stale module blocks start, and each
  fetched package is hash-verified. Module updates apply on next launch (hot reload is out of
  scope in v1).

## Publishing (installer side)

```
Beep.Installer.exe /BUILD=app.bsetup /PUBLISHFEED=\\host\feed [/FEEDURL=https://…] [/REPUBLISH]
```

Writes `<feed>/<version>/` (full Setup.exe + loose `_blobs/` + `_payload-manifest.json`) and
updates `feed.json` atomically. Publishing is then "copy a folder to your host".
