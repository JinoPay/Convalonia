using System.Windows;
using Jinobald.Core.Services.Settings;
using Jinobald.Core.Services.Theme;
using Serilog;

namespace Jinobald.Wpf.Services.Theme;

/// <summary>
///     WPF 테마 관리 서비스
///     SettingsService와 통합되어 테마 설정을 자동으로 저장/로드합니다.
///     사용자가 RegisterTheme()으로 테마 리소스를 등록해야 합니다.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string ThemeSettingsKey = "App.Theme";
    private const string DefaultTheme = "Light";

    private readonly ILogger _logger;
    private readonly Dictionary<string, ResourceDictionary> _registeredThemes = new();
    private readonly ISettingsService _settingsService;
    private string _currentTheme = DefaultTheme;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = Log.ForContext<ThemeService>();
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
    ///     앱 초기화 시 테마가 등록된 후 호출해야 합니다.
    /// </summary>
    public void ApplySavedTheme()
    {
        if (_registeredThemes.Count == 0)
        {
            _logger.Warning("등록된 테마가 없습니다. RegisterTheme()으로 테마를 먼저 등록하세요.");
            return;
        }

        var savedTheme = _settingsService.Get<string>(ThemeSettingsKey);
        if (!string.IsNullOrEmpty(savedTheme) && _registeredThemes.ContainsKey(savedTheme))
        {
            SetTheme(savedTheme);
            _logger.Information("저장된 테마 적용됨: {Theme}", savedTheme);
        }
        else
        {
            // 첫 번째 등록된 테마를 기본으로 사용
            var firstTheme = _registeredThemes.Keys.FirstOrDefault() ?? DefaultTheme;
            if (_registeredThemes.ContainsKey(firstTheme))
            {
                SetTheme(firstTheme);
                _logger.Information("기본 테마 적용됨: {Theme}", firstTheme);
            }
        }
    }

    /// <summary>
    ///     테마를 등록합니다.
    /// </summary>
    /// <param name="themeName">테마 이름 (예: Light, Dark)</param>
    /// <param name="theme">테마 ResourceDictionary</param>
    public void RegisterTheme(string themeName, object theme)
    {
        if (string.IsNullOrEmpty(themeName))
            throw new ArgumentException("테마 이름은 비어있을 수 없습니다.", nameof(themeName));

        if (theme is not ResourceDictionary resourceDictionary)
            throw new ArgumentException("WPF 테마는 ResourceDictionary여야 합니다.", nameof(theme));

        _registeredThemes[themeName] = resourceDictionary;
        _logger.Information("테마 등록됨: {ThemeName}", themeName);
    }

    /// <summary>
    ///     테마를 변경합니다.
    /// </summary>
    /// <param name="themeName">테마 이름</param>
    public void SetTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            throw new ArgumentNullException(nameof(themeName));

        if (!_registeredThemes.TryGetValue(themeName, out var theme))
        {
            _logger.Warning("등록되지 않은 테마: {Theme}", themeName);
            return;
        }

        var app = System.Windows.Application.Current;
        if (app == null)
        {
            _logger.Error("Application.Current가 null입니다.");
            return;
        }

        try
        {
            // WPF 테마 변경 - MergedDictionaries에서 기존 테마 제거 후 새 테마 추가
            // 주의: 앱의 다른 리소스(예: Generic.xaml)는 유지해야 함
            // 테마 리소스만 교체
            RemoveCurrentThemeFromMergedDictionaries(app);
            app.Resources.MergedDictionaries.Add(theme);

            CurrentTheme = themeName;

            // SettingsService에 테마 설정 저장
            _settingsService.Set(ThemeSettingsKey, themeName);

            _logger.Information("테마 변경됨: {Theme}", themeName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "테마 변경 실패: {Theme}", themeName);
        }
    }

    /// <summary>
    ///     테마 색상을 가져옵니다.
    /// </summary>
    public object? GetThemeColor(string colorKey)
    {
        if (string.IsNullOrEmpty(colorKey))
            return null;

        var app = System.Windows.Application.Current;
        if (app == null)
            return null;

        // Application 리소스에서 색상 검색
        if (app.Resources.Contains(colorKey))
            return app.Resources[colorKey];

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

        var app = System.Windows.Application.Current;
        if (app == null)
            return null;

        // Application 리소스에서 리소스 검색
        if (app.Resources.Contains(resourceKey))
            return app.Resources[resourceKey];

        _logger.Debug("테마 리소스를 찾을 수 없음: {ResourceKey}", resourceKey);
        return null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     현재 테마 리소스를 MergedDictionaries에서 제거합니다.
    /// </summary>
    private void RemoveCurrentThemeFromMergedDictionaries(System.Windows.Application app)
    {
        // 등록된 테마 리소스만 제거 (다른 리소스는 유지)
        var themesToRemove = app.Resources.MergedDictionaries
            .Where(rd => _registeredThemes.Values.Contains(rd))
            .ToList();

        foreach (var theme in themesToRemove)
        {
            app.Resources.MergedDictionaries.Remove(theme);
        }
    }

    #endregion
}
