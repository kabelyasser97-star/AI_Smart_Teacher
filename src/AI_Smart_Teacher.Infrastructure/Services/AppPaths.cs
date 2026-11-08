using AI_Smart_Teacher.Application.Abstractions;

namespace AI_Smart_Teacher.Infrastructure.Services;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        BaseDataDirectory = Path.Combine(appData, "AI_Smart_Teacher");
        DatabasePath = Path.Combine(BaseDataDirectory, "data", "ai_smart_teacher.db");
        BackupsDirectory = Path.Combine(BaseDataDirectory, "backups");
        LogsDirectory = Path.Combine(BaseDataDirectory, "logs");
        AttachmentsDirectory = Path.Combine(BaseDataDirectory, "attachments");
    }

    public string BaseDataDirectory { get; }
    public string DatabasePath { get; }
    public string BackupsDirectory { get; }
    public string LogsDirectory { get; }
    public string AttachmentsDirectory { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(AttachmentsDirectory);
    }
}
