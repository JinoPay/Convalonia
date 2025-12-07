using Jinobald.Core.Ioc;
using Serilog;

namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 매니저 기본 구현
/// </summary>
public class ModuleManager : IModuleManager
{
    private readonly IModuleCatalog _moduleCatalog;
    private readonly IContainerProvider _containerProvider;
    private readonly IContainerRegistry _containerRegistry;
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private bool _isRunning;

    /// <inheritdoc />
    public event EventHandler<ModuleInitializedEventArgs>? ModuleInitialized;

    /// <inheritdoc />
    public event EventHandler<ModuleInitializationFailedEventArgs>? ModuleInitializationFailed;

    public ModuleManager(
        IModuleCatalog moduleCatalog,
        IContainerProvider containerProvider,
        IContainerRegistry containerRegistry)
    {
        _moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
        _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
        _containerRegistry = containerRegistry ?? throw new ArgumentNullException(nameof(containerRegistry));
        _logger = Log.ForContext<ModuleManager>();
    }

    /// <inheritdoc />
    public void Run()
    {
        lock (_lock)
        {
            if (_isRunning)
                return;

            _isRunning = true;
        }

        _logger.Information("Starting module initialization...");

        _moduleCatalog.Initialize();

        var modulesToInitialize = _moduleCatalog.GetModulesForInitialization();

        foreach (var moduleInfo in modulesToInitialize)
        {
            if (moduleInfo.State == ModuleState.NotLoaded)
            {
                InitializeModule(moduleInfo);
            }
        }

        _logger.Information("Module initialization completed.");
    }

    /// <inheritdoc />
    public void LoadModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        var moduleInfo = _moduleCatalog.GetModule(moduleName)
            ?? throw new InvalidOperationException($"Module '{moduleName}' not found in catalog.");

        if (moduleInfo.State == ModuleState.Initialized)
        {
            _logger.Debug("Module '{ModuleName}' is already initialized.", moduleName);
            return;
        }

        // Initialize dependencies first
        var dependentModules = _moduleCatalog.GetDependentModules(moduleInfo);
        foreach (var dependentModule in dependentModules)
        {
            if (dependentModule.State == ModuleState.NotLoaded)
            {
                InitializeModule(dependentModule);
            }
        }
    }

    /// <inheritdoc />
    public void LoadModule<TModule>() where TModule : IModule
    {
        LoadModule(typeof(TModule).Name);
    }

    /// <inheritdoc />
    public bool IsModuleInitialized(string moduleName)
    {
        var moduleInfo = _moduleCatalog.GetModule(moduleName);
        return moduleInfo?.State == ModuleState.Initialized;
    }

    private void InitializeModule(ModuleInfo moduleInfo)
    {
        if (moduleInfo.State != ModuleState.NotLoaded)
            return;

        _logger.Debug("Initializing module '{ModuleName}'...", moduleInfo.ModuleName);

        moduleInfo.State = ModuleState.Initializing;

        try
        {
            // Create module instance
            var module = CreateModule(moduleInfo);

            // Register types
            module.RegisterTypes(_containerRegistry);

            // Initialize
            module.OnInitialized(_containerProvider);

            moduleInfo.State = ModuleState.Initialized;
            _logger.Information("Module '{ModuleName}' initialized successfully.", moduleInfo.ModuleName);

            ModuleInitialized?.Invoke(this, new ModuleInitializedEventArgs(moduleInfo));
        }
        catch (Exception ex)
        {
            moduleInfo.State = ModuleState.Failed;
            moduleInfo.InitializationException = ex;
            _logger.Error(ex, "Failed to initialize module '{ModuleName}'.", moduleInfo.ModuleName);

            ModuleInitializationFailed?.Invoke(this, new ModuleInitializationFailedEventArgs(moduleInfo, ex));

            throw new ModuleInitializationException(moduleInfo.ModuleName, ex);
        }
    }

    private IModule CreateModule(ModuleInfo moduleInfo)
    {
        try
        {
            // Try to resolve from container first
            var module = _containerProvider.Resolve(moduleInfo.ModuleType) as IModule;
            if (module != null)
                return module;
        }
        catch
        {
            // Fall through to Activator
        }

        // Fall back to Activator.CreateInstance
        var instance = Activator.CreateInstance(moduleInfo.ModuleType)
            ?? throw new InvalidOperationException($"Failed to create instance of module '{moduleInfo.ModuleName}'.");

        return (IModule)instance;
    }
}
