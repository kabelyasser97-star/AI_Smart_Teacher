using AI_Smart_Teacher.Application.Abstractions;
using AI_Smart_Teacher.Application.Admin;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.Admin;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IAppPaths _paths;
    private readonly IDatabaseBackupService _backup;
    public AdminService(AppDbContext db, IAppPaths paths, IDatabaseBackupService backup) { _db = db; _paths = paths; _backup = backup; }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken ct = default)
        => await _db.Users.IgnoreQueryFilters().Include(u => u.Role).OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new AdminUserDto(u.Id, u.Username, u.Email, u.DisplayName, u.Role!.Type, u.IsActive, u.LastLoginAtUtc, u.CreatedAtUtc))
            .ToListAsync(ct);

    public async Task<OperationResult> SetUserActiveAsync(Guid userId, bool active, CancellationToken ct = default)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return OperationResult.Fail("admin.user_not_found");
        user.IsActive = active;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(new AuditLog { Action = active ? "admin.user.activate" : "admin.user.deactivate", EntityName = "User", EntityId = user.Id.ToString(), IsAdministrative = true });
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok(user.Id);
    }

    public async Task<OperationResult> SetUserRoleAsync(Guid userId, UserRoleType role, CancellationToken ct = default)
    {
        var user = await _db.Users.IgnoreQueryFilters().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return OperationResult.Fail("admin.user_not_found");
        var roleRow = await _db.Roles.FirstOrDefaultAsync(r => r.Type == role, ct);
        if (roleRow is null) return OperationResult.Fail("admin.role_missing");
        user.RoleId = roleRow.Id;
        user.Role = roleRow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(new AuditLog { Action = "admin.user.role", EntityName = "User", EntityId = user.Id.ToString(), Details = role.ToString(), IsAdministrative = true });
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok(user.Id);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAuditLogsAsync(int take = 100, CancellationToken ct = default)
        => await _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).Take(Math.Clamp(take, 1, 500))
            .Select(a => new AuditLogDto(a.Id, a.Action, a.EntityName, a.EntityId, a.IsAdministrative, a.UserId, a.CreatedAtUtc, a.Details))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SystemSettingDto>> GetVisibleSettingsAsync(CancellationToken ct = default)
    {
        var rows = await _db.SystemSettings.AsNoTracking().IgnoreQueryFilters().OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync(ct);
        return rows.Select(s => new SystemSettingDto(s.Key, s.IsSensitive ? "[protected]" : s.Value, s.Category, s.IsSensitive, s.CreatedAtUtc, s.UpdatedAtUtc)).ToList();
    }

    public async Task<OperationResult> UpsertNonSensitiveSettingAsync(string key, string? value, string? category, CancellationToken ct = default)
    {
        key = (key ?? string.Empty).Trim();
        if (key.Length < 2 || key.Length > 128) return OperationResult.Fail("admin.setting_invalid");
        if (key.StartsWith("ai.provider.apiKey", StringComparison.OrdinalIgnoreCase)) return OperationResult.Fail("admin.setting_sensitive");
        var row = await _db.SystemSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null)
        {
            row = new SystemSetting { Key = key, Value = value, Category = string.IsNullOrWhiteSpace(category) ? "Admin" : category.Trim(), IsSensitive = false };
            _db.SystemSettings.Add(row);
        }
        else
        {
            if (row.IsSensitive) return OperationResult.Fail("admin.setting_sensitive");
            row.Value = value;
            row.Category = string.IsNullOrWhiteSpace(category) ? row.Category : category.Trim();
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
        _db.AuditLogs.Add(new AuditLog { Action = "admin.setting.upsert", EntityName = "SystemSetting", EntityId = key, IsAdministrative = true });
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<AdminStatusDto> GetStatusAsync(CancellationToken ct = default)
        => new(
            await _db.Users.IgnoreQueryFilters().CountAsync(ct),
            await _db.Assessments.CountAsync(ct),
            await _db.Conversations.CountAsync(ct),
            await _db.Attachments.CountAsync(ct),
            _paths.DatabasePath,
            _paths.BackupsDirectory);

    public async Task<(OperationResult Result, string? Path)> BackupDatabaseAsync(CancellationToken ct = default)
    {
        try
        {
            var path = await _backup.BackupAsync(ct);
            _db.AuditLogs.Add(new AuditLog { Action = "admin.backup.create", EntityName = "Database", EntityId = path, IsAdministrative = true });
            await _db.SaveChangesAsync(ct);
            return (OperationResult.Ok(), path);
        }
        catch (Exception ex)
        {
            return (OperationResult.Fail("admin.backup_failed:" + ex.GetType().Name), null);
        }
    }
}
