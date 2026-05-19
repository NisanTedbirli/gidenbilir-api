using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Data;
using Perspektif.API.DTOs;

namespace Perspektif.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupsController(AppDbContext db) : ControllerBase
{
    [HttpGet("nationalities")]
    [OutputCache(PolicyName = "lookups")]
    public async Task<IActionResult> Nationalities()
    {
        Response.Headers.CacheControl = "public, max-age=86400";
        return Ok(await db.Nationalities
            .OrderBy(n => n.Name)
            .Select(n => new LookupDto(n.Id, n.Name, n.FlagEmoji))
            .ToListAsync());
    }

    [HttpGet("countries")]
    [OutputCache(PolicyName = "lookups")]
    public async Task<IActionResult> Countries()
    {
        Response.Headers.CacheControl = "public, max-age=86400";
        return Ok(await db.Countries
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto(c.Id, c.Name, c.FlagEmoji))
            .ToListAsync());
    }

    [HttpGet("categories")]
    [OutputCache(PolicyName = "lookups")]
    public async Task<IActionResult> Categories()
    {
        Response.Headers.CacheControl = "public, max-age=86400";
        return Ok(await db.Categories
            .OrderBy(c => c.Id)
            .Select(c => new LookupDto(c.Id, c.Name, c.Icon))
            .ToListAsync());
    }
}
