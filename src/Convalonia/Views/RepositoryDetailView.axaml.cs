using Avalonia.Controls;
using Convalonia.ViewModels;

namespace Convalonia.Views;

public partial class RepositoryDetailView : UserControl
{
    public RepositoryDetailView(RepositoryDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public RepositoryDetailView() : this(null!) { }
}
