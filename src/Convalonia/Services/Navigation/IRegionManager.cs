using System;
using System.Threading.Tasks;

namespace Convalonia.Services.Navigation;

/// <summary>
/// Region manager for view navigation
/// </summary>
public interface IRegionManager
{
    /// <summary>
    /// Navigate to a view in a region
    /// </summary>
    Task NavigateAsync<TView>(string regionName, object? parameter = null) where TView : class;

    /// <summary>
    /// Register a region
    /// </summary>
    void RegisterRegion(string name, IRegion region);
}

/// <summary>
/// Region interface
/// </summary>
public interface IRegion
{
    /// <summary>
    /// Set the content of the region
    /// </summary>
    void SetContent(object content);
}
