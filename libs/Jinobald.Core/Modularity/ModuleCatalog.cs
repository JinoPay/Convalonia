namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 카탈로그 기본 구현
/// </summary>
public class ModuleCatalog : IModuleCatalog
{
    private readonly Dictionary<string, ModuleInfo> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _isInitialized;

    /// <inheritdoc />
    public IEnumerable<ModuleInfo> Modules
    {
        get
        {
            lock (_lock)
            {
                return _modules.Values.ToList();
            }
        }
    }

    /// <inheritdoc />
    public IModuleCatalog AddModule(ModuleInfo moduleInfo)
    {
        ArgumentNullException.ThrowIfNull(moduleInfo);

        lock (_lock)
        {
            if (_isInitialized)
                throw new InvalidOperationException("Cannot add modules after the catalog has been initialized.");

            if (_modules.ContainsKey(moduleInfo.ModuleName))
                throw new InvalidOperationException($"A module with the name '{moduleInfo.ModuleName}' is already registered.");

            _modules[moduleInfo.ModuleName] = moduleInfo;
        }

        return this;
    }

    /// <inheritdoc />
    public IModuleCatalog AddModule<TModule>() where TModule : IModule
    {
        return AddModule(ModuleInfo.Create<TModule>());
    }

    /// <inheritdoc />
    public IModuleCatalog AddModule<TModule>(
        InitializationMode initializationMode,
        params string[] dependsOn) where TModule : IModule
    {
        var moduleInfo = ModuleInfo.Create<TModule>();
        moduleInfo.InitializationMode = initializationMode;

        foreach (var dependency in dependsOn)
        {
            moduleInfo.DependsOn.Add(dependency);
        }

        return AddModule(moduleInfo);
    }

    /// <inheritdoc />
    public ModuleInfo? GetModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        lock (_lock)
        {
            return _modules.GetValueOrDefault(moduleName);
        }
    }

    /// <inheritdoc />
    public bool ContainsModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        lock (_lock)
        {
            return _modules.ContainsKey(moduleName);
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;

            ValidateDependencies();
            _isInitialized = true;
        }
    }

    /// <inheritdoc />
    public IEnumerable<ModuleInfo> GetDependentModules(ModuleInfo moduleInfo)
    {
        ArgumentNullException.ThrowIfNull(moduleInfo);

        var sortedModules = new List<ModuleInfo>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ResolveDependencies(moduleInfo, sortedModules, visited, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        return sortedModules;
    }

    /// <inheritdoc />
    public IEnumerable<ModuleInfo> GetModulesForInitialization()
    {
        var modulesToInitialize = Modules
            .Where(m => m.InitializationMode == InitializationMode.WhenAvailable)
            .ToList();

        var sortedModules = new List<ModuleInfo>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in modulesToInitialize)
        {
            if (!visited.Contains(module.ModuleName))
            {
                ResolveDependencies(module, sortedModules, visited, visiting);
            }
        }

        return sortedModules;
    }

    private void ResolveDependencies(
        ModuleInfo moduleInfo,
        List<ModuleInfo> sortedModules,
        HashSet<string> visited,
        HashSet<string> visiting)
    {
        if (visited.Contains(moduleInfo.ModuleName))
            return;

        if (visiting.Contains(moduleInfo.ModuleName))
            throw new InvalidOperationException($"Circular dependency detected involving module '{moduleInfo.ModuleName}'.");

        visiting.Add(moduleInfo.ModuleName);

        foreach (var dependencyName in moduleInfo.DependsOn)
        {
            var dependencyModule = GetModule(dependencyName);
            if (dependencyModule == null)
                throw new InvalidOperationException($"Module '{moduleInfo.ModuleName}' depends on unknown module '{dependencyName}'.");

            ResolveDependencies(dependencyModule, sortedModules, visited, visiting);
        }

        visiting.Remove(moduleInfo.ModuleName);
        visited.Add(moduleInfo.ModuleName);
        sortedModules.Add(moduleInfo);
    }

    private void ValidateDependencies()
    {
        foreach (var module in _modules.Values)
        {
            foreach (var dependency in module.DependsOn)
            {
                if (!_modules.ContainsKey(dependency))
                    throw new InvalidOperationException($"Module '{module.ModuleName}' depends on unknown module '{dependency}'.");
            }
        }

        // Check for circular dependencies
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in _modules.Values)
        {
            CheckCircularDependency(module, visited, visiting);
        }
    }

    private void CheckCircularDependency(
        ModuleInfo moduleInfo,
        HashSet<string> visited,
        HashSet<string> visiting)
    {
        if (visited.Contains(moduleInfo.ModuleName))
            return;

        if (visiting.Contains(moduleInfo.ModuleName))
            throw new InvalidOperationException($"Circular dependency detected involving module '{moduleInfo.ModuleName}'.");

        visiting.Add(moduleInfo.ModuleName);

        foreach (var dependency in moduleInfo.DependsOn)
        {
            if (_modules.TryGetValue(dependency, out var dependencyModule))
            {
                CheckCircularDependency(dependencyModule, visited, visiting);
            }
        }

        visiting.Remove(moduleInfo.ModuleName);
        visited.Add(moduleInfo.ModuleName);
    }
}
