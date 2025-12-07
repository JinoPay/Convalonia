using Jinobald.Wpf.Mvvm;

namespace Jinobald.Wpf.Tests.Mvvm
{
    public class ViewModelLocatorTests
    {
        [Fact]
        public void ResolveViewModelType_ShouldConvertViewToViewModel()
        {
            // Arrange
            var viewType = typeof(TestNamespace.Views.HomeView);

            // Act
            var viewModelType = ViewModelLocator.ResolveViewModelType(viewType);

            // Assert
            Assert.NotNull(viewModelType);
            Assert.Equal(typeof(TestNamespace.ViewModels.HomeViewModel), viewModelType);
        }

        [Fact]
        public void ResolveViewModelType_ShouldConvertWindowToWindowViewModel()
        {
            // Arrange
            var viewType = typeof(TestNamespace.Views.MainWindow);

            // Act
            var viewModelType = ViewModelLocator.ResolveViewModelType(viewType);

            // Assert
            Assert.NotNull(viewModelType);
            Assert.Equal(typeof(TestNamespace.ViewModels.MainWindowViewModel), viewModelType);
        }

        [Fact]
        public void ResolveViewModelType_ShouldReturnNullForUnmatchedType()
        {
            // Arrange
            var viewType = typeof(TestNamespace.Views.NoMatchingViewModel);

            // Act
            var viewModelType = ViewModelLocator.ResolveViewModelType(viewType);

            // Assert
            Assert.Null(viewModelType);
        }

        [Fact]
        public void ResolveViewType_ShouldConvertViewModelToView()
        {
            // Arrange
            var viewModelType = typeof(TestNamespace.ViewModels.HomeViewModel);

            // Act
            var viewType = ViewModelLocator.ResolveViewType(viewModelType);

            // Assert
            Assert.NotNull(viewType);
            Assert.Equal(typeof(TestNamespace.Views.HomeView), viewType);
        }

        [Fact]
        public void ResolveViewType_ShouldConvertWindowViewModelToWindow()
        {
            // Arrange
            var viewModelType = typeof(TestNamespace.ViewModels.MainWindowViewModel);

            // Act
            var viewType = ViewModelLocator.ResolveViewType(viewModelType);

            // Assert
            Assert.NotNull(viewType);
            Assert.Equal(typeof(TestNamespace.Views.MainWindow), viewType);
        }

        [Fact]
        public void ResolveViewType_ShouldReturnNullForUnmatchedType()
        {
            // Arrange
            var viewModelType = typeof(TestNamespace.ViewModels.OrphanViewModel);

            // Act
            var viewType = ViewModelLocator.ResolveViewType(viewModelType);

            // Assert
            Assert.Null(viewType);
        }
    }
}

// Test types for ViewModelLocator testing
namespace TestNamespace.Views
{
    public class HomeView { }
    public class MainWindow { }
    public class NoMatchingViewModel { }
}

namespace TestNamespace.ViewModels
{
    public class HomeViewModel { }
    public class MainWindowViewModel { }
    public class OrphanViewModel { }
}
