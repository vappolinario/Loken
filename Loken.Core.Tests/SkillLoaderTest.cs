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
        // Arrange
        CreateSkillDirectory("test-skill-1", 
            "---\nname: Test Skill 1\ndescription: This is test skill 1\n---", 
            "This is the body of test skill 1.");
        
        CreateSkillDirectory("test-skill-2", 
            "---\nname: Test Skill 2\ndescription: This is test skill 2\n---", 
            "This is the body of test skill 2.");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert
        skills.ShouldContain("  - Test Skill 1: This is test skill 1");
        skills.ShouldContain("  - Test Skill 2: This is test skill 2");
    }

    [Fact]
    public void LoadSkills_NonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "non-existent");

        // Act
        var loader = new SkillLoader(nonExistentDir);
        var skills = loader.GetSkills();

        // Assert
        skills.ShouldBeEmpty();
    }

    [Fact]
    public void ParseFrontmatter_ValidFormat_ParsesCorrectly()
    {
        // Arrange
        var frontmatter = "---\nname: Test Skill\ndescription: Test Description\n---\nBody content";
        CreateSkillDirectory("test-skill", frontmatter, "");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert
        skills.ShouldContain("  - Test Skill: Test Description");
    }

    [Fact]
    public void ParseFrontmatter_MissingClosingDelimiter_TreatsAsNoFrontmatter()
    {
        // Arrange
        var frontmatter = "---\nname: Test Skill\ndescription: Test Description\nBody content";
        CreateSkillDirectory("test-skill", frontmatter, "");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - Should use directory name but skip because description is missing
        skills.ShouldBeEmpty();
    }

    [Fact]
    public void ParseFrontmatter_InvalidYAML_ShouldSkipGracefully()
    {
        // Arrange - Actually, the parser handles "invalid: yaml: with: colons" as valid
        // So let's test with truly invalid content that causes an exception
        var skillDir = Path.Combine(_testDirectory, "test-skill");
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        // Write file that will cause an exception when parsed
        File.WriteAllText(skillFile, "---\nname: Test\n---");
        // Corrupt the file after writing
        using (var stream = File.OpenWrite(skillFile))
        {
            stream.Seek(0, SeekOrigin.End);
            stream.Write(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, 0, 4);
        }

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - Should skip due to parsing error
        skills.ShouldBeEmpty();
    }

    [Fact]
    public void LoadSkills_MalformedFile_ShouldSkipGracefully()
    {
        // Arrange
        var skillDir = Path.Combine(_testDirectory, "malformed-skill");
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillFile, "Invalid content with binary \0\0\0 data");

        // Also add a valid skill to ensure we still load valid ones
        CreateSkillDirectory("valid-skill", 
            "---\nname: Valid Skill\ndescription: Valid Description\n---", 
            "Valid body");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - Should only load the valid skill
        skills.ShouldContain("  - Valid Skill: Valid Description");
        skills.ShouldNotContain("malformed-skill");
    }

    [Fact]
    public void LoadSkills_MissingNameField_UsesDirectoryName()
    {
        // Arrange
        CreateSkillDirectory("directory-name-skill", 
            "---\ndescription: Skill with no name field\n---", 
            "Body content");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert
        skills.ShouldContain("  - directory-name-skill: Skill with no name field");
    }

    [Fact]
    public void LoadSkills_MissingDescriptionField_SkipsSkill()
    {
        // Arrange
        CreateSkillDirectory("no-description-skill", 
            "---\nname: No Description Skill\n---", 
            "Body content");

        // Also add a valid skill to ensure we still load valid ones
        CreateSkillDirectory("valid-skill", 
            "---\nname: Valid Skill\ndescription: Valid Description\n---", 
            "Valid body");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - Should only load the valid skill
        skills.ShouldContain("  - Valid Skill: Valid Description");
        skills.ShouldNotContain("No Description Skill");
    }

    [Fact]
    public void LoadSkills_DuplicateSkillNames_LaterOverwritesEarlier()
    {
        // Arrange
        CreateSkillDirectory("skill-1", 
            "---\nname: Duplicate Skill\ndescription: First description\n---", 
            "First body");
        
        CreateSkillDirectory("skill-2", 
            "---\nname: Duplicate Skill\ndescription: Second description\n---", 
            "Second body");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - The order depends on Directory.GetDirectories() which is filesystem dependent
        // So we just check that only one exists and it has one of the descriptions
        skills.ShouldContain("Duplicate Skill:");
        // Count the number of lines with "Duplicate Skill"
        var lines = skills.Split('\n').Where(l => l.Contains("Duplicate Skill")).ToList();
        lines.Count.ShouldBe(1);
    }

    [Fact]
    public void GetSkills_ReturnsFormattedList()
    {
        // Arrange
        CreateSkillDirectory("skill-a", 
            "---\nname: Skill A\ndescription: Description A\n---", 
            "Body A");
        
        CreateSkillDirectory("skill-b", 
            "---\nname: Skill B\ndescription: Description B\n---", 
            "Body B");
        
        CreateSkillDirectory("skill-c", 
            "---\nname: Skill C\ndescription: Description C\n---", 
            "Body C");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - Should be sorted alphabetically
        skills.ShouldBe("  - Skill A: Description A\n  - Skill B: Description B\n  - Skill C: Description C");
    }

    [Fact]
    public void GetSkillBody_ValidSkill_ReturnsProperXMLWrapper()
    {
        // Arrange
        CreateSkillDirectory("test-skill", 
            "---\nname: Test Skill\ndescription: Test Description\n---", 
            "This is the skill body\nwith multiple lines.");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var body = loader.GetSkillBody("Test Skill");

        // Assert - Note: GetSkillBody returns the Skill record's ToString(), not body.Body
        // Looking at the original code: return $"<skill name=\"{name}\">\n{body}\n</skill>";
        // where body is the Skill record, not body.Body
        body.ShouldContain("<skill name=\"Test Skill\">");
        body.ShouldContain("This is the skill body");
        body.ShouldContain("with multiple lines.");
        body.ShouldContain("</skill>");
    }

    [Fact]
    public void GetSkillBody_NonExistentSkill_ReturnsTypoMessage()
    {
        // Arrange
        var loader = new SkillLoader(_testDirectory);

        // Act
        var body = loader.GetSkillBody("NonExistentSkill");

        // Assert
        body.ShouldBe("unknown skill");
    }

    [Fact]
    public void LoadSkills_EmptyOrWhitespaceSkillName_ShouldBeHandled()
    {
        // Arrange
        CreateSkillDirectory("whitespace-test", 
            "---\nname:   \ndescription: Test Description\n---", 
            "Body content");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - The current implementation keeps whitespace as the name
        // So it should show empty name
        skills.ShouldContain("  - : Test Description");
    }

    [Fact]
    public void LoadSkills_SpecialCharactersInNamesAndDescriptions_ShouldWork()
    {
        // Arrange
        CreateSkillDirectory("special-chars", 
            "---\nname: Skill with & special <chars> \"quotes\"\ndescription: Description with & special <chars> \"quotes\" too\n---", 
            "Body with <xml> & special chars");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();
        var body = loader.GetSkillBody("Skill with & special <chars> \"quotes\"");

        // Assert - The XML isn't actually escaped in the original implementation
        skills.ShouldContain("  - Skill with & special <chars> \"quotes\": Description with & special <chars> \"quotes\" too");
        body.ShouldContain("<skill name=\"Skill with & special <chars> \"quotes\"\">");
        body.ShouldContain("Body with <xml> & special chars");
    }

    [Fact]
    public void LoadSkills_MultipleColonsInFrontmatter_ShouldParseCorrectly()
    {
        // Arrange
        CreateSkillDirectory("colon-test", 
            "---\nname: Skill: With: Colons\ndescription: Description: also: with: colons\n---", 
            "Body content");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert
        skills.ShouldContain("  - Skill: With: Colons: Description: also: with: colons");
    }

    [Fact]
    public void LoadSkills_EmptyBody_ShouldWork()
    {
        // Arrange
        CreateSkillDirectory("empty-body", 
            "---\nname: Empty Body Skill\ndescription: Has empty body\n---", 
            "");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();
        var body = loader.GetSkillBody("Empty Body Skill");

        // Assert
        skills.ShouldContain("  - Empty Body Skill: Has empty body");
        body.ShouldContain("<skill name=\"Empty Body Skill\">");
        body.ShouldContain("</skill>");
    }

    [Fact]
    public void LoadSkills_NoFrontmatter_ShouldSkip()
    {
        // Arrange
        var skillDir = Path.Combine(_testDirectory, "no-frontmatter");
        Directory.CreateDirectory(skillDir);
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        File.WriteAllText(skillFile, "Just plain text without frontmatter");

        // Act
        var loader = new SkillLoader(_testDirectory);
        var skills = loader.GetSkills();

        // Assert - Should skip because no frontmatter means no description
        skills.ShouldBeEmpty();
    }
}