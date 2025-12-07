using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Convalonia.Infrastructure;

/// <summary>
/// Base class for Avalonia applications with DI support
/// </summary>
/// <typeparam name="TMainWindow">Main window type</typeparam>
public abstract class ApplicationBase<TMainWindow> : Application
    where TMainWindow : Window
{
    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    public IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// Logger instance
    /// </summary>
    protected ILogger Logger { get; }

    protected ApplicationBase()
    {
        Logger = Log.ForContext(GetType());
        ConfigureExceptionHandling();
    }

    /// <summary>
    /// Configure global exception handling
    /// </summary>
    private void ConfigureExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Logger.Fatal(exception, "Unhandled exception in AppDomain");
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Logger.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    /// <summary>
    /// Called when framework initialization is completed
    /// </summary>
    public override async void OnFrameworkInitializationCompleted()
    {
        await InitializeAsync();
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Initialize the application
    /// </summary>
    private async Task InitializeAsync()
    {
        Logger.Information("Application initialization started");

        try
        {
            // 1. Configure logging
            ConfigureLogging();

            // 2. Build DI container
            var services = new ServiceCollection();

            // 3. Configure base services
            ConfigureServices(services);

            // 4. Register application types
            RegisterTypes(services);

            // 5. Build service provider
            ServiceProvider = services.BuildServiceProvider();

            // 6. Create and show main window
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = ServiceProvider.GetRequiredService<TMainWindow>();
                desktop.MainWindow = mainWindow;
            }

            // 7. Custom initialization
            await OnInitializeAsync();

            Logger.Information("Application initialization completed");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Application initialization failed");
            throw;
        }
    }

    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add main window
        services.AddTransient<TMainWindow>();
    }

    /// <summary>
    /// Configure logging - override to customize
    /// </summary>
    protected virtual void ConfigureLogging()
    {
        // Default implementation - can be overridden
    }

    /// <summary>
    /// Register application types - override to register ViewModels, Services, etc.
    /// </summary>
    public abstract void RegisterTypes(IServiceCollection services);

    /// <summary>
    /// Custom initialization logic - override to add custom initialization
    /// </summary>
    public virtual Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper to navigate - can be extended with navigation service
    /// </summary>
    protected void Navigate<TView>() where TView : class
    {
        Dispatcher.UIThread.Post(() =>
        {
            var view = ServiceProvider?.GetService<TView>();
            // Navigation logic here
        });
    }
}
