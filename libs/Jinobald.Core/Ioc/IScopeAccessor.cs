namespace Jinobald.Core.Ioc;

/// <summary>
///     현재 스코프에 접근할 수 있는 인터페이스
///     Region/Dialog/Navigation 별 스코프 관리에 사용됩니다.
/// </summary>
public interface IScopeAccessor
{
    /// <summary>
    ///     현재 스코프 (없으면 null)
    /// </summary>
    IScopedProvider? CurrentScope { get; }

    /// <summary>
    ///     스코프 내에서 서비스를 해결합니다.
    ///     현재 스코프가 있으면 스코프에서, 없으면 루트 컨테이너에서 해결합니다.
    /// </summary>
    T Resolve<T>() where T : notnull;

    /// <summary>
    ///     스코프 내에서 서비스를 해결합니다.
    /// </summary>
    object Resolve(Type serviceType);
}

/// <summary>
///     스코프 컨텍스트
///     using 블록 내에서 스코프를 관리합니다.
/// </summary>
public interface IScopeContext : IDisposable
{
    /// <summary>
    ///     이 컨텍스트의 스코프 프로바이더
    /// </summary>
    IScopedProvider Scope { get; }
}

/// <summary>
///     스코프를 생성하고 관리하는 팩토리 인터페이스
/// </summary>
public interface IScopeFactory
{
    /// <summary>
    ///     새 스코프를 생성하고 현재 스코프로 설정합니다.
    /// </summary>
    /// <returns>스코프 컨텍스트 (Dispose 시 이전 스코프로 복원)</returns>
    IScopeContext CreateScope();

    /// <summary>
    ///     이름이 지정된 스코프를 생성합니다.
    /// </summary>
    /// <param name="scopeName">스코프 이름 (디버깅/로깅용)</param>
    /// <returns>스코프 컨텍스트</returns>
    IScopeContext CreateScope(string scopeName);
}
