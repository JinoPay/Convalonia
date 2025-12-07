namespace Jinobald.Core.Ioc;

/// <summary>
///     DI 컨테이너를 추상화하는 인터페이스
///     서비스 등록과 해결을 모두 지원합니다.
/// </summary>
public interface IContainerExtension : IContainerProvider, IContainerRegistry
{
    /// <summary>
    ///     기본 DI 컨테이너 인스턴스
    /// </summary>
    object Instance { get; }

    /// <summary>
    ///     컨테이너를 빌드하여 ServiceProvider를 생성합니다.
    /// </summary>
    void FinalizeExtension();

    /// <summary>
    ///     자식 스코프를 생성합니다.
    /// </summary>
    /// <returns>스코프를 나타내는 IScopedProvider</returns>
    IScopedProvider CreateScope();
}

/// <summary>
///     스코프를 나타내는 인터페이스
/// </summary>
public interface IScopedProvider : IContainerProvider, IDisposable
{
}
