using Microsoft.EntityFrameworkCore;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Services;

// ── Interface ──────────────────────────────────────────────────────────────

public interface IPostService
{
    Task<IEnumerable<PostResponse>> GetAllAsync();
    Task<PostResponse?>             GetByIdAsync(int id);
    Task<PostResponse>              CreateAsync(CreatePostRequest request, int authorId);
    Task<PostResponse?>             UpdateAsync(int id, UpdatePostRequest request, int requesterId, string requesterRole);
    Task<bool>                      DeleteAsync(int id, int requesterId, string requesterRole);
}

// ── Implementation ─────────────────────────────────────────────────────────

public class PostService(AppDbContext db) : IPostService
{
    public async Task<IEnumerable<PostResponse>> GetAllAsync()
    {
        return await db.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ToResponse(p))
            .ToListAsync();
    }

    public async Task<PostResponse?> GetByIdAsync(int id)
    {
        var post = await db.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id);

        return post is null ? null : ToResponse(post);
    }

    public async Task<PostResponse> CreateAsync(CreatePostRequest request, int authorId)
    {
        var post = new Post
        {
            Title    = request.Title.Trim(),
            Content  = request.Content.Trim(),
            AuthorId = authorId
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync();
        await db.Entry(post).Reference(p => p.Author).LoadAsync();

        return ToResponse(post);
    }

    public async Task<PostResponse?> UpdateAsync(
        int id, UpdatePostRequest request,
        int requesterId, string requesterRole)
    {
        var post = await db.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post is null) return null;

        // Security: only author or admin may update
        if (post.AuthorId != requesterId && requesterRole != "Admin")
            return null;

        post.Title     = request.Title.Trim();
        post.Content   = request.Content.Trim();
        post.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ToResponse(post);
    }

    public async Task<bool> DeleteAsync(int id, int requesterId, string requesterRole)
    {
        var post = await db.Posts.FindAsync(id);

        if (post is null) return false;

        // Security: only author or admin may delete
        if (post.AuthorId != requesterId && requesterRole != "Admin")
            return false;

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Projection helper (no AutoMapper dependency) ───────────────────────
    private static PostResponse ToResponse(Post p) => new(
        p.Id,
        p.Title,
        p.Content,
        p.Author.Username,
        p.CreatedAt,
        p.UpdatedAt
    );
}
