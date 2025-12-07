using Jinobald.Core.Ioc;
using Jinobald.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jinobald.Core.Tests.Modularity;

public class ModuleManagerTests
{
    #region Test Modules

    public class TestModule : IModule
    {
        public static bool RegisterTypesCalled { get; set; }
        public static bool OnInitializedCalled { get; set; }

        public static void Reset()
        {
            RegisterTypesCalled = false;
            OnInitializedCalled = false;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterTypesCalled = true;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            OnInitializedCalled = true;
        }
    }

    public class DependentModule : IModule
    {
        public static bool RegisterTypesCalled { get; set; }
        public static bool OnInitializedCalled { get; set; }

        public static void Reset()
        {
            RegisterTypesCalled = false;
            OnInitializedCalled = false;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterTypesCalled = true;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            OnInitializedCalled = true;
        }
    }

    public class FailingModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            throw new InvalidOperationException("Intentional failure");
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }

    #endregion

    private static (IModuleManager, IModuleCatalog) CreateModuleManager()
    {
        var services = new ServiceCollection();
        var container = services.AsContainerExtension();
        container.FinalizeExtension();

        var catalog = new ModuleCatalog();
        var manager = new ModuleManager(catalog, container, container);

        return (manager, catalog);
    }

    [Fact]
    public void Run_ShouldInitializeWhenAvailableModules()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>();

        // Act
        manager.Run();

        // Assert
        Assert.True(TestModule.RegisterTypesCalled);
        Assert.True(TestModule.OnInitializedCalled);
        Assert.True(manager.IsModuleInitialized("TestModule"));
    }

    [Fact]
    public void Run_ShouldNotInitializeOnDemandModules()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>(InitializationMode.OnDemand);

        // Act
        manager.Run();

        // Assert
        Assert.False(TestModule.RegisterTypesCalled);
        Assert.False(TestModule.OnInitializedCalled);
        Assert.False(manager.IsModuleInitialized("TestModule"));
    }

    [Fact]
    public void LoadModule_ShouldInitializeOnDemandModule()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>(InitializationMode.OnDemand);
        manager.Run(); // Initialize catalog but not OnDemand modules

        // Act
        manager.LoadModule("TestModule");

        // Assert
        Assert.True(TestModule.RegisterTypesCalled);
        Assert.True(TestModule.OnInitializedCalled);
        Assert.True(manager.IsModuleInitialized("TestModule"));
    }

    [Fact]
    public void LoadModule_Generic_ShouldInitializeModule()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>(InitializationMode.OnDemand);
        manager.Run();

        // Act
        manager.LoadModule<TestModule>();

        // Assert
        Assert.True(manager.IsModuleInitialized("TestModule"));
    }

    [Fact]
    public void LoadModule_WithDependencies_ShouldInitializeDependenciesFirst()
    {
        // Arrange
        TestModule.Reset();
        DependentModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>(InitializationMode.OnDemand);
        catalog.AddModule<DependentModule>(InitializationMode.OnDemand, "TestModule");
        manager.Run();

        // Act
        manager.LoadModule("DependentModule");

        // Assert
        Assert.True(TestModule.RegisterTypesCalled);
        Assert.True(DependentModule.RegisterTypesCalled);
        Assert.True(manager.IsModuleInitialized("TestModule"));
        Assert.True(manager.IsModuleInitialized("DependentModule"));
    }

    [Fact]
    public void LoadModule_AlreadyInitialized_ShouldNotReinitialize()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>();
        manager.Run();
        TestModule.Reset(); // Reset after first initialization

        // Act
        manager.LoadModule("TestModule");

        // Assert
        Assert.False(TestModule.RegisterTypesCalled); // Should not be called again
    }

    [Fact]
    public void LoadModule_NonExistent_ShouldThrowException()
    {
        // Arrange
        var (manager, catalog) = CreateModuleManager();
        manager.Run();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.LoadModule("NonExistent"));
    }

    [Fact]
    public void Run_WithFailingModule_ShouldThrowModuleInitializationException()
    {
        // Arrange
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<FailingModule>();

        // Act & Assert
        var ex = Assert.Throws<ModuleInitializationException>(() => manager.Run());
        Assert.Equal("FailingModule", ex.ModuleName);
    }

    [Fact]
    public void ModuleInitialized_Event_ShouldBeRaised()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>();

        ModuleInfo? initializedModule = null;
        manager.ModuleInitialized += (_, args) => initializedModule = args.ModuleInfo;

        // Act
        manager.Run();

        // Assert
        Assert.NotNull(initializedModule);
        Assert.Equal("TestModule", initializedModule.ModuleName);
    }

    [Fact]
    public void ModuleInitializationFailed_Event_ShouldBeRaised()
    {
        // Arrange
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<FailingModule>();

        ModuleInfo? failedModule = null;
        Exception? exception = null;
        manager.ModuleInitializationFailed += (_, args) =>
        {
            failedModule = args.ModuleInfo;
            exception = args.Exception;
        };

        // Act
        try { manager.Run(); } catch { }

        // Assert
        Assert.NotNull(failedModule);
        Assert.Equal("FailingModule", failedModule.ModuleName);
        Assert.NotNull(exception);
    }

    [Fact]
    public void Run_MultipleTimes_ShouldOnlyInitializeOnce()
    {
        // Arrange
        TestModule.Reset();
        var (manager, catalog) = CreateModuleManager();
        catalog.AddModule<TestModule>();

        // Act
        manager.Run();
        TestModule.Reset();
        manager.Run(); // Second run

        // Assert
        Assert.False(TestModule.RegisterTypesCalled); // Should not be called again
    }

    [Fact]
    public void IsModuleInitialized_NonExistent_ShouldReturnFalse()
    {
        // Arrange
        var (manager, catalog) = CreateModuleManager();
        manager.Run();

        // Act & Assert
        Assert.False(manager.IsModuleInitialized("NonExistent"));
    }
}
