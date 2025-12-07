namespace Jinobald.Core.Services.Settings;

/// <summary>
///     Strongly-Typed 설정 서비스 인터페이스.
///     현대적인 Options 패턴을 제공하며, 컴파일 타임 타입 안전성을 보장합니다.
/// </summary>
/// <typeparam name="TSettings">설정 POCO 클래스 타입</typeparam>
public interface ITypedSettingsService<TSettings> where TSettings : class, new()
{
    /// <summary>
    ///     현재 설정 값을 가져옵니다.
    ///     IOptions&lt;T&gt;.Value와 유사한 패턴입니다.
    /// </summary>
    TSettings Value { get; }

    /// <summary>
    ///     설정이 변경되었을 때 발생하는 이벤트
    /// </summary>
    event Action<TSettings>? SettingsChanged;

    /// <summary>
    ///     설정을 업데이트합니다.
    ///     변경 후 자동으로 저장됩니다.
    /// </summary>
    /// <param name="updateAction">설정을 변경하는 액션</param>
    void Update(Action<TSettings> updateAction);

    /// <summary>
    ///     설정을 비동기로 업데이트합니다.
    ///     변경 후 자동으로 저장됩니다.
    /// </summary>
    /// <param name="updateAction">설정을 변경하는 비동기 액션</param>
    Task UpdateAsync(Func<TSettings, Task> updateAction);

    /// <summary>
    ///     설정을 디스크에 저장합니다.
    ///     일반적으로 Update 호출 시 자동 저장되므로 명시적 호출이 필요 없습니다.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    ///     설정을 디스크에서 다시 로드합니다.
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    ///     설정을 기본값으로 초기화합니다.
    /// </summary>
    void Reset();
}
