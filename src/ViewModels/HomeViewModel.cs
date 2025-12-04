using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Services;
using Jinobald.Core.Services.Toast;
using Jinobald.Core.Mvvm;

namespace Convalonia.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IToastService _toastService;
    private readonly WorkspaceService _workspaceService;

    [ObservableProperty]
    private int _totalWorkspaces = 0;

    [ObservableProperty]
    private int _totalAgents = 0;

    [ObservableProperty]
    private int _activeTasks = 0;

    public HomeViewModel(
        IToastService toastService,
        WorkspaceService workspaceService)
    {
        _toastService = toastService;
        _workspaceService = workspaceService;

        UpdateStats();
    }

    [RelayCommand]
    private async Task CreateWorkspaceAsync()
    {
        _toastService.ShowInfo("Please use the Workspaces view to create new workspaces");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ViewWorkspacesAsync()
    {
        _toastService.ShowInfo("Please navigate to Workspaces view");
        await Task.CompletedTask;
    }

    private void UpdateStats()
    {
        TotalWorkspaces = _workspaceService.Workspaces.Count;
        TotalAgents = _workspaceService.Workspaces.Sum(w => w.Agents.Count);
        ActiveTasks = _workspaceService.Workspaces
            .SelectMany(w => w.Agents)
            .Count(a => a.Status == Models.AgentStatus.Thinking || a.Status == Models.AgentStatus.UsingTool);
    }
}
