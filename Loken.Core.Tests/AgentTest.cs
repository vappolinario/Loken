namespace Loken.Core.Tests;

using NSubstitute;
using Shouldly;

public class AgentTest
{
    [Fact]
    public void Version_AgentMustReturnVersionNumber()
    {
      var chat = Substitute.For<IChatClient>();
      var executor = Substitute.For<IShellExecutor>();
      var agent = new Agent(executor, chat);
      var result = agent.Version();
      result.ShouldBe("0.1");
    }
}
