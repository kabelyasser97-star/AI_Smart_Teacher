using AI_Smart_Teacher.Application.Abstractions;
using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Application.Admin;
using AI_Smart_Teacher.Application.AI;
using AI_Smart_Teacher.Application.Assessments;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Data.Contexts;
using AI_Smart_Teacher.Data.Services;
using AI_Smart_Teacher.Infrastructure.Admin;
using AI_Smart_Teacher.Infrastructure.AI;
using AI_Smart_Teacher.Infrastructure.Assessments;
using AI_Smart_Teacher.Infrastructure.Curriculum;
using AI_Smart_Teacher.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AI_Smart_Teacher.AI;

var tempRoot = Path.Combine(Path.GetTempPath(), "AI_Smart_Teacher_Phase6Smoke_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
try
{
    var services = new ServiceCollection();
    services.AddSingleton<IAppPaths>(new TempAppPaths(tempRoot));
    services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    services.AddSingleton<ICurrentUserSession, CurrentUserSession>();
    services.AddSingleton<AiProviderCatalog>(_ => new AiProviderCatalog(AiRegistration.CreateDefaultProviders()));
    services.AddDbContextFactory<AppDbContext>((sp, options) => { var paths = sp.GetRequiredService<IAppPaths>(); paths.EnsureCreated(); options.UseSqlite($"Data Source={paths.DatabasePath};Cache=Shared"); });
    services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
    services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
    services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICurriculumService, CurriculumService>();
    services.AddScoped<IAiSettingsService, AiSettingsService>();
    services.AddScoped<IConversationService, ConversationService>();
    services.AddScoped<IAssessmentService, AssessmentService>();
    services.AddScoped<IAdminService, AdminService>();
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var init = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var aiSettings = scope.ServiceProvider.GetRequiredService<IAiSettingsService>();
    var conv = scope.ServiceProvider.GetRequiredService<IConversationService>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await init.InitializeAsync();

    var user = (await auth.RegisterAsync(new RegisterRequest("student1", null, "Student", "Passw0rd!123", UserRoleType.Student, "ar"), true)).User!;
    var before = await aiSettings.GetAsync();
    Console.WriteLine($"AI_DEFAULT={before.ActiveProvider};HAS_KEY={before.HasApiKey};CLOUD={before.AllowCloudRequests}");

    var msg1 = await conv.SendMessageAsync(new AiChatRequest(user.Id, null, null, "اشرح لي فكرة الكسور"));
    Console.WriteLine($"MSG1={msg1.Success}:{msg1.ErrorCode}:{msg1.ProviderName}:{msg1.ConversationId.HasValue}");
    Console.WriteLine($"CONV_COUNT={await db.Conversations.IgnoreQueryFilters().CountAsync()}");
    Console.WriteLine($"CHAT_COUNT={await db.ChatMessages.IgnoreQueryFilters().CountAsync()}");

    await aiSettings.SaveAsync(new AiSettingsDto("OpenAICompatible", "gpt-4o-mini", "https://example.invalid/v1/chat/completions", true, true, 50), "secret-key-value");
    var after = await aiSettings.GetAsync();
    var stored = await db.SystemSettings.IgnoreQueryFilters().FirstAsync(s => s.Key == "ai.provider.apiKey.protected");
    Console.WriteLine($"AI_SAVED={after.ActiveProvider};HAS_KEY={after.HasApiKey};CLOUD={after.AllowCloudRequests}");
    Console.WriteLine($"KEY_PROTECTED={stored.IsSensitive};KEY_NOT_PLAIN={stored.Value != "secret-key-value" && !string.IsNullOrWhiteSpace(stored.Value)}");

    await aiSettings.SaveAsync(new AiSettingsDto("OpenAICompatible", "gpt-4o-mini", "https://127.0.0.1:9/v1/chat/completions", true, false, 50), null);
    var blocked = await conv.SendMessageAsync(new AiChatRequest(user.Id, null, null, "اختبار"));
    Console.WriteLine($"CLOUD_BLOCKED={blocked.Success}:{blocked.ErrorCode}");
    var assess = scope.ServiceProvider.GetRequiredService<AI_Smart_Teacher.Application.Assessments.IAssessmentService>();
    var teacher = (await auth.RegisterAsync(new RegisterRequest("teacher1", null, "Teacher", "Passw0rd!123", UserRoleType.Teacher, "en"), false)).User!;
    var createdA = await assess.CreateAssessmentAsync("Math Basics", "Smoke assessment", null, null, 15);
    var qResult = await assess.AddQuestionAsync(createdA.Id!.Value, QuestionType.MultipleChoice, "2 + 2 = ?", 5, null, new[] { ("3", false), ("4", true), ("5", false) });
    await assess.SetPublishedAsync(createdA.Id.Value, true);
    var listA = await assess.ListAsync();
    var detailA = await assess.GetAsync(createdA.Id.Value, false);
    var correctId = detailA!.Questions[0].Options.First(o => o.Text == "4").Id;
    var (submitOk, score) = await assess.SubmitAsync(new AI_Smart_Teacher.Application.Assessments.SubmitAssessmentRequest(user.Id, createdA.Id.Value, new[] { new AI_Smart_Teacher.Application.Assessments.AssessmentAnswerInput(detailA.Questions[0].Id, correctId, null) }));
    var progress = await assess.GetMyProgressAsync(user.Id);
    Console.WriteLine($"ASSESS_CREATED={createdA.Success};Q={qResult.Success};LIST={listA.Count};PUBLISHED={detailA.Assessment.IsPublished}");
    Console.WriteLine($"ASSESS_SUBMIT={submitOk.Success};SCORE={score?.Score}/{score?.MaxScore};PROGRESS={progress.Count}");
    var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
    var adminStatus = await admin.GetStatusAsync();
    var adminUsers = await admin.GetUsersAsync();
    var adminSettings = await admin.GetVisibleSettingsAsync();
    var setRes = await admin.UpsertNonSensitiveSettingAsync("app.smoke.check", "ok", "Smoke");
    var (backupOk, backupPath) = await admin.BackupDatabaseAsync();
    Console.WriteLine($"ADMIN_STATUS=U{adminStatus.Users};A{adminStatus.Assessments};C{adminStatus.Conversations};AT{adminStatus.Attachments}");
    Console.WriteLine($"ADMIN_USERS={adminUsers.Count};SETTINGS={adminSettings.Count};SET={setRes.Success};BACKUP={backupOk.Success};BACKUP_EXISTS={(!string.IsNullOrWhiteSpace(backupPath) && File.Exists(backupPath))}");
}
finally { try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true); } catch { } }

file sealed class TempAppPaths(string root) : IAppPaths
{
    public string BaseDataDirectory => root; public string DatabasePath => Path.Combine(root, "data", "ai_smart_teacher.db"); public string BackupsDirectory => Path.Combine(root, "backups"); public string LogsDirectory => Path.Combine(root, "logs"); public string AttachmentsDirectory => Path.Combine(root, "attachments");
    public void EnsureCreated() { Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!); Directory.CreateDirectory(BackupsDirectory); Directory.CreateDirectory(LogsDirectory); Directory.CreateDirectory(AttachmentsDirectory); }
}
