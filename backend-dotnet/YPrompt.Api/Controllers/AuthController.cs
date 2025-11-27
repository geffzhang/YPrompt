using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Services;
using YPrompt.Api.Utils;

namespace YPrompt.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtUtil _jwtUtil;
    private readonly ILinuxDoOAuth _linuxDoOAuth;
    private readonly IPasswordUtil _passwordUtil;
    private readonly IUsernameUtil _usernameUtil;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtUtil jwtUtil,
        ILinuxDoOAuth linuxDoOAuth,
        IPasswordUtil passwordUtil,
        IUsernameUtil usernameUtil,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtUtil = jwtUtil;
        _linuxDoOAuth = linuxDoOAuth;
        _passwordUtil = passwordUtil;
        _usernameUtil = usernameUtil;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Linux.do OAuth登录
    /// </summary>
    [HttpPost("linux-do/login")]
    public async Task<ActionResult<ApiResponse<LoginData>>> LinuxDoLogin([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return Ok(ApiResponse<LoginData>.Error(400, "缺少code参数"));
            }

            // Get user info from Linux.do
            var userInfo = await _linuxDoOAuth.GetUserByCodeAsync(request.Code);

            // Create or update user
            var user = await _authService.CreateOrUpdateUserFromLinuxDoAsync(userInfo);

            // Generate JWT token
            var token = _jwtUtil.GenerateToken(user.Id, user.LinuxDoId ?? "", expireHours: 24 * 7);

            _logger.LogInformation("✅ Linux.do用户登录: id={Id}, username={Username}",
                user.Id, user.LinuxDoUsername);

            return Ok(ApiResponse<LoginData>.Success(new LoginData
            {
                Token = token,
                User = MapToUserInfo(user, "linux_do")
            }, "登录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Linux.do登录接口异常: {Message}", ex.Message);
            return Ok(ApiResponse<LoginData>.Error(500, $"登录失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 本地用户名密码登录
    /// </summary>
    [HttpPost("local/login")]
    public async Task<ActionResult<ApiResponse<LoginData>>> LocalLogin([FromBody] LocalLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return Ok(ApiResponse<LoginData>.Error(400, "用户名和密码不能为空"));
            }

            var user = await _authService.VerifyLocalUserAsync(request.Username.Trim(), request.Password);

            if (user == null)
            {
                return Ok(ApiResponse<LoginData>.Error(400, "用户名或密码错误"));
            }

            var token = _jwtUtil.GenerateToken(user.Id, request.Username.Trim(), expireHours: 24 * 7);

            _logger.LogInformation("✅ 本地用户登录成功: username={Username}, id={Id}",
                request.Username, user.Id);

            return Ok(ApiResponse<LoginData>.Success(new LoginData
            {
                Token = token,
                User = MapToUserInfo(user, "local")
            }, "登录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 本地登录接口异常: {Message}", ex.Message);
            return Ok(ApiResponse<LoginData>.Error(500, $"登录失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 本地用户注册
    /// </summary>
    [HttpPost("local/register")]
    public async Task<ActionResult<ApiResponse<object>>> LocalRegister([FromBody] LocalRegisterRequest request)
    {
        try
        {
            var username = request.Username.Trim();
            var password = request.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Ok(ApiResponse<object>.Error(400, "用户名和密码不能为空"));
            }

            // Validate username
            var (isValidUsername, usernameError) = _usernameUtil.ValidateUsername(username);
            if (!isValidUsername)
            {
                return Ok(ApiResponse<object>.Error(400, usernameError));
            }

            // Validate password
            var (isValidPassword, passwordError) = _passwordUtil.ValidatePasswordStrength(password);
            if (!isValidPassword)
            {
                return Ok(ApiResponse<object>.Error(400, passwordError));
            }

            var user = await _authService.CreateLocalUserAsync(username, password, request.Name?.Trim() ?? username);

            _logger.LogInformation("✅ 本地用户注册成功: username={Username}, id={Id}",
                username, user.Id);

            return Ok(ApiResponse<object>.Success(new
            {
                id = user.Id,
                username,
                name = user.Name
            }, "注册成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<object>.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 本地注册接口异常: {Message}", ex.Message);
            return Ok(ApiResponse<object>.Error(500, $"注册失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 刷新Token
    /// </summary>
    [HttpPost("refresh")]
    public ActionResult<ApiResponse<RefreshTokenData>> RefreshToken()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Ok(ApiResponse<RefreshTokenData>.Error(401, "缺少有效的Token"));
            }

            var oldToken = authHeader.Substring("Bearer ".Length);
            var newToken = _jwtUtil.RefreshToken(oldToken, expireHours: 24 * 7);

            if (string.IsNullOrEmpty(newToken))
            {
                return Ok(ApiResponse<RefreshTokenData>.Error(401, "Token无效或已过期,请重新登录"));
            }

            return Ok(ApiResponse<RefreshTokenData>.Success(new RefreshTokenData
            {
                Token = newToken
            }, "刷新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 刷新Token失败: {Message}", ex.Message);
            return Ok(ApiResponse<RefreshTokenData>.Error(500, $"刷新失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [Authorize]
    [HttpGet("userinfo")]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetUserInfo()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<UserInfo>.Error(401, "未授权"));
            }

            var user = await _authService.GetUserByIdAsync(userId.Value);

            if (user == null)
            {
                return Ok(ApiResponse<UserInfo>.Error(404, "用户不存在"));
            }

            return Ok(ApiResponse<UserInfo>.Success(MapToUserInfo(user, user.AuthType)));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 获取用户信息失败: {Message}", ex.Message);
            return Ok(ApiResponse<UserInfo>.Error(500, $"获取失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public ActionResult<ApiResponse> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("📤 用户登出: user_id={UserId}", userId);

            return Ok(ApiResponse.Success("登出成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 登出接口异常: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"登出失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取认证配置
    /// </summary>
    [HttpGet("config")]
    public ActionResult<ApiResponse<AuthConfigData>> GetAuthConfig()
    {
        try
        {
            var isLinuxDoEnabled = _linuxDoOAuth.IsConfigured();

            return Ok(ApiResponse<AuthConfigData>.Success(new AuthConfigData
            {
                LinuxDoEnabled = isLinuxDoEnabled,
                LinuxDoClientId = isLinuxDoEnabled ? _configuration["LinuxDo:ClientId"] ?? "" : "",
                LinuxDoRedirectUri = isLinuxDoEnabled ? _configuration["LinuxDo:RedirectUri"] ?? "" : "",
                LocalAuthEnabled = true,
                RegistrationEnabled = true
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 获取认证配置失败: {Message}", ex.Message);
            return Ok(ApiResponse<AuthConfigData>.Error(500, $"获取配置失败: {ex.Message}"));
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    private static UserInfo MapToUserInfo(Models.Entities.User user, string authType)
    {
        return new UserInfo
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username ?? user.LinuxDoUsername ?? "",
            Avatar = user.Avatar ?? "",
            AuthType = authType,
            IsAdmin = user.IsAdmin,
            LastLoginTime = user.LastLoginTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
            Email = user.Email,
            IsActive = user.IsActive,
            CreateTime = user.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
