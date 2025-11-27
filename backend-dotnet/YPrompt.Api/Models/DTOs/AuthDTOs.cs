namespace YPrompt.Api.Models.DTOs;

// Auth DTOs

public class LoginRequest
{
    public string Code { get; set; } = string.Empty;
}

public class LocalLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LocalRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public int IsAdmin { get; set; }
    public string LastLoginTime { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int IsActive { get; set; }
    public string CreateTime { get; set; } = string.Empty;
}

public class LoginData
{
    public string Token { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new UserInfo();
}

public class RefreshTokenData
{
    public string Token { get; set; } = string.Empty;
}

public class AuthConfigData
{
    public bool LinuxDoEnabled { get; set; }
    public string LinuxDoClientId { get; set; } = string.Empty;
    public string LinuxDoRedirectUri { get; set; } = string.Empty;
    public bool LocalAuthEnabled { get; set; }
    public bool RegistrationEnabled { get; set; }
}
