using Jinobald.Core.Mvvm;

namespace Jinobald.Core.Tests.Mvvm;

public class ViewModelBaseTests
{
    #region Test ViewModels

    private class TestViewModel : ViewModelBase
    {
        public bool OnDestroyDisposingCalled { get; private set; }
        public bool DisposingValue { get; private set; }
        public int DisposeCallCount { get; private set; }

        protected override void OnDestroy(bool disposing)
        {
            OnDestroyDisposingCalled = true;
            DisposingValue = disposing;
            DisposeCallCount++;
            base.OnDestroy(disposing);
        }
    }

    private class DisposableResourceViewModel : ViewModelBase
    {
        public bool ResourceDisposed { get; private set; }
        private readonly FakeDisposable _resource = new();

        public class FakeDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }
            public void Dispose() => IsDisposed = true;
        }

        protected override void OnDestroy(bool disposing)
        {
            if (disposing)
            {
                _resource.Dispose();
                ResourceDisposed = _resource.IsDisposed;
            }
            base.OnDestroy(disposing);
        }
    }

    #endregion

    [Fact]
    public void Dispose_ShouldCallOnDestroyWithDisposingTrue()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.Dispose();

        // Assert
        Assert.True(viewModel.OnDestroyDisposingCalled);
        Assert.True(viewModel.DisposingValue);
    }

    [Fact]
    public void Dispose_ShouldSetIsDisposedTrue()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.Dispose();

        // Assert
        Assert.True(viewModel.IsDisposed);
    }

    [Fact]
    public void Dispose_ShouldOnlyCallOnDestroyOnce()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.Dispose();
        viewModel.Dispose(); // Second call should be ignored

        // Assert
        Assert.Equal(1, viewModel.DisposeCallCount);
    }

    [Fact]
    public void Destroy_ShouldCallDispose()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.Destroy();

        // Assert
        Assert.True(viewModel.IsDisposed);
        Assert.True(viewModel.OnDestroyDisposingCalled);
    }

    [Fact]
    public void Destroy_ShouldOnlyCallOnDestroyOnce()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.Destroy();
        viewModel.Destroy(); // Second call should be ignored

        // Assert
        Assert.Equal(1, viewModel.DisposeCallCount);
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        // Arrange
        var viewModel = new DisposableResourceViewModel();

        // Act
        viewModel.Dispose();

        // Assert
        Assert.True(viewModel.ResourceDisposed);
    }

    [Fact]
    public void ThrowIfDisposed_ShouldNotThrowBeforeDispose()
    {
        // Arrange
        var viewModel = new ThrowIfDisposedTestViewModel();

        // Act & Assert - Should not throw
        viewModel.DoSomething();
    }

    [Fact]
    public void ThrowIfDisposed_ShouldThrowAfterDispose()
    {
        // Arrange
        var viewModel = new ThrowIfDisposedTestViewModel();
        viewModel.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => viewModel.DoSomething());
    }

    [Fact]
    public void Using_ShouldDisposeViewModel()
    {
        // Arrange
        TestViewModel? viewModel;

        // Act
        using (viewModel = new TestViewModel())
        {
            Assert.False(viewModel.IsDisposed);
        }

        // Assert
        Assert.True(viewModel.IsDisposed);
    }

    [Fact]
    public void IsDisposed_ShouldBeFalseInitially()
    {
        // Arrange & Act
        var viewModel = new TestViewModel();

        // Assert
        Assert.False(viewModel.IsDisposed);
    }

    private class ThrowIfDisposedTestViewModel : ViewModelBase
    {
        public void DoSomething()
        {
            ThrowIfDisposed();
            // Do something...
        }
    }
}
