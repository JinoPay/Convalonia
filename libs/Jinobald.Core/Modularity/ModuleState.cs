namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈의 상태를 나타냅니다.
/// </summary>
public enum ModuleState
{
    /// <summary>
    ///     모듈이 아직 초기화되지 않음
    /// </summary>
    NotLoaded,

    /// <summary>
    ///     모듈 초기화 중
    /// </summary>
    Initializing,

    /// <summary>
    ///     모듈 초기화 완료
    /// </summary>
    Initialized,

    /// <summary>
    ///     모듈 초기화 실패
    /// </summary>
    Failed
}
