namespace Jinobald.Core.Services.Settings;

/// <summary>
///     애플리케이션 설정 관리 서비스
///     타입 안전성과 자동 저장을 제공합니다.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    ///     설정 값이 변경되었을 때 발생하는 이벤트
    /// </summary>
    event Action<string, object?>? SettingChanged;

    /// <summary>
    ///     설정 값을 가져옵니다.
    /// </summary>
    /// <typeparam name="T">설정 값의 타입</typeparam>
    /// <param name="key">설정 키</param>
    /// <param name="defaultValue">기본값 (설정이 없을 경우)</param>
    /// <returns>설정 값 또는 기본값</returns>
    T Get<T>(string key, T defaultValue = default!);

    /// <summary>
    ///     설정 값을 저장합니다.
    /// </summary>
    /// <typeparam name="T">설정 값의 타입</typeparam>
    /// <param name="key">설정 키</param>
    /// <param name="value">저장할 값</param>
    void Set<T>(string key, T value);

    /// <summary>
    ///     설정 값이 존재하는지 확인합니다.
    /// </summary>
    /// <param name="key">설정 키</param>
    /// <returns>존재 여부</returns>
    bool Contains(string key);

    /// <summary>
    ///     설정 값을 삭제합니다.
    /// </summary>
    /// <param name="key">설정 키</param>
    /// <returns>삭제 성공 여부</returns>
    bool Remove(string key);

    /// <summary>
    ///     모든 설정을 삭제합니다.
    /// </summary>
    void Clear();

    /// <summary>
    ///     모든 설정 키를 가져옵니다.
    /// </summary>
    /// <returns>설정 키 목록</returns>
    IEnumerable<string> GetAllKeys();

    /// <summary>
    ///     설정을 디스크에 저장합니다.
    ///     (대부분의 구현은 자동 저장을 지원하므로 명시적 호출이 필요 없을 수 있음)
    /// </summary>
    Task SaveAsync();

    /// <summary>
    ///     설정을 디스크에서 다시 로드합니다.
    /// </summary>
    Task ReloadAsync();
}
