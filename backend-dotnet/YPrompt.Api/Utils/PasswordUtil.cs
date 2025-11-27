namespace YPrompt.Api.Utils;

public interface IPasswordUtil
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    (bool isValid, string errorMessage) ValidatePasswordStrength(string password);
    string GenerateRandomPassword(int length = 12);
}

public class PasswordUtil : IPasswordUtil
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("密码不能为空");

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    public (bool isValid, string errorMessage) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "密码不能为空");

        if (password.Length < 8)
            return (false, "密码长度至少8个字符");

        if (password.Length > 128)
            return (false, "密码长度不能超过128个字符");

        if (!password.Any(char.IsDigit))
            return (false, "密码必须包含数字");

        if (!password.Any(char.IsLetter))
            return (false, "密码必须包含字母");

        return (true, string.Empty);
    }

    public string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var password = new char[length];

        for (int i = 0; i < length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }

        // Ensure at least one digit and one letter
        var result = new string(password);
        if (!result.Any(char.IsDigit))
        {
            password[length - 1] = "0123456789"[random.Next(10)];
        }
        if (!result.Any(char.IsLetter))
        {
            password[length - 2] = chars[random.Next(52)]; // Only letters
        }

        return new string(password);
    }
}

public interface IUsernameUtil
{
    (bool isValid, string errorMessage) ValidateUsername(string username);
}

public class UsernameUtil : IUsernameUtil
{
    private static readonly string[] ReservedNames = { "admin", "root", "system", "administrator", "guest", "test" };

    public (bool isValid, string errorMessage) ValidateUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return (false, "用户名不能为空");

        if (username.Length < 3)
            return (false, "用户名长度至少3个字符");

        if (username.Length > 20)
            return (false, "用户名长度不能超过20个字符");

        if (!char.IsLetter(username[0]))
            return (false, "用户名必须以字母开头");

        if (!username.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return (false, "用户名只能包含字母、数字、下划线");

        if (ReservedNames.Contains(username.ToLower()) && username != "admin")
            return (false, "该用户名已被系统保留");

        return (true, string.Empty);
    }
}
