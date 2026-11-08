using AI_Smart_Teacher.Application.Abstractions;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Data.Services;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly IAppPaths _paths;

    public DatabaseInitializer(AppDbContext db, IAppPaths paths)
    {
        _db = db;
        _paths = paths;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        await _db.Database.MigrateAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedSettingsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var required = new[]
        {
            (UserRoleType.Admin, "Administrator"),
            (UserRoleType.Teacher, "Teacher"),
            (UserRoleType.Parent, "Parent"),
            (UserRoleType.Student, "Student")
        };

        foreach (var (type, name) in required)
        {
            if (!await _db.Roles.IgnoreQueryFilters().AnyAsync(r => r.Type == type, cancellationToken))
            {
                _db.Roles.Add(new Role { Name = name, Type = type, Description = $"System role: {name}" });
            }
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        async Task Ensure(string key, string value, string category)
        {
            if (!await _db.SystemSettings.IgnoreQueryFilters().AnyAsync(s => s.Key == key, cancellationToken))
                _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, Category = category });
        }

        await Ensure("app.ui.defaultLanguage", "ar", "UI");
        await Ensure("app.backup.enabled", "true", "Backup");
        await Ensure("ai.provider.active", "NotConfigured", "AI");
        await Ensure("privacy.collectImagesOnlyWithConsent", "true", "Privacy");
        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DatabaseBackupService : IDatabaseBackupService
{
    private readonly IAppPaths _paths;

    public DatabaseBackupService(IAppPaths paths) => _paths = paths;

    public async Task<string> BackupAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        if (!File.Exists(_paths.DatabasePath))
            throw new FileNotFoundException("Database file not found.", _paths.DatabasePath);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var target = Path.Combine(_paths.BackupsDirectory, $"ai_smart_teacher-{stamp}.db");

        await using var source = new SqliteConnection($"Data Source={_paths.DatabasePath};Mode=ReadOnly;Pooling=False");
        await using var dest = new SqliteConnection($"Data Source={target};Pooling=False");
        await source.OpenAsync(cancellationToken);
        await dest.OpenAsync(cancellationToken);
        source.BackupDatabase(dest);
        return target;
    }

    public async Task RestoreAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupPath)) throw new ArgumentException("Backup path is required.", nameof(backupPath));
        if (!File.Exists(backupPath)) throw new FileNotFoundException("Backup file not found.", backupPath);
        _paths.EnsureCreated();

        SqliteConnection.ClearAllPools();
        var tempTarget = _paths.DatabasePath + ".restore.tmp";
        if (File.Exists(tempTarget)) File.Delete(tempTarget);

        await using (var source = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly;Pooling=False"))
        await using (var dest = new SqliteConnection($"Data Source={tempTarget};Pooling=False"))
        {
            await source.OpenAsync(cancellationToken);
            await dest.OpenAsync(cancellationToken);
            source.BackupDatabase(dest);
        }

        File.Move(tempTarget, _paths.DatabasePath, overwrite: true);
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = _paths.DatabasePath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }
}
