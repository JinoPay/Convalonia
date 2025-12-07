namespace Jinobald.Core.Services.Regions;

/// <summary>
///     리전 내 뷰의 정렬 방식
/// </summary>
public enum ViewSortHint
{
    /// <summary>
    ///     뷰를 추가 순서대로 정렬
    /// </summary>
    Default,

    /// <summary>
    ///     뷰를 역순으로 정렬
    /// </summary>
    Reverse
}

/// <summary>
///     애플리케이션 내의 논리적 영역을 나타내는 인터페이스
///     각 리전은 독립적으로 뷰를 관리하고 네비게이션을 수행할 수 있습니다.
/// </summary>
public interface IRegion
{
    /// <summary>
    ///     리전의 고유 이름
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     리전에 연결된 UI 요소 (ContentControl, ItemsControl 등)
    /// </summary>
    object? RegionTarget { get; set; }

    /// <summary>
    ///     리전 내의 모든 뷰 컬렉션
    /// </summary>
    IEnumerable<object> Views { get; }

    /// <summary>
    ///     현재 활성화된 뷰 컬렉션
    /// </summary>
    IEnumerable<object> ActiveViews { get; }

    /// <summary>
    ///     뷰 정렬 방식
    /// </summary>
    ViewSortHint SortHint { get; set; }

    /// <summary>
    ///     리전에 뷰 추가
    /// </summary>
    /// <param name="view">추가할 뷰</param>
    /// <returns>추가된 뷰</returns>
    object Add(object view);

    /// <summary>
    ///     리전에서 뷰 제거
    /// </summary>
    /// <param name="view">제거할 뷰</param>
    void Remove(object view);

    /// <summary>
    ///     리전의 모든 뷰 제거
    /// </summary>
    void RemoveAll();

    /// <summary>
    ///     뷰 활성화
    /// </summary>
    /// <param name="view">활성화할 뷰</param>
    void Activate(object view);

    /// <summary>
    ///     뷰 비활성화
    /// </summary>
    /// <param name="view">비활성화할 뷰</param>
    void Deactivate(object view);

    /// <summary>
    ///     지정된 뷰가 리전에 포함되어 있는지 확인
    /// </summary>
    bool Contains(object view);

    /// <summary>
    ///     리전 내 뷰가 추가되었을 때 발생하는 이벤트
    /// </summary>
    event EventHandler<object>? ViewAdded;

    /// <summary>
    ///     리전 내 뷰가 제거되었을 때 발생하는 이벤트
    /// </summary>
    event EventHandler<object>? ViewRemoved;

    /// <summary>
    ///     뷰가 활성화되었을 때 발생하는 이벤트
    /// </summary>
    event EventHandler<object>? ViewActivated;

    /// <summary>
    ///     뷰가 비활성화되었을 때 발생하는 이벤트
    /// </summary>
    event EventHandler<object>? ViewDeactivated;
}
