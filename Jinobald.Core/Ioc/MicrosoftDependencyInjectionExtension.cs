using Microsoft.Extensions.DependencyInjection;

namespace Jinobald.Core.Ioc;

/// <summary>
///     Microsoft.Extensions.DependencyInjection 기반 컨테이너 확장
/// </summary>
public sealed class MicrosoftDependencyInjectionExtension : IContainerExtension
{
    private readonly IServiceCollection _services;
    private IServiceProvider? _serviceProvider;

    public MicrosoftDependencyInjectionExtension(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object Instance => (object?)_serviceProvider ?? _services;

    #region IContainerProvider Implementation

    public object Resolve(Type serviceType)
    {
        EnsureFinalized();
        return _serviceProvider!.GetRequiredService(serviceType);
    }

    public T Resolve<T>() where T : notnull
    {
        EnsureFinalized();
        return _serviceProvider!.GetRequiredService<T>();
    }

    public object Resolve(Type serviceType, string name)
    {
        // Microsoft.Extensions.DependencyInjection은 named resolution을 기본 지원하지 않음
        // 필요시 Keyed Services (.NET 8+) 사용 가능
        throw new NotSupportedException(
            "Named service resolution is not supported. Consider using Keyed Services in .NET 8+");
    }

    public T Resolve<T>(string name) where T : notnull
    {
        throw new NotSupportedException(
            "Named service resolution is not supported. Consider using Keyed Services in .NET 8+");
    }

    public object Resolve(Type serviceType, params (Type Type, object Instance)[] parameters)
    {
        EnsureFinalized();

        // 파라미터가 있는 경우 ActivatorUtilities 사용
        if (parameters.Length > 0)
        {
            var args = parameters.Select(p => p.Instance).ToArray();
            return ActivatorUtilities.CreateInstance(_serviceProvider!, serviceType, args);
        }

        return _serviceProvider!.GetRequiredService(serviceType);
    }

    public T Resolve<T>(params (Type Type, object Instance)[] parameters) where T : notnull
    {
        return (T)Resolve(typeof(T), parameters);
    }

    #endregion

    #region IContainerRegistry Implementation

    public IContainerRegistry RegisterSingleton(Type from, Type to)
    {
        _services.AddSingleton(from, to);
        return this;
    }

    public IContainerRegistry RegisterSingleton<TFrom, TTo>() where TFrom : class where TTo : class, TFrom
    {
        _services.AddSingleton<TFrom, TTo>();
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type type)
    {
        _services.AddSingleton(type);
        return this;
    }

    public IContainerRegistry RegisterSingleton<T>() where T : class
    {
        _services.AddSingleton<T>();
        return this;
    }

    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        _services.AddSingleton(type, instance);
        return this;
    }

    public IContainerRegistry RegisterInstance<T>(T instance) where T : class
    {
        _services.AddSingleton(instance!);
        return this;
    }

    public IContainerRegistry Register(Type from, Type to)
    {
        _services.AddTransient(from, to);
        return this;
    }

    public IContainerRegistry Register<TFrom, TTo>() where TFrom : class where TTo : class, TFrom
    {
        _services.AddTransient<TFrom, TTo>();
        return this;
    }

    public IContainerRegistry Register(Type type)
    {
        _services.AddTransient(type);
        return this;
    }

    public IContainerRegistry Register<T>() where T : class
    {
        _services.AddTransient<T>();
        return this;
    }

    public IContainerRegistry RegisterScoped(Type from, Type to)
    {
        _services.AddScoped(from, to);
        return this;
    }

    public IContainerRegistry RegisterScoped<TFrom, TTo>() where TFrom : class where TTo : class, TFrom
    {
        _services.AddScoped<TFrom, TTo>();
        return this;
    }

    public IContainerRegistry RegisterScoped(Type type)
    {
        _services.AddScoped(type);
        return this;
    }

    public IContainerRegistry RegisterScoped<T>() where T : class
    {
        _services.AddScoped<T>();
        return this;
    }

    #endregion

    #region IContainerExtension Implementation

    public void FinalizeExtension()
    {
        if (_serviceProvider != null)
            throw new InvalidOperationException("컨테이너가 이미 빌드되었습니다.");

        _serviceProvider = _services.BuildServiceProvider();
    }

    public IScopedProvider CreateScope()
    {
        EnsureFinalized();
        var scope = _serviceProvider!.CreateScope();
        return new MicrosoftScopedProvider(scope);
    }

    #endregion

    private void EnsureFinalized()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException(
                "컨테이너가 아직 빌드되지 않았습니다. FinalizeExtension()을 먼저 호출하세요.");
    }

    /// <summary>
    ///     Microsoft.Extensions.DependencyInjection 기반 스코프 프로바이더
    /// </summary>
    private sealed class MicrosoftScopedProvider : IScopedProvider
    {
        private readonly IServiceScope _scope;

        public MicrosoftScopedProvider(IServiceScope scope)
        {
            _scope = scope;
        }

        public object Resolve(Type serviceType)
        {
            return _scope.ServiceProvider.GetRequiredService(serviceType);
        }

        public T Resolve<T>() where T : notnull
        {
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        public object Resolve(Type serviceType, string name)
        {
            throw new NotSupportedException(
                "Named service resolution is not supported. Consider using Keyed Services in .NET 8+");
        }

        public T Resolve<T>(string name) where T : notnull
        {
            throw new NotSupportedException(
                "Named service resolution is not supported. Consider using Keyed Services in .NET 8+");
        }

        public object Resolve(Type serviceType, params (Type Type, object Instance)[] parameters)
        {
            if (parameters.Length > 0)
            {
                var args = parameters.Select(p => p.Instance).ToArray();
                return ActivatorUtilities.CreateInstance(_scope.ServiceProvider, serviceType, args);
            }

            return _scope.ServiceProvider.GetRequiredService(serviceType);
        }

        public T Resolve<T>(params (Type Type, object Instance)[] parameters) where T : notnull
        {
            return (T)Resolve(typeof(T), parameters);
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
