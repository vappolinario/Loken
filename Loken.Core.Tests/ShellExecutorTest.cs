namespace Loken.Core;

using Shouldly;

public class ShellExecutorHandlerTest
{
    [Theory]
    [InlineData("rm -rf /")]
    [InlineData("sudo rm -rf /")]
    [InlineData("reboot")]
    [InlineData("shutdown")]
    public async Task Version_ShellExecutor_ShouldNotRunDangerousCommands(string command)
    {
        var executor = new ShellExecutorHandler(new PathResolver("/tmp/safe-path"));
        var json = BinaryData.FromString($$"""
{
  "command": "{{command}}"
}
""");
        await Should.ThrowAsync<System.Security.SecurityException>(async () => await executor.ExecuteAsync(json));
    }
}
