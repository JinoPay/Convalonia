using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Convalonia.Infrastructure;
using Convalonia.Views;
using Convalonia.ViewModels;
using Convalonia.Services;
using Convalonia.Services.Toast;
using Convalonia.Models;
using Convalonia.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Convalonia;

public partial class App : ApplicationBase<MainWindow>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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

    public override void RegisterTypes(IServiceCollection services)
    {
        // Register Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: false);
        });

        // Register Validators
        services.AddSingleton<IValidator<Repository>, RepositoryValidator>();
        services.AddSingleton<IValidator<Workspace>, WorkspaceValidator>();
        services.AddSingleton<IValidator<Agent>, AgentValidator>();
        services.AddSingleton<IValidator<string>, GitCommitMessageValidator>();
        services.AddSingleton<IValidator<CommitMessageRequest>, CommitMessageRequestValidator>();

        // Register UI Services
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<Services.Dialog.IDialogService, Services.Dialog.DialogService>();
        services.AddSingleton<Services.Navigation.IRegionManager, Services.Navigation.RegionManager>();

        // Register Services with interfaces
        services.AddSingleton<IGitService, GitHubService>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IRepositoryService, RepositoryService>();
        services.AddSingleton<IRepositoryManagementService, RepositoryManagementService>();
        services.AddSingleton<IClaudeCodeServiceFactory, ClaudeCodeServiceFactory>();

        // Register Conductor-specific services
        services.AddSingleton<IConductorConfigService, ConductorConfigService>();
        services.AddSingleton<IPortAllocator, PortAllocator>();
        services.AddSingleton<IScriptExecutor, ScriptExecutor>();
        services.AddSingleton<ICheckpointService, CheckpointService>();

        // Register Persistence services
        services.AddSingleton<IWorkspacePersistenceService, WorkspacePersistenceService>();
        services.AddSingleton<IAgentPersistenceService, AgentPersistenceService>();

        // Also register concrete types for backwards compatibility
        services.AddSingleton<GitHubService>();
        services.AddSingleton<WorkspaceService>();
        services.AddSingleton<FileSystemService>();
        services.AddSingleton<RepositoryService>();
        services.AddSingleton<RepositoryManagementService>();

        // ClaudeCodeService is created per-agent via Factory pattern

        // Register ViewModels
        services.RegisterForNavigation<SetupView, SetupViewModel>();
        services.RegisterForNavigation<RepositoryListView, RepositoryListViewModel>();
        services.RegisterForNavigation<RepositoryDetailView, RepositoryDetailViewModel>();
        services.RegisterForNavigation<HomeView, HomeViewModel>();
        services.RegisterForNavigation<WorkspaceListView, WorkspaceListViewModel>();
        services.RegisterForNavigation<WorkspaceView, WorkspaceViewModel>();
        services.RegisterForNavigation<ChatView, ChatViewModel>();
        services.RegisterForNavigation<UnifiedMainView, UnifiedMainViewModel>();

        // Register additional ViewModels that may be injected
        services.AddTransient<DiffViewerViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<AddRepositoryViewModel>();
        services.AddTransient<BranchSelectorViewModel>();
    }

    /// <summary>
    /// Application initialization logic - called after DI container is set up
    /// </summary>
    public override async Task OnInitializeAsync()
    {
        // Initialize RepositoryManagementService to load existing repositories
        var repositoryService = ServiceProvider?.GetService<RepositoryManagementService>();
        if (repositoryService != null)
        {
            await repositoryService.InitializeAsync();
            Log.Information("RepositoryManagementService initialized with {Count} repositories", repositoryService.Repositories.Count);
        }

        await base.OnInitializeAsync();
    }
}
