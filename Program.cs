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
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

// 2. JWT Authentication using Supabase JWKS endpoint
// Supabase new-format tokens use RS256 with a key ID (kid)
// We validate using their public JWKS endpoint instead of the raw secret
var supabaseUrl = builder.Configuration["Supabase:Url"];

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

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite("Data Source=todo.db"));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();