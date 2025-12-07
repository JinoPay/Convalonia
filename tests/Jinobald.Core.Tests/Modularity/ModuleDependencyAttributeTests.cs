using Jinobald.Core.Ioc;
using Jinobald.Core.Modularity;
using Xunit;

namespace Jinobald.Core.Tests.Modularity;

public class ModuleDependencyAttributeTests
{
    [ModuleDependency("ModuleA")]
    [ModuleDependency("ModuleB")]
    public class ModuleWithDependencies : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry) { }
        public void OnInitialized(IContainerProvider containerProvider) { }
    }

    public class ModuleWithoutDependencies : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry) { }
        public void OnInitialized(IContainerProvider containerProvider) { }
    }

    [Fact]
    public void GetDependencies_WithDependencies_ShouldReturnAll()
    {
        // Act
        var dependencies = ModuleDependencyAttribute.GetDependencies(typeof(ModuleWithDependencies)).ToList();

        // Assert
        Assert.Equal(2, dependencies.Count);
        Assert.Contains("ModuleA", dependencies);
        Assert.Contains("ModuleB", dependencies);
    }

    [Fact]
    public void GetDependencies_WithoutDependencies_ShouldReturnEmpty()
    {
        // Act
        var dependencies = ModuleDependencyAttribute.GetDependencies(typeof(ModuleWithoutDependencies)).ToList();

        // Assert
        Assert.Empty(dependencies);
    }

    [Fact]
    public void GetDependencies_WithNullType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ModuleDependencyAttribute.GetDependencies(null!).ToList());
    }

    [Fact]
    public void Constructor_WithValidName_ShouldSetModuleName()
    {
        // Act
        var attribute = new ModuleDependencyAttribute("TestModule");

        // Assert
        Assert.Equal("TestModule", attribute.ModuleName);
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowException()
    {
        // Act & Assert (ThrowIfNullOrWhiteSpace throws ArgumentNullException for null)
        Assert.Throws<ArgumentNullException>(() => new ModuleDependencyAttribute(null!));
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ModuleDependencyAttribute(""));
    }
}
