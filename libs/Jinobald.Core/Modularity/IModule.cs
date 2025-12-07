using Jinobald.Core.Ioc;

namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 인터페이스
///     각 모듈은 이 인터페이스를 구현하여 자신의 서비스와 뷰를 등록합니다.
/// </summary>
public interface IModule
{
    /// <summary>
    ///     모듈에서 사용할 서비스와 타입을 DI 컨테이너에 등록합니다.
    ///     이 메서드는 OnInitialized 전에 호출됩니다.
    /// </summary>
    /// <param name="containerRegistry">DI 컨테이너 레지스트리</param>
    void RegisterTypes(IContainerRegistry containerRegistry);

    /// <summary>
    ///     모듈 초기화가 완료된 후 호출됩니다.
    ///     뷰를 리전에 등록하거나 초기 설정을 수행합니다.
    /// </summary>
    /// <param name="containerProvider">DI 컨테이너 프로바이더</param>
    void OnInitialized(IContainerProvider containerProvider);
}
