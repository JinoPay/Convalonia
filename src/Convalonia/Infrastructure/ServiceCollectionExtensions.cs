using Microsoft.Extensions.DependencyInjection;

namespace Convalonia.Infrastructure;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a View and its ViewModel for navigation
    /// </summary>
    public static IServiceCollection RegisterForNavigation<TView, TViewModel>(this IServiceCollection services)
        where TView : class
        where TViewModel : class
    {
        services.AddTransient<TView>();
        services.AddTransient<TViewModel>();
        return services;
    }

    /// <summary>
    /// Register a View for navigation
    /// </summary>
    public static IServiceCollection RegisterForNavigation<TView>(this IServiceCollection services)
        where TView : class
    {
        services.AddTransient<TView>();
        return services;
    }
}
