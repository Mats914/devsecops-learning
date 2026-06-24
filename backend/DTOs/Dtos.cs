// Dtos.cs – request/response-objekt för API:t.
// Data annotations validerar indata innan den når controllern.

using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.DTOs;

// ── Autentisering ──────────────────────────────────────────────────────────

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50),
     RegularExpression(@"^[a-zA-Z0-9_]+$",
         ErrorMessage = "Username: letters, digits and underscores only.")]
    string Username,

    [Required, MinLength(8), MaxLength(100),
     RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
         ErrorMessage = "Password must contain uppercase, lowercase, digit and special character.")]
    string Password,

    [EmailAddress, MaxLength(200)]
    string? Email
);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record RefreshRequest(
    [Required] string RefreshToken
);

public record AuthResponse(
    string   AccessToken,
    string   RefreshToken,
    string   Username,
    string   Role,
    bool     EmailVerified,
    DateTime AccessTokenExpiresAt
);

// ── Inlägg ─────────────────────────────────────────────────────────────────

public record CreatePostRequest(
    [Required, MinLength(3), MaxLength(200)]  string Title,
    [Required, MinLength(1), MaxLength(5000)] string Content
);

public record UpdatePostRequest(
    [Required, MinLength(3), MaxLength(200)]  string Title,
    [Required, MinLength(1), MaxLength(5000)] string Content
);

public record PostResponse(
    int      Id,
    string   Title,
    string   Content,
    string?  ImageUrl,
    string   AuthorUsername,
    int      ViewCount,
    int      CommentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PostsPagedResponse(
    IEnumerable<PostResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

// ── Kommentarer ────────────────────────────────────────────────────────────

public record CreateCommentRequest(
    [Required, MinLength(1), MaxLength(2000)] string Content
);

public record UpdateCommentRequest(
    [Required, MinLength(1), MaxLength(2000)] string Content
);

public record CommentResponse(
    int      Id,
    string   Content,
    string   AuthorUsername,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ── Hälsa (health) ─────────────────────────────────────────────────────────

public record HealthResponse(string Status, string Version, DateTime Timestamp);
