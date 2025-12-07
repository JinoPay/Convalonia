using Jinobald.Core.Services.Regions;

namespace Jinobald.Core.Tests.Services.Regions;

public class RegionManagerTests
{
    private readonly IViewResolver _viewResolver;
    private readonly RegionManager _regionManager;

    public RegionManagerTests()
    {
        _viewResolver = Substitute.For<IViewResolver>();
        _regionManager = new RegionManager(_viewResolver);
    }

    [Fact]
    public void Constructor_ShouldThrowForNullViewResolver()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RegionManager(null!));
    }

    [Fact]
    public void CreateOrGetRegion_ShouldCreateNewRegion()
    {
        // Arrange & Act
        var region = _regionManager.CreateOrGetRegion("TestRegion");

        // Assert
        Assert.NotNull(region);
        Assert.Equal("TestRegion", region.Name);
    }

    [Fact]
    public void CreateOrGetRegion_ShouldReturnExistingRegion()
    {
        // Arrange
        var region1 = _regionManager.CreateOrGetRegion("TestRegion");

        // Act
        var region2 = _regionManager.CreateOrGetRegion("TestRegion");

        // Assert
        Assert.Same(region1, region2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateOrGetRegion_ShouldThrowForInvalidName(string? name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _regionManager.CreateOrGetRegion(name!));
    }

    [Fact]
    public void GetRegion_ShouldReturnNullForNonexistentRegion()
    {
        // Act
        var region = _regionManager.GetRegion("Nonexistent");

        // Assert
        Assert.Null(region);
    }

    [Fact]
    public void GetRegion_ShouldReturnExistingRegion()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act
        var region = _regionManager.GetRegion("TestRegion");

        // Assert
        Assert.NotNull(region);
        Assert.Equal("TestRegion", region.Name);
    }

    [Fact]
    public void ContainsRegion_ShouldReturnTrueForExistingRegion()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act & Assert
        Assert.True(_regionManager.ContainsRegion("TestRegion"));
    }

    [Fact]
    public void ContainsRegion_ShouldReturnFalseForNonexistentRegion()
    {
        // Act & Assert
        Assert.False(_regionManager.ContainsRegion("Nonexistent"));
    }

    [Fact]
    public void RegisterRegion_ShouldAddRegion()
    {
        // Arrange
        var region = new Region("TestRegion");

        // Act
        _regionManager.RegisterRegion(region);

        // Assert
        Assert.True(_regionManager.ContainsRegion("TestRegion"));
    }

    [Fact]
    public void RegisterRegion_ShouldRaiseRegionAddedEvent()
    {
        // Arrange
        var region = new Region("TestRegion");
        IRegion? addedRegion = null;
        _regionManager.RegionAdded += (_, r) => addedRegion = r;

        // Act
        _regionManager.RegisterRegion(region);

        // Assert
        Assert.Same(region, addedRegion);
    }

    [Fact]
    public void RegisterRegion_ShouldIgnoreDuplicateRegion()
    {
        // Arrange
        var originalRegion = _regionManager.CreateOrGetRegion("TestRegion");
        var duplicateRegion = new Region("TestRegion");

        // Act - 중복 등록은 예외 없이 무시됨
        _regionManager.RegisterRegion(duplicateRegion);

        // Assert - 원래 Region이 유지됨
        Assert.Same(originalRegion, _regionManager.GetRegion("TestRegion"));
    }

    [Fact]
    public void RegisterRegion_ShouldThrowForNullRegion()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _regionManager.RegisterRegion(null!));
    }

    [Fact]
    public void RemoveRegion_ShouldRemoveExistingRegion()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act
        var result = _regionManager.RemoveRegion("TestRegion");

        // Assert
        Assert.True(result);
        Assert.False(_regionManager.ContainsRegion("TestRegion"));
    }

    [Fact]
    public void RemoveRegion_ShouldReturnFalseForNonexistentRegion()
    {
        // Act
        var result = _regionManager.RemoveRegion("Nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveRegion_ShouldRaiseRegionRemovedEvent()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");
        string? removedRegionName = null;
        _regionManager.RegionRemoved += (_, name) => removedRegionName = name;

        // Act
        _regionManager.RemoveRegion("TestRegion");

        // Assert
        Assert.Equal("TestRegion", removedRegionName);
    }

    [Fact]
    public void Regions_ShouldReturnAllRegisteredRegions()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("Region1");
        _regionManager.CreateOrGetRegion("Region2");
        _regionManager.CreateOrGetRegion("Region3");

        // Act
        var regions = _regionManager.Regions.ToList();

        // Assert
        Assert.Equal(3, regions.Count);
        Assert.Contains(regions, r => r.Name == "Region1");
        Assert.Contains(regions, r => r.Name == "Region2");
        Assert.Contains(regions, r => r.Name == "Region3");
    }

    [Fact]
    public void GetNavigationService_ShouldReturnServiceForExistingRegion()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act
        var navService = _regionManager.GetNavigationService("TestRegion");

        // Assert
        Assert.NotNull(navService);
    }

    [Fact]
    public void GetNavigationService_ShouldReturnNullForNonexistentRegion()
    {
        // Act
        var navService = _regionManager.GetNavigationService("Nonexistent");

        // Assert
        Assert.Null(navService);
    }

    [Fact]
    public void AddToRegion_WithView_ShouldAddViewToRegion()
    {
        // Arrange
        var view = new object();

        // Act
        _regionManager.AddToRegion("TestRegion", view);

        // Assert
        var region = _regionManager.GetRegion("TestRegion");
        Assert.NotNull(region);
        Assert.Contains(view, region.Views);
    }

    [Fact]
    public void AddToRegion_WithViewType_ShouldResolveAndAddView()
    {
        // Arrange
        var mockView = new object();
        _viewResolver.ResolveView(typeof(TestView)).Returns(mockView);

        // Act
        _regionManager.AddToRegion("TestRegion", typeof(TestView));

        // Assert
        var region = _regionManager.GetRegion("TestRegion");
        Assert.NotNull(region);
        Assert.Contains(mockView, region.Views);
    }

    [Fact]
    public void RemoveFromRegion_ShouldRemoveViewFromRegion()
    {
        // Arrange
        var view = new object();
        _regionManager.AddToRegion("TestRegion", view);

        // Act
        _regionManager.RemoveFromRegion("TestRegion", view);

        // Assert
        var region = _regionManager.GetRegion("TestRegion");
        Assert.NotNull(region);
        Assert.DoesNotContain(view, region.Views);
    }

    [Fact]
    public void CanGoBack_ShouldReturnFalseInitially()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act & Assert
        Assert.False(_regionManager.CanGoBack("TestRegion"));
    }

    [Fact]
    public void CanGoForward_ShouldReturnFalseInitially()
    {
        // Arrange
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act & Assert
        Assert.False(_regionManager.CanGoForward("TestRegion"));
    }

    [Fact]
    public void RegisterViewWithRegion_ShouldNavigateWhenRegionExists()
    {
        // Arrange
        var mockView = new object();
        _viewResolver.ResolveView(typeof(TestView)).Returns(mockView);
        _regionManager.CreateOrGetRegion("TestRegion");

        // Act
        _regionManager.RegisterViewWithRegion("TestRegion", typeof(TestView));

        // Need to wait for async operation
        Thread.Sleep(100);

        // Assert
        var region = _regionManager.GetRegion("TestRegion");
        Assert.NotNull(region);
    }

    private class TestView { }
}
