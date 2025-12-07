using System.Windows;
using System.Windows.Controls;
using Jinobald.Core.Ioc;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Wpf.Services.Regions;

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
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached(
            "Name",
            typeof(string),
            typeof(Region),
            new PropertyMetadata(string.Empty, OnNameChanged));

    public static string GetName(DependencyObject obj)
    {
        return (string)obj.GetValue(NameProperty);
    }

    public static void SetName(DependencyObject obj, string value)
    {
        obj.SetValue(NameProperty, value);
    }

    #endregion

    #region Manager Attached Property

    /// <summary>
    ///     Region.Manager Attached Property
    ///     리전이 속한 RegionManager를 저장합니다.
    /// </summary>
    public static readonly DependencyProperty ManagerProperty =
        DependencyProperty.RegisterAttached(
            "Manager",
            typeof(IRegionManager),
            typeof(Region),
            new PropertyMetadata(null));

    public static IRegionManager? GetManager(DependencyObject obj)
    {
        return (IRegionManager?)obj.GetValue(ManagerProperty);
    }

    public static void SetManager(DependencyObject obj, IRegionManager? value)
    {
        obj.SetValue(ManagerProperty, value);
    }

    #endregion

    #region DefaultView Attached Property

    /// <summary>
    ///     Region.DefaultView Attached Property
    ///     리전이 생성될 때 기본적으로 표시할 View 타입을 지정합니다.
    /// </summary>
    public static readonly DependencyProperty DefaultViewProperty =
        DependencyProperty.RegisterAttached(
            "DefaultView",
            typeof(Type),
            typeof(Region),
            new PropertyMetadata(null, OnDefaultViewChanged));

    public static Type? GetDefaultView(DependencyObject obj)
    {
        return (Type?)obj.GetValue(DefaultViewProperty);
    }

    public static void SetDefaultView(DependencyObject obj, Type? value)
    {
        obj.SetValue(DefaultViewProperty, value);
    }

    #endregion

    #region KeepAlive Attached Property

    /// <summary>
    ///     Region.KeepAlive Attached Property
    ///     네비게이션 시 뷰를 캐시하여 재사용할지 여부를 지정합니다.
    /// </summary>
    public static readonly DependencyProperty KeepAliveProperty =
        DependencyProperty.RegisterAttached(
            "KeepAlive",
            typeof(bool),
            typeof(Region),
            new PropertyMetadata(false));

    public static bool GetKeepAlive(DependencyObject obj)
    {
        return (bool)obj.GetValue(KeepAliveProperty);
    }

    public static void SetKeepAlive(DependencyObject obj, bool value)
    {
        obj.SetValue(KeepAliveProperty, value);
    }

    #endregion

    #region NavigationMode Attached Property

    /// <summary>
    ///     Region.NavigationMode Attached Property
    ///     리전의 네비게이션 모드를 지정합니다 (Stack/Replace/Accumulate).
    /// </summary>
    public static readonly DependencyProperty NavigationModeProperty =
        DependencyProperty.RegisterAttached(
            "NavigationMode",
            typeof(RegionNavigationMode),
            typeof(Region),
            new PropertyMetadata(RegionNavigationMode.Stack));

    public static RegionNavigationMode GetNavigationMode(DependencyObject obj)
    {
        return (RegionNavigationMode)obj.GetValue(NavigationModeProperty);
    }

    public static void SetNavigationMode(DependencyObject obj, RegionNavigationMode value)
    {
        obj.SetValue(NavigationModeProperty, value);
    }

    #endregion

    #region Event Handlers

    private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string regionName || string.IsNullOrWhiteSpace(regionName))
            return;

        // FrameworkElement가 로드될 때까지 대기
        if (d is FrameworkElement element)
        {
            if (element.IsLoaded)
                CreateRegion(element, regionName);
            else
                element.Loaded += (_, _) => CreateRegion(element, regionName);
        }
    }

    private static void OnDefaultViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // DefaultView가 설정되면 리전이 생성된 후 해당 뷰로 네비게이션
        if (e.NewValue is not Type defaultViewType)
            return;

        // 리전이 아직 생성되지 않았다면 나중에 처리
        var regionName = GetName(d);
        if (string.IsNullOrWhiteSpace(regionName))
            return;

        // FrameworkElement가 로드될 때까지 대기
        if (d is FrameworkElement element)
        {
            if (element.IsLoaded)
                NavigateToDefaultView(element, regionName, defaultViewType);
            else
                element.Loaded += (_, _) => NavigateToDefaultView(element, regionName, defaultViewType);
        }
    }

    #endregion

    #region Private Helpers

    private static void CreateRegion(FrameworkElement element, string regionName)
    {
        // RegionManager 찾기 (자신 또는 부모에서)
        var regionManager = GetManager(element);
        if (regionManager == null)
        {
            // 부모에서 RegionManager 찾기
            var parent = element.Parent as FrameworkElement;
            while (parent != null && regionManager == null)
            {
                regionManager = GetManager(parent);
                parent = parent.Parent as FrameworkElement;
            }

            // 부모에서도 찾지 못하면 ContainerLocator에서 가져오기
            if (regionManager == null)
            {
                try
                {
                    regionManager = ContainerLocator.Current.Resolve<IRegionManager>();
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

    private static void NavigateToDefaultView(FrameworkElement element, string regionName, Type defaultViewType)
    {
        var regionManager = GetManager(element);
        if (regionManager == null)
        {
            // 부모에서 RegionManager 찾기
            var parent = element.Parent as FrameworkElement;
            while (parent != null && regionManager == null)
            {
                regionManager = GetManager(parent);
                parent = parent.Parent as FrameworkElement;
            }

            // 부모에서도 찾지 못하면 ContainerLocator에서 가져오기
            if (regionManager == null)
            {
                try
                {
                    regionManager = ContainerLocator.Current.Resolve<IRegionManager>();
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
