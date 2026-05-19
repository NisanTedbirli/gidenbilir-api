using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Controllers;
using Perspektif.API.Data;
using Perspektif.API.DTOs;
using Perspektif.API.Models;
using System.Security.Claims;

namespace Perspektif.API.Tests;

public class ExperiencesControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ExperiencesController _sut;
    private const int OwnerId = 1;

    public ExperiencesControllerTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        var nat = new Nationality { Id = 1, Name = "Türk", Code = "TR", FlagEmoji = "🇹🇷" };
        var user = new User { Id = OwnerId, FullName = "Test Kullanıcı", Email = "test@test.com", NationalityId = 1, Nationality = nat };
        var country = new Country { Id = 1, Name = "Almanya", Code = "DE", FlagEmoji = "🇩🇪" };
        var category = new Category { Id = 1, Name = "Yemek", Icon = "🍕" };
        _db.Nationalities.Add(nat);
        _db.Users.Add(user);
        _db.Countries.Add(country);
        _db.Categories.Add(category);
        _db.SaveChanges();

        _sut = new ExperiencesController(_db);
        SetAuthenticatedUser(OwnerId);
    }

    public void Dispose() => _db.Dispose();

    private void SetAuthenticatedUser(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private async Task<int> CreateTestExperience(string title = "Test Deneyimi")
    {
        var req = new ExperienceCreateRequest(title, "Bu bir test deneyimidir ve yeterince uzundur.", 4, 1, 1, null, "Berlin", null);
        var result = (CreatedAtActionResult)(await _sut.Create(req));
        var idObj = result.Value!;
        return (int)idObj.GetType().GetProperty("id")!.GetValue(idObj)!;
    }

    // ─── GetAll ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyPagedResult()
    {
        var result = await _sut.GetAll(null, null, null, null, null, null, null);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResult<ExperienceDto>>().Subject;
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WithExperiences_ReturnsPagedResult()
    {
        await CreateTestExperience("Berlin Macerası");
        await CreateTestExperience("Paris Günleri");

        var result = await _sut.GetAll(null, null, null, null, null, null, null);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResult<ExperienceDto>>().Subject;
        paged.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_WithCountryFilter_ReturnsFiltered()
    {
        await CreateTestExperience("Almanya Deneyimi");

        var result = await _sut.GetAll(countryId: 1, null, null, null, null, null, null);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResult<ExperienceDto>>().Subject;
        paged.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_PaginationWorks()
    {
        for (int i = 1; i <= 5; i++)
            await CreateTestExperience($"Deneyim {i}");

        var result = await _sut.GetAll(null, null, null, null, null, null, null, page: 1, pageSize: 3);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResult<ExperienceDto>>().Subject;
        paged.Items.Should().HaveCount(3);
        paged.TotalCount.Should().Be(5);
        paged.HasNextPage.Should().BeTrue();
    }

    // ─── Create ──────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithId()
    {
        var req = new ExperienceCreateRequest("Yeni Deneyim", "Açıklama en az 10 karakter olmalı.", 5, 1, 1, null, null, null);
        var result = await _sut.Create(req);
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    // ─── Delete ──────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_OwnExperience_Returns200()
    {
        var id = await CreateTestExperience();
        var result = await _sut.Delete(id);
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_OtherUsersExperience_ReturnsForbid()
    {
        var id = await CreateTestExperience();
        SetAuthenticatedUser(userId: 999);

        var result = await _sut.Delete(id);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        var result = await _sut.Delete(99999);
        result.Should().BeOfType<NotFoundResult>();
    }

    // ─── Update ──────────────────────────────────────────────────────

    [Fact]
    public async Task Update_OwnExperience_Returns200()
    {
        var id = await CreateTestExperience();
        var req = new ExperienceUpdateRequest("Güncellenmiş Başlık", "Güncellenmiş açıklama metni buraya yazılır.", 3, 1);
        var result = await _sut.Update(id, req);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_OtherUsersExperience_ReturnsForbid()
    {
        var id = await CreateTestExperience();
        SetAuthenticatedUser(userId: 999);
        var req = new ExperienceUpdateRequest("Başlık", "Açıklama metni buraya yazılır.", 3, 1);
        var result = await _sut.Update(id, req);
        result.Should().BeOfType<ForbidResult>();
    }

    // ─── ToggleLike ──────────────────────────────────────────────────

    [Fact]
    public async Task ToggleLike_AddsLike_ThenRemovesOnSecondCall()
    {
        var id = await CreateTestExperience();

        var r1 = await _sut.ToggleLike(id);
        var ok1 = r1.Should().BeOfType<OkObjectResult>().Subject;
        var v1 = ok1.Value!;
        ((int)v1.GetType().GetProperty("likeCount")!.GetValue(v1)!).Should().Be(1);
        ((bool)v1.GetType().GetProperty("isLikedByMe")!.GetValue(v1)!).Should().BeTrue();

        var r2 = await _sut.ToggleLike(id);
        var ok2 = r2.Should().BeOfType<OkObjectResult>().Subject;
        var v2 = ok2.Value!;
        ((int)v2.GetType().GetProperty("likeCount")!.GetValue(v2)!).Should().Be(0);
        ((bool)v2.GetType().GetProperty("isLikedByMe")!.GetValue(v2)!).Should().BeFalse();
    }
}
