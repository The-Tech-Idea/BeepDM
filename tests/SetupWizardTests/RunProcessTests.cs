using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TheTechIdea.Beep.Installer;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// The shared child-process runner every install step now goes through.
///
/// Each launch site used to grow its own version and each got some part of it wrong: reading a
/// redirected stream to the end before waiting (which made the timeout unreachable), redirecting a
/// stream and never draining it, reading <c>ExitCode</c> after a wait that may have timed out
/// (which throws on a live process), and never killing a child that overran. Those are all
/// answered here once.
/// </summary>
public class RunProcessTests : IDisposable
{
    private readonly string _root;

    public RunProcessTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"BeepRunProcess_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* temp dir, best effort */ }
    }

    private string Script(string name, string body)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, "@echo off\r\n" + body + "\r\n");
        return path;
    }

    private static ProcessStartInfo Redirected(string file) => new(file)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    [Fact]
    public void ItCapturesBothStreamsAndTheExitCode()
    {
        var script = Script("both.cmd", "echo to stdout\r\necho to stderr 1>&2\r\nexit /b 7");

        var run = InstallHelpers.RunProcess(Redirected(script), 30_000);

        Assert.True(run.Started);
        Assert.False(run.TimedOut);
        Assert.Equal(7, run.ExitCode);
        Assert.Contains("to stdout", run.StandardOutput);
        Assert.Contains("to stderr", run.StandardError);
        Assert.False(run.Succeeded, "a non-zero exit code is not success");
    }

    [Fact]
    public void AChildThatOverrunsIsKilled_AndTheCallerIsTold()
    {
        var script = Script("slow.cmd", "ping -n 30 127.0.0.1");
        var clock = Stopwatch.StartNew();

        var run = InstallHelpers.RunProcess(Redirected(script), 2_000);
        clock.Stop();

        Assert.True(run.Started);
        Assert.True(run.TimedOut);
        Assert.False(run.Succeeded);
        Assert.Contains("did not finish", run.Error);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(20),
            $"the timeout has to bound the wait; took {clock.Elapsed.TotalSeconds:F1}s");
    }

    [Fact]
    public void AFloodOnOneStreamDoesNotBlockTheOther()
    {
        // Enough stderr to overrun the pipe buffer. Draining only stdout deadlocks here.
        var script = Script("flood.cmd", "for /L %%i in (1,1,2000) do @echo noisy line %%i 1>&2");

        var completed = Task.Run(() => InstallHelpers.RunProcess(Redirected(script), 60_000)).Wait(60_000);

        Assert.True(completed, "both pipes have to be drained concurrently or this never returns");
    }

    [Fact]
    public void AMissingExecutableIsReported_NotThrown()
    {
        // A step decides what a failure means; the runner never throws at it.
        var run = InstallHelpers.RunProcess(new ProcessStartInfo("no-such-program-b7f3a1")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        }, 5_000);

        Assert.False(run.Started);
        Assert.False(run.Succeeded);
        Assert.NotEmpty(run.Error);
    }

    [Fact]
    public void NoStartInfoIsReported_NotThrown()
    {
        var run = InstallHelpers.RunProcess(null, 5_000);

        Assert.False(run.Started);
        Assert.NotEmpty(run.Error);
    }

    [Fact]
    public void AnUnredirectedRunStillReportsItsExitCode()
    {
        // UseShellExecute rules out redirection; elevated prerequisite installers run this way.
        var script = Script("plain.cmd", "exit /b 0");

        var run = InstallHelpers.RunProcess(new ProcessStartInfo(script) { UseShellExecute = false, CreateNoWindow = true }, 30_000);

        Assert.True(run.Succeeded);
        Assert.Equal("", run.StandardOutput);
    }
}
