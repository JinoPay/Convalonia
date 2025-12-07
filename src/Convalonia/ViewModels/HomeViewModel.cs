using System.Linq;
using System.Threading.Tasks;
using Convalonia.Services;
using Convalonia.Services.Toast;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

public partial class HomeViewModel : ReactiveObject
{
    private readonly IToastService _toastService;
    private readonly WorkspaceService _workspaceService;

    [Reactive]
    private int _totalWorkspaces = 0;

    [Reactive]
    private int _totalAgents = 0;

    [Reactive]
    private int _activeTasks = 0;

    public HomeViewModel(
        IToastService toastService,
        WorkspaceService workspaceService)
    {
        _toastService = toastService;
        _workspaceService = workspaceService;

        UpdateStats();
    }

    [ReactiveCommand]
    private async Task CreateWorkspaceAsync()
    {
        _toastService.ShowInfo("Please use the Workspaces view to create new workspaces");
        await Task.CompletedTask;
    }

    [ReactiveCommand]
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
