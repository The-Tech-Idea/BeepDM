# Installer Service — Competitor Matrix

> Notes on what we borrow from each commercial / open-source installer
> product, and what we deliberately leave out. Not a contract; this is a
> reference for design discussions.

---

## Commercial-grade products (what we're benchmarking against)

### InstallShield (Flexera)

**What we borrow:**
- Multi-page wizard UX with a step progress strip at the top.
- Prerequisite chain (.NET runtime, Visual C++ Redistributable, etc.)
  declared in a single manifest and checked before install.
- Branded visuals (banner image, EULA, icon) loaded from a per-app
  resource folder.
- InstallScript for custom install steps (e.g. "register a Windows
  service") with a clear `BeforeInstall`, `AfterInstall`, `OnRollback`
  lifecycle.
- A single MSI + setup.exe bootstrapper pattern.

**What we leave out:**
- MSI authoring (too verbose, and the .NET self-contained publish
  gives us 80% of the benefits with 5% of the complexity).
- The InstallShield IDE — we don't want a designer-driven tool; we want
  a code-driven, version-controlled, testable install experience.
- Dimwood / InstallScript — we use C# throughout.

---

### WiX (open source, .NET-based MSI authoring)

**What we borrow:**
- MSI authoring patterns: per-component, per-feature, per-property.
- The "major upgrade" pattern (detect the previous version, uninstall
  it cleanly, install the new one).
- The "harvest" pattern: WiX can auto-discover the files in a directory
  and emit an MSI; we use a similar approach in our build pipeline
  (the project ships a `files.json` that lists every file the installer
  should install).
- The rollback pattern: if the install fails mid-way, the system
  restores the previous state.

**What we leave out:**
- The XML-based MSI authoring (we use C# throughout).
- The WiX toolchain dependency.
- Per-machine vs per-user install complexity (we ship per-user only
  for the first version; per-machine can come later).

---

### Advanced Installer

**What we borrow:**
- Auto-update protocol design: a JSON feed with `version`, `downloadUrl`,
  `sha256`, `releaseNotes`, etc.
- The "check for updates on launch + on schedule" pattern.
- The "install on next launch" UX (defer the update).
- The "delta update" concept (download only the changed files between
  versions). Note: we don't implement delta updates in v1; full
  downloads only.

**What we leave out:**
- The proprietary `.exe` build toolchain.
- The license management / activation server.
- The "patch" file format (`.msp`).

---

### ClickOnce (Microsoft)

**What we borrow:**
- Per-user install simplicity (no admin required).
- The "check for updates" hook in the app's startup.
- The "install on next launch" deferral UX.
- The "trust the publisher" model: a code-signing certificate is the
  trust anchor.

**What we leave out:**
- The ClickOnce manifest format (we use a JSON feed).
- The "application is installed in the user's AppData" complexity
  (we use a per-app directory under `%LocalAppData%\Beep\<AppName>`).
- The .NET Framework dependency (we target .NET 10 self-contained).

---

### Squirrel.Windows (open source)

**What we borrow:**
- The "delta update" concept (we don't implement it in v1 but the
  architecture supports it — the new build is staged in a side-by-side
  directory).
- The "atomic replace" pattern: the running app keeps running while the
  new build is staged; the replace happens on next launch.
- The "RELEASES" file format (a simple text file listing every version
  and its delta). We use a JSON variant.

**What we leave out:**
- The NuGet package format (we use a plain `.zip` or `.nupkg`).
- The PowerShell-based update hook (we use C# throughout).
- The C++ delta-update implementation.

---

### Microsoft Visual Studio Installer (VSIX installer)

**What we borrow:**
- The bootstrapper pattern: a tiny launcher downloads the actual
  installer. Useful for very small downloads (e.g. < 1MB launcher +
  on-demand payload).
- The prerequisite chain: the bootstrapper checks for `.NET`, `VC++`,
  etc. and downloads them on demand.

**What we leave out:**
- The VSIX manifest format.
- The Visual Studio Marketplace integration.

---

### Inno Setup (open source, Pascal-based)

**What we borrow:**
- The single-EXE deployment (we ship a single `setup.exe` that
  self-extracts).
- The scriptable install steps (PascalScript for Inno; C# for us).
- The "small footprint" ethos: the installer is < 5MB.

**What we leave out:**
- The Pascal-based scripting.
- The PascalScript custom-step language.

---

### NSIS (open source, C-based)

**What we borrow:**
- The scriptable install steps (we use C# instead of NSIS's scripting
  language).
- The "small footprint" ethos.
- The "no runtime required" goal (we self-contain .NET 10).

**What we leave out:**
- The C-based scripting.
- The NSIS plug-in architecture.

---

## The principles we follow (synthesised from the above)

1. **Per-user install by default** (ClickOnce model). No admin
   privileges required. Per-machine can come later.
2. **JSON feed for updates** (Advanced Installer model). Simple,
   version-controlled, easy to host on a CDN.
3. **Side-by-side staging + atomic replace** (Squirrel model). The
   running app keeps running while the new build is staged.
4. **Code-signing certificate is the trust anchor** (ClickOnce model).
   Verify the signature before installing.
5. **Rollback on failure** (WiX model). The previous build keeps
   running if anything fails.
6. **Branded visuals, multi-page wizard, step progress strip**
   (InstallShield model). Looks like a commercial product.
7. **Opt-in telemetry** (Sentry / Application Insights model). No
   surprises for the user.
8. **C# throughout** (no Pascal, no PowerShell, no XML-based MSI).
   The installer is a .NET app that hosts the engine in-process.
9. **Self-contained publish** (.NET 10 model). The installer does not
   require the user to install .NET separately.
10. **Single MSI / EXE deploy** (InstallShield / Inno model). The
    user's first install is a single double-click.

## What we DON'T do (deliberate omissions)

- **No app-store distribution.** We don't target the Microsoft Store,
  Apple App Store, or Google Play. Those are separate concerns handled
  by the MAUI build pipeline.
- **No enterprise software distribution system integration** (SCCM,
  Intune, Jamf, etc.) in v1. The silent-install command-line
  (Phase 8) is the integration point; a future phase can add a
  wrapper for each system.
- **No patching system** (Windows Update, WSUS). The app is updated
  via the in-app updater, not the OS.
- **No driver installation.** The app does not install kernel-mode
  drivers. If a future feature needs that, it's a separate concern.
- **No COM / ActiveX registration.** The app is .NET-native; no COM
  surface in v1.
