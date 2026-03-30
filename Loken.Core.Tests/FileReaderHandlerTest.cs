namespace Loken.Core;

using Shouldly;

public class FileReaderHandlerTest
{
    [Fact]
    public async Task FileHandler_ShouldPreventPathTraversal()
    {
        var handler = new FileReaderHandler(new PathResolver( "/tmp/safe_zone"));

        var maliciousArgs = BinaryData.FromString("{\"path\": \"../../../etc/passwd\"}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await handler.ExecuteAsync(maliciousArgs)
        );
    }
}
