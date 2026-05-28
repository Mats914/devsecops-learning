using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Controllers;

[ApiController]
[Route("api/posts/{postId:int}/comments")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    // GET /api/posts/{postId}/comments
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CommentResponse>), 200)]
    public async Task<IActionResult> GetAll(int postId) =>
        Ok(await commentService.GetByPostAsync(postId));

    // POST /api/posts/{postId}/comments
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponse), 201)]
    public async Task<IActionResult> Create(int postId, [FromBody] CreateCommentRequest request)
    {
        var comment = await commentService.CreateAsync(postId, request, GetUserId());
        if (comment is null) return NotFound(new { message = "Post not found." });
        return CreatedAtAction(nameof(GetAll), new { postId }, comment);
    }

    // PUT /api/posts/{postId}/comments/{id}
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int postId, int id, [FromBody] UpdateCommentRequest request)
    {
        var comment = await commentService.UpdateAsync(id, request, GetUserId(), GetRole());
        return comment is null ? NotFound(new { message = "Comment not found or access denied." }) : Ok(comment);
    }

    // DELETE /api/posts/{postId}/comments/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int postId, int id)
    {
        var ok = await commentService.DeleteAsync(id, GetUserId(), GetRole());
        return ok ? NoContent() : NotFound(new { message = "Comment not found or access denied." });
    }

    private int    GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole()   => User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
