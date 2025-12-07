using Jinobald.Core.Ioc;
using Jinobald.Core.Modularity;
using Xunit;

namespace Jinobald.Core.Tests.Modularity;

public class ModuleCatalogTests
{
    #region Test Modules

    public class ModuleA : IModule
    {
        public bool RegisterTypesCalled { get; private set; }
        public bool OnInitializedCalled { get; private set; }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterTypesCalled = true;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            OnInitializedCalled = true;
        }
    }

    public class ModuleB : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry) { }
        public void OnInitialized(IContainerProvider containerProvider) { }
    }

    public class ModuleC : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry) { }
        public void OnInitialized(IContainerProvider containerProvider) { }
    }

    #endregion

    [Fact]
    public void AddModule_WithModuleInfo_ShouldAddToModules()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        var moduleInfo = ModuleInfo.Create<ModuleA>();

        // Act
        catalog.AddModule(moduleInfo);

        // Assert
        Assert.Single(catalog.Modules);
        Assert.Equal("ModuleA", catalog.Modules.First().ModuleName);
    }

    [Fact]
    public void AddModule_Generic_ShouldAddToModules()
    {
        // Arrange
        var catalog = new ModuleCatalog();

        // Act
        catalog.AddModule<ModuleA>();

        // Assert
        Assert.Single(catalog.Modules);
        Assert.Equal("ModuleA", catalog.Modules.First().ModuleName);
    }

    [Fact]
    public void AddModule_Duplicate_ShouldThrowException()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleA>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalog.AddModule<ModuleA>());
    }

    [Fact]
    public void AddModule_AfterInitialize_ShouldThrowException()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.Initialize();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalog.AddModule<ModuleA>());
    }

    [Fact]
    public void ContainsModule_ExistingModule_ShouldReturnTrue()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleA>();

        // Act & Assert
        Assert.True(catalog.ContainsModule("ModuleA"));
    }

    [Fact]
    public void ContainsModule_NonExistingModule_ShouldReturnFalse()
    {
        // Arrange
        var catalog = new ModuleCatalog();

        // Act & Assert
        Assert.False(catalog.ContainsModule("NonExistent"));
    }

    [Fact]
    public void GetModule_ExistingModule_ShouldReturnModuleInfo()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleA>();

        // Act
        var moduleInfo = catalog.GetModule("ModuleA");

        // Assert
        Assert.NotNull(moduleInfo);
        Assert.Equal("ModuleA", moduleInfo.ModuleName);
        Assert.Equal(typeof(ModuleA), moduleInfo.ModuleType);
    }

    [Fact]
    public void GetModule_NonExistingModule_ShouldReturnNull()
    {
        // Arrange
        var catalog = new ModuleCatalog();

        // Act
        var moduleInfo = catalog.GetModule("NonExistent");

        // Assert
        Assert.Null(moduleInfo);
    }

    [Fact]
    public void AddModule_WithDependencies_ShouldSetDependsOn()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleA>();

        // Act
        catalog.AddModule<ModuleB>(InitializationMode.WhenAvailable, "ModuleA");

        // Assert
        var moduleB = catalog.GetModule("ModuleB");
        Assert.NotNull(moduleB);
        Assert.Contains("ModuleA", moduleB.DependsOn);
    }

    [Fact]
    public void Initialize_WithMissingDependency_ShouldThrowException()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleB>(InitializationMode.WhenAvailable, "NonExistent");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalog.Initialize());
    }

    [Fact]
    public void Initialize_WithCircularDependency_ShouldThrowException()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        var moduleA = ModuleInfo.Create<ModuleA>();
        moduleA.DependsOn.Add("ModuleB");

        var moduleB = new ModuleInfo("ModuleB", typeof(ModuleB));
        moduleB.DependsOn.Add("ModuleA");

        catalog.AddModule(moduleA);
        catalog.AddModule(moduleB);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalog.Initialize());
    }

    [Fact]
    public void GetModulesForInitialization_ShouldReturnDependencyOrder()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleC>(InitializationMode.WhenAvailable, "ModuleB");
        catalog.AddModule<ModuleB>(InitializationMode.WhenAvailable, "ModuleA");
        catalog.AddModule<ModuleA>();
        catalog.Initialize();

        // Act
        var modules = catalog.GetModulesForInitialization().ToList();

        // Assert
        Assert.Equal(3, modules.Count);
        // ModuleA should be first (no dependencies)
        // ModuleB should be second (depends on A)
        // ModuleC should be last (depends on B)
        Assert.Equal("ModuleA", modules[0].ModuleName);
        Assert.Equal("ModuleB", modules[1].ModuleName);
        Assert.Equal("ModuleC", modules[2].ModuleName);
    }

    [Fact]
    public void GetModulesForInitialization_ShouldExcludeOnDemandModules()
    {
        // Arrange
        var catalog = new ModuleCatalog();
        catalog.AddModule<ModuleA>();
        catalog.AddModule<ModuleB>(InitializationMode.OnDemand);
        catalog.Initialize();

        // Act
        var modules = catalog.GetModulesForInitialization().ToList();

        // Assert
        Assert.Single(modules);
        Assert.Equal("ModuleA", modules[0].ModuleName);
    }

    [Fact]
    public void MethodChaining_ShouldWork()
    {
        // Arrange & Act
        var catalog = new ModuleCatalog()
            .AddModule<ModuleA>()
            .AddModule<ModuleB>()
            .AddModule<ModuleC>();

        // Assert
        Assert.Equal(3, catalog.Modules.Count());
    }
}
