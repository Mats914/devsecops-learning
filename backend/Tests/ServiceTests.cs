using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Tests;

// ── Helpers ────────────────────────────────────────────────────────────────

file static class TestHelpers
{
    public static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    public static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]           = "SuperSecretKeyForTestingPurposesOnly123!",
                ["Jwt:Issuer"]        = "TestIssuer",
                ["Jwt:Audience"]      = "TestAudience",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
}

// ── AuthService tests ──────────────────────────────────────────────────────

public class AuthServiceTests
{
    [Fact]
    public async Task Register_WithValidData_ReturnsToken()
    {
        using var db    = TestHelpers.CreateInMemoryDb(nameof(Register_WithValidData_ReturnsToken));
        var service     = new AuthService(db, TestHelpers.CreateConfig());
        var request     = new RegisterRequest("alice", "Password123!");

        var result = await service.RegisterAsync(request);

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.Username.Should().Be("alice");
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsNull()
    {
        using var db = TestHelpers.CreateInMemoryDb(nameof(Register_WithDuplicateUsername_ReturnsNull));
        var service  = new AuthService(db, TestHelpers.CreateConfig());

        await service.RegisterAsync(new RegisterRequest("bob", "Password123!"));
        var result = await service.RegisterAsync(new RegisterRequest("bob", "Different1!"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        using var db = TestHelpers.CreateInMemoryDb(nameof(Login_WithCorrectCredentials_ReturnsToken));
        var service  = new AuthService(db, TestHelpers.CreateConfig());

        await service.RegisterAsync(new RegisterRequest("charlie", "Pass1234!"));
        var result = await service.LoginAsync(new LoginRequest("charlie", "Pass1234!"));

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsNull()
    {
        using var db = TestHelpers.CreateInMemoryDb(nameof(Login_WithWrongPassword_ReturnsNull));
        var service  = new AuthService(db, TestHelpers.CreateConfig());

        await service.RegisterAsync(new RegisterRequest("dave", "RealPass1!"));
        var result = await service.LoginAsync(new LoginRequest("dave", "WrongPass1!"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsNull()
    {
        using var db = TestHelpers.CreateInMemoryDb(nameof(Login_WithNonExistentUser_ReturnsNull));
        var service  = new AuthService(db, TestHelpers.CreateConfig());

        var result = await service.LoginAsync(new LoginRequest("nobody", "Whatever1!"));

        result.Should().BeNull();
    }
}

// ── PostService tests ──────────────────────────────────────────────────────

public class PostServiceTests
{
    private static async Task<AppDbContext> DbWithUserAsync(string dbName)
    {
        var db   = TestHelpers.CreateInMemoryDb(dbName);
        db.Users.Add(new User
        {
            Id           = 1,
            Username     = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("x"),
            Role         = "User"
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task CreatePost_ReturnsPostWithCorrectAuthor()
    {
        using var db = await DbWithUserAsync(nameof(CreatePost_ReturnsPostWithCorrectAuthor));
        var service  = new PostService(db);

        var result = await service.CreateAsync(
            new CreatePostRequest("Hello", "World content"), authorId: 1);

        result.Title.Should().Be("Hello");
        result.AuthorUsername.Should().Be("testuser");
    }

    [Fact]
    public async Task GetAll_ReturnsAllPosts()
    {
        using var db = await DbWithUserAsync(nameof(GetAll_ReturnsAllPosts));
        var service  = new PostService(db);

        await service.CreateAsync(new CreatePostRequest("A", "Content A"), 1);
        await service.CreateAsync(new CreatePostRequest("B", "Content B"), 1);

        var all = await service.GetAllAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdatePost_ByNonOwner_NonAdmin_ReturnsNull()
    {
        // Security test: a different user must not update someone else's post
        using var db = await DbWithUserAsync(nameof(UpdatePost_ByNonOwner_NonAdmin_ReturnsNull));

        // Add attacker user
        db.Users.Add(new User { Id = 2, Username = "attacker",
            PasswordHash = "x", Role = "User" });
        await db.SaveChangesAsync();

        var service = new PostService(db);
        var post    = await service.CreateAsync(new CreatePostRequest("Title", "Content"), authorId: 1);

        // Attacker tries to update
        var result = await service.UpdateAsync(
            post.Id, new UpdatePostRequest("Hacked", "Hacked content"),
            requesterId: 2, requesterRole: "User");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletePost_ByAdmin_Succeeds()
    {
        using var db = await DbWithUserAsync(nameof(DeletePost_ByAdmin_Succeeds));
        var service  = new PostService(db);

        var post    = await service.CreateAsync(new CreatePostRequest("X", "Y"), authorId: 1);
        var deleted = await service.DeleteAsync(post.Id, requesterId: 99, requesterRole: "Admin");

        deleted.Should().BeTrue();
    }
}
