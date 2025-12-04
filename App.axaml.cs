using System;
using System.IO;
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
        var baseWorkspacePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ConvaloniaWorkspaces"
        );

        containerRegistry.RegisterSingleton<WorkspaceService>(() =>
            new WorkspaceService(baseWorkspacePath));

        containerRegistry.RegisterSingleton<GitHubService>();
        containerRegistry.RegisterSingleton<FileSystemService>();

        // Note: ClaudeCodeService is created per-agent with workspace path

        // Register ViewModels
        containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
        containerRegistry.RegisterForNavigation<WorkspaceListView, WorkspaceListViewModel>();
        containerRegistry.RegisterForNavigation<WorkspaceView, WorkspaceViewModel>();
        containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
    }
}
