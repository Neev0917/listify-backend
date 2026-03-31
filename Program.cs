using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebApplication3.Models;
using Npgsql;

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
var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url")
    ?? builder.Configuration["Supabase__Url"]
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

// 3. Database configuration
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("Using PostgreSQL from DATABASE_URL");
    
    // Use NpgsqlConnectionStringBuilder to parse the URL
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo.Length > 1 ? userInfo[1] : "",
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    };

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(npgsqlBuilder.ConnectionString));
}
else
{
    Console.WriteLine("Using SQLite (local)");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=todo.db"));
}

var app = builder.Build();

// Auto-create tables on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        Console.WriteLine("Database ready!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
    }
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
