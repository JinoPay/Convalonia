namespace Jinobald.Core.Mvvm;

/// <summary>
///     네비게이션 이벤트를 처리하는 ViewModel 인터페이스
/// </summary>
public interface INavigationAware
{
    /// <summary>
    ///     이 화면에서 완전히 나간 후 호출
    /// </summary>
    Task OnNavigatedFromAsync(NavigationContext context);

    /// <summary>
    ///     이 화면으로 네비게이션 완료 후 호출
    /// </summary>
    Task OnNavigatedToAsync(NavigationContext context);

    /// <summary>
    ///     이 화면에서 다른 화면으로 나가기 전 호출
    ///     네비게이션을 취소하려면 false 반환
    /// </summary>
    Task<bool> OnNavigatingFromAsync(NavigationContext context);

    /// <summary>
    ///     이 화면으로 네비게이션 되기 전 호출
    ///     네비게이션을 취소하려면 false 반환
    /// </summary>
    Task<bool> OnNavigatingToAsync(NavigationContext context);
}

/// <summary>
///     네비게이션 컨텍스트 - 파라미터 및 네비게이션 정보 포함
///     Prism의 NavigationContext를 참고하여 View 기반으로 설계됨
/// </summary>
public class NavigationContext
{
    /// <summary>
    ///     추가 데이터를 저장할 수 있는 딕셔너리
    /// </summary>
    public IDictionary<string, object?> Data { get; } = new Dictionary<string, object?>();

    /// <summary>
    ///     네비게이션 방향 (Forward, Back, Replace)
    /// </summary>
    public NavigationDirection Direction { get; init; }

    /// <summary>
    ///     네비게이션 파라미터
    /// </summary>
    public object? Parameter { get; init; }

    /// <summary>
    ///     이전 View 타입
    /// </summary>
    public Type? SourceViewType { get; init; }

    /// <summary>
    ///     대상 View 타입
    /// </summary>
    public Type? TargetViewType { get; init; }

    /// <summary>
    ///     이전 ViewModel 타입
    /// </summary>
    public Type? SourceViewModelType { get; init; }

    /// <summary>
    ///     대상 ViewModel 타입
    /// </summary>
    public Type? TargetViewModelType { get; init; }

    /// <summary>
    ///     취소 토큰
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    ///     타입 안전한 파라미터 가져오기
    /// </summary>
    public T? GetParameter<T>()
    {
        return Parameter is T value ? value : default;
    }

    #region 레거시 호환성

    /// <summary>
    ///     [레거시] 이전 ViewModel 타입 (호환성)
    /// </summary>
    [Obsolete("Use SourceViewModelType instead")]
    public Type? SourceType => SourceViewModelType;

    /// <summary>
    ///     [레거시] 대상 ViewModel 타입 (호환성)
    /// </summary>
    [Obsolete("Use TargetViewModelType instead")]
    public Type? TargetType => TargetViewModelType;

    #endregion
}

/// <summary>
///     네비게이션 방향
/// </summary>
public enum NavigationDirection
{
    Forward,
    Back,
    Replace
}
