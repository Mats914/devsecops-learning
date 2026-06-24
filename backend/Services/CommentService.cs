// CommentService.cs – kommentarer kopplade till inlägg.
// Samma mönster som PostService: ägare eller Admin får ändra/radera.

using Microsoft.EntityFrameworkCore;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Services;

public interface ICommentService
{
    Task<IEnumerable<CommentResponse>> GetByPostAsync(int postId);
    Task<CommentResponse?>             CreateAsync(int postId, CreateCommentRequest req, int authorId);
    Task<CommentResponse?>             UpdateAsync(int id, UpdateCommentRequest req, int requesterId, string role);
    Task<bool>                         DeleteAsync(int id, int requesterId, string role);
}

/// <summary>
/// Hanterar kommentarer under ett visst inlägg.
/// </summary>
public class CommentService(AppDbContext db) : ICommentService
{
    public async Task<IEnumerable<CommentResponse>> GetByPostAsync(int postId) =>
        await db.Comments
            .Include(c => c.Author)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => ToResponse(c))
            .ToListAsync();

    public async Task<CommentResponse?> CreateAsync(int postId, CreateCommentRequest req, int authorId)
    {
        // Man kan bara kommentera på publicerade inlägg
        var postExists = await db.Posts.AnyAsync(p => p.Id == postId && p.IsPublished);
        if (!postExists) return null;

        var comment = new Comment
        {
            PostId   = postId,
            Content  = req.Content.Trim(),
            AuthorId = authorId
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        await db.Entry(comment).Reference(c => c.Author).LoadAsync();
        return ToResponse(comment);
    }

    public async Task<CommentResponse?> UpdateAsync(int id, UpdateCommentRequest req, int requesterId, string role)
    {
        var comment = await db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null) return null;
        if (comment.AuthorId != requesterId && role != "Admin") return null;

        comment.Content   = req.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToResponse(comment);
    }

    public async Task<bool> DeleteAsync(int id, int requesterId, string role)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment is null) return false;
        if (comment.AuthorId != requesterId && role != "Admin") return false;
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return true;
    }

    private static CommentResponse ToResponse(Comment c) =>
        new(c.Id, c.Content, c.Author.Username, c.CreatedAt, c.UpdatedAt);
}
