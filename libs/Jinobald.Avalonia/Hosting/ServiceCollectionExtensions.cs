using Jinobald.Avalonia.Services.Dialog;
using Jinobald.Avalonia.Services.Events;
using Jinobald.Avalonia.Services.Regions;
using Jinobald.Avalonia.Services.Theme;
using Jinobald.Avalonia.Services.Toast;
using Jinobald.Core.Services.Dialog;
using Jinobald.Core.Services.Events;
using Jinobald.Core.Services.Regions;
using Jinobald.Core.Services.Settings;
using Jinobald.Core.Services.Theme;
using Jinobald.Core.Services.Toast;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Jinobald.Avalonia.Hosting;

/// <summary>
///     Jinobald Avalonia 서비스 등록 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Jinobald 핵심 서비스를 DI 컨테이너에 등록합니다.
    ///     RegionManager, EventAggregator, DialogService, ToastService, ThemeService, SettingsService가 자동으로 등록됩니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="settingsFilePath">설정 파일 경로 (null이면 기본 경로 사용)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddJinobaldAvalonia(
        this IServiceCollection services,
        string? settingsFilePath = null)
    {
        // Serilog ILogger 등록 (Log.Logger를 사용, ConfigureLogging()에서 설정됨)
        services.AddSingleton<ILogger>(_ => Log.Logger);

        // 핵심 서비스 등록
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddSingleton<ISettingsService>(sp => new JsonSettingsService(settingsFilePath));
        services.AddSingleton<IThemeService>(sp =>
            new ThemeService(sp.GetRequiredService<ISettingsService>()));
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IToastService, ToastService>();

        // Region 서비스 등록 (네비게이션은 Region 기반으로 통합됨)
        services.AddSingleton<IViewResolver, AvaloniaViewResolver>();
        services.AddSingleton<IRegionManager>(sp =>
            new RegionManager(sp.GetRequiredService<IViewResolver>()));

        return services;
    }
}
