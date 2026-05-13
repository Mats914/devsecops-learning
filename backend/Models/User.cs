using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

public class User
{
    public int    Id           { get; set; }

    [Required, MaxLength(50)]
    public string Username     { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role         { get; set; } = "User";   // "User" | "Admin"

    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Post> Posts { get; set; } = [];
}
