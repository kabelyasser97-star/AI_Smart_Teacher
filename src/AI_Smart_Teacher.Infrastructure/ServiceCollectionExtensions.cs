using AI_Smart_Teacher.AI;
using AI_Smart_Teacher.Application.Abstractions;
using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Application.AI;
using AI_Smart_Teacher.Application.Admin;
using AI_Smart_Teacher.Application.Assessments;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Application.Vision;
using AI_Smart_Teacher.Application.Voice;
using AI_Smart_Teacher.Data.Contexts;
using AI_Smart_Teacher.Data.Services;
using AI_Smart_Teacher.Infrastructure.AI;
using AI_Smart_Teacher.Infrastructure.Admin;
using AI_Smart_Teacher.Infrastructure.Assessments;
using AI_Smart_Teacher.Infrastructure.Curriculum;
using AI_Smart_Teacher.Infrastructure.Security;
using AI_Smart_Teacher.Infrastructure.Vision;
using AI_Smart_Teacher.Vision.Services;
using AI_Smart_Teacher.Voice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartTeacherInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, Services.AppPaths>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ICurrentUserSession, CurrentUserSession>();
        services.AddSingleton<AiProviderCatalog>(_ => new AiProviderCatalog(AiRegistration.CreateDefaultProviders()));
        services.AddSingleton<IOcrService, WindowsOcrService>();
        services.AddSingleton<ISpeechToTextService, WindowsSpeechToTextService>();
        services.AddSingleton<ITextToSpeechService, WindowsTextToSpeechService>();
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var paths = sp.GetRequiredService<IAppPaths>();
            paths.EnsureCreated();
            options.UseSqlite($"Data Source={paths.DatabasePath};Cache=Shared");
        });
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurriculumService, CurriculumService>();
        services.AddScoped<IAiSettingsService, AiSettingsService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IImageAnalysisService, ImageAnalysisService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}
