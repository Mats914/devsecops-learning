// Comment.cs – kommentar kopplad till ett inlägg och en författare.

using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

/// <summary>
/// Kommentar under ett blogginlägg.
/// </summary>
public class Comment
{
    public int    Id        { get; set; }

    [Required, MaxLength(2000)]
    public string Content   { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int  PostId   { get; set; }
    public Post Post     { get; set; } = null!;

    public int  AuthorId { get; set; }
    public User Author   { get; set; } = null!;
}
