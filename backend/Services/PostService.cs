// PostService.cs – CRUD för blogginlägg med paginering, sök och behörighetskontroll.
// Bara publicerade inlägg syns publikt; ägare eller Admin får redigera/radera.

using Microsoft.EntityFrameworkCore;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Services;

public interface IPostService
{
    Task<PostsPagedResponse>  GetAllAsync(int page, int pageSize, string? search, string? author);
    Task<PostResponse?>        GetByIdAsync(int id);
    Task<PostResponse>         CreateAsync(CreatePostRequest req, int authorId, string? imagePath);
    Task<PostResponse?>        UpdateAsync(int id, UpdatePostRequest req, int requesterId, string role);
    Task<bool>                 DeleteAsync(int id, int requesterId, string role);
}

/// <summary>
/// Affärslogik för inlägg – filtrering, sidindelning och åtkomstkontroll.
/// </summary>
public class PostService(AppDbContext db) : IPostService
{
    public async Task<PostsPagedResponse> GetAllAsync(int page, int pageSize, string? search, string? author)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);  // max 50 per sida så ingen kan hämta hela DB:n

        var query = db.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Where(p => p.IsPublished)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(s) ||
                p.Content.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            var a = author.Trim().ToLower();
            query = query.Where(p => p.Author.Username.ToLower() == a);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToResponse(p))
            .ToListAsync();

        return new PostsPagedResponse(items, page, pageSize, total,
            (int)Math.Ceiling((double)total / pageSize));
    }

    public async Task<PostResponse?> GetByIdAsync(int id)
    {
        var post = await db.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

        if (post is null) return null;

        // Räkna upp visningar varje gång någon öppnar inlägget
        post.ViewCount++;
        await db.SaveChangesAsync();

        return ToResponse(post);
    }

    public async Task<PostResponse> CreateAsync(CreatePostRequest req, int authorId, string? imagePath)
    {
        var post = new Post
        {
            Title     = req.Title.Trim(),
            Content   = req.Content.Trim(),
            AuthorId  = authorId,
            ImagePath = imagePath
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        await db.Entry(post).Reference(p => p.Author).LoadAsync();
        return ToResponse(post);
    }

    public async Task<PostResponse?> UpdateAsync(int id, UpdatePostRequest req, int requesterId, string role)
    {
        var post = await db.Posts.Include(p => p.Author).Include(p => p.Comments)
                                  .FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return null;
        // Bara skribenten själv eller Admin får ändra
        if (post.AuthorId != requesterId && role != "Admin") return null;

        post.Title     = req.Title.Trim();
        post.Content   = req.Content.Trim();
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToResponse(post);
    }

    public async Task<bool> DeleteAsync(int id, int requesterId, string role)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return false;
        if (post.AuthorId != requesterId && role != "Admin") return false;
        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return true;
    }

    private static PostResponse ToResponse(Post p) => new(
        p.Id, p.Title, p.Content,
        p.ImagePath is not null ? $"/uploads/{p.ImagePath}" : null,
        p.Author.Username, p.ViewCount, p.Comments.Count,
        p.CreatedAt, p.UpdatedAt);
}
