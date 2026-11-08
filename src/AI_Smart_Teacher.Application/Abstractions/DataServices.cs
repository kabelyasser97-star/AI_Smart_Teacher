namespace AI_Smart_Teacher.Application.Abstractions;

public interface IAppPaths
{
    string BaseDataDirectory { get; }
    string DatabasePath { get; }
    string BackupsDirectory { get; }
    string LogsDirectory { get; }
    string AttachmentsDirectory { get; }
    void EnsureCreated();
}

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseBackupService
{
    Task<string> BackupAsync(CancellationToken cancellationToken = default);
    Task RestoreAsync(string backupPath, CancellationToken cancellationToken = default);
}
