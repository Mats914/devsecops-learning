using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController(IPostService postService) : ControllerBase
{
    // GET /api/posts  – public
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await postService.GetAllAsync());

    // GET /api/posts/{id}  – public
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await postService.GetByIdAsync(id);
        return post is null ? NotFound() : Ok(post);
    }

    // POST /api/posts  – authenticated users only
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var authorId = GetCurrentUserId();
        var post     = await postService.CreateAsync(request, authorId);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    // PUT /api/posts/{id}  – authenticated; owner or admin
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostRequest request)
    {
        var requesterId   = GetCurrentUserId();
        var requesterRole = GetCurrentUserRole();

        var post = await postService.UpdateAsync(id, request, requesterId, requesterRole);

        if (post is null)
        {
            // Return 404 to avoid leaking existence to unauthorised callers
            return NotFound(new { message = "Post not found or access denied." });
        }

        return Ok(post);
    }

    // DELETE /api/posts/{id}  – authenticated; owner or admin
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var requesterId   = GetCurrentUserId();
        var requesterRole = GetCurrentUserRole();

        var deleted = await postService.DeleteAsync(id, requesterId, requesterRole);
        return deleted ? NoContent() : NotFound(new { message = "Post not found or access denied." });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim missing."));

    private string GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
