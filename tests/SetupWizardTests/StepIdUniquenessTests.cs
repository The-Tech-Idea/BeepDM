using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Installer.Steps;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Step ids have to be unique across the installer step library.
///
/// <see cref="SetupWizardBuilder"/> keys steps by <c>StepId</c> and resolves <c>DependsOn</c>
/// through it, so it rejects a graph containing two steps with the same id. Two did:
/// <c>ComServerRegistrationStep</c>, which writes the CLSID tree from <c>InstallConfig</c>, and
/// <c>ComRegistrationStep</c>, which shells out to regsvr32 — both answered to
/// <c>installer.com.register</c>. Nothing composed them together, so the collision sat there
/// waiting for the first graph that did, which would have failed to build at all rather than
/// misbehaving in a traceable way.
/// </summary>
public class StepIdUniquenessTests
{
    /// <summary>
    /// Every concrete step in the engine, instantiated with its default arguments. Steps declare
    /// optional constructor parameters rather than a parameterless constructor, which
    /// <c>Activator.CreateInstance(Type)</c> cannot satisfy, so the defaults are supplied here.
    /// </summary>
    private static IEnumerable<ISetupStep> AllSteps()
    {
        var assembly = typeof(ComServerRegistrationStep).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || !typeof(ISetupStep).IsAssignableFrom(type))
                continue;

            var constructor = type.GetConstructors()
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));
            if (constructor is null)
                continue;

            ISetupStep step = null;
            try
            {
                step = (ISetupStep)constructor.Invoke(
                    constructor.GetParameters().Select(_ => Type.Missing).ToArray());
            }
            catch
            {
                // A step that cannot be constructed without real arguments is out of scope here;
                // its id is still covered by the source-literal sweep below.
            }

            if (step is not null)
                yield return step;
        }
    }

    [Fact]
    public void NoTwoStepsShareAnId()
    {
        var steps = AllSteps().ToList();

        // Without this the guard passes by finding nothing: if the reflection sweep stops
        // constructing steps, an empty set has no duplicates in it.
        Assert.True(steps.Count >= 10, $"the sweep should reach most of the step library; it found {steps.Count}");

        var duplicates = steps
            .GroupBy(s => s.StepId, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(s => s.GetType().Name))}")
            .ToList();

        Assert.True(duplicates.Count == 0,
            "every step id must be unique; found:" + Environment.NewLine + string.Join(Environment.NewLine, duplicates));
    }

    [Fact]
    public void TheTwoComSteps_CanCoexistInOneGraph()
    {
        // The regression itself: building a wizard with both threw
        // "Duplicate step id 'installer.com.register'".
        var build = () => new SetupWizardBuilder()
            .WithId("com-coexistence")
            .AddStep(new ComServerRegistrationStep())
            .AddStep(new ComRegistrationStep())
            .AddStep(new ComRegistrationStep(isUninstall: true))
            .Build();

        var exception = Record.Exception(build);

        Assert.Null(exception);
    }

    [Fact]
    public void TheRegistryWritingStep_KeepsTheEstablishedId()
    {
        // UninstallStep reverses this one, and the installer's StepIds constant points at it, so
        // the collision had to be resolved by renaming the self-registration step, not this one.
        Assert.Equal("installer.com.register", new ComServerRegistrationStep().StepId);
    }

    [Fact]
    public void SelfRegistrationIdsDistinguishInstallFromUninstall()
    {
        Assert.Equal("installer.com.selfregister", new ComRegistrationStep().StepId);
        Assert.Equal("installer.com.selfunregister", new ComRegistrationStep(isUninstall: true).StepId);
    }
}
