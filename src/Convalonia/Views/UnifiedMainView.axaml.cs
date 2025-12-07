using Avalonia.Controls;
using Convalonia.ViewModels;

namespace Convalonia.Views;

public partial class UnifiedMainView : UserControl
{
    public UnifiedMainView(UnifiedMainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // Parameterless constructor for designer
    public UnifiedMainView() : this(null!)
    {
    }
}
