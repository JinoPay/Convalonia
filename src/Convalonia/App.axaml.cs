using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Jinobald.Avalonia.Application;
using Jinobald.Core.Ioc;
using Convalonia.Views;
using Convalonia.ViewModels;
using Convalonia.Services;
using Convalonia.Models;
using Convalonia.Validators;
using FluentValidation;
using Serilog;
using Serilog.Events;

namespace Convalonia;

public partial class App : ApplicationBase<MainWindow>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Setup global exception handlers
        SetupExceptionHandling();
    }

    /// <summary>
    /// Configure Serilog for Convalonia with custom settings
    /// </summary>
    protected override void ConfigureLogging()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Convalonia",
            "logs"
        );

        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Convalonia")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                path: Path.Combine(logDirectory, "convalonia-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024 // 10 MB
            )
            .WriteTo.File(
                path: Path.Combine(logDirectory, "convalonia-errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                fileSizeLimitBytes: 10 * 1024 * 1024 // 10 MB
            )
            .CreateLogger();

        Log.Information("Convalonia logging initialized. Log directory: {LogDirectory}", logDirectory);
    }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register Validators
        containerRegistry.RegisterSingleton<IValidator<Repository>, RepositoryValidator>();
        containerRegistry.RegisterSingleton<IValidator<Workspace>, WorkspaceValidator>();
        containerRegistry.RegisterSingleton<IValidator<Agent>, AgentValidator>();
        containerRegistry.RegisterSingleton<IValidator<string>, GitCommitMessageValidator>();
        containerRegistry.RegisterSingleton<IValidator<CommitMessageRequest>, CommitMessageRequestValidator>();

        // Register Services with interfaces
        containerRegistry.RegisterSingleton<IGitService, GitHubService>();
        containerRegistry.RegisterSingleton<IWorkspaceService, WorkspaceService>();
        containerRegistry.RegisterSingleton<IFileSystemService, FileSystemService>();
        containerRegistry.RegisterSingleton<IRepositoryService, RepositoryService>();
        containerRegistry.RegisterSingleton<IRepositoryManagementService, RepositoryManagementService>();
        containerRegistry.RegisterSingleton<IClaudeCodeServiceFactory, ClaudeCodeServiceFactory>();

        // Also register concrete types for backwards compatibility
        containerRegistry.RegisterSingleton<GitHubService>();
        containerRegistry.RegisterSingleton<WorkspaceService>();
        containerRegistry.RegisterSingleton<FileSystemService>();
        containerRegistry.RegisterSingleton<RepositoryService>();
        containerRegistry.RegisterSingleton<RepositoryManagementService>();

        // ClaudeCodeService is created per-agent via Factory pattern

        // Register ViewModels
        containerRegistry.RegisterForNavigation<SetupView, SetupViewModel>();
        containerRegistry.RegisterForNavigation<RepositoryListView, RepositoryListViewModel>();
        containerRegistry.RegisterForNavigation<RepositoryDetailView, RepositoryDetailViewModel>();
        containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
        containerRegistry.RegisterForNavigation<WorkspaceListView, WorkspaceListViewModel>();
        containerRegistry.RegisterForNavigation<WorkspaceView, WorkspaceViewModel>();
        containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
    }

    private void SetupExceptionHandling()
    {
        // Handle unhandled exceptions in the current AppDomain
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Log.Fatal(exception, "[AppDomain.UnhandledException] Critical unhandled exception occurred. IsTerminating={IsTerminating}", args.IsTerminating);
        };

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "[TaskScheduler.UnobservedTaskException] Unobserved task exception occurred");
            args.SetObserved(); // Prevent process termination
        };
    }
}
