using AI_Smart_Teacher.Application.Admin;
using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop.ViewModels;

public sealed class AdminViewModel
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserSession _session;
    public AdminViewModel(IServiceScopeFactory scopeFactory, ICurrentUserSession session) { _scopeFactory = scopeFactory; _session = session; }

    public string Status { get; private set; } = string.Empty;
    public bool IsError { get; private set; }
    public bool CanManageUsers => _session.HasPermission(AppPermissions.ManageUsers);
    public bool CanViewUsers => _session.HasPermission(AppPermissions.ViewUsers) || CanManageUsers;
    public bool CanViewReports => _session.HasPermission(AppPermissions.ViewAllReports);
    public bool CanManageSettings => _session.HasPermission(AppPermissions.ManageSystemSettings);
    public IReadOnlyList<AdminUserDto> Users { get; private set; } = Array.Empty<AdminUserDto>();
    public IReadOnlyList<AuditLogDto> AuditLogs { get; private set; } = Array.Empty<AuditLogDto>();
    public IReadOnlyList<SystemSettingDto> Settings { get; private set; } = Array.Empty<SystemSettingDto>();
    public AdminStatusDto? SystemStatus { get; private set; }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();
        if (CanViewUsers || CanViewReports || CanManageSettings) SystemStatus = await svc.GetStatusAsync(ct);
        if (CanViewUsers) Users = await svc.GetUsersAsync(ct);
        if (CanViewReports) AuditLogs = await svc.GetRecentAuditLogsAsync(80, ct);
        if (CanManageSettings || CanViewReports) Settings = await svc.GetVisibleSettingsAsync(ct);
    }

    public async Task SetActiveAsync(Guid userId, bool active, CancellationToken ct = default)
    {
        if (!CanManageUsers) { Fail("Permission denied."); return; }
        using var scope = _scopeFactory.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAdminService>().SetUserActiveAsync(userId, active, ct);
        OkFail(result.Success, result.ErrorCode, active ? "User activated." : "User deactivated.");
        await RefreshAsync(ct);
    }

    public async Task SetRoleAsync(Guid userId, UserRoleType role, CancellationToken ct = default)
    {
        if (!CanManageUsers) { Fail("Permission denied."); return; }
        using var scope = _scopeFactory.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAdminService>().SetUserRoleAsync(userId, role, ct);
        OkFail(result.Success, result.ErrorCode, "User role updated.");
        await RefreshAsync(ct);
    }

    public async Task SaveSettingAsync(string key, string? value, string? category, CancellationToken ct = default)
    {
        if (!CanManageSettings) { Fail("Permission denied."); return; }
        using var scope = _scopeFactory.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAdminService>().UpsertNonSensitiveSettingAsync(key, value, category, ct);
        OkFail(result.Success, result.ErrorCode, "Setting saved.");
        await RefreshAsync(ct);
    }

    public async Task BackupAsync(CancellationToken ct = default)
    {
        if (!CanManageSettings) { Fail("Permission denied."); return; }
        using var scope = _scopeFactory.CreateScope();
        var (result, path) = await scope.ServiceProvider.GetRequiredService<IAdminService>().BackupDatabaseAsync(ct);
        OkFail(result.Success, result.ErrorCode, path is null ? "Backup completed." : "Backup created: " + path);
        await RefreshAsync(ct);
    }

    private void OkFail(bool ok, string? error, string success) { Status = ok ? success : (error ?? "Failed."); IsError = !ok; }
    private void Fail(string message) { Status = message; IsError = true; }
}
