using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Data;
using Perspektif.API.DTOs;
using Perspektif.API.Models;

namespace Perspektif.API.Controllers;

[ApiController]
[Route("api/experiences/{experienceId:int}/comments")]
public class CommentsController(AppDbContext db) : ControllerBase
{
    // GET /api/experiences/{id}/comments
    [HttpGet]
    public async Task<IActionResult> GetAll(int experienceId)
    {
        var comments = await db.Comments
            .Include(c => c.User).ThenInclude(u => u.Nationality)
            .Where(c => c.ExperienceId == experienceId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id, c.Text, c.CreatedAt,
                c.User.Id, c.User.FullName,
                c.User.Nationality.FlagEmoji))
            .ToListAsync();
        return Ok(comments);
    }

    // POST /api/experiences/{id}/comments
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Add(int experienceId, [FromBody] AddCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { message = "Yorum boş olamaz." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var exp = await db.Experiences.FindAsync(experienceId);
        if (exp == null) return NotFound();

        var comment = new Comment
        {
            ExperienceId = experienceId,
            UserId = userId,
            Text = req.Text.Trim()
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var user = await db.Users.Include(u => u.Nationality).FirstAsync(u => u.Id == userId);
        return Ok(new CommentDto(comment.Id, comment.Text, comment.CreatedAt,
            user.Id, user.FullName, user.Nationality.FlagEmoji));
    }

    // DELETE /api/experiences/{expId}/comments/{commentId}
    [HttpDelete("{commentId:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int experienceId, int commentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.ExperienceId == experienceId);
        if (comment == null) return NotFound();
        if (comment.UserId != userId) return Forbid();

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return Ok();
    }
}
