using Avalonia;
using Avalonia.Controls;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Avalonia.Services.Regions;

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
    public static readonly AttachedProperty<string> RegionNameProperty =
        AvaloniaProperty.RegisterAttached<Control, string>(
            "RegionName",
            typeof(RegionManagerExtensions),
            string.Empty);

    /// <summary>
    ///     RegionManager Attached Property
    ///     리전이 속한 RegionManager를 저장합니다.
    /// </summary>
    public static readonly AttachedProperty<IRegionManager?> RegionManagerProperty =
        AvaloniaProperty.RegisterAttached<Control, IRegionManager?>(
            "RegionManager",
            typeof(RegionManagerExtensions),
            null);

    static RegionManagerExtensions()
    {
        RegionNameProperty.Changed.AddClassHandler<Control>(OnRegionNameChanged);
    }

    public static string GetRegionName(Control control)
    {
        return control.GetValue(RegionNameProperty);
    }

    public static void SetRegionName(Control control, string value)
    {
        control.SetValue(RegionNameProperty, value);
    }

    public static IRegionManager? GetRegionManager(Control control)
    {
        return control.GetValue(RegionManagerProperty);
    }

    public static void SetRegionManager(Control control, IRegionManager? value)
    {
        control.SetValue(RegionManagerProperty, value);
    }

    private static void OnRegionNameChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string regionName || string.IsNullOrWhiteSpace(regionName))
            return;

        // Control이 로드될 때까지 대기
        if (control.IsLoaded)
            CreateRegion(control, regionName);
        else
            control.Loaded += (_, _) => CreateRegion(control, regionName);
    }

    private static void CreateRegion(Control element, string regionName)
    {
        // RegionManager 찾기 (자신 또는 부모에서)
        var regionManager = GetRegionManager(element);
        if (regionManager == null)
        {
            // 부모에서 RegionManager 찾기
            var parent = element.Parent as Control;
            while (parent != null && regionManager == null)
            {
                regionManager = GetRegionManager(parent);
                parent = parent.Parent as Control;
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
