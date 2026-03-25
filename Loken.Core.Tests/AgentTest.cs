namespace Loken.Core.Tests;

using Shouldly;

public class AgentTest
{
    [Fact]
    public void Version_AgentMustReturnVersionNumber()
    {
      var result = Agent.Version();
      result.ShouldBe("0.1");

    }
}
