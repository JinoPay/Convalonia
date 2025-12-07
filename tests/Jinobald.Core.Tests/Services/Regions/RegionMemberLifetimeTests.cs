using Jinobald.Core.Ioc;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Regions;
using NSubstitute;

namespace Jinobald.Core.Tests.Services.Regions;

public class RegionMemberLifetimeTests
{
    private readonly IViewResolver _viewResolver;
    private readonly IRegion _region;
    private readonly RegionNavigationService _navigationService;

    public RegionMemberLifetimeTests()
    {
        _viewResolver = Substitute.For<IViewResolver>();
        _region = new Region("TestRegion");
        _navigationService = new RegionNavigationService(_region, _viewResolver);
    }

    #region Test ViewModels

    private class KeepAliveViewModel : IRegionMemberLifetime
    {
        public bool KeepAlive => true;
    }

    private class NoKeepAliveViewModel : IRegionMemberLifetime
    {
        public bool KeepAlive => false;
    }

    private class RegularViewModel
    {
    }

    private class TestView
    {
        public object? DataContext { get; set; }
    }

    private class KeepAliveView : IRegionMemberLifetime
    {
        public object? DataContext { get; set; }
        public bool KeepAlive => true;
    }

    private class NoKeepAliveView : IRegionMemberLifetime
    {
        public object? DataContext { get; set; }
        public bool KeepAlive => false;
    }

    #endregion

    [Fact]
    public async Task Navigate_WithKeepAliveViewModel_ShouldCacheView()
    {
        // Arrange
        var viewModel = new KeepAliveViewModel();
        var view = new TestView { DataContext = viewModel };

        _viewResolver.ResolveView(typeof(TestView)).Returns(view);
        _viewResolver.ResolveViewModelType(typeof(TestView)).Returns(typeof(KeepAliveViewModel));

        // Act - Navigate to view
        await _navigationService.NavigateAsync<TestView>();

        // Navigate away
        var view2 = new TestView { DataContext = new RegularViewModel() };
        _viewResolver.ResolveView(typeof(TestView)).Returns(view2);

        // Navigate back to same view type
        _viewResolver.ResolveView(typeof(TestView)).Returns(view);
        await _navigationService.NavigateAsync<TestView>();

        // Assert - Should reuse cached view (same instance)
        Assert.Same(view, _navigationService.CurrentView);
    }

    [Fact]
    public async Task Navigate_WithNoKeepAliveViewModel_ShouldNotCacheView()
    {
        // Arrange
        var viewModel = new NoKeepAliveViewModel();
        var view1 = new TestView { DataContext = viewModel };

        _viewResolver.ResolveView(typeof(TestView)).Returns(view1);
        _viewResolver.ResolveViewModelType(typeof(TestView)).Returns(typeof(NoKeepAliveViewModel));

        // Act - Navigate to view
        await _navigationService.NavigateAsync<TestView>();
        var firstView = _navigationService.CurrentView;

        // Create new view for second navigation
        var view2 = new TestView { DataContext = new NoKeepAliveViewModel() };
        _viewResolver.ResolveView(typeof(TestView)).Returns(view2);

        // Navigate to another view type then back
        var otherView = new TestView { DataContext = new RegularViewModel() };
        _viewResolver.ResolveView(typeof(object)).Returns(otherView);
        await _navigationService.NavigateAsync(typeof(object));

        // Navigate back to TestView
        await _navigationService.NavigateAsync<TestView>();

        // Assert - Should create new view (different instance)
        Assert.Same(view2, _navigationService.CurrentView);
    }

    [Fact]
    public async Task Navigate_WithKeepAliveView_ShouldCacheView()
    {
        // Arrange
        var viewModel = new RegularViewModel();
        var view = new KeepAliveView { DataContext = viewModel };

        _viewResolver.ResolveView(typeof(KeepAliveView)).Returns(view);
        _viewResolver.ResolveViewModelType(typeof(KeepAliveView)).Returns(typeof(RegularViewModel));

        // Act - Navigate to view
        await _navigationService.NavigateAsync<KeepAliveView>();

        // Assert - View should be in region
        Assert.Same(view, _navigationService.CurrentView);
    }

    [Fact]
    public async Task Navigate_ViewModelKeepAlive_ShouldOverrideRegionSetting()
    {
        // Arrange
        _navigationService.KeepAlive = false; // Region level: no cache

        var viewModel = new KeepAliveViewModel(); // ViewModel level: cache
        var view = new TestView { DataContext = viewModel };

        _viewResolver.ResolveView(typeof(TestView)).Returns(view);
        _viewResolver.ResolveViewModelType(typeof(TestView)).Returns(typeof(KeepAliveViewModel));

        // Act
        await _navigationService.NavigateAsync<TestView>();

        // Navigate away
        var otherView = new TestView { DataContext = new RegularViewModel() };
        _viewResolver.ResolveView(typeof(object)).Returns(otherView);
        await _navigationService.NavigateAsync(typeof(object));

        // Navigate back
        await _navigationService.NavigateAsync<TestView>();

        // Assert - ViewModel's KeepAlive should override Region's setting
        Assert.Same(view, _navigationService.CurrentView);
    }

    [Fact]
    public async Task Navigate_ViewModelNoKeepAlive_ShouldOverrideRegionSetting()
    {
        // Arrange
        _navigationService.KeepAlive = true; // Region level: cache

        var viewModel = new NoKeepAliveViewModel(); // ViewModel level: no cache
        var view1 = new TestView { DataContext = viewModel };

        _viewResolver.ResolveView(typeof(TestView)).Returns(view1);
        _viewResolver.ResolveViewModelType(typeof(TestView)).Returns(typeof(NoKeepAliveViewModel));

        // Act
        await _navigationService.NavigateAsync<TestView>();

        // Navigate away
        var otherView = new TestView { DataContext = new RegularViewModel() };
        _viewResolver.ResolveView(typeof(object)).Returns(otherView);
        await _navigationService.NavigateAsync(typeof(object));

        // Create new view for return navigation
        var view2 = new TestView { DataContext = new NoKeepAliveViewModel() };
        _viewResolver.ResolveView(typeof(TestView)).Returns(view2);

        // Navigate back
        await _navigationService.NavigateAsync<TestView>();

        // Assert - ViewModel's KeepAlive=false should override Region's KeepAlive=true
        Assert.Same(view2, _navigationService.CurrentView);
    }

    [Fact]
    public async Task Navigate_WithRegularViewModel_ShouldUseRegionKeepAliveSetting()
    {
        // Arrange - Region KeepAlive enabled
        _navigationService.KeepAlive = true;

        var viewModel = new RegularViewModel(); // No IRegionMemberLifetime
        var view = new TestView { DataContext = viewModel };

        _viewResolver.ResolveView(typeof(TestView)).Returns(view);
        _viewResolver.ResolveViewModelType(typeof(TestView)).Returns(typeof(RegularViewModel));

        // Act
        await _navigationService.NavigateAsync<TestView>();

        // Navigate away
        var otherView = new TestView { DataContext = new RegularViewModel() };
        _viewResolver.ResolveView(typeof(object)).Returns(otherView);
        await _navigationService.NavigateAsync(typeof(object));

        // Navigate back
        await _navigationService.NavigateAsync<TestView>();

        // Assert - Should use Region's KeepAlive setting (cache)
        Assert.Same(view, _navigationService.CurrentView);
    }

    [Fact]
    public async Task Navigate_ViewModelHasPriorityOverView()
    {
        // Arrange
        var viewModel = new NoKeepAliveViewModel(); // ViewModel: no cache
        var view = new KeepAliveView { DataContext = viewModel }; // View: cache

        _viewResolver.ResolveView(typeof(KeepAliveView)).Returns(view);
        _viewResolver.ResolveViewModelType(typeof(KeepAliveView)).Returns(typeof(NoKeepAliveViewModel));

        // Act
        await _navigationService.NavigateAsync<KeepAliveView>();

        // Navigate away
        var otherView = new TestView { DataContext = new RegularViewModel() };
        _viewResolver.ResolveView(typeof(object)).Returns(otherView);
        await _navigationService.NavigateAsync(typeof(object));

        // Create new view for return navigation
        var view2 = new KeepAliveView { DataContext = new NoKeepAliveViewModel() };
        _viewResolver.ResolveView(typeof(KeepAliveView)).Returns(view2);

        // Navigate back
        await _navigationService.NavigateAsync<KeepAliveView>();

        // Assert - ViewModel's setting should take priority over View's
        Assert.Same(view2, _navigationService.CurrentView);
    }
}
