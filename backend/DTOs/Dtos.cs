using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.DTOs;

// ── Auth ───────────────────────────────────────────────────────────────────

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50),
     RegularExpression(@"^[a-zA-Z0-9_]+$",
         ErrorMessage = "Username may only contain letters, digits, and underscores.")]
    string Username,

    [Required, MinLength(8), MaxLength(100)]
    string Password
);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record AuthResponse(
    string Token,
    string Username,
    string Role,
    DateTime ExpiresAt
);

// ── Posts ──────────────────────────────────────────────────────────────────

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
    string   AuthorUsername,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
