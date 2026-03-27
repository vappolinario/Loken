namespace Loken.Core;

using Shouldly;

public class ShellExecutorTest
{
    [Theory]
    [InlineData("rm -rf /")]
    [InlineData("sudo rm -rf /")]
    [InlineData("reboot")]
    [InlineData("shutdown")]
    public async Task Version_AgentMustReturnVersionNumber(string command)
    {
        var executor = new ShellExecutor();
        var result = await executor.ExecuteAsync(command);
        result.ExitCode.ShouldBe(1);
    }
}
