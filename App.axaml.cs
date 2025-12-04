using Avalonia.Markup.Xaml;
using Jinobald.Avalonia.Application;
using Jinobald.Core.Ioc;
using Convalonia.Views;

namespace Convalonia;

public partial class App : ApplicationBase<MainWindow>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register ViewModels and Views for navigation
        containerRegistry.RegisterForNavigation<HomeView>();
    }
}
