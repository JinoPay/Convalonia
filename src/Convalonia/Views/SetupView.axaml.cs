using Avalonia.Controls;
using Convalonia.ViewModels;

namespace Convalonia.Views;

public partial class SetupView : UserControl
{
    public SetupView(SetupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public SetupView() : this(null!) { }
}
