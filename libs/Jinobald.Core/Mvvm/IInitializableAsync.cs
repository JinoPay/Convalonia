namespace Jinobald.Core.Mvvm;

/// <summary>
///     비동기 초기화가 필요한 ViewModel 인터페이스
///     네비게이션 시 자동으로 호출됨
/// </summary>
public interface IInitializableAsync
{
    /// <summary>
    ///     초기화 완료 여부
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    ///     비동기 초기화 수행
    ///     한 번만 호출됨 (중복 호출 방지)
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
