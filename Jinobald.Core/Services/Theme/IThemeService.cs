namespace Jinobald.Core.Services.Theme;

/// <summary>
///     테마 관리 서비스
///     Light/Dark 모드뿐만 아니라 커스텀 테마 스타일도 지원합니다.
/// </summary>
public interface IThemeService
{
    /// <summary>
    ///     현재 테마 이름
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    ///     사용 가능한 테마 목록
    /// </summary>
    IEnumerable<string> AvailableThemes { get; }

    /// <summary>
    ///     테마 변경 이벤트
    /// </summary>
    event Action<string>? ThemeChanged;

    /// <summary>
    ///     저장된 테마 설정을 적용합니다.
    /// </summary>
    void ApplySavedTheme();

    /// <summary>
    ///     테마를 변경합니다.
    /// </summary>
    /// <param name="themeName">테마 이름 (예: Light, Dark, System, Custom1)</param>
    void SetTheme(string themeName);

    /// <summary>
    ///     테마 색상을 가져옵니다.
    ///     색상 하드코딩 방지를 위해 반드시 이 메서드를 사용해야 합니다.
    /// </summary>
    /// <param name="colorKey">색상 키 (예: PrimaryColor, BackgroundColor)</param>
    /// <returns>색상 값 (플랫폼별 색상 타입으로 변환 필요)</returns>
    object? GetThemeColor(string colorKey);

    /// <summary>
    ///     테마 스타일 리소스를 가져옵니다.
    /// </summary>
    /// <param name="resourceKey">리소스 키</param>
    /// <returns>리소스 값</returns>
    object? GetThemeResource(string resourceKey);

    /// <summary>
    ///     테마 스타일을 등록합니다.
    /// </summary>
    /// <param name="themeName">테마 이름</param>
    /// <param name="resourceDictionary">테마 리소스 딕셔너리</param>
    void RegisterTheme(string themeName, object resourceDictionary);
}

