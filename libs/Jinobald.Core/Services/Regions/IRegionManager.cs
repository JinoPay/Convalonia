namespace Jinobald.Core.Services.Regions;

/// <summary>
///     애플리케이션 내 리전들을 관리하는 매니저 인터페이스
///     Prism의 IRegionManager를 참고하여 View 기반 네비게이션으로 설계됨
/// </summary>
public interface IRegionManager
{
    #region 리전 관리

    /// <summary>
    ///     등록된 모든 리전의 컬렉션
    /// </summary>
    IEnumerable<IRegion> Regions { get; }

    /// <summary>
    ///     지정된 이름의 리전을 생성하거나 가져옵니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <returns>생성되거나 존재하는 리전</returns>
    IRegion CreateOrGetRegion(string regionName);

    /// <summary>
    ///     지정된 이름의 리전을 가져옵니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <returns>리전 (없으면 null)</returns>
    IRegion? GetRegion(string regionName);

    /// <summary>
    ///     리전이 존재하는지 확인합니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <returns>리전 존재 여부</returns>
    bool ContainsRegion(string regionName);

    /// <summary>
    ///     리전을 등록합니다.
    /// </summary>
    /// <param name="region">등록할 리전</param>
    void RegisterRegion(IRegion region);

    /// <summary>
    ///     리전을 제거합니다.
    /// </summary>
    /// <param name="regionName">제거할 리전 이름</param>
    /// <returns>제거 성공 여부</returns>
    bool RemoveRegion(string regionName);

    /// <summary>
    ///     리전별 네비게이션 서비스 접근
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <returns>네비게이션 서비스 (리전이 없으면 null)</returns>
    IRegionNavigationService? GetNavigationService(string regionName);

    #endregion

    #region View 추가/제거

    /// <summary>
    ///     지정된 리전에 뷰를 추가합니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="view">추가할 뷰</param>
    /// <returns>추가된 뷰</returns>
    object AddToRegion(string regionName, object view);

    /// <summary>
    ///     지정된 리전에 View 타입으로 뷰를 추가합니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="viewType">View 타입</param>
    /// <returns>추가된 뷰</returns>
    object AddToRegion(string regionName, Type viewType);

    /// <summary>
    ///     지정된 리전에 View 타입으로 뷰를 추가합니다 (제네릭).
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <typeparam name="TView">View 타입</typeparam>
    /// <returns>추가된 뷰</returns>
    object AddToRegion<TView>(string regionName) where TView : class;

    /// <summary>
    ///     지정된 리전에서 뷰를 제거합니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="view">제거할 뷰</param>
    void RemoveFromRegion(string regionName, object view);

    #endregion

    #region View 기반 네비게이션

    /// <summary>
    ///     지정된 리전으로 네비게이션합니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="viewType">네비게이션할 View 타입</param>
    /// <param name="parameter">네비게이션 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> NavigateAsync(string regionName, Type viewType, object? parameter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     지정된 리전으로 네비게이션합니다 (제네릭).
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <typeparam name="TView">네비게이션할 View 타입</typeparam>
    /// <param name="parameter">네비게이션 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> NavigateAsync<TView>(string regionName, object? parameter = null,
        CancellationToken cancellationToken = default) where TView : class;

    #endregion

    #region Back/Forward 네비게이션

    /// <summary>
    ///     지정된 리전에서 뒤로 갈 수 있는지 확인
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <returns>뒤로 갈 수 있는지 여부</returns>
    bool CanGoBack(string regionName);

    /// <summary>
    ///     지정된 리전에서 앞으로 갈 수 있는지 확인
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <returns>앞으로 갈 수 있는지 여부</returns>
    bool CanGoForward(string regionName);

    /// <summary>
    ///     지정된 리전에서 이전 뷰로 이동
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> GoBackAsync(string regionName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     지정된 리전에서 다음 뷰로 이동
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>네비게이션 성공 여부</returns>
    Task<bool> GoForwardAsync(string regionName, CancellationToken cancellationToken = default);

    #endregion

    #region RegisterViewWithRegion

    /// <summary>
    ///     리전에 기본 View를 등록합니다.
    ///     리전이 생성될 때 자동으로 해당 View로 네비게이션됩니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <param name="viewType">기본으로 표시할 View 타입</param>
    void RegisterViewWithRegion(string regionName, Type viewType);

    /// <summary>
    ///     리전에 기본 View를 등록합니다 (제네릭).
    ///     리전이 생성될 때 자동으로 해당 View로 네비게이션됩니다.
    /// </summary>
    /// <param name="regionName">리전 이름</param>
    /// <typeparam name="TView">기본으로 표시할 View 타입</typeparam>
    void RegisterViewWithRegion<TView>(string regionName) where TView : class;

    #endregion

    #region 이벤트

    /// <summary>
    ///     리전이 추가되었을 때 발생하는 이벤트
    /// </summary>
    event EventHandler<IRegion>? RegionAdded;

    /// <summary>
    ///     리전이 제거되었을 때 발생하는 이벤트
    /// </summary>
    event EventHandler<string>? RegionRemoved;

    #endregion
}
