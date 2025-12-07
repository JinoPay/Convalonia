namespace Jinobald.Core.Services.Regions;

/// <summary>
///     리전의 네비게이션 모드
///     Prism의 Navigation Mode 개념을 참고하여 설계됨
/// </summary>
public enum RegionNavigationMode
{
    /// <summary>
    ///     스택 기반 네비게이션 (Back/Forward 지원)
    ///     ContentControl에 적합하며 히스토리 관리
    /// </summary>
    Stack,

    /// <summary>
    ///     현재 뷰를 교체 (히스토리 없음)
    ///     ContentControl에 적합하며 단순 교체
    /// </summary>
    Replace,

    /// <summary>
    ///     뷰를 누적 (여러 뷰 동시 표시)
    ///     ItemsControl에 적합하며 Tab/Multi-view 패턴
    /// </summary>
    Accumulate
}
