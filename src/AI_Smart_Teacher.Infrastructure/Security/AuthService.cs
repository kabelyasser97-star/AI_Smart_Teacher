using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Core.Security;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.Security;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public AuthService(AppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default)
        => _db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken);

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var id = (request.UsernameOrEmail ?? string.Empty).Trim();
        var password = request.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("auth.required");

        var normalized = id.ToUpperInvariant();
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.IsActive && (u.NormalizedUsername == normalized || u.NormalizedEmail == normalized), cancellationToken);
        if (user?.Role is null) return AuthResult.Fail("auth.invalid");
        if (!_hasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            return AuthResult.Fail("auth.invalid");

        user.LastLoginAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "auth.login", EntityName = "User", EntityId = user.Id.ToString(), IsAdministrative = user.Role.Type == UserRoleType.Admin });
        await _db.SaveChangesAsync(cancellationToken);

        return AuthResult.Ok(ToAuthUser(user));
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, bool allowAdminBootstrap, CancellationToken cancellationToken = default)
    {
        var username = (request.Username ?? string.Empty).Trim();
        var display = (request.DisplayName ?? string.Empty).Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var password = request.Password ?? string.Empty;
        if (username.Length < 3 || display.Length < 2 || password.Length < 8)
            return AuthResult.Fail("auth.validation");
        if (request.Role == UserRoleType.Admin && !allowAdminBootstrap)
            return AuthResult.Fail("auth.admin_not_allowed");

        var normalizedUsername = username.ToUpperInvariant();
        var normalizedEmail = email?.ToUpperInvariant();
        var exists = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.NormalizedUsername == normalizedUsername ||
            (normalizedEmail != null && u.NormalizedEmail == normalizedEmail), cancellationToken);
        if (exists) return AuthResult.Fail("auth.exists");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Type == request.Role, cancellationToken);
        if (role is null) return AuthResult.Fail("auth.role_missing");

        var (hash, salt) = _hasher.HashPassword(password);
        var user = new User
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = display,
            PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "ar" : request.PreferredLanguage,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };

        _db.Users.Add(user);
        if (request.Role == UserRoleType.Student)
            _db.StudentProfiles.Add(new StudentProfile { User = user });
        if (request.Role == UserRoleType.Parent)
            _db.ParentProfiles.Add(new ParentProfile { User = user });

        _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "auth.register", EntityName = "User", EntityId = user.Id.ToString(), Details = request.Role.ToString(), IsAdministrative = request.Role == UserRoleType.Admin });
        await _db.SaveChangesAsync(cancellationToken);
        return AuthResult.Ok(ToAuthUser(user));
    }

    private static AuthUser ToAuthUser(User user) => new(user.Id, user.Username, user.DisplayName, user.Role!.Type, user.PreferredLanguage);
}

public sealed class CurrentUserSession : ICurrentUserSession
{
    public AuthUser? User { get; private set; }
    public bool IsAuthenticated => User is not null;
    public bool HasPermission(string permission) => User is not null && AppPermissions.ForRole(User.Role).Contains(permission);
    public void Set(AuthUser user) => User = user;
    public void Clear() => User = null;
}
