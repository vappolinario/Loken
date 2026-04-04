using System.Text.Json;

namespace Loken.Core;

public class SkillHandler : IToolHandler
{
    private readonly ISkillService _skillService;

    public string Name => "load_skill";

    public string Description => "Load specialized knowledge by name.";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",
                    properties = new
                    {
                        name = new
                        {
                            type = "string",
                            description = "Skill name to load"
                        },
                    },
                    required = new[] { "name" }
                });


    public SkillHandler(ISkillService skillService)
    {
        _skillService = skillService;
    }

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            if (!doc.RootElement.TryGetProperty("name", out var nameElement))
                throw new MissingParameterException("name");
            
            var name = nameElement.GetString()
                       ?? throw new MissingParameterException("name");

            return _skillService.GetSkillBody(name);
        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid Json.");
        }
    }
}
