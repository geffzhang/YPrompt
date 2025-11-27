using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Data;
using YPrompt.Api.Models.Entities;
using YPrompt.Api.Utils;

namespace YPrompt.Api.Services;

public interface IAuthService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByLinuxDoIdAsync(string linuxDoId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateOrUpdateUserFromLinuxDoAsync(LinuxDoUserInfo userInfo);
    Task<User> CreateLocalUserAsync(string username, string password, string? name);
    Task<User?> VerifyLocalUserAsync(string username, string password);
    Task UpdateLastLoginTimeAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly YPromptDbContext _context;
    private readonly IPasswordUtil _passwordUtil;
    private readonly ILinuxDoOAuth _linuxDoOAuth;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        YPromptDbContext context,
        IPasswordUtil passwordUtil,
        ILinuxDoOAuth linuxDoOAuth,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordUtil = passwordUtil;
        _linuxDoOAuth = linuxDoOAuth;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByLinuxDoIdAsync(string linuxDoId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.LinuxDoId == linuxDoId);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateOrUpdateUserFromLinuxDoAsync(LinuxDoUserInfo userInfo)
    {
        var linuxDoId = userInfo.Id.ToString();
        var existingUser = await GetUserByLinuxDoIdAsync(linuxDoId);

        var avatar = _linuxDoOAuth.GetAvatarUrl(userInfo.AvatarTemplate);
        var name = userInfo.Name ?? userInfo.Username ?? "未知用户";

        if (existingUser != null)
        {
            // Update existing user
            existingUser.Name = name;
            existingUser.LinuxDoUsername = userInfo.Username;
            existingUser.Avatar = avatar;
            existingUser.LastLoginTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Linux.do用户信息更新成功: id={Id}", existingUser.Id);

            return existingUser;
        }
        else
        {
            // Create new user
            var newUser = new User
            {
                LinuxDoId = linuxDoId,
                LinuxDoUsername = userInfo.Username,
                Name = name,
                Avatar = avatar,
                AuthType = "linux_do",
                IsActive = userInfo.Active ? 1 : 0,
                LastLoginTime = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Linux.do新用户创建成功: id={Id}", newUser.Id);

            return newUser;
        }
    }

    public async Task<User> CreateLocalUserAsync(string username, string password, string? name)
    {
        var existingUser = await GetUserByUsernameAsync(username);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"用户名 {username} 已存在");
        }

        var passwordHash = _passwordUtil.HashPassword(password);

        var newUser = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            Name = name ?? username,
            AuthType = "local",
            IsActive = 1,
            LastLoginTime = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 本地用户创建成功: username={Username}, id={Id}", username, newUser.Id);

        return newUser;
    }

    public async Task<User?> VerifyLocalUserAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.AuthType == "local");

        if (user == null)
        {
            _logger.LogWarning("⚠️  用户不存在: username={Username}", username);
            return null;
        }

        if (user.IsActive == 0)
        {
            _logger.LogWarning("⚠️  用户已被禁用: username={Username}", username);
            return null;
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("⚠️  用户密码哈希为空: username={Username}", username);
            return null;
        }

        if (!_passwordUtil.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("⚠️  密码错误: username={Username}", username);
            return null;
        }

        await UpdateLastLoginTimeAsync(user.Id);
        _logger.LogInformation("✅ 本地用户登录成功: username={Username}, id={Id}", username, user.Id);

        return user;
    }

    public async Task UpdateLastLoginTimeAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
