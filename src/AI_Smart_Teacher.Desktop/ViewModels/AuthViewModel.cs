using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Core.Security;
using AI_Smart_Teacher.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop.ViewModels;

public sealed class AuthViewModel
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserSession _session;

    public AuthViewModel(IServiceScopeFactory scopeFactory, ICurrentUserSession session)
    {
        _scopeFactory = scopeFactory;
        _session = session;
    }

    public bool BootstrapMode { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsError { get; private set; }
    public AuthUser? CurrentUser => _session.User;

    public async Task RefreshModeAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        BootstrapMode = !await auth.HasAnyUsersAsync(ct);
    }

    public async Task<bool> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var result = await auth.LoginAsync(new LoginRequest(usernameOrEmail, password), ct);
        return ApplyResult(result, successKey: "login.success");
    }

    public async Task<bool> RegisterAsync(string username, string? email, string displayName, string password, UserRoleType role, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var result = await auth.RegisterAsync(new RegisterRequest(username, email, displayName, password, role, LocalizationService.Current == UiLanguage.Arabic ? "ar" : "en"), BootstrapMode, ct);
        if (result.Success && BootstrapMode) BootstrapMode = false;
        return ApplyResult(result, successKey: "register.success");
    }

    public void Logout()
    {
        _session.Clear();
        Message = T("logout.success");
        IsError = false;
    }

    public IEnumerable<string> AllowedRolesForRegistration()
    {
        if (BootstrapMode) return new[] { "Admin", "Teacher", "Parent", "Student" };
        return new[] { "Student", "Parent", "Teacher" };
    }

    public IReadOnlyCollection<string> Permissions() => CurrentUser is null ? Array.Empty<string>() : AppPermissions.ForRole(CurrentUser.Role);

    private bool ApplyResult(AuthResult result, string successKey)
    {
        if (result.Success && result.User is not null)
        {
            _session.Set(result.User);
            Message = T(successKey);
            IsError = false;
            return true;
        }
        Message = T(result.ErrorCode ?? "auth.failed");
        IsError = true;
        return false;
    }

    private static string T(string key)
    {
        var ar = LocalizationService.Current == UiLanguage.Arabic;
        return key switch
        {
            "login.success" => ar ? "تم تسجيل الدخول بنجاح." : "Signed in successfully.",
            "register.success" => ar ? "تم إنشاء الحساب بنجاح." : "Account created successfully.",
            "logout.success" => ar ? "تم تسجيل الخروج." : "Signed out.",
            "auth.required" => ar ? "اسم المستخدم/البريد وكلمة المرور مطلوبان." : "Username/email and password are required.",
            "auth.invalid" => ar ? "بيانات الدخول غير صحيحة أو الحساب غير نشط." : "Invalid credentials or inactive account.",
            "auth.validation" => ar ? "تحقق من البيانات: اسم المستخدم 3+ أحرف، الاسم الظاهر 2+، وكلمة المرور 8+ أحرف." : "Check input: username 3+, display name 2+, password 8+ characters.",
            "auth.exists" => ar ? "اسم المستخدم أو البريد مستخدم بالفعل." : "Username or email already exists.",
            "auth.admin_not_allowed" => ar ? "إنشاء حساب مسؤول متاح فقط عند تهيئة النظام لأول مرة." : "Admin registration is allowed only during initial setup.",
            "auth.role_missing" => ar ? "دور المستخدم غير موجود في قاعدة البيانات." : "User role is missing in the database.",
            _ => ar ? "تعذر إكمال العملية." : "The operation could not be completed."
        };
    }
}
