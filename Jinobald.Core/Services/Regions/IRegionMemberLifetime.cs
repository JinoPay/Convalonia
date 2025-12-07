namespace Jinobald.Core.Services.Regions;

/// <summary>
///     View 또는 ViewModel이 Region 내에서 캐싱될지 여부를 제어하는 인터페이스
///     Prism의 IRegionMemberLifetime과 동일한 패턴을 따릅니다.
/// </summary>
/// <remarks>
///     이 인터페이스를 구현하면 Region의 전역 KeepAlive 설정을 개별적으로 오버라이드할 수 있습니다.
///     <para>
///         - KeepAlive = true: 뷰가 비활성화되어도 메모리에 유지되어 재사용됩니다.
///         - KeepAlive = false: 뷰가 비활성화되면 제거되고 리소스가 정리됩니다.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public class SettingsViewModel : ViewModelBase, IRegionMemberLifetime
/// {
///     // 설정 화면은 자주 접근하므로 캐싱
///     public bool KeepAlive => true;
/// }
///
/// public class ReportViewModel : ViewModelBase, IRegionMemberLifetime
/// {
///     // 리포트는 매번 새로 로드해야 하므로 캐싱 안 함
///     public bool KeepAlive => false;
/// }
/// </code>
/// </example>
public interface IRegionMemberLifetime
{
    /// <summary>
    ///     뷰가 Region 내에서 캐싱되어야 하는지 여부
    /// </summary>
    /// <value>
    ///     <c>true</c>이면 뷰가 비활성화되어도 메모리에 유지됩니다.
    ///     <c>false</c>이면 뷰가 비활성화될 때 제거됩니다.
    /// </value>
    bool KeepAlive { get; }
}
