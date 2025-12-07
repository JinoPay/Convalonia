using Convalonia.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    private readonly WorkspaceService _workspaceService;

    [Reactive]
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
