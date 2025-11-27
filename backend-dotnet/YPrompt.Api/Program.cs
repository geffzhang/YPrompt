using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YPrompt.Api.Data;
using YPrompt.Api.Services;
using YPrompt.Api.Utils;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// Configure Database
// ====================================
var dbType = builder.Configuration["Database:Type"] ?? "sqlite";

// Ensure data directory exists
var dataDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "data"));
Directory.CreateDirectory(dataDir);

var sqlitePath = Path.Combine(dataDir, "yprompt.db");
var connectionString = dbType.ToLower() switch
{
    "mysql" => builder.Configuration.GetConnectionString("MySql"),
    _ => $"Data Source={sqlitePath}"
};

if (dbType.ToLower() == "mysql")
{
    builder.Services.AddDbContext<YPromptDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}
else
{
    builder.Services.AddDbContext<YPromptDbContext>(options =>
        options.UseSqlite(connectionString));
}

// ====================================
// Configure JWT Authentication
// ====================================
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-secret-key-change-in-production";
var key = Encoding.UTF8.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ====================================
// Configure CORS
// ====================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ====================================
// Configure Services (Dependency Injection)
// ====================================
builder.Services.AddScoped<IJwtUtil, JwtUtil>();
builder.Services.AddScoped<IPasswordUtil, PasswordUtil>();
builder.Services.AddScoped<IUsernameUtil, UsernameUtil>();
builder.Services.AddHttpClient<ILinuxDoOAuth, LinuxDoOAuth>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<IVersionService, VersionService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IPromptRulesService, PromptRulesService>();
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

// ====================================
// Configure Controllers and Swagger
// ====================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ====================================
// Initialize Database
// ====================================
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await dbInitializer.InitializeAsync();
}

// ====================================
// Configure HTTP Pipeline
// ====================================
// Enable Swagger in all environments for this API
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "YPrompt API v1");
    c.RoutePrefix = "docs";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow });

// OpenAPI endpoint
app.MapGet("/openapi.json", async context =>
{
    context.Response.Redirect("/swagger/v1/swagger.json");
    await Task.CompletedTask;
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8888";
app.Urls.Add($"http://127.0.0.1:{port}");

Console.WriteLine($"📦 YPrompt API 正在启动于端口 {port}...");
Console.WriteLine($"📖 Swagger 文档: http://localhost:{port}/docs");

app.Run();
