using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Perspektif.API.Common;
using Perspektif.API.Data;
using Perspektif.API.Services;
using Serilog;

// Serilog bootstrap
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Port configuration — Render uses PORT env var
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .Enrich.FromLogContext()
    // Hassas veriyi log'lardan exclude et
    .Filter.ByExcluding(e =>
        e.MessageTemplate.Text.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        e.MessageTemplate.Text.Contains("token", StringComparison.OrdinalIgnoreCase)));

// === Configuration ===

// DB — Postgres veya SQLite (geriye dönük uyumluluk).
// DATABASE_URL env var verilirse Postgres, yoksa appsettings'taki SQLite.
var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    if (connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        opts.UseNpgsql(connectionString);
    else
        opts.UseSqlite(connectionString);
});

// JWT — env var override into config (production'da Render/Railway JWT_SECRET kullanır)
var jwtSecretEnv = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrEmpty(jwtSecretEnv))
{
    builder.Configuration["Jwt:Key"] = jwtSecretEnv;
}

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(o => !string.IsNullOrEmpty(o.Key) && o.Key.Length >= 32,
        "Jwt:Key en az 32 karakter olmalı (JWT_SECRET env var veya appsettings).")
    .ValidateOnStart();

// JWT validation parameters — config'ten direkt oku (BuildServiceProvider'dan kaçın)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSection["Key"] ?? "";
var jwtIssuer = jwtSection["Issuer"] ?? "perspektif.api";
var jwtAudience = jwtSection["Audience"] ?? "perspektif.clients";

if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key en az 32 karakter olmalı.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        // Token validation — soft-deleted user'ları reddet
        opts.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var nameId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(nameId, out var userId))
                {
                    ctx.Fail("Geçersiz token.");
                    return;
                }

                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var isDeleted = await db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => (bool?)u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (isDeleted != false) // null (yok) veya true → fail
                    ctx.Fail("Hesap geçersiz.");
            }
        };
    });

// Rate limit — global + auth specific
builder.Services.AddRateLimiter(opts =>
{
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    opts.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10_000_000; // 10 MB
});

// OutputCache — lookup'lar için 24 saat
builder.Services.AddOutputCache(opts =>
{
    opts.AddBasePolicy(b => b.NoCache());
    opts.AddPolicy("lookups", b => b
        .Expire(TimeSpan.FromDays(1))
        .SetVaryByHost(false));
});

// Response compression
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// CORS — env var öncelikli
var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ORIGINS")
    ?? builder.Configuration["Cors:Origins"]
    ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(corsOrigins)
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
        .WithHeaders("Content-Type", "Authorization", "Accept")
        .AllowCredentials()));

var app = builder.Build();

// === Pipeline ===

// Migrations — production'da RUN_MIGRATIONS=true env ile çalışır
if (app.Environment.IsDevelopment() ||
    Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Security headers — tüm response'lara
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    if (!app.Environment.IsDevelopment())
    {
        ctx.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    }

    await next();
});

// Exception handler — traceId ile production-friendly
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json";
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        var traceId = ctx.TraceIdentifier;

        if (feature != null)
            Log.Error(feature.Error, "Unhandled exception — traceId={TraceId}", traceId);

        var message = app.Environment.IsDevelopment() && feature != null
            ? (feature.Error.InnerException?.Message ?? feature.Error.Message)
            : "Bir hata oluştu. Lütfen tekrar deneyin.";

        await ctx.Response.WriteAsJsonAsync(new { message, traceId });
    });
});

app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseCors();

app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Root endpoint
app.MapGet("/", () => Results.Ok(new { message = "GidenBilir API is running", environment = app.Environment.EnvironmentName }));

// Health — public minimal "OK"
app.MapGet("/health", () => Results.Text("OK"));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

// Swagger — production'da da aç (test + monitoring için)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GidenBilir API v1");
});

// Detailed health — sadece development (info leak'i önle)
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecks("/health/details", new HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
            };
            await ctx.Response.WriteAsJsonAsync(result);
        }
    });
}

// Expo web build (legacy mobile launcher için)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

// === Helpers ===

static string ResolveConnectionString(IConfiguration config)
{
    // 1) Production: DATABASE_URL (Supabase/Railway formatı)
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(dbUrl))
    {
        try
        {
            var uri = new Uri(dbUrl);
            var userInfo = uri.UserInfo.Split(':');
            var user = Uri.UnescapeDataString(userInfo[0]);
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
                   $"Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
        }
        catch
        {
            // Eğer zaten Npgsql formatındaysa olduğu gibi kullan
            return dbUrl;
        }
    }

    // 2) appsettings'tan default
    return config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string yok.");
}

// WebApplicationFactory'nin Program'ı bulabilmesi için
public partial class Program { }
