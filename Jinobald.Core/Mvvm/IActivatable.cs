namespace Jinobald.Core.Mvvm;

/// <summary>
///     활성화/비활성화 상태를 관리하는 ViewModel 인터페이스
///     View가 표시되거나 숨겨질 때 호출됨
/// </summary>
public interface IActivatable
{
    /// <summary>
    ///     현재 활성화 상태
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     ViewModel 활성화 (View가 표시될 때)
    ///     데이터 새로고침, 타이머 시작 등
    /// </summary>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     ViewModel 비활성화 (View가 숨겨질 때)
    ///     리소스 해제, 타이머 중지 등
    /// </summary>
    Task DeactivateAsync(CancellationToken cancellationToken = default);
}
