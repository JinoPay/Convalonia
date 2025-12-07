using Avalonia.Controls;
using Convalonia.ViewModels;

namespace Convalonia.Views;

public partial class HomeView : UserControl
{
    public HomeView(HomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public HomeView() : this(null!) { }
}
