namespace Jinobald.Core.Services.Regions;

/// <summary>
///     리전 내에서 네비게이션을 담당하는 서비스 인터페이스
///     Prism의 IRegionNavigationService를 참고하여 View 기반 네비게이션으로 설계됨
/// </summary>
public interface IRegionNavigationService
{
    /// <summary>
    ///     이 네비게이션 서비스가 속한 리전
    /// </summary>
    IRegion Region { get; }

    #region 현재 상태

    /// <summary>
    ///     현재 표시 중인 View
    /// </summary>
    object? CurrentView { get; }

    /// <summary>
    ///     현재 ViewModel
    /// </summary>
    object? CurrentViewModel { get; }

    /// <summary>
    ///     네비게이션 진행 중 여부
    /// </summary>
    bool IsNavigating { get; }

    #endregion

    #region View 기반 네비게이션

    /// <summary>
    ///     지정된 View 타입으로 네비게이션
    /// </summary>
    /// <param name="viewType">View 타입</param>
    /// <param name="parameter">네비게이션 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> NavigateAsync(Type viewType, object? parameter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     지정된 View 타입으로 네비게이션 (제네릭)
    /// </summary>
    /// <typeparam name="TView">View 타입</typeparam>
    /// <param name="parameter">네비게이션 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> NavigateAsync<TView>(object? parameter = null,
        CancellationToken cancellationToken = default) where TView : class;

    #endregion

    #region Back/Forward 네비게이션

    /// <summary>
    ///     뒤로 갈 수 있는지 여부
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    ///     앞으로 갈 수 있는지 여부
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    ///     이전 뷰로 이동
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> GoBackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     다음 뷰로 이동
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> GoForwardAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 히스토리 및 캐시 관리

    /// <summary>
    ///     네비게이션 히스토리 개수
    /// </summary>
    int HistoryCount { get; }

    /// <summary>
    ///     네비게이션 히스토리 초기화
    /// </summary>
    void ClearHistory();

    /// <summary>
    ///     현재 뷰를 리전에서 제거
    /// </summary>
    void ClearCurrentView();

    /// <summary>
    ///     Keep-Alive 캐시 제거
    /// </summary>
    void ClearCache();

    #endregion

    #region 설정

    /// <summary>
    ///     Keep-Alive 설정 (뷰 재사용 여부)
    ///     true: 네비게이션 시 뷰를 캐시에 보관하고 재사용
    ///     false: 네비게이션 시 뷰를 제거하고 새로 생성
    /// </summary>
    bool KeepAlive { get; set; }

    /// <summary>
    ///     네비게이션 모드
    /// </summary>
    RegionNavigationMode NavigationMode { get; set; }

    #endregion

    #region 이벤트

    /// <summary>
    ///     네비게이션 완료 시 발생하는 이벤트
    /// </summary>
    event EventHandler<object?>? Navigated;

    #endregion
}
