using CommunityToolkit.Mvvm.ComponentModel;
using Convalonia.Services;
using Jinobald.Core.Mvvm;

namespace Convalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly WorkspaceService _workspaceService;

    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public void NavigateTo(object view)
    {
        CurrentView = view;
    }

    public void NavigateToSetup()
    {
        CurrentView = "Setup";
    }

    public void NavigateToWorkspaceList()
    {
        CurrentView = "WorkspaceList";
    }

    public void NavigateToHome()
    {
        CurrentView = "Home";
    }

    public bool ShouldShowSetup()
    {
        // Show setup if no workspaces exist
        return _workspaceService.Workspaces.Count == 0;
    }
}
