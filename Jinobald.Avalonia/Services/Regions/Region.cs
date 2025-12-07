using Avalonia;
using Avalonia.Controls;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Avalonia.Services.Regions;

/// <summary>
///     XAML에서 리전을 정의하기 위한 Attached Property 클래스
///     Prism의 RegionManager.RegionName 패턴을 따름
///     사용 예: jino:Region.Name="MainRegion"
/// </summary>
public static class Region
{
    private static readonly Dictionary<Type, object> _adapters = new()
    {
        { typeof(ContentControl), new ContentControlRegionAdapter() },
        { typeof(ItemsControl), new ItemsControlRegionAdapter() }
    };

    #region Name Attached Property

    /// <summary>
    ///     Region.Name Attached Property
    ///     XAML에서 리전 이름을 지정합니다.
    /// </summary>
    public static readonly AttachedProperty<string> NameProperty =
        AvaloniaProperty.RegisterAttached<Control, string>(
            "Name",
            typeof(Region),
            string.Empty);

    static Region()
    {
        NameProperty.Changed.AddClassHandler<Control>(OnNameChanged);
        DefaultViewProperty.Changed.AddClassHandler<Control>(OnDefaultViewChanged);
    }

    public static string GetName(Control control)
    {
        return control.GetValue(NameProperty);
    }

    public static void SetName(Control control, string value)
    {
        control.SetValue(NameProperty, value);
    }

    #endregion

    #region Manager Attached Property (Internal)

    /// <summary>
    ///     Region.Manager Attached Property (내부용)
    ///     리전이 속한 RegionManager를 저장합니다.
    /// </summary>
    public static readonly AttachedProperty<IRegionManager?> ManagerProperty =
        AvaloniaProperty.RegisterAttached<Control, IRegionManager?>(
            "Manager",
            typeof(Region),
            null);

    public static IRegionManager? GetManager(Control control)
    {
        return control.GetValue(ManagerProperty);
    }

    public static void SetManager(Control control, IRegionManager? value)
    {
        control.SetValue(ManagerProperty, value);
    }

    #endregion

    #region DefaultView Attached Property

    /// <summary>
    ///     Region.DefaultView Attached Property
    ///     리전이 생성될 때 기본적으로 표시할 View 타입을 지정합니다.
    /// </summary>
    public static readonly AttachedProperty<Type?> DefaultViewProperty =
        AvaloniaProperty.RegisterAttached<Control, Type?>(
            "DefaultView",
            typeof(Region),
            null);

    public static Type? GetDefaultView(Control control)
    {
        return control.GetValue(DefaultViewProperty);
    }

    public static void SetDefaultView(Control control, Type? value)
    {
        control.SetValue(DefaultViewProperty, value);
    }

    #endregion

    #region KeepAlive Attached Property

    /// <summary>
    ///     Region.KeepAlive Attached Property
    ///     네비게이션 시 뷰를 캐시하여 재사용할지 여부를 지정합니다.
    /// </summary>
    public static readonly AttachedProperty<bool> KeepAliveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "KeepAlive",
            typeof(Region),
            false);

    public static bool GetKeepAlive(Control control)
    {
        return control.GetValue(KeepAliveProperty);
    }

    public static void SetKeepAlive(Control control, bool value)
    {
        control.SetValue(KeepAliveProperty, value);
    }

    #endregion

    #region NavigationMode Attached Property

    /// <summary>
    ///     Region.NavigationMode Attached Property
    ///     리전의 네비게이션 모드를 지정합니다 (Stack/Replace/Accumulate).
    /// </summary>
    public static readonly AttachedProperty<RegionNavigationMode> NavigationModeProperty =
        AvaloniaProperty.RegisterAttached<Control, RegionNavigationMode>(
            "NavigationMode",
            typeof(Region),
            RegionNavigationMode.Stack);

    public static RegionNavigationMode GetNavigationMode(Control control)
    {
        return control.GetValue(NavigationModeProperty);
    }

    public static void SetNavigationMode(Control control, RegionNavigationMode value)
    {
        control.SetValue(NavigationModeProperty, value);
    }

    #endregion

    #region Event Handlers

    private static void OnNameChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string regionName || string.IsNullOrWhiteSpace(regionName))
            return;

        // Control이 로드될 때까지 대기
        if (control.IsLoaded)
            CreateRegion(control, regionName);
        else
            control.Loaded += (_, _) => CreateRegion(control, regionName);
    }

    private static void OnDefaultViewChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        // DefaultView가 설정되면 리전이 생성된 후 해당 뷰로 네비게이션
        if (e.NewValue is not Type defaultViewType)
            return;

        // 리전이 아직 생성되지 않았다면 나중에 처리
        var regionName = GetName(control);
        if (string.IsNullOrWhiteSpace(regionName))
            return;

        // Control이 로드될 때까지 대기
        if (control.IsLoaded)
            NavigateToDefaultView(control, regionName, defaultViewType);
        else
            control.Loaded += (_, _) => NavigateToDefaultView(control, regionName, defaultViewType);
    }

    #endregion

    #region Private Helpers

    private static void CreateRegion(Control element, string regionName)
    {
        // RegionManager 찾기 (자신 또는 부모에서)
        var regionManager = GetManager(element);
        if (regionManager == null)
        {
            // 부모에서 RegionManager 찾기
            var parent = element.Parent as Control;
            while (parent != null && regionManager == null)
            {
                regionManager = GetManager(parent);
                parent = parent.Parent as Control;
            }

            // 부모에서도 찾지 못하면 ContainerLocator에서 가져오기
            if (regionManager == null)
            {
                try
                {
                    regionManager = Core.Ioc.ContainerLocator.Current.Resolve<IRegionManager>();
                }
                catch
                {
                    // ContainerLocator에서도 못 찾으면 리턴
                    return;
                }
            }
        }

        // 적절한 어댑터 찾기
        object? adapter = null;
        foreach (var kvp in _adapters)
            if (kvp.Key.IsInstanceOfType(element))
            {
                adapter = kvp.Value;
                break;
            }

        if (adapter == null)
            throw new InvalidOperationException(
                $"No region adapter found for control type {element.GetType().FullName}");

        // 리전 생성 및 등록
        IRegion? region = null;

        if (adapter is ContentControlRegionAdapter contentAdapter && element is ContentControl contentControl)
            region = contentAdapter.Initialize(contentControl, regionName);
        else if (adapter is ItemsControlRegionAdapter itemsAdapter && element is ItemsControl itemsControl)
            region = itemsAdapter.Initialize(itemsControl, regionName);

        if (region != null)
        {
            regionManager.RegisterRegion(region);

            // 리전 생성 후 KeepAlive 및 NavigationMode 설정
            var navigationService = regionManager.GetNavigationService(regionName);
            if (navigationService != null)
            {
                navigationService.KeepAlive = GetKeepAlive(element);
                navigationService.NavigationMode = GetNavigationMode(element);
            }

            // DefaultView가 설정되어 있으면 네비게이션
            var defaultViewType = GetDefaultView(element);
            if (defaultViewType != null)
            {
                // 비동기 네비게이션 시작 (fire-and-forget)
                _ = regionManager.NavigateAsync(regionName, defaultViewType);
            }
        }
    }

    private static void NavigateToDefaultView(Control element, string regionName, Type defaultViewType)
    {
        var regionManager = GetManager(element);
        if (regionManager == null)
        {
            // 부모에서 RegionManager 찾기
            var parent = element.Parent as Control;
            while (parent != null && regionManager == null)
            {
                regionManager = GetManager(parent);
                parent = parent.Parent as Control;
            }

            // 부모에서도 찾지 못하면 ContainerLocator에서 가져오기
            if (regionManager == null)
            {
                try
                {
                    regionManager = Core.Ioc.ContainerLocator.Current.Resolve<IRegionManager>();
                }
                catch
                {
                    return;
                }
            }
        }

        // 비동기 네비게이션 시작 (fire-and-forget)
        _ = regionManager.NavigateAsync(regionName, defaultViewType);
    }

    #endregion
}
