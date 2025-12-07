using Serilog;

namespace Jinobald.Core.Ioc;

/// <summary>
///     스코프 접근자 구현
///     AsyncLocal을 사용하여 async/await 컨텍스트에서도 스코프를 유지합니다.
/// </summary>
public sealed class ScopeAccessor : IScopeAccessor, IScopeFactory
{
    private static readonly AsyncLocal<IScopedProvider?> _currentScope = new();
    private readonly IContainerExtension _container;
    private readonly ILogger _logger;

    /// <summary>
    ///     현재 스코프 (정적 접근용)
    /// </summary>
    public static IScopedProvider? Current => _currentScope.Value;

    /// <inheritdoc />
    public IScopedProvider? CurrentScope => _currentScope.Value;

    public ScopeAccessor(IContainerExtension container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _logger = Log.ForContext<ScopeAccessor>();
    }

    /// <inheritdoc />
    public T Resolve<T>() where T : notnull
    {
        if (_currentScope.Value != null)
            return _currentScope.Value.Resolve<T>();
        return _container.Resolve<T>();
    }

    /// <inheritdoc />
    public object Resolve(Type serviceType)
    {
        return _currentScope.Value?.Resolve(serviceType) ?? _container.Resolve(serviceType);
    }

    /// <inheritdoc />
    public IScopeContext CreateScope()
    {
        return CreateScope(null);
    }

    /// <inheritdoc />
    public IScopeContext CreateScope(string? scopeName)
    {
        var previousScope = _currentScope.Value;
        var newScope = _container.CreateScope();
        _currentScope.Value = newScope;

        _logger.Debug("Scope created: {ScopeName}", scopeName ?? "unnamed");

        return new ScopeContext(newScope, previousScope, scopeName, _logger);
    }

    /// <summary>
    ///     현재 스코프를 명시적으로 설정합니다.
    ///     일반적으로 CreateScope()를 사용하는 것이 권장됩니다.
    /// </summary>
    /// <param name="scope">설정할 스코프 (null이면 스코프 해제)</param>
    internal static void SetCurrentScope(IScopedProvider? scope)
    {
        _currentScope.Value = scope;
    }

    private sealed class ScopeContext : IScopeContext
    {
        private readonly IScopedProvider _scope;
        private readonly IScopedProvider? _previousScope;
        private readonly string? _scopeName;
        private readonly ILogger _logger;
        private bool _disposed;

        public IScopedProvider Scope => _scope;

        public ScopeContext(
            IScopedProvider scope,
            IScopedProvider? previousScope,
            string? scopeName,
            ILogger logger)
        {
            _scope = scope;
            _previousScope = previousScope;
            _scopeName = scopeName;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // 이전 스코프로 복원
            _currentScope.Value = _previousScope;

            // 스코프 정리
            _scope.Dispose();

            _logger.Debug("Scope disposed: {ScopeName}", _scopeName ?? "unnamed");
        }
    }
}
