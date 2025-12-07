using Jinobald.Core.Services.Regions;

namespace Jinobald.Core.Tests.Services.Regions;

public class RegionTests
{
    [Fact]
    public void Constructor_ShouldSetName()
    {
        // Arrange & Act
        var region = new Region("TestRegion");

        // Assert
        Assert.Equal("TestRegion", region.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowForInvalidName(string? name)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new Region(name!));
    }

    [Fact]
    public void Add_ShouldAddViewToCollection()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();

        // Act
        region.Add(view);

        // Assert
        Assert.Contains(view, region.Views);
    }

    [Fact]
    public void Add_ShouldRaiseViewAddedEvent()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        object? addedView = null;
        region.ViewAdded += (_, v) => addedView = v;

        // Act
        region.Add(view);

        // Assert
        Assert.Same(view, addedView);
    }

    [Fact]
    public void Add_ShouldNotAddDuplicateView()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();

        // Act
        region.Add(view);
        region.Add(view);

        // Assert
        Assert.Single(region.Views);
    }

    [Fact]
    public void Add_ShouldThrowForNullView()
    {
        // Arrange
        var region = new Region("TestRegion");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => region.Add(null!));
    }

    [Fact]
    public void Remove_ShouldRemoveViewFromCollection()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);

        // Act
        region.Remove(view);

        // Assert
        Assert.DoesNotContain(view, region.Views);
    }

    [Fact]
    public void Remove_ShouldRaiseViewRemovedEvent()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);
        object? removedView = null;
        region.ViewRemoved += (_, v) => removedView = v;

        // Act
        region.Remove(view);

        // Assert
        Assert.Same(view, removedView);
    }

    [Fact]
    public void Remove_ShouldDeactivateViewFirst()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);
        region.Activate(view);
        object? deactivatedView = null;
        region.ViewDeactivated += (_, v) => deactivatedView = v;

        // Act
        region.Remove(view);

        // Assert
        Assert.Same(view, deactivatedView);
        Assert.DoesNotContain(view, region.ActiveViews);
    }

    [Fact]
    public void Activate_ShouldAddToActiveViews()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);

        // Act
        region.Activate(view);

        // Assert
        Assert.Contains(view, region.ActiveViews);
    }

    [Fact]
    public void Activate_ShouldRaiseViewActivatedEvent()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);
        object? activatedView = null;
        region.ViewActivated += (_, v) => activatedView = v;

        // Act
        region.Activate(view);

        // Assert
        Assert.Same(view, activatedView);
    }

    [Fact]
    public void Activate_ShouldThrowForViewNotInRegion()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => region.Activate(view));
    }

    [Fact]
    public void Activate_ShouldNotActivateTwice()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);
        var activationCount = 0;
        region.ViewActivated += (_, _) => activationCount++;

        // Act
        region.Activate(view);
        region.Activate(view);

        // Assert
        Assert.Equal(1, activationCount);
    }

    [Fact]
    public void Deactivate_ShouldRemoveFromActiveViews()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);
        region.Activate(view);

        // Act
        region.Deactivate(view);

        // Assert
        Assert.DoesNotContain(view, region.ActiveViews);
    }

    [Fact]
    public void Deactivate_ShouldRaiseViewDeactivatedEvent()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);
        region.Activate(view);
        object? deactivatedView = null;
        region.ViewDeactivated += (_, v) => deactivatedView = v;

        // Act
        region.Deactivate(view);

        // Assert
        Assert.Same(view, deactivatedView);
    }

    [Fact]
    public void Contains_ShouldReturnTrueForAddedView()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();
        region.Add(view);

        // Act & Assert
        Assert.True(region.Contains(view));
    }

    [Fact]
    public void Contains_ShouldReturnFalseForNotAddedView()
    {
        // Arrange
        var region = new Region("TestRegion");
        var view = new object();

        // Act & Assert
        Assert.False(region.Contains(view));
    }

    [Fact]
    public void RemoveAll_ShouldClearAllViews()
    {
        // Arrange
        var region = new Region("TestRegion");
        region.Add(new object());
        region.Add(new object());
        region.Add(new object());

        // Act
        region.RemoveAll();

        // Assert
        Assert.Empty(region.Views);
    }

    [Fact]
    public void SortHint_DefaultShouldBeDefault()
    {
        // Arrange & Act
        var region = new Region("TestRegion");

        // Assert
        Assert.Equal(ViewSortHint.Default, region.SortHint);
    }

    [Fact]
    public void Add_WithReverseSortHint_ShouldInsertAtBeginning()
    {
        // Arrange
        var region = new Region("TestRegion") { SortHint = ViewSortHint.Reverse };
        var view1 = new object();
        var view2 = new object();

        // Act
        region.Add(view1);
        region.Add(view2);

        // Assert
        Assert.Same(view2, region.Views.First());
        Assert.Same(view1, region.Views.Last());
    }

    [Fact]
    public void RegionTarget_ShouldBeSettable()
    {
        // Arrange
        var region = new Region("TestRegion");
        var target = new object();

        // Act
        region.RegionTarget = target;

        // Assert
        Assert.Same(target, region.RegionTarget);
    }
}
