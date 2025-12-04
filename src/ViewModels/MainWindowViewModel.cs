using CommunityToolkit.Mvvm.ComponentModel;
using Jinobald.Core.Mvvm;

namespace Convalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel()
    {
    }

    public void NavigateTo(object view)
    {
        CurrentView = view;
    }
}
