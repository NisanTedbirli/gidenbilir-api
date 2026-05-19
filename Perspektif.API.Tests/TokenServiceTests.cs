using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Perspektif.API.Models;
using Perspektif.API.Services;

namespace Perspektif.API.Tests;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly User _testUser;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = "TestSecretKeyForUnitTests_MinimumLength32Chars!",
                ["Jwt:Issuer"]   = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        _sut = new TokenService(config);

        _testUser = new User
        {
            Id = 42,
            FullName = "Ahmet Yılmaz",
            Email = "ahmet@test.com",
            NationalityId = 1,
            Nationality = new Nationality { Id = 1, Name = "Türk", Code = "TR", FlagEmoji = "🇹🇷" }
        };
    }

    [Fact]
    public void Generate_ReturnsNonEmptyToken()
    {
        var token = _sut.Generate(_testUser);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Generate_TokenContainsCorrectUserId()
    {
        var token = _sut.Generate(_testUser);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var idClaim = parsed.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                   ?? parsed.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

        idClaim.Should().Be("42");
    }

    [Fact]
    public void Generate_TokenContainsCorrectEmail()
    {
        var token = _sut.Generate(_testUser);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var emailClaim = parsed.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

        emailClaim.Should().Be("ahmet@test.com");
    }

    [Fact]
    public void Generate_TokenContainsCorrectFullName()
    {
        var token = _sut.Generate(_testUser);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var nameClaim = parsed.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Name || c.Type == "unique_name")?.Value;

        nameClaim.Should().Be("Ahmet Yılmaz");
    }

    [Fact]
    public void Generate_TokenExpiresInApprox30Days()
    {
        var before = DateTime.UtcNow.AddDays(29);
        var token = _sut.Generate(_testUser);
        var after = DateTime.UtcNow.AddDays(31);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.ValidTo.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Generate_TokenHasCorrectIssuer()
    {
        var token = _sut.Generate(_testUser);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Issuer.Should().Be("test-issuer");
    }

    [Fact]
    public void Generate_DifferentUsersDifferentTokens()
    {
        var user2 = new User { Id = 99, FullName = "Zeynep Kaya", Email = "zeynep@test.com", NationalityId = 1 };
        var token1 = _sut.Generate(_testUser);
        var token2 = _sut.Generate(user2);
        token1.Should().NotBe(token2);
    }
}
