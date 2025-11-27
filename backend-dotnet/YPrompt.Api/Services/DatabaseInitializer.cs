using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Data;
using YPrompt.Api.Models.Entities;
using YPrompt.Api.Utils;

namespace YPrompt.Api.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly YPromptDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordUtil _passwordUtil;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        YPromptDbContext context,
        IConfiguration configuration,
        IPasswordUtil passwordUtil,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _configuration = configuration;
        _passwordUtil = passwordUtil;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("✅ 数据库结构已确保创建");

            // Create or sync admin account
            await SyncAdminAccountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 数据库初始化失败: {Message}", ex.Message);
            throw;
        }
    }

    private async Task SyncAdminAccountAsync()
    {
        var adminUsername = _configuration["DefaultAdmin:Username"] ?? "admin";
        var adminPassword = _configuration["DefaultAdmin:Password"] ?? "admin123";
        var adminName = _configuration["DefaultAdmin:Name"] ?? "管理员";

        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == adminUsername && u.AuthType == "local");

        if (existingAdmin != null)
        {
            // Check if password needs to be updated
            if (!_passwordUtil.VerifyPassword(adminPassword, existingAdmin.PasswordHash ?? ""))
            {
                existingAdmin.PasswordHash = _passwordUtil.HashPassword(adminPassword);
                existingAdmin.Name = adminName;
                await _context.SaveChangesAsync();
                _logger.LogInformation("🔄 管理员账号密码已更新: {Username}", adminUsername);
            }
            else
            {
                _logger.LogInformation("✅ 管理员账号配置正确: {Username}", adminUsername);
            }
        }
        else
        {
            // Create admin account
            var admin = new User
            {
                Username = adminUsername,
                PasswordHash = _passwordUtil.HashPassword(adminPassword),
                Name = adminName,
                AuthType = "local",
                IsAdmin = 1,
                IsActive = 1
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ 管理员账号创建成功: {Username} / {Password}", adminUsername, adminPassword);
        }
    }
}
