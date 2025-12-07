using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Convalonia.Services.Navigation;

/// <summary>
/// Attached properties for region support
/// </summary>
public class Region
{
    public static readonly AttachedProperty<string?> NameProperty =
        AvaloniaProperty.RegisterAttached<Region, Control, string?>("Name");

    public static readonly AttachedProperty<bool> KeepAliveProperty =
        AvaloniaProperty.RegisterAttached<Region, Control, bool>("KeepAlive", defaultValue: false);

    static Region()
    {
        NameProperty.Changed.AddClassHandler<Control>(OnNameChanged);
    }

    public static string? GetName(Control control)
    {
        return control.GetValue(NameProperty);
    }

    public static void SetName(Control control, string? value)
    {
        control.SetValue(NameProperty, value);
    }

    public static bool GetKeepAlive(Control control)
    {
        return control.GetValue(KeepAliveProperty);
    }

    public static void SetKeepAlive(Control control, bool value)
    {
        control.SetValue(KeepAliveProperty, value);
    }

    private static void OnNameChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        var regionName = e.NewValue as string;
        if (string.IsNullOrEmpty(regionName))
            return;

        // Get the RegionManager from the application service provider
        // This is a simple implementation - in production you'd want a better way to access DI
        if (Application.Current is App app && app.ServiceProvider != null)
        {
            var regionManager = app.ServiceProvider.GetService<IRegionManager>();
            if (regionManager != null && control is ContentControl contentControl)
            {
                var region = new ContentControlRegion(contentControl);
                regionManager.RegisterRegion(regionName, region);
            }
        }
    }
}

/// <summary>
/// ContentControl-based region implementation
/// </summary>
internal class ContentControlRegion : IRegion
{
    private readonly ContentControl _control;

    public ContentControlRegion(ContentControl control)
    {
        _control = control;
    }

    public void SetContent(object content)
    {
        _control.Content = content;
    }
}
