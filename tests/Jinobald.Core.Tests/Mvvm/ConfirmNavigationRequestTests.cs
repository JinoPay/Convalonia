using Jinobald.Core.Mvvm;
using Xunit;

namespace Jinobald.Core.Tests.Mvvm;

public class ConfirmNavigationRequestTests
{
    #region Test ViewModels

    private class ConfirmingViewModel : IConfirmNavigationRequest
    {
        public bool ShouldAllow { get; set; } = true;
        public int ConfirmRequestCount { get; private set; }

        public void ConfirmNavigationRequest(NavigationContext context, Action<bool> continuationCallback)
        {
            ConfirmRequestCount++;
            continuationCallback(ShouldAllow);
        }

        public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;
        public Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;
        public Task<bool> OnNavigatingFromAsync(NavigationContext context) => Task.FromResult(true);
        public Task<bool> OnNavigatingToAsync(NavigationContext context) => Task.FromResult(true);
    }

    private class AsyncConfirmingViewModel : IConfirmNavigationRequestAsync
    {
        public bool ShouldAllow { get; set; } = true;
        public int ConfirmRequestCount { get; private set; }

        public Task<bool> ConfirmNavigationRequestAsync(NavigationContext context)
        {
            ConfirmRequestCount++;
            return Task.FromResult(ShouldAllow);
        }

        public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;
        public Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;
        public Task<bool> OnNavigatingFromAsync(NavigationContext context) => Task.FromResult(true);
        public Task<bool> OnNavigatingToAsync(NavigationContext context) => Task.FromResult(true);
    }

    private class NavigationAwareViewModel : INavigationAware
    {
        public bool ShouldAllow { get; set; } = true;
        public int NavigatingFromCount { get; private set; }

        public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;
        public Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;

        public Task<bool> OnNavigatingFromAsync(NavigationContext context)
        {
            NavigatingFromCount++;
            return Task.FromResult(ShouldAllow);
        }

        public Task<bool> OnNavigatingToAsync(NavigationContext context) => Task.FromResult(true);
    }

    private class PlainViewModel { }

    #endregion

    [Fact]
    public void TryConfirmNavigation_WithConfirmableViewModel_ShouldReturnTrue()
    {
        // Arrange
        var viewModel = new ConfirmingViewModel();
        var context = new NavigationContext();
        bool? result = null;

        // Act
        var handled = context.TryConfirmNavigation(viewModel, r => result = r);

        // Assert
        Assert.True(handled);
        Assert.True(result);
        Assert.Equal(1, viewModel.ConfirmRequestCount);
    }

    [Fact]
    public void TryConfirmNavigation_WithNonConfirmableViewModel_ShouldReturnFalse()
    {
        // Arrange
        var viewModel = new PlainViewModel();
        var context = new NavigationContext();
        bool callbackCalled = false;

        // Act
        var handled = context.TryConfirmNavigation(viewModel, _ => callbackCalled = true);

        // Assert
        Assert.False(handled);
        Assert.False(callbackCalled);
    }

    [Fact]
    public void TryConfirmNavigation_WhenDenied_ShouldReturnFalse()
    {
        // Arrange
        var viewModel = new ConfirmingViewModel { ShouldAllow = false };
        var context = new NavigationContext();
        bool? result = null;

        // Act
        context.TryConfirmNavigation(viewModel, r => result = r);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryConfirmNavigationAsync_WithAsyncConfirmable_ShouldWork()
    {
        // Arrange
        var viewModel = new AsyncConfirmingViewModel();
        var context = new NavigationContext();

        // Act
        var result = await context.TryConfirmNavigationAsync(viewModel);

        // Assert
        Assert.True(result);
        Assert.Equal(1, viewModel.ConfirmRequestCount);
    }

    [Fact]
    public async Task TryConfirmNavigationAsync_WithNonConfirmable_ShouldReturnTrue()
    {
        // Arrange
        var viewModel = new PlainViewModel();
        var context = new NavigationContext();

        // Act
        var result = await context.TryConfirmNavigationAsync(viewModel);

        // Assert
        Assert.True(result); // Default allows navigation
    }

    [Fact]
    public async Task TryConfirmNavigationAsync_WhenDenied_ShouldReturnFalse()
    {
        // Arrange
        var viewModel = new AsyncConfirmingViewModel { ShouldAllow = false };
        var context = new NavigationContext();

        // Act
        var result = await context.TryConfirmNavigationAsync(viewModel);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_WithAsyncConfirmable_ShouldUseAsync()
    {
        // Arrange
        var viewModel = new AsyncConfirmingViewModel();
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(viewModel);

        // Assert
        Assert.True(result);
        Assert.Equal(1, viewModel.ConfirmRequestCount);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_WithCallbackConfirmable_ShouldWrapAsTask()
    {
        // Arrange
        var viewModel = new ConfirmingViewModel();
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(viewModel);

        // Assert
        Assert.True(result);
        Assert.Equal(1, viewModel.ConfirmRequestCount);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_WithNavigationAware_ShouldUseOnNavigatingFrom()
    {
        // Arrange
        var viewModel = new NavigationAwareViewModel();
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(viewModel);

        // Assert
        Assert.True(result);
        Assert.Equal(1, viewModel.NavigatingFromCount);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_WithNavigationAwareDenying_ShouldReturnFalse()
    {
        // Arrange
        var viewModel = new NavigationAwareViewModel { ShouldAllow = false };
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(viewModel);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_WithNull_ShouldReturnTrue()
    {
        // Arrange
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_WithPlainObject_ShouldReturnTrue()
    {
        // Arrange
        var viewModel = new PlainViewModel();
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(viewModel);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmNavigationAsync_AsyncTakesPrecedenceOverCallback()
    {
        // Arrange - ViewModel implementing both interfaces
        var viewModel = new BothInterfacesViewModel { AsyncShouldAllow = false, CallbackShouldAllow = true };
        var context = new NavigationContext();

        // Act
        var result = await context.ConfirmNavigationAsync(viewModel);

        // Assert - Async version should take precedence and deny
        Assert.False(result);
    }

    private class BothInterfacesViewModel : IConfirmNavigationRequestAsync, IConfirmNavigationRequest
    {
        public bool AsyncShouldAllow { get; set; } = true;
        public bool CallbackShouldAllow { get; set; } = true;

        public Task<bool> ConfirmNavigationRequestAsync(NavigationContext context)
            => Task.FromResult(AsyncShouldAllow);

        public void ConfirmNavigationRequest(NavigationContext context, Action<bool> continuationCallback)
            => continuationCallback(CallbackShouldAllow);

        public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;
        public Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;
        public Task<bool> OnNavigatingFromAsync(NavigationContext context) => Task.FromResult(true);
        public Task<bool> OnNavigatingToAsync(NavigationContext context) => Task.FromResult(true);
    }
}
