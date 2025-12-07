namespace Jinobald.Core.Mvvm;

/// <summary>
///     명시적 리소스 정리가 필요한 ViewModel 인터페이스
///     ViewModel이 더 이상 사용되지 않을 때 호출됨
/// </summary>
public interface IDestructible
{
    /// <summary>
    ///     리소스 정리 수행
    ///     이벤트 구독 해제, 타이머 정리, 캐시 클리어 등
    /// </summary>
    void Destroy();
}
