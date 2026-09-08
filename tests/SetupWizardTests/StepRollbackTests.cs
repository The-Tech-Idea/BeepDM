using System;
using System.Collections.Generic;
using System.IO;
using TheTechIdea.Beep.Installer;
using TheTechIdea.Beep.Installer.Steps;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Step-level rollback for the mutating steps (2.B.2).
///
/// Only EnvironmentVariableStep declared <c>SupportsRollback</c>, so a failure part-way through an
/// install left copied files, shortcuts and COM registrations behind for the host to clean up by
/// other means. Each of these steps already recorded what it applied; they can now undo it.
///
/// The file case carries a hazard worth stating: <c>RollbackManager.RegisterFileCreated</c> deletes
/// unconditionally, and FileCopyStep registers every destination it wrote — including one that
/// overwrote a file the machine already had. Step rollback deliberately does not do that.
/// </summary>
public class StepRollbackTests : IDisposable
{
    private readonly string _root;

    public StepRollbackTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"BeepStepRollback_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* temp dir, best effort */ }
    }

    private SetupContext ContextForCopy(out string source, out string install, bool preExisting)
    {
        source = Path.Combine(_root, "src");
        install = Path.Combine(_root, "install");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(install);
        File.WriteAllText(Path.Combine(source, "new.txt"), "fresh from the payload");
        File.WriteAllText(Path.Combine(source, "existing.txt"), "payload version");

        if (preExisting)
            File.WriteAllText(Path.Combine(install, "existing.txt"), "the machine's own copy");

        var config = new InstallConfig
        {
            AppId = "b2f0a7c4-1d38-4e59-9a02-6c7d8e9f0a1b",
            ProductName = "RollbackApp",
            Components =
            {
                new InstallComponent
                {
                    Id = "core", Name = "Core", Required = true, Selected = true,
                    Files =
                    {
                        new FileCopyOperation { SourcePath = Path.Combine(source, "new.txt"), DestinationPath = "new.txt", Overwrite = true },
                        new FileCopyOperation { SourcePath = Path.Combine(source, "existing.txt"), DestinationPath = "existing.txt", Overwrite = true }
                    }
                }
            }
        };

        var context = new SetupContext();
        context.Properties["InstallConfig"] = config;
        context.Properties["InstallPath"] = install;
        return context;
    }

    [Fact]
    public void FileCopy_RollbackRemovesWhatItCreated()
    {
        var context = ContextForCopy(out _, out var install, preExisting: false);
        new FileCopyStep().Execute(context);
        Assert.True(File.Exists(Path.Combine(install, "new.txt")));

        var result = new FileCopyStep().RollbackAsync(context).GetAwaiter().GetResult();

        Assert.Equal(ConfigUtil.Errors.Ok, result.Flag);
        Assert.False(File.Exists(Path.Combine(install, "new.txt")));
    }

    [Fact]
    public void FileCopy_RollbackLeavesAFileItMerelyOverwrote()
    {
        // The machine had this file before the install. Deleting it on rollback would take away
        // something the installer never owned; restoring its contents is the upgrade backup's job.
        var context = ContextForCopy(out _, out var install, preExisting: true);
        new FileCopyStep().Execute(context);

        new FileCopyStep().RollbackAsync(context).GetAwaiter().GetResult();

        Assert.False(File.Exists(Path.Combine(install, "new.txt")), "the step created this one");
        Assert.True(File.Exists(Path.Combine(install, "existing.txt")), "the step only overwrote this one");
    }

    [Fact]
    public void FileCopy_RollbackIsIdempotent()
    {
        var context = ContextForCopy(out _, out _, preExisting: false);
        new FileCopyStep().Execute(context);
        var step = new FileCopyStep();

        step.RollbackAsync(context).GetAwaiter().GetResult();
        var second = step.RollbackAsync(context).GetAwaiter().GetResult();

        Assert.Equal(ConfigUtil.Errors.Ok, second.Flag);
        Assert.Contains("No copied files", second.Message);
    }

    [Fact]
    public void FileCopy_DeclaresThatItRollsBack()
    {
        Assert.True(new FileCopyStep().SupportsRollback);
        Assert.True(new ShortcutCreateStep().SupportsRollback);
        Assert.True(new ComServerRegistrationStep().SupportsRollback);
    }

    [Fact]
    public void Shortcut_RollbackWithNothingRecorded_IsAQuietNoOp()
    {
        // The interesting failure is a rollback that throws when the step never ran, which is
        // exactly when a host calls it.
        var context = new SetupContext();
        context.Properties["InstallConfig"] = new InstallConfig { ProductName = "RollbackApp" };

        var result = new ShortcutCreateStep().RollbackAsync(context).GetAwaiter().GetResult();

        Assert.Equal(ConfigUtil.Errors.Ok, result.Flag);
        Assert.Contains("No shortcuts", result.Message);
    }

    [Fact]
    public void Com_RollbackWithNothingRecorded_IsAQuietNoOp()
    {
        var context = new SetupContext();
        context.Properties["InstallConfig"] = new InstallConfig { ProductName = "RollbackApp" };

        var result = new ComServerRegistrationStep().RollbackAsync(context).GetAwaiter().GetResult();

        Assert.Equal(ConfigUtil.Errors.Ok, result.Flag);
        Assert.Contains("No COM registrations", result.Message);
    }

    [Fact]
    public void Shortcut_RollbackRemovesTheShortcutsItRecorded()
    {
        var install = Path.Combine(_root, "sc-install");
        Directory.CreateDirectory(install);
        var target = Path.Combine(install, "App.exe");
        File.WriteAllText(target, "app");

        var config = new InstallConfig
        {
            ProductName = "RollbackApp",
            StartMenuFolder = "RollbackApp",
            Shortcuts = { new ShortcutDefinition { Name = "RollbackApp", TargetPath = "App.exe" } }
        };

        var context = new SetupContext();
        context.Properties["InstallConfig"] = config;
        context.Properties["InstallPath"] = install;
        context.Properties["PerUser"] = true;

        new ShortcutCreateStep().Execute(context);
        var created = context.TryGetProperty<List<ShortcutDefinition>>("ShortcutsCreated");

        var result = new ShortcutCreateStep().RollbackAsync(context).GetAwaiter().GetResult();

        Assert.Equal(ConfigUtil.Errors.Ok, result.Flag);
        // Whatever it created, it removed; and the recorded list is cleared either way.
        Assert.Empty(context.TryGetProperty<List<ShortcutDefinition>>("ShortcutsCreated"));
        if (created is { Count: > 0 })
        {
            var linkPath = ShortcutPathResolver.Resolve(created[0], config, perUser: true);
            Assert.False(File.Exists(linkPath));
        }
    }
}
