using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Perspektif.API.Controllers;
using Perspektif.API.Data;
using Perspektif.API.DTOs;
using Perspektif.API.Models;
using Perspektif.API.Services;

namespace Perspektif.API.Tests;

public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        _db.Nationalities.Add(new Nationality { Id = 1, Name = "Türk", Code = "TR", FlagEmoji = "🇹🇷" });
        _db.SaveChanges();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = "TestSecretKeyForUnitTests_MinimumLength32Chars!",
                ["Jwt:Issuer"]   = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        var tokens = new TokenService(config);
        _sut = new AuthController(_db, tokens);
    }

    public void Dispose() => _db.Dispose();

    // ─── Register ──────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns200AndToken()
    {
        var req = new RegisterRequest("Test Kullanıcı", "test@example.com", "Sifre1234!", 1);
        var result = await _sut.Register(req);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var req = new RegisterRequest("Test", "duplicate@example.com", "Sifre1234!", 1);
        await _sut.Register(req);

        var result = await _sut.Register(req);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WithInvalidNationality_Returns400()
    {
        var req = new RegisterRequest("Test", "new@example.com", "Sifre1234!", 999);
        var result = await _sut.Register(req);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Login ──────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200()
    {
        await _sut.Register(new RegisterRequest("Test", "login@example.com", "Sifre1234!", 1));

        var result = await _sut.Login(new LoginRequest("login@example.com", "Sifre1234!"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await _sut.Register(new RegisterRequest("Test", "pass@example.com", "DogruSifre123!", 1));

        var result = await _sut.Login(new LoginRequest("pass@example.com", "YanlisPassword!"));
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var result = await _sut.Login(new LoginRequest("nobody@example.com", "Sifre1234!"));
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        await _sut.Register(new RegisterRequest("Test", "case@example.com", "Sifre1234!", 1));

        var result = await _sut.Login(new LoginRequest("CASE@EXAMPLE.COM", "Sifre1234!"));
        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── ResetPassword ──────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithValidEmail_Returns200()
    {
        await _sut.Register(new RegisterRequest("Test", "reset@example.com", "EskiSifre123!", 1));

        var result = await _sut.ResetPassword(new ResetPasswordRequest("reset@example.com", "YeniSifre123!"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_WithUnknownEmail_Returns404()
    {
        var result = await _sut.ResetPassword(new ResetPasswordRequest("nobody@example.com", "Sifre1234!"));
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_ThenLoginWithNewPassword_Succeeds()
    {
        await _sut.Register(new RegisterRequest("Test", "flow@example.com", "EskiSifre123!", 1));
        await _sut.ResetPassword(new ResetPasswordRequest("flow@example.com", "YeniSifre456!"));

        var loginResult = await _sut.Login(new LoginRequest("flow@example.com", "YeniSifre456!"));
        loginResult.Should().BeOfType<OkObjectResult>();
    }
}
