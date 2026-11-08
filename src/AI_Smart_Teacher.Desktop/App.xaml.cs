using AI_Smart_Teacher.Application.Abstractions;
using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(args.Exception.Message, "AI Smart Teacher", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            args.Handled = true;
        };

        var services = new ServiceCollection();
        services.AddSmartTeacherInfrastructure();
        services.AddTransient<ViewModels.AuthViewModel>();
        services.AddTransient<ViewModels.CurriculumViewModel>();
        services.AddTransient<ViewModels.AiChatViewModel>();
        services.AddTransient<ViewModels.VisionVoiceViewModel>();
        services.AddTransient<ViewModels.AssessmentViewModel>();
        services.AddTransient<ViewModels.AdminViewModel>();
        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();

        try
        {
            using var scope = Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Database initialization failed:\n{ex.Message}", "AI Smart Teacher", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }
}
