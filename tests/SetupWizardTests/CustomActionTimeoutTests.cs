using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TheTechIdea.Beep.Installer.Steps;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// A custom action must not be able to hang the installer.
///
/// <c>CustomActionStep</c> used to call <c>StandardOutput.ReadToEnd()</c> before
/// <c>WaitForExit(timeout)</c>. ReadToEnd blocks until the child closes the pipe and has no timeout
/// of its own, so the timeout below it was unreachable: an action that never exited blocked the
/// install forever, on a customer's machine, mid-install. An action that filled the stderr buffer
/// while the step sat on stdout deadlocked the pair outright.
///
/// Every test here bounds its own wait, so a regression fails the run instead of hanging it.
/// </summary>
public class CustomActionTimeoutTests : IDisposable
{
    private readonly string _root;

    public CustomActionTimeoutTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"BeepActionTimeout_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* temp dir, best effort */ }
    }

    private string WriteScript(string name, string body)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, "@echo off\r\n" + body + "\r\n");
        return path;
    }

    private static SetupContext ContextFor(string script, int timeoutMs)
    {
        var context = new SetupContext();
        context.Properties["CustomActions"] = new List<CustomAction>
        {
            new()
            {
                Path = script,
                Timing = CustomActionTiming.AfterInstall,
                Description = "long running action",
                TimeoutMs = timeoutMs,
                Required = true,
                FailOnError = true
            }
        };
        return context;
    }

    /// <summary>Runs the step off-thread so a regression cannot wedge the whole test run.</summary>
    private static (bool Completed, IErrorsInfo Result, TimeSpan Elapsed) RunBounded(SetupContext context, int waitMs)
    {
        IErrorsInfo result = null;
        var clock = Stopwatch.StartNew();
        var completed = Task.Run(() => result = new CustomActionStep(CustomActionTiming.AfterInstall).Execute(context))
            .Wait(waitMs);
        clock.Stop();
        return (completed, result, clock.Elapsed);
    }

    [Fact]
    public void AnActionThatNeverExits_IsKilledAtItsTimeout()
    {
        // ~30s of work against a 2s timeout: the step has to give up, not wait it out.
        var script = WriteScript("hang.cmd", "ping -n 30 127.0.0.1");
        var run = RunBounded(ContextFor(script, timeoutMs: 2_000), waitMs: 20_000);

        Assert.True(run.Completed,
            "the step must return; before the fix ReadToEnd blocked with no timeout and this never came back");
        Assert.True(run.Elapsed < TimeSpan.FromSeconds(15),
            $"the timeout should bound the wait; took {run.Elapsed.TotalSeconds:F1}s");
        Assert.Equal(ConfigUtil.Errors.Failed, run.Result.Flag);
        Assert.Contains("timed out", run.Result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnActionThatFloodsStderr_DoesNotDeadlockThePipes()
    {
        // Enough stderr to overrun the pipe buffer while the step is reading stdout.
        var script = WriteScript("noisy.cmd", "for /L %%i in (1,1,2000) do @echo staging line %%i 1>&2");
        var run = RunBounded(ContextFor(script, timeoutMs: 60_000), waitMs: 60_000);

        Assert.True(run.Completed, "draining both pipes concurrently is what stops this deadlocking");
        Assert.Equal(ConfigUtil.Errors.Ok, run.Result.Flag);
    }

    [Fact]
    public void AWellBehavedAction_StillSucceedsAndItsOutputIsCaptured()
    {
        var script = WriteScript("quiet.cmd", "echo done");
        var context = ContextFor(script, timeoutMs: 30_000);

        var run = RunBounded(context, waitMs: 30_000);

        Assert.True(run.Completed);
        Assert.Equal(ConfigUtil.Errors.Ok, run.Result.Flag);
        Assert.Contains("done", (string)context.Properties["ActionOutput_long running action"]);
    }
}
