using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController(IPostService postService, IImageService imageService) : ControllerBase
{
    // GET /api/posts?page=1&pageSize=10&search=...&author=...
    [HttpGet]
    [ProducesResponseType(typeof(PostsPagedResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int    page     = 1,
        [FromQuery] int    pageSize = 10,
        [FromQuery] string? search  = null,
        [FromQuery] string? author  = null)
        => Ok(await postService.GetAllAsync(page, pageSize, search, author));

    // GET /api/posts/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PostResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await postService.GetByIdAsync(id);
        return post is null ? NotFound() : Ok(post);
    }

    // POST /api/posts  (multipart/form-data to support image)
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PostResponse), 201)]
    public async Task<IActionResult> Create(
        [FromForm] CreatePostRequest request,
        IFormFile? image)
    {
        string? imagePath = null;
        if (image is not null)
        {
            imagePath = await imageService.SaveImageAsync(image);
            if (imagePath is null)
                return BadRequest(new { message = "Invalid image. Use JPG/PNG under 5 MB." });
        }

        var post = await postService.CreateAsync(request, GetUserId(), imagePath);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    // PUT /api/posts/{id}
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostRequest request)
    {
        var post = await postService.UpdateAsync(id, request, GetUserId(), GetRole());
        return post is null ? NotFound(new { message = "Post not found or access denied." }) : Ok(post);
    }

    // DELETE /api/posts/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await postService.DeleteAsync(id, GetUserId(), GetRole());
        return ok ? NoContent() : NotFound(new { message = "Post not found or access denied." });
    }

    private int    GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole()   => User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
