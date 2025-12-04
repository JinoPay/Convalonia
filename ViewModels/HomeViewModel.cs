using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jinobald.Core.Services.Toast;
using Jinobald.Core.Mvvm;

namespace Convalonia.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IToastService _toastService;

    [ObservableProperty]
    private string _title = "Jinobald Framework Demo";

    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Medan App using Jinobald Framework!";

    [ObservableProperty]
    private int _counter = 0;

    public HomeViewModel(IToastService toastService)
    {
        _toastService = toastService;
    }

    [RelayCommand]
    private void IncrementCounter()
    {
        Counter++;
        _toastService.ShowSuccess($"Counter increased to {Counter}!");
    }

    [RelayCommand]
    private void ShowInfo()
    {
        _toastService.ShowInfo("This is a demo application using Jinobald MVVM Framework", duration: 3);
    }

    [RelayCommand]
    private void ResetCounter()
    {
        Counter = 0;
        _toastService.ShowWarning("Counter has been reset");
    }
}
