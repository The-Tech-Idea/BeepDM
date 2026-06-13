using System.Collections.Concurrent;

namespace TheTechIdea.Beep.SetUp.Tests;

public class SetupContextTests
{
    [Fact]
    public void Properties_IsConcurrentDictionary()
    {
        var context = new SetupContext();
        Assert.IsType<ConcurrentDictionary<string, object>>(context.Properties);
    }

    [Fact]
    public void Properties_SupportsConcurrentAccess()
    {
        var context = new SetupContext();

        var writeTask = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
                context.Properties[$"key-{i}"] = i;
        });

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
                context.Properties.TryGetValue("nonexistent", out _);
        });

        Task.WaitAll(writeTask, readTask);
        Assert.Equal(1000, context.Properties.Count);
    }

    [Fact]
    public void CompletedSeederIds_ReturnsFromState()
    {
        var context = new SetupContext
        {
            State = new SetupState()
        };
        context.State.CompletedSeederIds.Add("seeder-1");
        context.State.CompletedSeederIds.Add("seeder-2");

        var ids = context.CompletedSeederIds;
        Assert.Contains("seeder-1", ids);
        Assert.Contains("seeder-2", ids);
    }

    [Fact]
    public void CompletedSeederIds_Empty_WhenStateNull()
    {
        var context = new SetupContext { State = null! };
        var ids = context.CompletedSeederIds;
        Assert.Empty(ids);
    }

    [Fact]
    public void Options_Defaults_WhenNotSet()
    {
        var context = new SetupContext();
        Assert.NotNull(context.Options);
        Assert.False(context.Options.DryRun);
    }
}
