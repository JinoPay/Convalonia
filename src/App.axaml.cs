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
    }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register Services
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
}
