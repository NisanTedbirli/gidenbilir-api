using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Data;
using Perspektif.API.DTOs;
using Perspektif.API.Models;
using Perspektif.API.Services;
using Serilog;

namespace Perspektif.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });

        if (!await db.Nationalities.AnyAsync(n => n.Id == req.NationalityId))
            return BadRequest(new { message = "Geçersiz milliyet seçimi." });

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = req.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            NationalityId = req.NationalityId
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var full = await db.Users
            .Include(u => u.Nationality)
            .FirstAsync(u => u.Id == user.Id);

        return Ok(BuildAuthResponse(full));
    }

    // Email enumeration koruması: kullanıcı yoksa bile aynı süre cevap
    private const string DummyBcryptHash =
        "$2a$11$AbcdefghijklmnopqrstuvWxYz1234567890ABCDEFGHIJKLMNOPQRSTU";
    private const string GenericAuthError = "E-posta veya şifre hatalı.";

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users
            .Include(u => u.Nationality)
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower() && !u.IsDeleted);

        // Her zaman BCrypt çalıştır — timing attack koruması
        var passwordValid = user != null
            ? BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)
            : BCrypt.Net.BCrypt.Verify(req.Password, DummyBcryptHash);

        if (user == null || !passwordValid)
            return Unauthorized(new { message = GenericAuthError });

        return Ok(BuildAuthResponse(user));
    }

    // Token geçerli mi? Kullanıcı hâlâ DB'de var mı?
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.Include(u => u.Nationality).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.IsDeleted) return NotFound(new { message = "Hesap bulunamadı." });
        return Ok(BuildAuthResponse(user));
    }

    // POST /api/auth/forgot-password — Token üret, kullanıcıya e-posta (TODO) ile gönder
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Email == req.Email.ToLower() && !u.IsDeleted);

        // Email enumeration koruması: user yoksa bile aynı mesaj, dummy delay
        if (user != null)
        {
            // 32 byte random base64url
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            await db.SaveChangesAsync();

            // TODO: E-mail service ile reset link gönder.
            // Şimdilik log'a yazıyoruz; production'da Resend/SendGrid entegre edilecek.
            Log.Information("Password reset token for {Email}: {Token}", user.Email, rawToken);
        }
        else
        {
            // Constant-time benzeri davranış için dummy delay
            await Task.Delay(50);
        }

        return Ok(new
        {
            message = "Eğer bu e-posta kayıtlıysa, sıfırlama bağlantısı gönderildi."
        });
    }

    // POST /api/auth/reset-password — Token ile şifre sıfırlama
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(req.Token)));

        var prt = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (prt == null || prt.UsedAt != null || prt.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Geçersiz veya süresi dolmuş bağlantı." });

        prt.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        prt.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Şifreniz başarıyla güncellendi." });
    }

    // PUT /api/auth/profile — Profil bilgilerini güncelle (ad + milliyet)
    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.Include(u => u.Nationality).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "Hesap bulunamadı." });

        if (!await db.Nationalities.AnyAsync(n => n.Id == req.NationalityId))
            return BadRequest(new { message = "Geçersiz milliyet seçimi." });

        user.FullName = req.FullName.Trim();
        user.NationalityId = req.NationalityId;
        await db.SaveChangesAsync();

        var refreshed = await db.Users.Include(u => u.Nationality).FirstAsync(u => u.Id == userId);
        return Ok(BuildAuthResponse(refreshed));
    }

    // PUT /api/auth/password — Mevcut şifre doğrulamasıyla şifre değiştir
    [HttpPut("password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "Hesap bulunamadı." });

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mevcut şifre hatalı." });

        if (req.NewPassword.Length < 8)
            return BadRequest(new { message = "Yeni şifre en az 8 karakter olmalıdır." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return Ok(new { message = "Şifreniz başarıyla güncellendi." });
    }

    // DELETE /api/auth/account — Hesabı ve tüm verileri sil (şifre doğrulamasıyla)
    [HttpDelete("account")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "Hesap bulunamadı." });

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return BadRequest(new { message = "Şifre hatalı." });

        // Kullanıcının paylaşımları, yorumları, beğenileri silinir.
        // Konuşmalar ve mesajlar KORUNUR — karşı taraf geçmişi görebilsin diye.
        // Kullanıcı kaydı soft-delete: FullName "Bilinmeyen kullanıcı", giriş engellenir.
        var userExperienceIds = await db.Experiences.Where(e => e.UserId == userId).Select(e => e.Id).ToListAsync();
        if (userExperienceIds.Any())
        {
            db.ExperiencePhotos.RemoveRange(db.ExperiencePhotos.Where(p => userExperienceIds.Contains(p.ExperienceId)));
            db.ExperienceLikes.RemoveRange(db.ExperienceLikes.Where(l => userExperienceIds.Contains(l.ExperienceId)));
            db.Comments.RemoveRange(db.Comments.Where(c => userExperienceIds.Contains(c.ExperienceId)));
        }
        db.Comments.RemoveRange(db.Comments.Where(c => c.UserId == userId));
        db.ExperienceLikes.RemoveRange(db.ExperienceLikes.Where(l => l.UserId == userId));
        db.Experiences.RemoveRange(db.Experiences.Where(e => e.UserId == userId));

        // Soft-delete user (mesajlar için FK korunsun)
        user.IsDeleted = true;
        user.FullName = "Bilinmeyen kullanıcı";
        user.Email = $"deleted_{user.Id}@gidenbilir.local";
        user.PasswordHash = string.Empty; // login imkânsız
        await db.SaveChangesAsync();

        return Ok(new { message = "Hesabınız silindi. Konuşma geçmişiniz karşı tarafta korunur." });
    }

    // GET /api/auth/stats/{userId} — Kullanıcı istatistikleri (deneyim sayısı, alınan beğeni, ortalama puan)
    [HttpGet("stats/{userId:int}")]
    public async Task<IActionResult> GetUserStats(int userId)
    {
        var user = await db.Users.Include(u => u.Nationality).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        var experiences = db.Experiences.Where(e => e.UserId == userId);
        var experienceCount = await experiences.CountAsync();
        var totalLikes = await db.ExperienceLikes.CountAsync(l => experiences.Any(e => e.Id == l.ExperienceId));
        var averageRating = experienceCount > 0
            ? await experiences.AverageAsync(e => (double)e.Rating)
            : 0.0;

        return Ok(new UserStatsDto(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            NationalityCode: user.Nationality.Code,
            NationalityFlag: user.Nationality.FlagEmoji,
            CreatedAt: user.CreatedAt,
            ExperienceCount: experienceCount,
            TotalLikes: totalLikes,
            AverageRating: Math.Round(averageRating, 1)));
    }

    private AuthResponse BuildAuthResponse(User user) => new(
        Token: tokens.Generate(user),
        UserId: user.Id,
        FullName: user.FullName,
        Email: user.Email,
        NationalityCode: user.Nationality.Code,
        NationalityFlag: user.Nationality.FlagEmoji);
}
