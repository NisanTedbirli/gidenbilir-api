using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Data;
using Perspektif.API.DTOs;
using Perspektif.API.Models;
using Microsoft.Extensions.Logging;

namespace Perspektif.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExperiencesController(AppDbContext db) : ControllerBase
{
    private int? CurrentUserId =>
        User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            : null;

    // GET /api/experiences?countryId=2&nationalityId=1&categoryId=1&search=berlin&minRating=4&city=Berlin&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? countryId, [FromQuery] int? nationalityId,
        [FromQuery] int? categoryId, [FromQuery] string? search,
        [FromQuery] int? minRating, [FromQuery] string? city,
        [FromQuery] string? budgetLevel,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var uid = CurrentUserId;
        var query = db.Experiences
            .Include(x => x.User).ThenInclude(u => u.Nationality)
            .Include(x => x.Country).Include(x => x.Category)
            .Include(x => x.Photos.OrderBy(p => p.Order))
            .Include(x => x.Likes)
            .AsQueryable();

        if (countryId.HasValue)     query = query.Where(x => x.CountryId == countryId.Value);
        if (nationalityId.HasValue) query = query.Where(x => x.User.NationalityId == nationalityId.Value);
        if (categoryId.HasValue)    query = query.Where(x => x.CategoryId == categoryId.Value);
        if (minRating.HasValue)     query = query.Where(x => x.Rating >= minRating.Value);
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => x.City != null && x.City.ToLower().Contains(city.ToLower()));
        if (!string.IsNullOrWhiteSpace(budgetLevel))
            query = query.Where(x => x.BudgetLevel == budgetLevel);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(s) || x.Description.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<ExperienceDto>(
            Items: items.Select(x => Map(x, uid)),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            HasNextPage: page * pageSize < totalCount);

        return Ok(result);
    }

    // GET /api/experiences/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var uid = CurrentUserId;
        var x = await db.Experiences
            .Include(x => x.User).ThenInclude(u => u.Nationality)
            .Include(x => x.Country).Include(x => x.Category)
            .Include(x => x.Photos.OrderBy(p => p.Order))
            .Include(x => x.Likes)
            .FirstOrDefaultAsync(x => x.Id == id);
        return x == null ? NotFound() : Ok(Map(x, uid));
    }

    // GET /api/experiences/{id}/similar
    [HttpGet("{id:int}/similar")]
    public async Task<IActionResult> GetSimilar(int id)
    {
        var uid = CurrentUserId;
        var source = await db.Experiences.FindAsync(id);
        if (source == null) return NotFound();

        var list = await db.Experiences
            .Include(x => x.User).ThenInclude(u => u.Nationality)
            .Include(x => x.Country).Include(x => x.Category)
            .Include(x => x.Photos.OrderBy(p => p.Order))
            .Include(x => x.Likes)
            .Where(x => x.CountryId == source.CountryId && x.Id != id)
            .OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync();
        return Ok(list.Select(x => Map(x, uid)));
    }

    // POST /api/experiences
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ExperienceCreateRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (req.Rating is < 1 or > 5)
            return BadRequest(new { message = "Puan 1-5 arasında olmalıdır." });

        var exp = new Experience
        {
            Title = req.Title.Trim(), Description = req.Description.Trim(),
            Rating = req.Rating, CountryId = req.CountryId, CategoryId = req.CategoryId,
            UserId = userId, VisitDate = req.VisitDate,
            City = req.City?.Trim(), BudgetLevel = req.BudgetLevel
        };
        db.Experiences.Add(exp);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = exp.Id }, new { id = exp.Id });
    }

    // POST /api/experiences/{id}/like — toggle
    [HttpPost("{id:int}/like")]
    [Authorize]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var existing = await db.ExperienceLikes
            .FirstOrDefaultAsync(l => l.ExperienceId == id && l.UserId == userId);

        if (existing != null)
            db.ExperienceLikes.Remove(existing);
        else
            db.ExperienceLikes.Add(new ExperienceLike { ExperienceId = id, UserId = userId });

        await db.SaveChangesAsync();
        var count = await db.ExperienceLikes.CountAsync(l => l.ExperienceId == id);
        return Ok(new { likeCount = count, isLikedByMe = existing == null });
    }

    // GET /api/experiences/{id}/photos — id + url listesi (düzenleme ekranı için)
    [HttpGet("{id:int}/photos")]
    public async Task<IActionResult> GetPhotos(int id)
    {
        var photos = await db.ExperiencePhotos
            .Where(p => p.ExperienceId == id)
            .OrderBy(p => p.Order)
            .Select(p => new { id = p.Id, url = p.CloudinaryUrl, order = p.Order })
            .ToListAsync();
        return Ok(photos);
    }

    // POST /api/experiences/{id}/photos
    [HttpPost("{id:int}/photos")]
    [Authorize]
    public async Task<IActionResult> AddPhoto(int id, [FromBody] AddPhotoRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var exp = await db.Experiences.FindAsync(id);
        if (exp == null) return NotFound();
        if (exp.UserId != userId) return Forbid();

        var order = await db.ExperiencePhotos.CountAsync(p => p.ExperienceId == id);
        db.ExperiencePhotos.Add(new ExperiencePhoto
            { ExperienceId = id, CloudinaryUrl = req.CloudinaryUrl, PublicId = req.PublicId, Order = order });
        await db.SaveChangesAsync();
        return Ok();
    }

    // GET /api/experiences/user/{userId}
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var uid = CurrentUserId;
        var list = await db.Experiences
            .Include(x => x.User).ThenInclude(u => u.Nationality)
            .Include(x => x.Country).Include(x => x.Category)
            .Include(x => x.Photos.OrderBy(p => p.Order))
            .Include(x => x.Likes)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(list.Select(x => Map(x, uid)));
    }

    // PUT /api/experiences/{id}
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, ExperienceUpdateRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var exp = await db.Experiences.FindAsync(id);
        if (exp == null) return NotFound();
        if (exp.UserId != userId) return Forbid();

        exp.Title = req.Title.Trim(); exp.Description = req.Description.Trim();
        exp.Rating = req.Rating; exp.CategoryId = req.CategoryId;
        await db.SaveChangesAsync();
        return Ok(new { id = exp.Id });
    }

    // DELETE /api/experiences/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var exp = await db.Experiences.FindAsync(id);
        if (exp == null) return NotFound();
        if (exp.UserId != userId) return Forbid();

        // EF Core SaveChangesAsync tüm değişiklikleri tek transaction'da uygular
        db.ExperiencePhotos.RemoveRange(db.ExperiencePhotos.Where(p => p.ExperienceId == id));
        db.ExperienceLikes.RemoveRange(db.ExperienceLikes.Where(l => l.ExperienceId == id));
        db.Comments.RemoveRange(db.Comments.Where(c => c.ExperienceId == id));
        db.Experiences.Remove(exp);
        await db.SaveChangesAsync();
        return Ok();
    }

    private static ExperienceDto Map(Experience x, int? currentUserId) => new(
        x.Id, x.Title, x.Description, x.Rating, x.VisitDate, x.CreatedAt,
        x.Country?.Name ?? "—", x.Country?.FlagEmoji ?? "🌍",
        x.Category?.Name ?? "Genel", x.Category?.Icon ?? "💬",
        x.User?.Id ?? 0, x.User?.FullName ?? "—",
        x.User?.Nationality?.Name ?? "—", x.User?.Nationality?.FlagEmoji ?? "🌐",
        x.Photos.Select(p => p.CloudinaryUrl).ToList(),
        x.City, x.BudgetLevel,
        x.Likes.Count,
        currentUserId.HasValue && x.Likes.Any(l => l.UserId == currentUserId.Value));
}

public record AddPhotoRequest(string CloudinaryUrl, string PublicId);
