using Avalonia.Controls;
using Convalonia.ViewModels;

namespace Convalonia.Views;

public partial class RepositoryListView : UserControl
{
    public RepositoryListView(RepositoryListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public RepositoryListView() : this(null!) { }
}
