using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using DevSecOpsApi.Data;
using DevSecOpsApi.Middleware;
using DevSecOpsApi.Services;

// ── Serilog bootstrap ──────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "logs/app-.log",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── JWT ────────────────────────────────────────────────────────────────
    var jwtKey      = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key not configured.");
    var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "DevSecOpsApi";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DevSecOpsClient";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtIssuer,
                ValidAudience            = jwtAudience,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew                = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization(o =>
    {
        o.AddPolicy("AdminOnly",   p => p.RequireRole("Admin"));
        o.AddPolicy("UserOrAdmin", p => p.RequireRole("User", "Admin"));
    });

    // ── Database ───────────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
             ?? "Data Source=app.db"));

    // ── Health Checks ──────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    // ── Rate Limiting ──────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(
        builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // ── Services ───────────────────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService,    AuthService>();
    builder.Services.AddScoped<IPostService,    PostService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IAuditService,   AuditService>();
    builder.Services.AddScoped<IImageService,   ImageService>();
    builder.Services.AddScoped<IEmailService,   EmailService>();

    // ── CORS ───────────────────────────────────────────────────────────────
    builder.Services.AddCors(o =>
        o.AddPolicy("Frontend", p =>
            p.WithOrigins("http://localhost:5173", "http://localhost:3000")
             .AllowAnyHeader()
             .AllowAnyMethod()));

    // ── Static files (image uploads) ───────────────────────────────────────
    builder.Services.AddDirectoryBrowser();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "DevSecOps Demo API v2",
            Version     = "v2",
            Description = "Secure .NET 8 Web API with DevSecOps best practices."
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Bearer token",
            Name        = "Authorization",
            In          = ParameterLocation.Header,
            Type        = SecuritySchemeType.ApiKey,
            Scheme      = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ── Migrate & seed ─────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    // ── Pipeline ───────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseIpRateLimiting();

    app.UseStaticFiles();   // serve uploaded images from wwwroot/uploads
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Dedicated health endpoint (used by CI/CD)
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResultStatusCodes =
        {
            [HealthStatus.Healthy]   = 200,
            [HealthStatus.Degraded]  = 200,
            [HealthStatus.Unhealthy] = 503
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
