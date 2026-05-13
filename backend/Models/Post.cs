using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

public class Post
{
    public int    Id        { get; set; }

    [Required, MaxLength(200)]
    public string Title     { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string Content   { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public int    AuthorId  { get; set; }
    public User   Author    { get; set; } = null!;
}
