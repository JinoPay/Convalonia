using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Jinobald.Core.Services.Settings;
using Jinobald.Core.Services.Theme;
using Serilog;

namespace Jinobald.Avalonia.Services.Theme;

/// <summary>
///     Avalonia 테마 관리 서비스
///     SettingsService와 통합되어 테마 설정을 자동으로 저장/로드합니다.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string ThemeSettingsKey = "App.Theme";
    private const string DefaultTheme = "Light";

    private readonly ILogger _logger;
    private readonly Dictionary<string, object> _registeredThemes = new();
    private readonly ISettingsService _settingsService;
    private string _currentTheme = DefaultTheme;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = Log.ForContext<ThemeService>();

        // 기본 테마 등록
        RegisterDefaultThemes();
    }

    #region IThemeService Properties

    public string CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ThemeChanged?.Invoke(value);
            }
        }
    }

    public IEnumerable<string> AvailableThemes => _registeredThemes.Keys;

    public event Action<string>? ThemeChanged;

    #endregion

    #region IThemeService Methods

    /// <summary>
    ///     저장된 테마 설정을 적용합니다.
    /// </summary>
    public void ApplySavedTheme()
    {
        var savedTheme = _settingsService.Get<string>(ThemeSettingsKey);
        if (!string.IsNullOrEmpty(savedTheme) && _registeredThemes.ContainsKey(savedTheme))
        {
            SetTheme(savedTheme);
            _logger.Information("저장된 테마 적용됨: {Theme}", savedTheme);
        }
        else
        {
            SetTheme(DefaultTheme);
            _logger.Information("기본 테마 적용됨: {Theme}", DefaultTheme);
        }
    }

    /// <summary>
    ///     테마를 변경합니다.
    /// </summary>
    /// <param name="themeName">테마 이름 (예: Light, Dark, System)</param>
    public void SetTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            throw new ArgumentNullException(nameof(themeName));

        if (!_registeredThemes.TryGetValue(themeName, out var theme))
        {
            _logger.Warning("등록되지 않은 테마: {Theme}", themeName);
            return;
        }

        var app = global::Avalonia.Application.Current;
        if (app == null)
        {
            _logger.Error("Application.Current가 null입니다.");
            return;
        }

        try
        {
            // Avalonia 테마 변경
            if (theme is ThemeVariant variant)
            {
                // Simple 또는 Fluent 테마의 ThemeVariant 변경
                app.RequestedThemeVariant = variant;
            }
            else if (theme is Styles styles)
            {
                // 커스텀 스타일 적용
                app.Styles.Clear();
                app.Styles.Add(styles);
            }

            CurrentTheme = themeName;

            // SettingsService에 테마 설정 저장
            _settingsService.Set(ThemeSettingsKey, themeName);

            _logger.Information("테마 변경됨: {Theme}", themeName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "테마 변경 중 오류 발생: {Theme}", themeName);
            throw;
        }
    }

    /// <summary>
    ///     테마 색상을 가져옵니다.
    /// </summary>
    public object? GetThemeColor(string colorKey)
    {
        if (string.IsNullOrEmpty(colorKey))
            return null;

        var app = global::Avalonia.Application.Current;
        if (app == null)
            return null;

        // Application 리소스에서 색상 검색
        if (app.Resources.TryGetResource(colorKey, app.ActualThemeVariant, out var color))
            return color;

        _logger.Debug("테마 색상을 찾을 수 없음: {ColorKey}", colorKey);
        return null;
    }

    /// <summary>
    ///     테마 스타일 리소스를 가져옵니다.
    /// </summary>
    public object? GetThemeResource(string resourceKey)
    {
        if (string.IsNullOrEmpty(resourceKey))
            return null;

        var app = global::Avalonia.Application.Current;
        if (app == null)
            return null;

        // Application 리소스에서 리소스 검색
        if (app.Resources.TryGetResource(resourceKey, app.ActualThemeVariant, out var resource))
            return resource;

        _logger.Debug("테마 리소스를 찾을 수 없음: {ResourceKey}", resourceKey);
        return null;
    }

    /// <summary>
    ///     테마 스타일을 등록합니다.
    /// </summary>
    /// <param name="themeName">테마 이름</param>
    /// <param name="resourceDictionary">테마 리소스 딕셔너리 (Styles 또는 ThemeVariant)</param>
    public void RegisterTheme(string themeName, object resourceDictionary)
    {
        if (string.IsNullOrEmpty(themeName))
            throw new ArgumentNullException(nameof(themeName));

        if (resourceDictionary == null)
            throw new ArgumentNullException(nameof(resourceDictionary));

        if (resourceDictionary is not Styles && resourceDictionary is not ThemeVariant)
            throw new ArgumentException(
                "resourceDictionary는 Avalonia.Styling.Styles 또는 Avalonia.Styling.ThemeVariant 타입이어야 합니다.",
                nameof(resourceDictionary));

        _registeredThemes[themeName] = resourceDictionary;
        _logger.Information("테마 등록됨: {Theme}", themeName);
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     기본 테마 등록 (Light, Dark, System)
    /// </summary>
    private void RegisterDefaultThemes()
    {
        // Avalonia의 기본 ThemeVariant 등록
        _registeredThemes["Light"] = ThemeVariant.Light;
        _registeredThemes["Dark"] = ThemeVariant.Dark;
        _registeredThemes["System"] = ThemeVariant.Default; // 시스템 테마 따라감

        _logger.Debug("기본 테마 등록 완료: Light, Dark, System");
    }

    #endregion
}
