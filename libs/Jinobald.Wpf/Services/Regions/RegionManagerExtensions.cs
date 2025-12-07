using System.Windows;
using System.Windows.Controls;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Wpf.Services.Regions;

/// <summary>
///     XAML에서 리전을 정의하기 위한 Attached Property
/// </summary>
public static class RegionManagerExtensions
{
    private static readonly Dictionary<Type, object> _adapters = new()
    {
        { typeof(ContentControl), new ContentControlRegionAdapter() },
        { typeof(ItemsControl), new ItemsControlRegionAdapter() }
    };

    /// <summary>
    ///     RegionName Attached Property
    ///     XAML에서 리전 이름을 지정합니다.
    /// </summary>
    public static readonly DependencyProperty RegionNameProperty =
        DependencyProperty.RegisterAttached(
            "RegionName",
            typeof(string),
            typeof(RegionManagerExtensions),
            new PropertyMetadata(null, OnRegionNameChanged));

    /// <summary>
    ///     RegionManager Attached Property
    ///     리전이 속한 RegionManager를 저장합니다.
    /// </summary>
    public static readonly DependencyProperty RegionManagerProperty =
        DependencyProperty.RegisterAttached(
            "RegionManager",
            typeof(IRegionManager),
            typeof(RegionManagerExtensions),
            new PropertyMetadata(null));

    public static string GetRegionName(DependencyObject obj)
    {
        return (string)obj.GetValue(RegionNameProperty);
    }

    public static void SetRegionName(DependencyObject obj, string value)
    {
        obj.SetValue(RegionNameProperty, value);
    }

    public static IRegionManager GetRegionManager(DependencyObject obj)
    {
        return (IRegionManager)obj.GetValue(RegionManagerProperty);
    }

    public static void SetRegionManager(DependencyObject obj, IRegionManager value)
    {
        obj.SetValue(RegionManagerProperty, value);
    }

    private static void OnRegionNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string regionName || string.IsNullOrWhiteSpace(regionName))
            return;

        // Window가 로드될 때까지 대기
        if (d is FrameworkElement element)
        {
            if (element.IsLoaded)
                CreateRegion(element, regionName);
            else
                element.Loaded += (_, _) => CreateRegion(element, regionName);
        }
    }

    private static void CreateRegion(FrameworkElement element, string regionName)
    {
        // RegionManager 찾기 (자신 또는 부모에서)
        var regionManager = GetRegionManager(element);
        if (regionManager == null)
        {
            // 부모에서 RegionManager 찾기
            var parent = element;
            while (parent != null && regionManager == null)
            {
                parent = parent.Parent as FrameworkElement;
                if (parent != null) regionManager = GetRegionManager(parent);
            }

            // RegionManager가 없으면 리턴
            if (regionManager == null)
                return;
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

        if (region != null) regionManager.RegisterRegion(region);
    }
}
