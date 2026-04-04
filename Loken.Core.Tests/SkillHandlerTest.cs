#pragma warning disable OPENAI001
namespace Loken.Core.Tests;

using System.Text.Json;
using NSubstitute;
using Shouldly;

public class SkillHandlerTest
{
    private readonly ISkillService _skillService;
    private readonly SkillHandler _handler;

    public SkillHandlerTest()
    {
        _skillService = Substitute.For<ISkillService>();
        _handler = new SkillHandler(_skillService);
    }

    [Fact]
    public void SkillHandler_ShouldImplementIToolHandlerInterface()
    {
        _handler.ShouldBeAssignableTo<IToolHandler>();
    }

    [Fact]
    public void Name_ShouldReturnLoadSkill()
    {
        _handler.Name.ShouldBe("load_skill");
    }

    [Fact]
    public void Description_ShouldReturnCorrectDescription()
    {
        _handler.Description.ShouldBe("Load specialized knowledge by name.");
    }

    [Fact]
    public void Parameters_ShouldReturnValidJsonSchema()
    {
        var parameters = _handler.Parameters;
        parameters.ShouldNotBeNull();

        var json = parameters.ToString();
        json.ShouldContain("\"type\":\"object\"");
        json.ShouldContain("\"properties\"");
        json.ShouldContain("\"name\"");
        json.ShouldContain("\"required\":[\"name\"]");
    }

    [Fact]
    public async Task ExecuteAsync_ValidSkillName_ReturnsSkillBody()
    {
        // Arrange
        var skillName = "test-skill";
        var expectedBody = "<skill name=\"test-skill\">\nTest skill content\n</skill>";
        var input = BinaryData.FromString($"{{\"name\":\"{skillName}\"}}");

        _skillService.GetSkillBody(skillName).Returns(expectedBody);

        // Act
        var result = await _handler.ExecuteAsync(input);

        // Assert
        result.ShouldBe(expectedBody);
        _skillService.Received(1).GetSkillBody(skillName);
    }

    [Fact]
    public async Task ExecuteAsync_MissingNameParameter_ThrowsMissingParameterException()
    {
        // Arrange
        var input = BinaryData.FromString("{\"other\":\"value\"}");

        // Act & Assert
        await Should.ThrowAsync<MissingParameterException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJsonInput_ThrowsExecutionFailedException()
    {
        // Arrange
        var input = BinaryData.FromString("invalid json");

        // Act & Assert
        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentSkill_ReturnsUnknownSkillMessage()
    {
        // Arrange
        var skillName = "non-existent";
        var input = BinaryData.FromString($"{{\"name\":\"{skillName}\"}}");

        _skillService.GetSkillBody(skillName).Returns("unknown skill");

        // Act
        var result = await _handler.ExecuteAsync(input);

        // Assert
        result.ShouldBe("unknown skill");
        _skillService.Received(1).GetSkillBody(skillName);
    }

    [Fact]
    public async Task ExecuteAsync_NullSkillName_ThrowsMissingParameterException()
    {
        // Arrange
        var input = BinaryData.FromString("{\"name\":null}");

        // Act & Assert
        await Should.ThrowAsync<MissingParameterException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_EmptySkillName_ReturnsSkillBody()
    {
        // Arrange
        var skillName = "";
        var input = BinaryData.FromString($"{{\"name\":\"{skillName}\"}}");
        var expectedBody = "<skill name=\"\">\nEmpty skill\n</skill>";

        _skillService.GetSkillBody(skillName).Returns(expectedBody);

        // Act
        var result = await _handler.ExecuteAsync(input);

        // Assert
        result.ShouldBe(expectedBody);
        _skillService.Received(1).GetSkillBody(skillName);
    }

    [Fact]
    public async Task ExecuteAsync_SkillServiceThrowsException_PropagatesException()
    {
        // Arrange
        var skillName = "error-skill";
        var input = BinaryData.FromString($"{{\"name\":\"{skillName}\"}}");

        _skillService.GetSkillBody(skillName).Returns(x => throw new Exception("Skill service error"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WhitespaceSkillName_ReturnsSkillBody()
    {
        // Arrange
        var skillName = "  test  ";
        var input = BinaryData.FromString($"{{\"name\":\"{skillName}\"}}");
        var expectedBody = "<skill name=\"  test  \">\nWhitespace skill\n</skill>";

        _skillService.GetSkillBody(skillName).Returns(expectedBody);

        // Act
        var result = await _handler.ExecuteAsync(input);

        // Assert
        result.ShouldBe(expectedBody);
        _skillService.Received(1).GetSkillBody(skillName);
    }

    [Fact]
    public async Task ExecuteAsync_SpecialCharactersInSkillName_ReturnsSkillBody()
    {
        // Arrange
        var skillName = "test-skill@v1.0#special";
        var input = BinaryData.FromString($"{{\"name\":\"{skillName}\"}}");
        var expectedBody = $"<skill name=\"{skillName}\">\nSpecial skill\n</skill>";

        _skillService.GetSkillBody(skillName).Returns(expectedBody);

        // Act
        var result = await _handler.ExecuteAsync(input);

        // Assert
        result.ShouldBe(expectedBody);
        _skillService.Received(1).GetSkillBody(skillName);
    }

    [Fact]
    public void ToolInterfaceContract_ShouldBeConsistent()
    {
        // Verify the tool handler follows the expected contract
        _handler.Name.ShouldNotBeNullOrEmpty();
        _handler.Description.ShouldNotBeNullOrEmpty();
        _handler.Parameters.ShouldNotBeNull();

        // Verify parameters is valid JSON
        var json = _handler.Parameters.ToString();
        Should.NotThrow(() => JsonDocument.Parse(json));
    }
}
#pragma warning restore OPENAI001