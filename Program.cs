using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebApplication3.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://listify-frontend-ten.vercel.app",
                "http://127.0.0.1:5500",
                "http://localhost:5500"
              )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. JWT Authentication using Supabase JWKS endpoint
var supabaseUrl = builder.Configuration["Supabase__Url"]
    ?? builder.Configuration["Supabase:Url"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = supabaseUrl + "/auth/v1";
        options.MetadataAddress = supabaseUrl + "/auth/v1/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("AUTH FAILED: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("TOKEN VALID: " + context.Principal?.FindFirst("sub")?.Value);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 3. Database — use PostgreSQL on Railway, SQLite locally
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=todo.db";

Console.WriteLine($"Connection string starts with: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}");

if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
{
    // Convert Railway's postgres:// URL format to Npgsql format
    connectionString = connectionString
        .Replace("postgresql://", "")
        .Replace("postgres://", "");

    var userInfo = connectionString.Split('@')[0];
    var hostInfo = connectionString.Split('@')[1];
    var user = userInfo.Split(':')[0];
    var password = userInfo.Split(':')[1];
    var host = hostInfo.Split('/')[0].Split(':')[0];
    var port = hostInfo.Split('/')[0].Contains(':') ? hostInfo.Split('/')[0].Split(':')[1] : "5432";
    var database = hostInfo.Split('/')[1].Split('?')[0];

    connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    Console.WriteLine("Using PostgreSQL");
    
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    Console.WriteLine("Using SQLite");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

var app = builder.Build();

// Auto-run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
