// ServiceTests.cs – enhetstester för AuthService, PostService och CommentService.
// Använder in-memory-databas och mockade beroenden där det behövs.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Tests;

file static class Helpers
{
    public static AppDbContext InMemoryDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new AppDbContext(opts);
    }

    public static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"]                    = "SuperSecretKeyForTestingPurposesOnly123!!",
            ["Jwt:Issuer"]                 = "TestIssuer",
            ["Jwt:Audience"]               = "TestAudience",
            ["Jwt:AccessTokenExpiryMinutes"] = "15",
            ["Jwt:RefreshTokenExpiryDays"] = "7"
        }).Build();

    public static AuthService MakeAuthService(AppDbContext db)
    {
        var email = new Mock<IEmailService>();
        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string?>(),
                                    It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool>()))
             .Returns(Task.CompletedTask);
        return new AuthService(db, Config(), email.Object, audit.Object);
    }
}

// ── AuthService-tester ─────────────────────────────────────────────────────

public class AuthServiceTests
{
    [Fact]
    public async Task Register_ValidData_ReturnsTokens()
    {
        using var db = Helpers.InMemoryDb(nameof(Register_ValidData_ReturnsTokens));
        var svc      = Helpers.MakeAuthService(db);

        var result = await svc.RegisterAsync(new RegisterRequest("alice", "Password1!", null), null);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsNull()
    {
        using var db = Helpers.InMemoryDb(nameof(Register_DuplicateUsername_ReturnsNull));
        var svc      = Helpers.MakeAuthService(db);

        await svc.RegisterAsync(new RegisterRequest("bob", "Password1!", null), null);
        var result = await svc.RegisterAsync(new RegisterRequest("bob", "Other1!pass", null), null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsTokens()
    {
        using var db = Helpers.InMemoryDb(nameof(Login_CorrectCredentials_ReturnsTokens));
        var svc      = Helpers.MakeAuthService(db);

        await svc.RegisterAsync(new RegisterRequest("charlie", "Pass1234!", null), null);
        var result = await svc.LoginAsync(new LoginRequest("charlie", "Pass1234!"), null);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        using var db = Helpers.InMemoryDb(nameof(Login_WrongPassword_ReturnsNull));
        var svc      = Helpers.MakeAuthService(db);

        await svc.RegisterAsync(new RegisterRequest("dave", "RealPass1!", null), null);
        var result = await svc.LoginAsync(new LoginRequest("dave", "WrongPass1!"), null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        using var db = Helpers.InMemoryDb(nameof(Refresh_ValidToken_ReturnsNewTokens));
        var svc      = Helpers.MakeAuthService(db);

        var reg     = await svc.RegisterAsync(new RegisterRequest("eve", "Pass1234!", null), null);
        var result  = await svc.RefreshAsync(reg!.RefreshToken, null);

        result.Should().NotBeNull();
        result!.RefreshToken.Should().NotBe(reg.RefreshToken);
        var oldToken = await db.RefreshTokens.FirstAsync(t => t.Token == reg.RefreshToken);
        oldToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsNull()
    {
        using var db = Helpers.InMemoryDb(nameof(Refresh_InvalidToken_ReturnsNull));
        var svc      = Helpers.MakeAuthService(db);

        var result = await svc.RefreshAsync("fake-token", null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsTrue()
    {
        using var db = Helpers.InMemoryDb(nameof(VerifyEmail_ValidToken_ReturnsTrue));
        var svc      = Helpers.MakeAuthService(db);

        await svc.RegisterAsync(new RegisterRequest("frank", "Pass1234!", "frank@test.com"), null);
        var user = await db.Users.FirstAsync(u => u.Username == "frank");

        var ok = await svc.VerifyEmailAsync(user.VerificationToken!);
        ok.Should().BeTrue();

        var updated = await db.Users.FindAsync(user.Id);
        updated!.EmailVerified.Should().BeTrue();
    }
}

// ── PostService-tester ─────────────────────────────────────────────────────

public class PostServiceTests
{
    private static async Task<AppDbContext> DbWithUser(string name)
    {
        var db = Helpers.InMemoryDb(name);
        db.Users.Add(new User
        {
            Id = 1, Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("x"), Role = "User"
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Create_ReturnsPostWithAuthor()
    {
        using var db = await DbWithUser(nameof(Create_ReturnsPostWithAuthor));
        var svc      = new PostService(db);

        var result = await svc.CreateAsync(new CreatePostRequest("Title", "Content"), 1, null);

        result.Title.Should().Be("Title");
        result.AuthorUsername.Should().Be("testuser");
    }

    [Fact]
    public async Task GetAll_Paged_ReturnsCorrectPage()
    {
        using var db = await DbWithUser(nameof(GetAll_Paged_ReturnsCorrectPage));
        var svc      = new PostService(db);

        for (int i = 1; i <= 15; i++)
            await svc.CreateAsync(new CreatePostRequest($"Post {i}", "Content"), 1, null);

        var page1 = await svc.GetAllAsync(1, 10, null, null);
        var page2 = await svc.GetAllAsync(2, 10, null, null);

        page1.Items.Count().Should().Be(10);
        page2.Items.Count().Should().Be(5);
        page1.TotalCount.Should().Be(15);
        page1.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_Search_FiltersCorrectly()
    {
        using var db = await DbWithUser(nameof(GetAll_Search_FiltersCorrectly));
        var svc      = new PostService(db);

        await svc.CreateAsync(new CreatePostRequest("Hello World", "Content A"), 1, null);
        await svc.CreateAsync(new CreatePostRequest("Goodbye",     "Content B"), 1, null);

        var result = await svc.GetAllAsync(1, 10, "hello", null);
        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("Hello World");
    }

    [Fact]
    public async Task Update_ByNonOwner_ReturnsNull()
    {
        using var db = await DbWithUser(nameof(Update_ByNonOwner_ReturnsNull));
        db.Users.Add(new User { Id = 2, Username = "attacker",
            PasswordHash = "x", Role = "User" });
        await db.SaveChangesAsync();

        var svc  = new PostService(db);
        var post = await svc.CreateAsync(new CreatePostRequest("T", "C"), 1, null);

        var result = await svc.UpdateAsync(post.Id,
            new UpdatePostRequest("Hacked", "Hacked"), 2, "User");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ByAdmin_Succeeds()
    {
        using var db = await DbWithUser(nameof(Delete_ByAdmin_Succeeds));
        var svc      = new PostService(db);
        var post     = await svc.CreateAsync(new CreatePostRequest("X", "Y"), 1, null);

        var ok = await svc.DeleteAsync(post.Id, 999, "Admin");
        ok.Should().BeTrue();
    }
}

// ── CommentService-tester ──────────────────────────────────────────────────

public class CommentServiceTests
{
    private static async Task<(AppDbContext db, int postId)> Setup(string name)
    {
        var db = Helpers.InMemoryDb(name);
        db.Users.Add(new User { Id = 1, Username = "user1",
            PasswordHash = "x", Role = "User" });
        var post = new Post { Title = "T", Content = "C", AuthorId = 1 };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return (db, post.Id);
    }

    [Fact]
    public async Task CreateComment_OnValidPost_Succeeds()
    {
        var (db, postId) = await Setup(nameof(CreateComment_OnValidPost_Succeeds));
        var svc          = new CommentService(db);

        var result = await svc.CreateAsync(postId,
            new CreateCommentRequest("Great post!"), 1);

        result.Should().NotBeNull();
        result!.Content.Should().Be("Great post!");
    }

    [Fact]
    public async Task DeleteComment_ByNonOwner_ReturnsFalse()
    {
        var (db, postId) = await Setup(nameof(DeleteComment_ByNonOwner_ReturnsFalse));
        db.Users.Add(new User { Id = 2, Username = "other",
            PasswordHash = "x", Role = "User" });
        await db.SaveChangesAsync();

        var svc     = new CommentService(db);
        var comment = await svc.CreateAsync(postId, new CreateCommentRequest("My comment"), 1);

        var ok = await svc.DeleteAsync(comment!.Id, 2, "User");
        ok.Should().BeFalse();
    }
}
