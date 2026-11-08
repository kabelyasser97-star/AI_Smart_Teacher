using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Application.Auth;

public sealed record AuthUser(Guid Id, string Username, string DisplayName, UserRoleType Role, string PreferredLanguage);

public sealed record LoginRequest(string UsernameOrEmail, string Password);
public sealed record RegisterRequest(string Username, string? Email, string DisplayName, string Password, UserRoleType Role, string PreferredLanguage = "ar");
public sealed record AuthResult(bool Success, string? ErrorCode, AuthUser? User)
{
    public static AuthResult Ok(AuthUser user) => new(true, null, user);
    public static AuthResult Fail(string code) => new(false, code, null);
}

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool Verify(string password, string storedHash, string storedSalt);
}

public interface IAuthService
{
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RegisterAsync(RegisterRequest request, bool allowAdminBootstrap, CancellationToken cancellationToken = default);
}

public interface ICurrentUserSession
{
    AuthUser? User { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
    void Set(AuthUser user);
    void Clear();
}
