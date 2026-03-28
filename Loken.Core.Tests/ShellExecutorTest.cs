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
        await Should.ThrowAsync<System.Security.SecurityException>(async () => await executor.ExecuteAsync(command));
    }
}
