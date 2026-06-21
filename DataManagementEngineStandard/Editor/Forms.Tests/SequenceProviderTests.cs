using TheTechIdea.Beep.Editor.Forms.Helpers;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class SequenceProviderTests
{
    [Fact]
    public void DropSequence_RemovesExistingSequenceAndReportsMissingSequence()
    {
        var provider = new SequenceProvider();
        provider.CreateSequence("ORDER_SEQ", 100);

        Assert.True(provider.DropSequence("ORDER_SEQ"));
        Assert.False(provider.SequenceExists("ORDER_SEQ"));
        Assert.False(provider.DropSequence("ORDER_SEQ"));
    }
}
