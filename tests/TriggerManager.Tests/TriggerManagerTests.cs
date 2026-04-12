using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using Xunit;

namespace TriggerManager.Tests;

public class TriggerManagerTests
{
    [Fact]
    public async Task RegisterBlockTriggerAsync_AndFireBlockTriggerAsync_RaisesExecutionEvents()
    {
        var manager = new TheTechIdea.Beep.Editor.Forms.Helpers.TriggerManager();
        TriggerExecutedEventArgs? executedArgs = null;
        TriggerChainCompletedEventArgs? chainArgs = null;
        var callCount = 0;

        manager.TriggerExecuted += (_, args) => executedArgs = args;
        manager.TriggerChainCompleted += (_, args) => chainArgs = args;

        manager.RegisterBlockTriggerAsync(
            TriggerType.PreInsert,
            "Customers",
            (_, _) =>
            {
                callCount++;
                return Task.FromResult(TriggerResult.Success);
            });

        manager.TriggerCount.Should().Be(1);
        manager.HasBlockTrigger(TriggerType.PreInsert, "Customers").Should().BeTrue();
        manager.GetBlockTriggers(TriggerType.PreInsert, "Customers").Should().ContainSingle();

        var result = await manager.FireBlockTriggerAsync(TriggerType.PreInsert, "Customers");

        result.Should().Be(TriggerResult.Success);
        callCount.Should().Be(1);
        executedArgs.Should().NotBeNull();
        executedArgs!.Trigger.BlockName.Should().Be("Customers");
        executedArgs.Result.Should().Be(TriggerResult.Success);
        executedArgs.Context.BlockName.Should().Be("Customers");
        executedArgs.DurationMs.Should().BeGreaterThanOrEqualTo(0);
        chainArgs.Should().NotBeNull();
        chainArgs!.TriggerCount.Should().Be(1);
        chainArgs.SuccessCount.Should().Be(1);
        chainArgs.FailureCount.Should().Be(0);
        chainArgs.SkippedCount.Should().Be(0);
        chainArgs.OverallResult.Should().Be(TriggerResult.Success);
    }

    [Fact]
    public void FireBlockTrigger_ExecutesHandlersInPriorityOrder()
    {
        var manager = new TheTechIdea.Beep.Editor.Forms.Helpers.TriggerManager();
        var executionOrder = new List<string>();

        manager.RegisterBlockTrigger(TriggerType.PreUpdate, "Customers", _ =>
        {
            executionOrder.Add("low");
            return TriggerResult.Success;
        }, TriggerPriority.Low);

        manager.RegisterBlockTrigger(TriggerType.PreUpdate, "Customers", _ =>
        {
            executionOrder.Add("highest");
            return TriggerResult.Success;
        }, TriggerPriority.Highest);

        manager.RegisterBlockTrigger(TriggerType.PreUpdate, "Customers", _ =>
        {
            executionOrder.Add("normal");
            return TriggerResult.Success;
        }, TriggerPriority.Normal);

        var result = manager.FireBlockTrigger(TriggerType.PreUpdate, "Customers");

        result.Should().Be(TriggerResult.Success);
        executionOrder.Should().Equal("highest", "normal", "low");
    }

    [Fact]
    public void FireBlockTrigger_WhenFailureDoesNotAllowContinuation_StopsRemainingHandlers()
    {
        var manager = new TheTechIdea.Beep.Editor.Forms.Helpers.TriggerManager();
        TriggerChainCompletedEventArgs? chainArgs = null;
        var executedHandlers = new List<string>();

        manager.TriggerChainCompleted += (_, args) => chainArgs = args;

        manager.RegisterBlockTrigger(TriggerType.PreDelete, "Customers", _ =>
        {
            executedHandlers.Add("failure");
            return TriggerResult.Failure;
        }, TriggerPriority.High);

        manager.RegisterBlockTrigger(TriggerType.PreDelete, "Customers", _ =>
        {
            executedHandlers.Add("success");
            return TriggerResult.Success;
        }, TriggerPriority.Normal);

        var result = manager.FireBlockTrigger(TriggerType.PreDelete, "Customers");

        result.Should().Be(TriggerResult.Failure);
        executedHandlers.Should().Equal("failure");
        chainArgs.Should().NotBeNull();
        chainArgs!.FailureCount.Should().Be(1);
        chainArgs.SuccessCount.Should().Be(0);
        chainArgs.SkippedCount.Should().Be(1);
        chainArgs.OverallResult.Should().Be(TriggerResult.Failure);
    }

    [Fact]
    public void FireBlockTrigger_WhenFailureAllowsContinuation_ExecutesRemainingHandlers()
    {
        var manager = new TheTechIdea.Beep.Editor.Forms.Helpers.TriggerManager();
        var executedHandlers = new List<string>();

        manager.RegisterTrigger(new TriggerDefinition(TriggerType.PreCommit, TriggerScope.Block)
        {
            BlockName = "Customers",
            Priority = TriggerPriority.High,
            ContinueOnFailure = true,
            Handler = _ =>
            {
                executedHandlers.Add("failure");
                return TriggerResult.Failure;
            }
        });

        manager.RegisterTrigger(new TriggerDefinition(TriggerType.PreCommit, TriggerScope.Block)
        {
            BlockName = "Customers",
            Priority = TriggerPriority.Normal,
            Handler = _ =>
            {
                executedHandlers.Add("success");
                return TriggerResult.Success;
            }
        });

        var result = manager.FireBlockTrigger(TriggerType.PreCommit, "Customers");

        result.Should().Be(TriggerResult.Failure);
        executedHandlers.Should().Equal("failure", "success");
    }

    [Fact]
    public void SuspendAndResumeTriggers_SkipsExecutionUntilResumed()
    {
        var manager = new TheTechIdea.Beep.Editor.Forms.Helpers.TriggerManager();
        var callCount = 0;

        manager.RegisterBlockTrigger(TriggerType.PostInsert, "Customers", _ =>
        {
            callCount++;
            return TriggerResult.Success;
        });

        manager.SuspendTriggers();

        var suspendedResult = manager.FireBlockTrigger(TriggerType.PostInsert, "Customers");

        suspendedResult.Should().Be(TriggerResult.Skipped);
        manager.IsSuspended.Should().BeTrue();
        callCount.Should().Be(0);

        manager.ResumeTriggers();

        var resumedResult = manager.FireBlockTrigger(TriggerType.PostInsert, "Customers");

        resumedResult.Should().Be(TriggerResult.Success);
        manager.IsSuspended.Should().BeFalse();
        callCount.Should().Be(1);
    }
}