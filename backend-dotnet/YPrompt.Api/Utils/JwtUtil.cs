using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace YPrompt.Api.Utils;

public interface IJwtUtil
{
    string GenerateToken(int userId, string openId, int expireHours = 24 * 7);
    ClaimsPrincipal? VerifyToken(string token);
    string? RefreshToken(string oldToken, int expireHours = 24 * 7);
    IDictionary<string, object>? DecodeTokenWithoutVerify(string token);
}

public class JwtUtil : IJwtUtil
{
    private readonly string _secretKey;
    private readonly string _algorithm = SecurityAlgorithms.HmacSha256;
    private readonly ILogger<JwtUtil> _logger;

    public JwtUtil(IConfiguration configuration, ILogger<JwtUtil> logger)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? "your-secret-key-change-in-production";
        _logger = logger;

        if (_secretKey == "your-secret-key-change-in-production")
        {
            _logger.LogWarning("⚠️  警告: 使用默认SECRET_KEY,生产环境请务必修改配置!");
        }
    }

    public string GenerateToken(int userId, string openId, int expireHours = 24 * 7)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, _algorithm);

        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim("open_id", openId),
            new Claim("type", "access_token"),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(expireHours),
            claims: claims,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogDebug("✅ 为用户 {UserId} 生成Token成功, 有效期: {Hours}小时", userId, expireHours);
        
        return tokenString;
    }

    public ClaimsPrincipal? VerifyToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            
            var userIdClaim = principal.FindFirst("user_id");
            if (userIdClaim != null)
            {
                _logger.LogDebug("✅ Token验证成功, user_id: {UserId}", userIdClaim.Value);
            }
            
            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("⚠️  Token已过期");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("⚠️  Token无效: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Token验证异常: {Message}", ex.Message);
            return null;
        }
    }

    public string? RefreshToken(string oldToken, int expireHours = 24 * 7)
    {
        var principal = VerifyToken(oldToken);
        if (principal == null)
            return null;

        var userIdClaim = principal.FindFirst("user_id");
        var openIdClaim = principal.FindFirst("open_id");

        if (userIdClaim == null || openIdClaim == null)
            return null;

        if (!int.TryParse(userIdClaim.Value, out int userId))
            return null;

        return GenerateToken(userId, openIdClaim.Value, expireHours);
    }

    public IDictionary<string, object>? DecodeTokenWithoutVerify(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Payload;
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Token解码失败: {Message}", ex.Message);
            return null;
        }
    }
}
