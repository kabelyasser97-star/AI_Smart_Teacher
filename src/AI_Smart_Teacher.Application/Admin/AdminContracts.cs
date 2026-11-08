using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Application.Admin;

public sealed record AdminUserDto(Guid Id, string Username, string? Email, string DisplayName, UserRoleType Role, bool IsActive, DateTime? LastLoginAtUtc, DateTime CreatedAtUtc);
public sealed record AuditLogDto(Guid Id, string Action, string? EntityName, string? EntityId, bool IsAdministrative, Guid? UserId, DateTime CreatedAtUtc, string? Details);
public sealed record SystemSettingDto(string Key, string? Value, string? Category, bool IsSensitive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
public sealed record AdminStatusDto(int Users, int Assessments, int Conversations, int Attachments, string DatabasePath, string BackupsDirectory);

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<OperationResult> SetUserActiveAsync(Guid userId, bool active, CancellationToken ct = default);
    Task<OperationResult> SetUserRoleAsync(Guid userId, UserRoleType role, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> GetRecentAuditLogsAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetVisibleSettingsAsync(CancellationToken ct = default);
    Task<OperationResult> UpsertNonSensitiveSettingAsync(string key, string? value, string? category, CancellationToken ct = default);
    Task<AdminStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<(OperationResult Result, string? Path)> BackupDatabaseAsync(CancellationToken ct = default);
}
