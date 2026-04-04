namespace Loken.Core.Tests;

using NSubstitute;
using Shouldly;
using System.IO;

public class SkillServiceTest
{
    private readonly string _testDirectory;

    public SkillServiceTest()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "SkillServiceTest_" + Guid.NewGuid().ToString());
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
    public void Constructor_WithValidSkillLoader_InitializesSuccessfully()
    {
        // Arrange
        var skillLoader = new SkillLoader(_testDirectory);

        // Act
        var skillService = new SkillService(skillLoader);

        // Assert
        skillService.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSkillLoader_DoesNotThrow()
    {
        // Arrange
        SkillLoader nullLoader = null!;

        // Act
        var exception = Record.Exception(() => new SkillService(nullLoader));

        // Assert
        // Note: The actual SkillService constructor doesn't check for null,
        // so it doesn't throw. It will only throw when methods are called.
        exception.ShouldBeNull();
    }

    [Fact]
    public void GetSkills_DelegatesToSkillLoader()
    {
        // Note: Since SkillLoader methods are not virtual, we cannot mock them with NSubstitute.
        // Instead, we test that SkillService returns the same result as SkillLoader directly.
        
        // Arrange
        CreateSkillDirectory("test-skill-1", 
            "---\nname: Test Skill 1\ndescription: Description 1\n---", 
            "Body 1");
        
        CreateSkillDirectory("test-skill-2", 
            "---\nname: Test Skill 2\ndescription: Description 2\n---", 
            "Body 2");

        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act
        var serviceResult = skillService.GetSkills();
        var loaderResult = skillLoader.GetSkills();

        // Assert
        serviceResult.ShouldBe(loaderResult);
        serviceResult.ShouldContain("  - Test Skill 1: Description 1");
        serviceResult.ShouldContain("  - Test Skill 2: Description 2");
    }

    [Fact]
    public void GetSkillBody_DelegatesToSkillLoader()
    {
        // Note: Since SkillLoader methods are not virtual, we cannot mock them with NSubstitute.
        // Instead, we test that SkillService returns the same result as SkillLoader directly.
        
        // Arrange
        CreateSkillDirectory("test-skill", 
            "---\nname: Test Skill\ndescription: Test Description\n---", 
            "Skill body content");

        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act
        var serviceResult = skillService.GetSkillBody("Test Skill");
        var loaderResult = skillLoader.GetSkillBody("Test Skill");

        // Assert
        serviceResult.ShouldBe(loaderResult);
        serviceResult.ShouldContain("<skill name=\"Test Skill\">");
        serviceResult.ShouldContain("Skill body content");
        serviceResult.ShouldContain("</skill>");
    }

    [Fact]
    public void GetSkillBody_WithDifferentSkillNames_DelegatesCorrectly()
    {
        // Arrange
        CreateSkillDirectory("first-skill", 
            "---\nname: First Skill\ndescription: First Description\n---", 
            "First body");
        
        CreateSkillDirectory("second-skill", 
            "---\nname: Second Skill\ndescription: Second Description\n---", 
            "Second body");

        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act
        var serviceResult1 = skillService.GetSkillBody("First Skill");
        var serviceResult2 = skillService.GetSkillBody("Second Skill");
        var loaderResult1 = skillLoader.GetSkillBody("First Skill");
        var loaderResult2 = skillLoader.GetSkillBody("Second Skill");

        // Assert
        serviceResult1.ShouldBe(loaderResult1);
        serviceResult2.ShouldBe(loaderResult2);
        
        serviceResult1.ShouldContain("<skill name=\"First Skill\">");
        serviceResult1.ShouldContain("First body");
        serviceResult2.ShouldContain("<skill name=\"Second Skill\">");
        serviceResult2.ShouldContain("Second body");
    }

    [Fact]
    public void Implements_ISkillService_Interface()
    {
        // Arrange
        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act & Assert
        skillService.ShouldBeAssignableTo<ISkillService>();
        
        // Verify interface methods
        var skillServiceAsInterface = skillService as ISkillService;
        skillServiceAsInterface.ShouldNotBeNull();
        
        // The interface should have both methods
        var interfaceType = typeof(ISkillService);
        interfaceType.GetMethod("GetSkills").ShouldNotBeNull();
        interfaceType.GetMethod("GetSkillBody").ShouldNotBeNull();
    }

    [Fact]
    public void GetSkillBody_NonExistentSkill_ReturnsTypoMessage()
    {
        // Arrange
        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act
        var serviceResult = skillService.GetSkillBody("NonExistentSkill");
        var loaderResult = skillLoader.GetSkillBody("NonExistentSkill");

        // Assert
        serviceResult.ShouldBe(loaderResult);
        serviceResult.ShouldBe("unknown skill");
    }

    [Fact]
    public void GetSkills_EmptyDirectory_ReturnsEmptyString()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(emptyDir);
        var skillLoader = new SkillLoader(emptyDir);
        var skillService = new SkillService(skillLoader);

        // Act
        var serviceResult = skillService.GetSkills();
        var loaderResult = skillLoader.GetSkills();

        // Assert
        serviceResult.ShouldBe(loaderResult);
        serviceResult.ShouldBe("");
    }

    [Fact]
    public void GetSkills_MultipleCalls_ReturnsConsistentResults()
    {
        // Arrange
        CreateSkillDirectory("test-skill", 
            "---\nname: Test Skill\ndescription: Test Description\n---", 
            "Body");

        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act
        var result1 = skillService.GetSkills();
        var result2 = skillService.GetSkills();
        var loaderResult = skillLoader.GetSkills();

        // Assert
        result1.ShouldBe(result2);
        result1.ShouldBe(loaderResult);
        result1.ShouldContain("  - Test Skill: Test Description");
    }

    [Fact]
    public void GetSkillBody_EmptySkillName_ReturnsTypoMessage()
    {
        // Arrange
        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act
        var serviceResult = skillService.GetSkillBody("");
        var loaderResult = skillLoader.GetSkillBody("");

        // Assert
        serviceResult.ShouldBe(loaderResult);
        serviceResult.ShouldBe("unknown skill");
    }

    [Fact]
    public void GetSkillBody_NullSkillName_ThrowsException()
    {
        // Arrange
        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader);

        // Act & Assert
        // Note: SkillLoader.GetSkillBody will throw an exception
        // when trying to use null as a dictionary key
        // The exact exception type may vary
        Should.Throw<Exception>(() => skillService.GetSkillBody(null!));
    }

    [Fact]
    public void SkillService_IsProperlyConstructed_WithDependencyInjection()
    {
        // Arrange
        CreateSkillDirectory("test-skill", 
            "---\nname: Test Skill\ndescription: Test Description\n---", 
            "Body");

        var skillLoader = new SkillLoader(_testDirectory);

        // Act
        var skillService = new SkillService(skillLoader);
        var skills = skillService.GetSkills();
        
        // Assert
        skills.ShouldContain("  - Test Skill: Test Description");
    }

    [Fact]
    public void InterfaceMethods_MatchSkillLoaderMethods()
    {
        // Arrange
        CreateSkillDirectory("test-skill", 
            "---\nname: Test Skill\ndescription: Test Description\n---", 
            "Test body");

        var skillLoader = new SkillLoader(_testDirectory);
        var skillService = new SkillService(skillLoader) as ISkillService;

        // Act & Assert - Verify both interface methods exist and work
        var skills = skillService.GetSkills();
        var body = skillService.GetSkillBody("Test Skill");

        skills.ShouldContain("  - Test Skill: Test Description");
        body.ShouldContain("<skill name=\"Test Skill\">");
        body.ShouldContain("Test body");
        body.ShouldContain("</skill>");
    }

    // Note about NSubstitute: The SkillLoader class methods are not virtual,
    // so they cannot be mocked with NSubstitute. This is a design limitation
    // that prevents proper unit testing with mocking frameworks.
    //
    // Note about exception propagation: The SkillLoader class catches exceptions
    // internally with `catch { continue; }`, so exceptions are not propagated.
    // Therefore, SkillService cannot propagate exceptions from SkillLoader.
}