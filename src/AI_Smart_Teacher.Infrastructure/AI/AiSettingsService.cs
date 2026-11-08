using System.Security.Cryptography;
using System.Text;
using AI_Smart_Teacher.Application.AI;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.AI;

public sealed class AiSettingsService : IAiSettingsService
{
    private const string Prefix = "ai.";
    private readonly AppDbContext _db;
    public AiSettingsService(AppDbContext db) => _db = db;

    public async Task<AiSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var map = await LoadAsync(ct);
        var hasKey = map.TryGetValue("provider.apiKey.protected", out var v) && !string.IsNullOrWhiteSpace(v);
        return new AiSettingsDto(
            Get(map, "provider.active", "NotConfigured"),
            Null(Get(map, "provider.model", "")),
            Null(Get(map, "provider.endpoint", "")),
            hasKey,
            bool.TryParse(Get(map, "cloud.enabled", "false"), out var b) && b,
            int.TryParse(Get(map, "usage.maxPerDay", "50"), out var n) ? Math.Clamp(n, 1, 10000) : 50);
    }

    public async Task SaveAsync(AiSettingsDto settings, string? newApiKey, CancellationToken ct = default)
    {
        await SetAsync("provider.active", string.IsNullOrWhiteSpace(settings.ActiveProvider) ? "NotConfigured" : settings.ActiveProvider.Trim(), "AI", false, ct);
        await SetAsync("provider.model", settings.Model ?? string.Empty, "AI", false, ct);
        await SetAsync("provider.endpoint", settings.Endpoint ?? string.Empty, "AI", false, ct);
        await SetAsync("cloud.enabled", settings.AllowCloudRequests.ToString(), "AI", false, ct);
        await SetAsync("usage.maxPerDay", Math.Clamp(settings.MaxRequestsPerDay, 1, 10000).ToString(), "AI", false, ct);
        if (!string.IsNullOrWhiteSpace(newApiKey))
        {
            var protectedValue = Protect(newApiKey);
            await SetAsync("provider.apiKey.protected", protectedValue, "AI", true, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AiRuntimeSettings> GetRuntimeAsync(CancellationToken ct = default)
    {
        var dto = await GetAsync(ct);
        var map = await LoadAsync(ct);
        string? key = null;
        if (map.TryGetValue("provider.apiKey.protected", out var v) && !string.IsNullOrWhiteSpace(v))
        {
            try { key = Unprotect(v); } catch { key = null; }
        }
        return new AiRuntimeSettings(dto.ActiveProvider, dto.Model, dto.Endpoint, key, dto.AllowCloudRequests, dto.MaxRequestsPerDay);
    }

    private async Task<Dictionary<string, string?>> LoadAsync(CancellationToken ct)
    {
        var items = await _db.SystemSettings.IgnoreQueryFilters().Where(s => s.Key.StartsWith(Prefix)).ToListAsync(ct);
        return items.ToDictionary(s => s.Key[Prefix.Length..], s => s.Value, StringComparer.OrdinalIgnoreCase);
    }

    private async Task SetAsync(string shortKey, string value, string category, bool sensitive, CancellationToken ct)
    {
        var key = Prefix + shortKey;
        var row = await _db.SystemSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null) _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, Category = category, IsSensitive = sensitive });
        else { row.Value = value; row.Category = category; row.IsSensitive = sensitive; row.UpdatedAtUtc = DateTime.UtcNow; }
    }

    private static string Get(Dictionary<string, string?> map, string key, string fallback) => map.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
    private static string? Null(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Protect(string clear)
    {
        var bytes = Encoding.UTF8.GetBytes(clear);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string protectedBase64)
    {
        var bytes = Convert.FromBase64String(protectedBase64);
        var clear = ProtectedData.Unprotect(bytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(clear);
    }
}
