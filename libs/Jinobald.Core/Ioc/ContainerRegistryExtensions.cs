using Jinobald.Core.Services.Settings;

namespace Jinobald.Core.Ioc;

/// <summary>
///     IContainerRegistry 확장 메서드
///     Prism 스타일의 RegisterForNavigation, RegisterDialog 등을 제공합니다.
/// </summary>
public static class ContainerRegistryExtensions
{
    #region RegisterForNavigation

    /// <summary>
    ///     View와 ViewModel을 네비게이션용으로 등록합니다.
    ///     View는 Transient로, ViewModel은 Transient로 등록됩니다.
    /// </summary>
    /// <typeparam name="TView">View 타입</typeparam>
    /// <typeparam name="TViewModel">ViewModel 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterForNavigation<TView, TViewModel>(this IContainerRegistry containerRegistry)
        where TView : class
        where TViewModel : class
    {
        containerRegistry.Register<TView>();
        containerRegistry.Register<TViewModel>();
        return containerRegistry;
    }

    /// <summary>
    ///     View만 네비게이션용으로 등록합니다.
    ///     ViewModel은 컨벤션에 따라 자동으로 resolve됩니다.
    /// </summary>
    /// <typeparam name="TView">View 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterForNavigation<TView>(this IContainerRegistry containerRegistry)
        where TView : class
    {
        containerRegistry.Register<TView>();
        return containerRegistry;
    }

    /// <summary>
    ///     View와 ViewModel을 싱글톤으로 등록합니다.
    ///     KeepAlive 모드에서 사용합니다.
    /// </summary>
    /// <typeparam name="TView">View 타입</typeparam>
    /// <typeparam name="TViewModel">ViewModel 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterForNavigationSingleton<TView, TViewModel>(this IContainerRegistry containerRegistry)
        where TView : class
        where TViewModel : class
    {
        containerRegistry.RegisterSingleton<TView>();
        containerRegistry.RegisterSingleton<TViewModel>();
        return containerRegistry;
    }

    #endregion

    #region RegisterDialog

    /// <summary>
    ///     다이얼로그 View와 ViewModel을 등록합니다.
    /// </summary>
    /// <typeparam name="TView">다이얼로그 View 타입</typeparam>
    /// <typeparam name="TViewModel">다이얼로그 ViewModel 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterDialog<TView, TViewModel>(this IContainerRegistry containerRegistry)
        where TView : class
        where TViewModel : class
    {
        containerRegistry.Register<TView>();
        containerRegistry.Register<TViewModel>();
        return containerRegistry;
    }

    /// <summary>
    ///     다이얼로그 View만 등록합니다.
    ///     ViewModel은 컨벤션에 따라 자동으로 resolve됩니다.
    /// </summary>
    /// <typeparam name="TView">다이얼로그 View 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterDialog<TView>(this IContainerRegistry containerRegistry)
        where TView : class
    {
        containerRegistry.Register<TView>();
        return containerRegistry;
    }

    #endregion

    #region RegisterViewModel

    /// <summary>
    ///     ViewModel만 등록합니다.
    ///     View는 ActivatorUtilities로 자동 생성됩니다.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterViewModel<TViewModel>(this IContainerRegistry containerRegistry)
        where TViewModel : class
    {
        containerRegistry.Register<TViewModel>();
        return containerRegistry;
    }

    /// <summary>
    ///     ViewModel을 싱글톤으로 등록합니다.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterViewModelSingleton<TViewModel>(this IContainerRegistry containerRegistry)
        where TViewModel : class
    {
        containerRegistry.RegisterSingleton<TViewModel>();
        return containerRegistry;
    }

    #endregion

    #region RegisterSettings

    /// <summary>
    ///     Strongly-Typed 설정 서비스를 싱글톤으로 등록합니다.
    ///     기본 경로(%AppData%/Jinobald/{typename}.json)에 저장됩니다.
    /// </summary>
    /// <typeparam name="TSettings">설정 POCO 클래스 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterSettings<TSettings>(this IContainerRegistry containerRegistry)
        where TSettings : class, new()
    {
        containerRegistry.RegisterSingleton<ITypedSettingsService<TSettings>, JsonTypedSettingsService<TSettings>>();
        return containerRegistry;
    }

    /// <summary>
    ///     Strongly-Typed 설정 서비스를 싱글톤으로 등록합니다.
    ///     사용자 지정 파일 경로에 저장됩니다.
    /// </summary>
    /// <typeparam name="TSettings">설정 POCO 클래스 타입</typeparam>
    /// <param name="containerRegistry">컨테이너 레지스트리</param>
    /// <param name="settingsFilePath">설정 파일의 전체 경로</param>
    /// <returns>현재 레지스트리</returns>
    public static IContainerRegistry RegisterSettings<TSettings>(
        this IContainerRegistry containerRegistry,
        string settingsFilePath)
        where TSettings : class, new()
    {
        var service = new JsonTypedSettingsService<TSettings>(settingsFilePath);
        containerRegistry.RegisterInstance<ITypedSettingsService<TSettings>>(service);
        return containerRegistry;
    }

    #endregion
}
