using Jinobald.Core.Ioc;
using Jinobald.Core.Modularity;
using Xunit;

namespace Jinobald.Core.Tests.Modularity;

public class ModuleInfoTests
{
    public class ValidModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry) { }
        public void OnInitialized(IContainerProvider containerProvider) { }
    }

    public class NonModuleClass { }

    [Fact]
    public void Constructor_WithValidType_ShouldCreateModuleInfo()
    {
        // Act
        var moduleInfo = new ModuleInfo("MyModule", typeof(ValidModule));

        // Assert
        Assert.Equal("MyModule", moduleInfo.ModuleName);
        Assert.Equal(typeof(ValidModule), moduleInfo.ModuleType);
        Assert.Equal(ModuleState.NotLoaded, moduleInfo.State);
        Assert.Equal(InitializationMode.WhenAvailable, moduleInfo.InitializationMode);
        Assert.Empty(moduleInfo.DependsOn);
        Assert.Null(moduleInfo.InitializationException);
    }

    [Fact]
    public void Constructor_WithTypeOnly_ShouldUseTypeNameAsModuleName()
    {
        // Act
        var moduleInfo = new ModuleInfo(typeof(ValidModule));

        // Assert
        Assert.Equal("ValidModule", moduleInfo.ModuleName);
        Assert.Equal(typeof(ValidModule), moduleInfo.ModuleType);
    }

    [Fact]
    public void Constructor_WithNullModuleName_ShouldThrowException()
    {
        // Act & Assert (ThrowIfNullOrWhiteSpace throws ArgumentNullException for null)
        Assert.Throws<ArgumentNullException>(() => new ModuleInfo(null!, typeof(ValidModule)));
    }

    [Fact]
    public void Constructor_WithEmptyModuleName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ModuleInfo("", typeof(ValidModule)));
    }

    [Fact]
    public void Constructor_WithNullType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModuleInfo("Test", null!));
    }

    [Fact]
    public void Constructor_WithNonModuleType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ModuleInfo("Test", typeof(NonModuleClass)));
    }

    [Fact]
    public void Create_Generic_ShouldCreateModuleInfo()
    {
        // Act
        var moduleInfo = ModuleInfo.Create<ValidModule>();

        // Assert
        Assert.Equal("ValidModule", moduleInfo.ModuleName);
        Assert.Equal(typeof(ValidModule), moduleInfo.ModuleType);
    }

    [Fact]
    public void Create_GenericWithName_ShouldCreateModuleInfoWithCustomName()
    {
        // Act
        var moduleInfo = ModuleInfo.Create<ValidModule>("CustomName");

        // Assert
        Assert.Equal("CustomName", moduleInfo.ModuleName);
        Assert.Equal(typeof(ValidModule), moduleInfo.ModuleType);
    }

    [Fact]
    public void InitializationMode_CanBeSet()
    {
        // Arrange
        var moduleInfo = ModuleInfo.Create<ValidModule>();

        // Act
        moduleInfo.InitializationMode = InitializationMode.OnDemand;

        // Assert
        Assert.Equal(InitializationMode.OnDemand, moduleInfo.InitializationMode);
    }

    [Fact]
    public void DependsOn_CanAddDependencies()
    {
        // Arrange
        var moduleInfo = ModuleInfo.Create<ValidModule>();

        // Act
        moduleInfo.DependsOn.Add("ModuleA");
        moduleInfo.DependsOn.Add("ModuleB");

        // Assert
        Assert.Equal(2, moduleInfo.DependsOn.Count);
        Assert.Contains("ModuleA", moduleInfo.DependsOn);
        Assert.Contains("ModuleB", moduleInfo.DependsOn);
    }
}
