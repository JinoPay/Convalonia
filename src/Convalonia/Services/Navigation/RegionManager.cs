using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Convalonia.Services.Navigation;

/// <summary>
/// Simple region manager implementation
/// </summary>
public class RegionManager : IRegionManager
{
    private readonly Dictionary<string, IRegion> _regions = new();
    private readonly IServiceProvider _serviceProvider;

    public RegionManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void RegisterRegion(string name, IRegion region)
    {
        _regions[name] = region;
    }

    public async Task NavigateAsync<TView>(string regionName, object? parameter = null) where TView : class
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!_regions.TryGetValue(regionName, out var region))
            {
                throw new InvalidOperationException($"Region '{regionName}' not found");
            }

            // Resolve view from DI container
            var view = _serviceProvider.GetRequiredService<TView>();

            // If view has DataContext, try to set parameter
            if (view is Avalonia.Controls.Control control && parameter != null)
            {
                // Try to pass parameter to ViewModel if it has a method to receive it
                if (control.DataContext != null)
                {
                    var dataContextType = control.DataContext.GetType();
                    var method = dataContextType.GetMethod("SetParameter") ??
                                dataContextType.GetMethod("Initialize");

                    method?.Invoke(control.DataContext, new[] { parameter });
                }
            }

            region.SetContent(view);
        });
    }
}
