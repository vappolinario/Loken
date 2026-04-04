namespace Loken.Core.Tests;

using Shouldly;
using System.IO;

public class SkillLoaderTest
{
    private readonly string _testDirectory;

    public SkillLoaderTest()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "SkillLoaderTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    private void CreateSkillDirectory(string skillName, string frontmatter, string body)
    {
        var skillDir = Path.Combine(_testDirectory, skillName);
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillFile, frontmatter + "\n" + body);
    }

    [Fact]
    public void LoadSkills_ValidDirectory_LoadsSkillsCorrectly()
    {
        CreateSkillDirectory("test-skill-1",
            "---\nname: Test Skill 1\ndescription: This is test skill 1\n---",
            "This is the body of test skill 1.");

        CreateSkillDirectory("test-skill-2",
            "---\nname: Test Skill 2\ndescription: This is test skill 2\n---",
            "This is the body of test skill 2.");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - Test Skill 1: This is test skill 1");
        skills.ShouldContain("  - Test Skill 2: This is test skill 2");
    }

    [Fact]
    public void LoadSkills_NonExistentDirectory_ReturnsEmpty()
    {
        var nonExistentDir = Path.Combine(_testDirectory, "non-existent");

        var loader = new SkillLoader(nonExistentDir);
        var skills = loader.GetSkills();

        skills.ShouldBeEmpty();
    }

    [Fact]
    public void ParseFrontmatter_ValidFormat_ParsesCorrectly()
    {
        var frontmatter = "---\nname: Test Skill\ndescription: Test Description\n---\nBody content";
        CreateSkillDirectory("test-skill", frontmatter, "");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - Test Skill: Test Description");
    }

    [Fact]
    public void ParseFrontmatter_MissingClosingDelimiter_TreatsAsNoFrontmatter()
    {
        var frontmatter = "---\nname: Test Skill\ndescription: Test Description\nBody content";
        CreateSkillDirectory("test-skill", frontmatter, "");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldBeEmpty();
    }

    [Fact]
    public void ParseFrontmatter_InvalidYAML_ShouldSkipGracefully()
    {
        var skillDir = Path.Combine(_testDirectory, "test-skill");
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillFile, "---\nname: Test\n---");
        using (var stream = File.OpenWrite(skillFile))
        {
            stream.Seek(0, SeekOrigin.End);
            stream.Write(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, 0, 4);
        }

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldBeEmpty();
    }

    [Fact]
    public void LoadSkills_MalformedFile_ShouldSkipGracefully()
    {
        var skillDir = Path.Combine(_testDirectory, "malformed-skill");
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillFile, "Invalid content with binary \0\0\0 data");

        CreateSkillDirectory("valid-skill",
            "---\nname: Valid Skill\ndescription: Valid Description\n---",
            "Valid body");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - Valid Skill: Valid Description");
        skills.ShouldNotContain("malformed-skill");
    }

    [Fact]
    public void LoadSkills_MissingNameField_UsesDirectoryName()
    {
        CreateSkillDirectory("directory-name-skill",
            "---\ndescription: Skill with no name field\n---",
            "Body content");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - directory-name-skill: Skill with no name field");
    }

    [Fact]
    public void LoadSkills_MissingDescriptionField_SkipsSkill()
    {
        CreateSkillDirectory("no-description-skill",
            "---\nname: No Description Skill\n---",
            "Body content");

        CreateSkillDirectory("valid-skill",
            "---\nname: Valid Skill\ndescription: Valid Description\n---",
            "Valid body");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - Valid Skill: Valid Description");
        skills.ShouldNotContain("No Description Skill");
    }

    [Fact]
    public void LoadSkills_DuplicateSkillNames_LaterOverwritesEarlier()
    {
        CreateSkillDirectory("skill-1",
            "---\nname: Duplicate Skill\ndescription: First description\n---",
            "First body");

        CreateSkillDirectory("skill-2",
            "---\nname: Duplicate Skill\ndescription: Second description\n---",
            "Second body");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("Duplicate Skill:");
        var lines = skills.Split('\n').Where(l => l.Contains("Duplicate Skill")).ToList();
        lines.Count.ShouldBe(1);
    }

    [Fact]
    public void GetSkills_ReturnsFormattedList()
    {
        CreateSkillDirectory("skill-a",
            "---\nname: Skill A\ndescription: Description A\n---",
            "Body A");

        CreateSkillDirectory("skill-b",
            "---\nname: Skill B\ndescription: Description B\n---",
            "Body B");

        CreateSkillDirectory("skill-c",
            "---\nname: Skill C\ndescription: Description C\n---",
            "Body C");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldBe("  - Skill A: Description A\n  - Skill B: Description B\n  - Skill C: Description C");
    }

    [Fact]
    public void GetSkillBody_ValidSkill_ReturnsProperXMLWrapper()
    {
        CreateSkillDirectory("test-skill",
            "---\nname: Test Skill\ndescription: Test Description\n---",
            "This is the skill body\nwith multiple lines.");

        var loader = new SkillLoader(_testDirectory);
        var body = loader.GetSkillBody("Test Skill");

        body.ShouldContain("<skill name=\"Test Skill\">");
        body.ShouldContain("This is the skill body");
        body.ShouldContain("with multiple lines.");
        body.ShouldContain("</skill>");
    }

    [Fact]
    public void GetSkillBody_NonExistentSkill_ReturnsTypoMessage()
    {
        var loader = new SkillLoader(_testDirectory);

        var body = loader.GetSkillBody("NonExistentSkill");

        body.ShouldBe("unknown skill");
    }

    [Fact]
    public void LoadSkills_EmptyOrWhitespaceSkillName_ShouldBeHandled()
    {
        CreateSkillDirectory("whitespace-test",
            "---\nname:   \ndescription: Test Description\n---",
            "Body content");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - : Test Description");
    }

    [Fact]
    public void LoadSkills_SpecialCharactersInNamesAndDescriptions_ShouldWork()
    {
        CreateSkillDirectory("special-chars",
            "---\nname: Skill with & special <chars> \"quotes\"\ndescription: Description with & special <chars> \"quotes\" too\n---",
            "Body with <xml> & special chars");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();
        var body = loader.GetSkillBody("Skill with & special <chars> \"quotes\"");

        skills.ShouldContain("  - Skill with & special <chars> \"quotes\": Description with & special <chars> \"quotes\" too");
        body.ShouldContain("<skill name=\"Skill with & special <chars> \"quotes\"\">");
        body.ShouldContain("Body with <xml> & special chars");
    }

    [Fact]
    public void LoadSkills_MultipleColonsInFrontmatter_ShouldParseCorrectly()
    {
        CreateSkillDirectory("colon-test",
            "---\nname: Skill: With: Colons\ndescription: Description: also: with: colons\n---",
            "Body content");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldContain("  - Skill: With: Colons: Description: also: with: colons");
    }

    [Fact]
    public void LoadSkills_EmptyBody_ShouldWork()
    {
        CreateSkillDirectory("empty-body",
            "---\nname: Empty Body Skill\ndescription: Has empty body\n---",
            "");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();
        var body = loader.GetSkillBody("Empty Body Skill");

        skills.ShouldContain("  - Empty Body Skill: Has empty body");
        body.ShouldContain("<skill name=\"Empty Body Skill\">");
        body.ShouldContain("</skill>");
    }

    [Fact]
    public void LoadSkills_NoFrontmatter_ShouldSkip()
    {
        var skillDir = Path.Combine(_testDirectory, "no-frontmatter");
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillFile, "Just plain text without frontmatter");

        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        skills.ShouldBeEmpty();
    }
}
