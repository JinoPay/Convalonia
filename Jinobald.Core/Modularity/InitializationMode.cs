namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 초기화 모드
/// </summary>
public enum InitializationMode
{
    /// <summary>
    ///     애플리케이션 시작 시 즉시 초기화
    /// </summary>
    WhenAvailable,

    /// <summary>
    ///     명시적으로 요청될 때 초기화 (지연 로딩)
    /// </summary>
    OnDemand
}
