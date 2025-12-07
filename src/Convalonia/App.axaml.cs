using System;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Jinobald.Avalonia.Application;
using Jinobald.Core.Ioc;
using Convalonia.Views;
using Convalonia.ViewModels;
using Convalonia.Services;

namespace Convalonia;

public partial class App : ApplicationBase<MainWindow>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Setup global exception handlers
        SetupExceptionHandling();
    }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register Services with interfaces
        containerRegistry.RegisterSingleton<IGitService, GitHubService>();
        containerRegistry.RegisterSingleton<IWorkspaceService, WorkspaceService>();
        containerRegistry.RegisterSingleton<IFileSystemService, FileSystemService>();
        containerRegistry.RegisterSingleton<IRepositoryService, RepositoryService>();
        containerRegistry.RegisterSingleton<IRepositoryManagementService, RepositoryManagementService>();

        // Also register concrete types for backwards compatibility
        containerRegistry.RegisterSingleton<GitHubService>();
        containerRegistry.RegisterSingleton<WorkspaceService>();
        containerRegistry.RegisterSingleton<FileSystemService>();
        containerRegistry.RegisterSingleton<RepositoryService>();
        containerRegistry.RegisterSingleton<RepositoryManagementService>();

        // Note: ClaudeCodeService is created per-agent with workspace path

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
            LogException("AppDomain.UnhandledException", exception);
        };

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            LogException("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved(); // Prevent process termination
        };
    }

    private void LogException(string source, Exception? exception)
    {
        // TODO: Replace with Serilog when added
        Console.WriteLine($"[{source}] Unhandled exception: {exception?.Message}");
        Console.WriteLine(exception?.StackTrace);
    }
}
