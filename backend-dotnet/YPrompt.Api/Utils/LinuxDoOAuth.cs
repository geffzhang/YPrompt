using System.Net.Http.Headers;
using System.Text.Json;

namespace YPrompt.Api.Utils;

public interface ILinuxDoOAuth
{
    bool IsConfigured();
    string GetAuthorizationUrl(string? state = null);
    Task<LinuxDoTokenData> GetAccessTokenAsync(string code);
    Task<LinuxDoUserInfo> GetUserInfoAsync(string accessToken);
    Task<LinuxDoUserInfo> GetUserByCodeAsync(string code);
    string GetAvatarUrl(string? avatarTemplate, int size = 240);
}

public class LinuxDoOAuth : ILinuxDoOAuth
{
    private const string AuthUrl = "https://connect.linux.do/oauth2/authorize";
    private const string TokenUrl = "https://connect.linux.do/oauth2/token";
    private const string UserInfoUrl = "https://connect.linux.do/api/user";

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinuxDoOAuth> _logger;

    public LinuxDoOAuth(IConfiguration configuration, HttpClient httpClient, ILogger<LinuxDoOAuth> logger)
    {
        _clientId = configuration["LinuxDo:ClientId"] ?? string.Empty;
        _clientSecret = configuration["LinuxDo:ClientSecret"] ?? string.Empty;
        _redirectUri = configuration["LinuxDo:RedirectUri"] ?? string.Empty;
        _httpClient = httpClient;
        _logger = logger;

        if (!IsConfigured())
        {
            _logger.LogWarning("⚠️  Linux.do OAuth配置不完整，请检查配置文件");
        }
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_clientId) &&
               !string.IsNullOrEmpty(_clientSecret) &&
               !string.IsNullOrEmpty(_redirectUri);
    }

    public string GetAuthorizationUrl(string? state = null)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["redirect_uri"] = _redirectUri,
            ["response_type"] = "code",
            ["scope"] = "user"
        };

        if (!string.IsNullOrEmpty(state))
        {
            queryParams["state"] = state;
        }

        var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var authUrl = $"{AuthUrl}?{queryString}";

        _logger.LogInformation("📍 生成授权URL: {Url}", authUrl);
        return authUrl;
    }

    public async Task<LinuxDoTokenData> GetAccessTokenAsync(string code)
    {
        var data = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code"
        };

        _logger.LogInformation("🔑 请求访问令牌，code={Code}...", code.Substring(0, Math.Min(10, code.Length)));

        var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(data)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<LinuxDoTokenData>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
        {
            _logger.LogError("❌ Token响应缺少access_token: {Json}", json);
            throw new InvalidOperationException("获取访问令牌失败：响应格式错误");
        }

        _logger.LogInformation("✅ 成功获取访问令牌");
        return tokenData;
    }

    public async Task<LinuxDoUserInfo> GetUserInfoAsync(string accessToken)
    {
        _logger.LogInformation("👤 请求用户信息");

        var request = new HttpRequestMessage(HttpMethod.Get, UserInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<LinuxDoUserInfo>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (userInfo == null || userInfo.Id == 0)
        {
            _logger.LogError("❌ 用户信息响应缺少id字段: {Json}", json);
            throw new InvalidOperationException("获取用户信息失败：响应格式错误");
        }

        _logger.LogInformation("✅ 成功获取用户信息: id={Id}, username={Username}", userInfo.Id, userInfo.Username);
        return userInfo;
    }

    public async Task<LinuxDoUserInfo> GetUserByCodeAsync(string code)
    {
        var tokenData = await GetAccessTokenAsync(code);
        return await GetUserInfoAsync(tokenData.AccessToken!);
    }

    public string GetAvatarUrl(string? avatarTemplate, int size = 240)
    {
        if (string.IsNullOrEmpty(avatarTemplate))
            return string.Empty;

        if (avatarTemplate.Contains("{size}"))
            return avatarTemplate.Replace("{size}", size.ToString());

        return avatarTemplate;
    }
}

public class LinuxDoTokenData
{
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
}

public class LinuxDoUserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AvatarTemplate { get; set; }
    public bool Active { get; set; }
    public int TrustLevel { get; set; }
    public bool Silenced { get; set; }
    public Dictionary<string, object>? ExternalIds { get; set; }
    public string? ApiKey { get; set; }
}
