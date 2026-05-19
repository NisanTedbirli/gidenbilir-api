using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Common;
using Perspektif.API.Data;
using Perspektif.API.Models;
using Perspektif.API.Services;

namespace Perspektif.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadsController(AppDbContext db, CloudinaryService cloudinary) : ControllerBase
{
    // POST /api/uploads/experience/{experienceId}
    // multipart/form-data: file=<image>
    [HttpPost("experience/{experienceId}")]
    [RequestSizeLimit(10_000_000)] // 10 MB
    public async Task<IActionResult> UploadExperiencePhoto(int experienceId, IFormFile file)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var experience = await db.Experiences.FindAsync(experienceId);
        if (experience == null) return NotFound(new { message = "Deneyim bulunamadı." });
        if (experience.UserId != userId) return Forbid();

        var currentCount = await db.ExperiencePhotos.CountAsync(p => p.ExperienceId == experienceId);
        if (currentCount >= 5)
            return BadRequest(new { message = "Bir deneyime en fazla 5 fotoğraf eklenebilir." });

        // Magic byte validation — content-type ile dosya başlığı eşleşmeli
        if (!await FileValidator.IsValidImageAsync(file))
            return BadRequest(new { message = "Geçersiz görsel dosyası. JPG, PNG, WebP veya GIF olmalı, 10 MB'tan küçük olmalı." });

        var (url, publicId) = await cloudinary.UploadAsync(file);

        db.ExperiencePhotos.Add(new ExperiencePhoto
        {
            ExperienceId = experienceId,
            CloudinaryUrl = url,
            PublicId = publicId,
            Order = currentCount
        });
        await db.SaveChangesAsync();

        return Ok(new { url, publicId });
    }

    // DELETE /api/uploads/photo/{photoId}
    [HttpDelete("photo/{photoId}")]
    public async Task<IActionResult> DeletePhoto(int photoId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var photo = await db.ExperiencePhotos
            .Include(p => p.Experience)
            .FirstOrDefaultAsync(p => p.Id == photoId);

        if (photo == null) return NotFound();
        if (photo.Experience.UserId != userId) return Forbid();

        await cloudinary.DeleteAsync(photo.PublicId);
        db.ExperiencePhotos.Remove(photo);
        await db.SaveChangesAsync();

        return Ok();
    }
}
